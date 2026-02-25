"""Post-processing pipeline – called after segmentation completes."""

import asyncio
import json
import logging
import subprocess
from pathlib import Path

from app.config import ASSETS_DIR
from app.storage import get_scan_dir, load_metadata, save_metadata

logger = logging.getLogger(__name__)

BLENDER_BIN = "blender"
STL_TO_FBX_SCRIPT = Path(__file__).parent.parent / "scripts" / "stl_to_fbx.py"
ORGAN_COLORS_JSON = ASSETS_DIR / "organ_colors.json"


def _validate_stl_files(scan_id: str) -> list[Path]:
    """
    Return a list of valid (non-empty) STL file paths for a scan.
    Raises ValueError if the stl/ directory is missing or has no valid files.
    """
    stl_dir = get_scan_dir(scan_id) / "stl"
    if not stl_dir.exists():
        raise ValueError(f"STL directory does not exist: {stl_dir}")

    all_stls = sorted(stl_dir.glob("*.stl"))
    if not all_stls:
        raise ValueError(f"No STL files found in {stl_dir}")

    valid = [f for f in all_stls if f.stat().st_size > 84]
    skipped = len(all_stls) - len(valid)
    if skipped:
        logger.warning("Skipped %d empty STL file(s) for scan %s", skipped, scan_id)

    if not valid:
        raise ValueError("All STL files are empty (0 triangles)")

    return valid


async def _run_blender(scan_id: str) -> Path:
    """
    Call Blender headless to convert all STL files into a single FBX.
    Returns the path to the produced model.fbx.
    """
    scan_dir = get_scan_dir(scan_id)
    stl_dir = scan_dir / "stl"
    output_fbx = scan_dir / "model.fbx"
    output_usdz = scan_dir / "model.usdz"
    offset_file = scan_dir / "model_offset.json"

    cmd = [
        BLENDER_BIN,
        "--background",
        "--python", str(STL_TO_FBX_SCRIPT),
        "--",
        "--stl-dir", str(stl_dir),
        "--output", str(output_fbx),
        "--usdz-output", str(output_usdz),
        "--colors", str(ORGAN_COLORS_JSON),
        "--offset-file", str(offset_file),
    ]

    logger.info("Running Blender: %s", " ".join(cmd))

    proc = await asyncio.to_thread(
        subprocess.run,
        cmd,
        capture_output=True,
        text=True,
        timeout=300,  # 5 min hard limit
    )

    if proc.returncode != 0:
        logger.error("Blender stderr:\n%s", proc.stderr[-2000:] if proc.stderr else "(empty)")
        raise RuntimeError(f"Blender exited with code {proc.returncode}")

    logger.info("Blender stdout:\n%s", proc.stdout[-1000:] if proc.stdout else "(empty)")

    if not output_fbx.exists():
        raise RuntimeError("Blender finished but model.fbx was not created")

    return output_fbx


async def run_post_processing(scan_id: str) -> dict:
    """
    Full post-processing pipeline after STL files are available:

    1. Validate that STL files exist and are non-empty.
    2. Call Blender to merge STLs into a single FBX with per-organ materials.
    3. Update scan metadata.

    Returns a summary dict.
    """
    logger.info("Post-processing started for scan %s", scan_id)

    metadata = load_metadata(scan_id)
    if not metadata:
        logger.error("Metadata not found for scan %s – aborting", scan_id)
        return {"error": "Metadata not found"}

    metadata["status"] = "post_processing"
    save_metadata(scan_id, metadata)

    # Step 1 – validate STL files
    try:
        stl_files = _validate_stl_files(scan_id)
    except ValueError as exc:
        logger.error("STL validation failed for %s: %s", scan_id, exc)
        metadata["status"] = "error"
        metadata["processing_error"] = str(exc)
        save_metadata(scan_id, metadata)
        return {"error": str(exc)}

    logger.info("Validated %d STL file(s) for scan %s", len(stl_files), scan_id)

    try:
        fbx_path = await _run_blender(scan_id)
    except Exception as exc:
        logger.exception("Blender conversion failed for scan %s", scan_id)
        metadata["status"] = "error"
        metadata["processing_error"] = f"FBX conversion failed: {exc}"
        save_metadata(scan_id, metadata)
        return {"error": str(exc)}

    metadata["status"] = "completed"
    metadata["has_fbx"] = True
    metadata["fbx_size"] = fbx_path.stat().st_size

    usdz_file = get_scan_dir(scan_id) / "model.usdz"
    if usdz_file.exists():
        metadata["has_usdz"] = True
        metadata["usdz_size"] = usdz_file.stat().st_size
    else:
        metadata["has_usdz"] = False

    # Load the centering offset written by Blender so we can transform
    # annotation points into FBX-model coordinate space later.
    offset_file = get_scan_dir(scan_id) / "model_offset.json"
    if offset_file.exists():
        try:
            with open(offset_file, "r") as f:
                offset_data = json.load(f)
            metadata["fbx_centre_offset"] = offset_data.get("centre_offset", [0, 0, 0])
            logger.info("Stored FBX centre offset for %s: %s", scan_id, metadata["fbx_centre_offset"])
        except Exception as exc:
            logger.warning("Failed to read model_offset.json for %s: %s", scan_id, exc)

    # Clear any stale error from previous failed attempts
    metadata.pop("processing_error", None)
    save_metadata(scan_id, metadata)

    summary = {
        "scan_id": scan_id,
        "status": "completed",
        "stl_count": len(stl_files),
        "fbx_size": metadata["fbx_size"],
    }
    logger.info("Post-processing finished for scan %s: %s", scan_id, summary)
    return summary
