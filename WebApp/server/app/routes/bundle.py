"""Bundle endpoint – all data Unity needs in one call."""

from typing import Optional

from fastapi import APIRouter, HTTPException

from app.storage import load_metadata, scan_exists, get_fbx_path

router = APIRouter(prefix="/scans", tags=["bundle"])


def _transform_point_to_fbx_space(
    point: dict,
    centre_offset: list[float],
) -> Optional[dict]:
    """
    Convert an annotation point from CT world coordinates (mm) into the
    FBX model's coordinate system (metres, centred at origin).

    The STL meshes produced by TotalSegmentator are already in metres
    (spacing and origin are multiplied by 0.001 during mesh generation).
    The Blender script then shifts everything by ``-centre_offset`` so the
    combined bounding-box centre sits at (0, 0, 0).

    The CT viewer stores points in **mm** (raw spacing × voxel + origin).
    So the transform is:
        point_fbx = point_mm × 0.001 − centre_offset
    """
    if not point:
        return None

    try:
        mm_to_m = 0.001
        return {
            "x": point["x"] * mm_to_m - centre_offset[0],
            "y": point["y"] * mm_to_m - centre_offset[1],
            "z": point["z"] * mm_to_m - centre_offset[2],
            "label": point.get("label"),
            "set_at": point.get("set_at"),
        }
    except (KeyError, TypeError, IndexError):
        return point  # fall back to raw point if anything is unexpected


@router.get("/{scan_id}/bundle")
async def get_bundle(scan_id: str):
    """
    Get the complete bundle for Unity: FBX download URL + point + metadata.
    This is the main endpoint for the AR application.

    The returned ``point`` is already transformed into the FBX model's
    coordinate space (metres, centred) so Unity can place it directly.
    The original (untransformed) point is available under ``point_raw``.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")

    has_fbx = get_fbx_path(scan_id) is not None

    raw_point = metadata.get("point")
    centre_offset = metadata.get("fbx_centre_offset", [0.0, 0.0, 0.0])

    # Transform the point so it aligns with the FBX model
    transformed_point = _transform_point_to_fbx_space(raw_point, centre_offset)

    return {
        "scan_id": scan_id,
        "fbx_url": f"/scans/{scan_id}/fbx" if has_fbx else None,
        "point": transformed_point,
        "point_raw": raw_point,
        "status": metadata.get("status"),
        "created_at": metadata.get("created_at"),
        "has_fbx": has_fbx,
    }
