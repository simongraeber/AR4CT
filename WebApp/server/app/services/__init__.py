"""Post-processing pipeline – called after segmentation completes."""

import logging

from app.storage import load_metadata, save_metadata

logger = logging.getLogger(__name__)


async def run_post_processing(scan_id: str) -> None:
    """
    Run any server-side post-processing after RunPod delivers the STL files.

    This is the hook for future work (e.g. Blender STL → FBX conversion,
    mesh simplification, thumbnail generation, etc.).

    For now this is intentionally empty – it only updates the scan status.
    """
    logger.info("Post-processing started for scan %s", scan_id)

    metadata = load_metadata(scan_id)
    if not metadata:
        logger.error("Metadata not found for scan %s – skipping post-processing", scan_id)
        return

    # ── Future processing steps go here ──────────────────────────────────
    # e.g.  await convert_stl_to_fbx(scan_id)
    #       await generate_thumbnail(scan_id)
    # ─────────────────────────────────────────────────────────────────────

    metadata["status"] = "post_processed"
    save_metadata(scan_id, metadata)

    logger.info("Post-processing finished for scan %s", scan_id)
