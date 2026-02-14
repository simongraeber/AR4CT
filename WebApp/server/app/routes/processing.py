"""Processing routes – trigger segmentation & check job status."""

import logging
from datetime import datetime

from fastapi import APIRouter, HTTPException

from app.config import API_BASE_URL, DEFAULT_ORGANS
from app.storage import load_metadata, save_metadata, scan_exists
from app.services.runpod import submit_segmentation_job, poll_job_status
from app.services import run_post_processing

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/scans", tags=["processing"])


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

    # Don't re-trigger if already processing or completed
    if metadata.get("status") in ("processing", "segmented", "completed", "post_processed"):
        return {
            "scan_id": scan_id,
            "status": metadata["status"],
            "runpod_job_id": metadata.get("runpod_job_id"),
            "message": f"Scan is already in status '{metadata['status']}'",
        }

    # Build URLs that RunPod will use
    ct_url = f"{API_BASE_URL}/scans/{scan_id}/ct"
    callback_url = f"{API_BASE_URL}/scans/{scan_id}"

    result = await submit_segmentation_job(
        file_url=ct_url,
        organs=DEFAULT_ORGANS,
        callback_url=callback_url,
    )

    if "error" in result:
        raise HTTPException(status_code=502, detail=result["error"])

    # Persist job info
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


@router.get("/{scan_id}/process/status")
async def get_processing_status(scan_id: str):
    """
    Check the current processing status.

    If a RunPod job_id exists and local status is still 'processing',
    we poll RunPod for an update and sync the metadata accordingly.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    runpod_job_id = metadata.get("runpod_job_id")

    # If we have a pending job, poll RunPod
    if runpod_job_id and metadata.get("status") == "processing":
        rp_status = await poll_job_status(runpod_job_id)
        rp_state = rp_status.get("status", "UNKNOWN")

        if rp_state == "COMPLETED":
            metadata["status"] = "segmented"
            metadata["processing_completed_at"] = datetime.utcnow().isoformat() + "Z"

            output = rp_status.get("output", {})
            metadata["organs_processed"] = output.get("organs_processed", [])
            save_metadata(scan_id, metadata)

            # Fire post-processing (non-blocking intent, but awaited for now)
            try:
                await run_post_processing(scan_id)
            except Exception:
                logger.exception("Post-processing failed for scan %s", scan_id)

        elif rp_state == "FAILED":
            metadata["status"] = "error"
            metadata["processing_error"] = rp_status.get("error", "Unknown RunPod error")
            save_metadata(scan_id, metadata)

    return {
        "scan_id": scan_id,
        "status": metadata.get("status"),
        "runpod_job_id": runpod_job_id,
        "organs_processed": metadata.get("organs_processed", []),
        "error": metadata.get("processing_error"),
    }
