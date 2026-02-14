"""Bundle endpoint – all data Unity needs in one call."""

from fastapi import APIRouter, HTTPException

from app.storage import load_metadata, scan_exists, get_fbx_path

router = APIRouter(prefix="/scans", tags=["bundle"])


@router.get("/{scan_id}/bundle")
async def get_bundle(scan_id: str):
    """
    Get the complete bundle for Unity: FBX download URL + point + metadata.
    This is the main endpoint for the AR application.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    has_fbx = get_fbx_path(scan_id) is not None

    return {
        "scan_id": scan_id,
        "fbx_url": f"/scans/{scan_id}/fbx" if has_fbx else None,
        "point": metadata.get("point"),
        "status": metadata.get("status"),
        "created_at": metadata.get("created_at"),
        "has_fbx": has_fbx,
    }
