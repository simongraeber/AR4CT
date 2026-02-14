"""Annotation point routes – set / get."""

from datetime import datetime

from fastapi import APIRouter, HTTPException

from app.models import Point3D
from app.storage import load_metadata, save_metadata, scan_exists

router = APIRouter(prefix="/scans", tags=["points"])


@router.post("/{scan_id}/point")
async def set_point(scan_id: str, point: Point3D):
    """Set or update the annotation point for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    metadata["point"] = {
        "x": point.x,
        "y": point.y,
        "z": point.z,
        "label": point.label,
        "set_at": datetime.utcnow().isoformat() + "Z",
    }
    save_metadata(scan_id, metadata)

    return {
        "scan_id": scan_id,
        "point": metadata["point"],
        "message": "Point saved successfully",
    }


@router.get("/{scan_id}/point")
async def get_point(scan_id: str):
    """Get the annotation point for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    return {"scan_id": scan_id, "point": metadata.get("point")}
