"""Processing routes – trigger segmentation, post-processing & status."""

import logging
from datetime import datetime

from fastapi import APIRouter, HTTPException

from app.config import API_BASE_URL, DEFAULT_ORGANS
from app.storage import load_metadata, save_metadata, scan_exists
from app.services.runpod import submit_segmentation_job
from app.services import run_post_processing

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/scans", tags=["processing"])


# ── Trigger RunPod segmentation ──────────────────────────────────────────

@router.post("/{scan_id}/process")
async def start_processing(scan_id: str):
    """
    Trigger RunPod segmentation for an uploaded CT scan.

    1.  Builds the CT download URL and callback URL from the scan_id.
    2.  Submits an async job to RunPod.
    3.  Stores the RunPod job_id in the scan metadata so the client can poll.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    if metadata.get("status") in ("processing", "segmented", "post_processing", "completed"):
        return {
            "scan_id": scan_id,
            "status": metadata["status"],
            "runpod_job_id": metadata.get("runpod_job_id"),
            "message": f"Scan is already in status '{metadata['status']}'",
        }

    ct_url = f"{API_BASE_URL}/scans/{scan_id}/ct"
    callback_url = f"{API_BASE_URL}/scans/{scan_id}"

    result = await submit_segmentation_job(
        file_url=ct_url,
        organs=DEFAULT_ORGANS,
        callback_url=callback_url,
    )

    if "error" in result:
        raise HTTPException(status_code=503, detail=result["error"])

    metadata["status"] = "processing"
    metadata["runpod_job_id"] = result["job_id"]
    metadata["processing_started_at"] = datetime.utcnow().isoformat() + "Z"
    save_metadata(scan_id, metadata)

    return {
        "scan_id": scan_id,
        "status": "processing",
        "runpod_job_id": result["job_id"],
        "message": "Segmentation job submitted to RunPod",
    }


# ── Reset scan to allow re-processing ────────────────────────────────────

@router.post("/{scan_id}/reset")
async def reset_scan(scan_id: str):
    """
    Reset a scan back to 'uploaded' status so it can be re-processed.
    Clears RunPod job info and processing errors.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    old_status = metadata.get("status")
    metadata["status"] = "uploaded"
    metadata.pop("runpod_job_id", None)
    metadata.pop("processing_started_at", None)
    metadata.pop("processing_completed_at", None)
    metadata.pop("processing_error", None)
    metadata.pop("organs_processed", None)
    save_metadata(scan_id, metadata)

    logger.info("Reset scan %s from '%s' to 'uploaded'", scan_id, old_status)
    return {
        "scan_id": scan_id,
        "status": "uploaded",
        "previous_status": old_status,
        "message": "Scan reset to 'uploaded' – ready for re-processing",
    }


# ── Trigger post-processing independently ────────────────────────────────

@router.post("/{scan_id}/postprocess")
async def start_post_processing(scan_id: str):
    """
    Trigger the STL → FBX post-processing pipeline.

    Can be called at any time – it does NOT require RunPod to have just
    completed.  As long as there are STL files in the scan's stl/ directory,
    Blender will convert them to an FBX.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    if metadata.get("status") == "post_processing":
        return {
            "scan_id": scan_id,
            "status": "post_processing",
            "message": "Post-processing is already running",
        }

    result = await run_post_processing(scan_id)

    if "error" in result:
        raise HTTPException(status_code=500, detail=result["error"])

    return result

