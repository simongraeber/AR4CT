"""Post-processing pipeline – called after segmentation completes."""

import asyncio
import logging
import subprocess
import sys
from pathlib import Path

from app.config import ASSETS_DIR
from app.storage import get_scan_dir, load_metadata, save_metadata

logger = logging.getLogger(__name__)

BLENDER_BIN = "blender"
STL_TO_FBX_SCRIPT = Path(__file__).parent.parent / "scripts" / "stl_to_fbx.py"
EXTRACT_BODY_SCRIPT = Path(__file__).parent.parent / "scripts" / "extract_body.py"
ORGAN_COLORS_JSON = ASSETS_DIR / "organ_colors.json"


def _find_ct_file(scan_id: str) -> Path | None:
    """Find the CT NIfTI file for a scan, or None if not available."""
    scan_dir = get_scan_dir(scan_id)
    ct_files = list(scan_dir.glob("ct_original_*"))
    if ct_files:
        return ct_files[0]
    return None


async def _extract_body_surface(scan_id: str) -> Path | None:
    """
    Extract the outer body surface from the CT scan and save as body.stl.

    Skips if body.stl already exists (e.g. uploaded by RunPod handler).
    Returns the path to body.stl, or None if extraction was skipped/failed.
    """
    scan_dir = get_scan_dir(scan_id)
    stl_dir = scan_dir / "stl"
    body_stl = stl_dir / "body.stl"

    # Skip if body.stl already exists and has geometry (> 84 byte header)
    if body_stl.exists() and body_stl.stat().st_size > 84:
        logger.info("body.stl already exists for scan %s – skipping extraction", scan_id)
        return body_stl

    ct_path = _find_ct_file(scan_id)
    if ct_path is None:
        logger.warning("No CT file found for scan %s – cannot extract body surface", scan_id)
        return None

    stl_dir.mkdir(exist_ok=True)

    cmd = [
        sys.executable,  # same Python that runs the server
        str(EXTRACT_BODY_SCRIPT),
        "--ct", str(ct_path),
        "--output", str(body_stl),
    ]

    logger.info("Extracting body surface for scan %s from %s", scan_id, ct_path.name)

    proc = await asyncio.to_thread(
        subprocess.run,
        cmd,
        capture_output=True,
        text=True,
        timeout=600,  # 10 min hard limit for large CTs
    )

    if proc.returncode != 0:
        logger.error(
            "Body extraction failed for scan %s (exit %d):\n%s",
            scan_id, proc.returncode, proc.stderr[-2000:] if proc.stderr else "(empty)",
        )
        return None

    logger.info("Body extraction stdout:\n%s", proc.stdout[-500:] if proc.stdout else "(empty)")

    if body_stl.exists() and body_stl.stat().st_size > 84:
        logger.info("body.stl created: %d bytes", body_stl.stat().st_size)
        return body_stl

    logger.warning("body.stl was not created or is empty for scan %s", scan_id)
    return None


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

    cmd = [
        BLENDER_BIN,
        "--background",
        "--python", str(STL_TO_FBX_SCRIPT),
        "--",
        "--stl-dir", str(stl_dir),
        "--output", str(output_fbx),
        "--colors", str(ORGAN_COLORS_JSON),
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

    # Step 0 – extract body surface from CT if body.stl is missing
    body_included = False
    try:
        body_result = await _extract_body_surface(scan_id)
        if body_result:
            logger.info("Body surface available: %s", body_result)
            body_included = True
        else:
            logger.warning(
                "Body surface NOT available for scan %s – "
                "no ct_original_* file found in scan directory. "
                "Upload the CT file via POST /scans/%s/ct to enable body extraction.",
                scan_id, scan_id,
            )
    except Exception as exc:
        # Non-fatal – continue without the body surface
        logger.warning("Body surface extraction error for %s: %s", scan_id, exc)

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
    metadata["has_body"] = body_included
    metadata["fbx_size"] = fbx_path.stat().st_size
    # Clear any stale error from previous failed attempts
    metadata.pop("processing_error", None)
    save_metadata(scan_id, metadata)

    summary = {
        "scan_id": scan_id,
        "status": "completed",
        "stl_count": len(stl_files),
        "has_body": body_included,
        "fbx_size": metadata["fbx_size"],
    }
    if not body_included:
        summary["warning"] = (
            "Body surface not included – CT file not found in scan directory. "
            "Upload CT via POST /scans/{scan_id}/ct and re-run post-processing."
        )
    logger.info("Post-processing finished for scan %s: %s", scan_id, summary)
    return summary
