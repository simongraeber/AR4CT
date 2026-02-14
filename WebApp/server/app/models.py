"""Pydantic models for request / response validation."""

from typing import Optional
from pydantic import BaseModel


class Point3D(BaseModel):
    x: float
    y: float
    z: float
    label: Optional[str] = "target"


class ScanMetadata(BaseModel):
    scan_id: str
    created_at: str
    original_filename: Optional[str] = None
    status: str  # "uploaded", "processing", "completed", "segmented", "error"
    point: Optional[dict] = None
    has_fbx: bool = False


class PointResponse(BaseModel):
    scan_id: str
    point: Optional[dict] = None


class BundleResponse(BaseModel):
    scan_id: str
    fbx_url: Optional[str] = None
    point: Optional[dict] = None
    metadata: dict
