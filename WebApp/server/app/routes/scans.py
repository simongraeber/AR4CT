"""Scan CRUD routes – upload, get, list, delete."""

import os
import uuid
import shutil
from datetime import datetime

from fastapi import APIRouter, UploadFile, File, HTTPException

from app.config import MAX_FILE_SIZE
from app.storage import (
    get_scan_dir,
    load_metadata,
    save_metadata,
    scan_exists,
    get_fbx_path,
)

router = APIRouter(prefix="/scans", tags=["scans"])


@router.post("/upload")
async def upload_scan(file: UploadFile = File(...)):
    """
    Upload a CT scan or FBX file. Returns a unique scan_id for access.
    Supports: .fbx, .zip, .nii, .nii.gz, .mhd, .nrrd
    """
    scan_id = str(uuid.uuid4())
    scan_dir = get_scan_dir(scan_id)
    scan_dir.mkdir(parents=True, exist_ok=True)

    original_filename = file.filename or "unnamed"
    lower_filename = original_filename.lower()

    if lower_filename.endswith(".fbx"):
        file_path = scan_dir / "model.fbx"
        status = "completed"
    else:
        file_path = scan_dir / f"ct_original_{original_filename}"
        status = "uploaded"

    total_size = 0
    try:
        with open(file_path, "wb") as buffer:
            while chunk := await file.read(1024 * 1024):
                total_size += len(chunk)
                if total_size > MAX_FILE_SIZE:
                    buffer.close()
                    shutil.rmtree(scan_dir)
                    raise HTTPException(
                        status_code=413,
                        detail=f"File too large. Maximum size is {MAX_FILE_SIZE / (1024**2):.0f} MB",
                    )
                buffer.write(chunk)
    except HTTPException:
        raise
    except Exception as e:
        if scan_dir.exists():
            shutil.rmtree(scan_dir)
        raise HTTPException(status_code=500, detail=f"Failed to save file: {str(e)}")

    metadata = {
        "scan_id": scan_id,
        "created_at": datetime.utcnow().isoformat() + "Z",
        "original_filename": original_filename,
        "file_size": total_size,
        "status": status,
        "point": None,
        "has_fbx": lower_filename.endswith(".fbx"),
    }
    save_metadata(scan_id, metadata)

    # Auto-trigger segmentation for CT uploads
    if not metadata["has_fbx"]:
        from app.services.runpod import submit_segmentation_job
        from app.config import API_BASE_URL, DEFAULT_ORGANS
        import logging

        logger = logging.getLogger(__name__)
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
                metadata["processing_started_at"] = datetime.utcnow().isoformat() + "Z"
                save_metadata(scan_id, metadata)
        except Exception:
            logger.exception("Failed to auto-trigger RunPod for scan %s", scan_id)

    return {
        "scan_id": scan_id,
        "filename": original_filename,
        "size": total_size,
        "status": status,
        "has_fbx": metadata["has_fbx"],
        "message": (
            "FBX uploaded successfully"
            if metadata["has_fbx"]
            else "CT scan uploaded – segmentation submitted to RunPod"
            if metadata.get("status") == "processing"
            else "CT scan uploaded"
        ),
    }


@router.get("/{scan_id}")
async def get_scan(scan_id: str):
    """Get scan metadata and status."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    metadata["has_fbx"] = get_fbx_path(scan_id) is not None
    return metadata


@router.get("")
async def list_scans():
    """List all scans."""
    from app.config import DATA_DIR

    scans = []
    for scan_dir in DATA_DIR.iterdir():
        if scan_dir.is_dir():
            scan_id = scan_dir.name
            metadata = load_metadata(scan_id)
            if metadata:
                metadata["has_fbx"] = get_fbx_path(scan_id) is not None
                scans.append(
                    {
                        "scan_id": scan_id,
                        "created_at": metadata.get("created_at"),
                        "status": metadata.get("status"),
                        "has_point": metadata.get("point") is not None,
                        "has_fbx": metadata["has_fbx"],
                        "original_filename": metadata.get("original_filename"),
                    }
                )

    scans.sort(key=lambda x: x.get("created_at", ""), reverse=True)
    return {"scans": scans, "count": len(scans)}


@router.delete("/{scan_id}")
async def delete_scan(scan_id: str):
    """Delete a scan and all its files."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    scan_dir = get_scan_dir(scan_id)
    shutil.rmtree(scan_dir)
    return {"message": "Scan deleted successfully", "scan_id": scan_id}
