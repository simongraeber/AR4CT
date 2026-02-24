"""File upload / download routes – FBX, CT scan, STL."""

import os
from datetime import datetime

from fastapi import APIRouter, UploadFile, File, HTTPException
from fastapi.responses import FileResponse

from app.config import MAX_FILE_SIZE, MAX_STL_SIZE
from app.storage import (
    get_scan_dir,
    load_metadata,
    save_metadata,
    scan_exists,
    get_fbx_path,
)

router = APIRouter(prefix="/scans", tags=["files"])


# ── FBX ──────────────────────────────────────────────────────────────────

@router.get("/{scan_id}/fbx")
async def download_fbx(scan_id: str):
    """Download the FBX model file for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    fbx_path = get_fbx_path(scan_id)
    if not fbx_path:
        raise HTTPException(
            status_code=404,
            detail="FBX file not found. Scan may not be processed yet.",
        )

    return FileResponse(
        path=fbx_path,
        filename=f"{scan_id}.fbx",
        media_type="application/octet-stream",
    )


# ── CT Scan ──────────────────────────────────────────────────────────────

@router.post("/{scan_id}/ct")
async def upload_ct_for_scan(scan_id: str, file: UploadFile = File(...)):
    """
    Upload/replace the raw CT scan file for an existing scan.
    Supports: .zip, .nii, .nii.gz, .mhd, .nrrd
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    original_filename = file.filename or "ct_scan"
    scan_dir = get_scan_dir(scan_id)
    ct_path = scan_dir / f"ct_original_{original_filename}"

    total_size = 0
    try:
        with open(ct_path, "wb") as buffer:
            while chunk := await file.read(1024 * 1024):
                total_size += len(chunk)
                if total_size > MAX_FILE_SIZE:
                    buffer.close()
                    if ct_path.exists():
                        os.remove(ct_path)
                    raise HTTPException(
                        status_code=413,
                        detail=f"File too large. Maximum size is {MAX_FILE_SIZE / (1024**2):.0f} MB",
                    )
                buffer.write(chunk)
    except HTTPException:
        raise
    except Exception as e:
        if ct_path.exists():
            os.remove(ct_path)
        raise HTTPException(status_code=500, detail=f"Failed to save CT: {str(e)}")

    metadata = load_metadata(scan_id)
    metadata["ct_filename"] = original_filename
    metadata["ct_size"] = total_size
    metadata["ct_uploaded_at"] = datetime.utcnow().isoformat() + "Z"
    save_metadata(scan_id, metadata)

    return {
        "scan_id": scan_id,
        "message": "CT scan uploaded successfully",
        "filename": original_filename,
        "size": total_size,
    }


@router.get("/{scan_id}/ct")
async def download_ct(scan_id: str):
    """Download the raw CT scan file for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    scan_dir = get_scan_dir(scan_id)
    ct_files = list(scan_dir.glob("ct_original_*"))

    if not ct_files:
        raise HTTPException(status_code=404, detail="CT scan not found")

    ct_path = ct_files[0]
    original_filename = ct_path.name.replace("ct_original_", "")

    return FileResponse(
        path=ct_path,
        filename=original_filename,
        media_type="application/octet-stream",
    )


# ── STL (organ segmentation) ────────────────────────────────────────────

@router.post("/{scan_id}/stl/{organ}")
async def upload_stl(scan_id: str, organ: str, file: UploadFile = File(...)):
    """Upload an STL file for a specific organ segmentation."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    safe_organ = "".join(c for c in organ if c.isalnum() or c in "_-").lower()
    if not safe_organ:
        raise HTTPException(status_code=400, detail="Invalid organ name")

    scan_dir = get_scan_dir(scan_id)
    stl_dir = scan_dir / "stl"
    stl_dir.mkdir(exist_ok=True)
    stl_path = stl_dir / f"{safe_organ}.stl"

    total_size = 0
    try:
        with open(stl_path, "wb") as buffer:
            while chunk := await file.read(1024 * 1024):
                total_size += len(chunk)
                if total_size > MAX_STL_SIZE:
                    buffer.close()
                    if stl_path.exists():
                        os.remove(stl_path)
                    raise HTTPException(status_code=413, detail="STL file too large")
                buffer.write(chunk)
    except HTTPException:
        raise
    except Exception as e:
        if stl_path.exists():
            os.remove(stl_path)
        raise HTTPException(status_code=500, detail=f"Failed to save STL: {str(e)}")

    metadata = load_metadata(scan_id)
    if "stl_files" not in metadata:
        metadata["stl_files"] = {}
    metadata["stl_files"][safe_organ] = {
        "size": total_size,
        "uploaded_at": datetime.utcnow().isoformat() + "Z",
    }
    metadata["status"] = "segmented"
    save_metadata(scan_id, metadata)

    return {
        "scan_id": scan_id,
        "organ": safe_organ,
        "size": total_size,
        "message": "STL uploaded successfully",
    }


@router.get("/{scan_id}/stl/{organ}")
async def download_stl(scan_id: str, organ: str):
    """Download an STL file for a specific organ."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    safe_organ = "".join(c for c in organ if c.isalnum() or c in "_-").lower()
    scan_dir = get_scan_dir(scan_id)
    stl_path = scan_dir / "stl" / f"{safe_organ}.stl"

    if not stl_path.exists():
        raise HTTPException(
            status_code=404, detail=f"STL for organ '{organ}' not found"
        )

    return FileResponse(
        path=stl_path, filename=f"{safe_organ}.stl", media_type="model/stl"
    )


@router.get("/{scan_id}/stl")
async def list_stls(scan_id: str):
    """List all available STL files for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    scan_dir = get_scan_dir(scan_id)
    stl_dir = scan_dir / "stl"

    if not stl_dir.exists():
        return {"scan_id": scan_id, "stl_files": [], "count": 0}

    stl_files = [
        {
            "organ": p.stem,
            "size": p.stat().st_size,
            "url": f"/scans/{scan_id}/stl/{p.stem}",
        }
        for p in stl_dir.glob("*.stl")
    ]

    return {"scan_id": scan_id, "stl_files": stl_files, "count": len(stl_files)}
