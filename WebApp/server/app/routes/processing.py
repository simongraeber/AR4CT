"""Processing routes – trigger segmentation, post-processing & status."""

import logging
from datetime import datetime

from fastapi import APIRouter, HTTPException

from app.config import API_BASE_URL, DEFAULT_ORGANS
from app.storage import load_metadata, save_metadata, scan_exists
from app.services.runpod import submit_segmentation_job, poll_job_status
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


# ── Poll status ──────────────────────────────────────────────────────────

@router.get("/{scan_id}/process/status")
async def get_processing_status(scan_id: str):
    """
    Check the current processing status.

    If a RunPod job_id exists and local status is still 'processing',
    we poll RunPod for an update and sync the metadata accordingly.
    When segmentation completes, post-processing is triggered automatically.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    runpod_job_id = metadata.get("runpod_job_id")

    # ── Auto-retry: if stuck in "uploaded" with no RunPod job, kick off
    #    segmentation automatically so the user doesn't have to do it manually.
    if metadata.get("status") == "uploaded" and not runpod_job_id:
        try:
            ct_url = f"{API_BASE_URL}/scans/{scan_id}/ct"
            callback_url = f"{API_BASE_URL}/scans/{scan_id}"
            result = await submit_segmentation_job(
                file_url=ct_url,
                organs=DEFAULT_ORGANS,
                callback_url=callback_url,
            )
            if "job_id" in result:
                metadata["status"] = "processing"
                metadata["runpod_job_id"] = result["job_id"]
                from datetime import datetime as _dt
                metadata["processing_started_at"] = _dt.utcnow().isoformat() + "Z"
                save_metadata(scan_id, metadata)
                runpod_job_id = result["job_id"]
                logger.info("Auto-triggered segmentation for stuck scan %s", scan_id)
        except Exception:
            logger.exception("Auto-trigger segmentation failed for scan %s", scan_id)

    if runpod_job_id and metadata.get("status") == "processing":
        rp_status = await poll_job_status(runpod_job_id)
        rp_state = rp_status.get("status", "UNKNOWN")

        if rp_state == "COMPLETED":
            metadata["status"] = "segmented"
            metadata["processing_completed_at"] = datetime.utcnow().isoformat() + "Z"
            # Clear errors from any previous failed attempt
            metadata.pop("processing_error", None)

            output = rp_status.get("output", {})
            metadata["organs_processed"] = output.get("organs_processed", [])
            save_metadata(scan_id, metadata)

            try:
                await run_post_processing(scan_id)
                metadata = load_metadata(scan_id)
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
        "has_fbx": metadata.get("has_fbx", False),
        "fbx_size": metadata.get("fbx_size"),
        "error": metadata.get("processing_error"),
    }
