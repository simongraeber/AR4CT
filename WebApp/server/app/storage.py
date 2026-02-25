"""Low-level helpers for scan storage and metadata persistence."""

import json
from pathlib import Path
from typing import Optional

from app.config import DATA_DIR


def get_scan_dir(scan_id: str) -> Path:
    """Get the directory for a scan."""
    return DATA_DIR / scan_id


def get_metadata_path(scan_id: str) -> Path:
    """Get the metadata.json path for a scan."""
    return get_scan_dir(scan_id) / "metadata.json"


def load_metadata(scan_id: str) -> Optional[dict]:
    """Load metadata for a scan."""
    metadata_path = get_metadata_path(scan_id)
    if metadata_path.exists():
        with open(metadata_path, "r") as f:
            return json.load(f)
    return None


def save_metadata(scan_id: str, metadata: dict):
    """Save metadata for a scan."""
    metadata_path = get_metadata_path(scan_id)
    with open(metadata_path, "w") as f:
        json.dump(metadata, f, indent=2)


def scan_exists(scan_id: str) -> bool:
    """Check if a scan exists."""
    return get_scan_dir(scan_id).exists()


def get_fbx_path(scan_id: str) -> Optional[Path]:
    """Get the FBX file path if it exists."""
    fbx_path = get_scan_dir(scan_id) / "model.fbx"
    if fbx_path.exists():
        return fbx_path
    return None


def get_usdz_path(scan_id: str) -> Optional[Path]:
    """Get the USDZ file path if it exists."""
    usdz_path = get_scan_dir(scan_id) / "model.usdz"
    if usdz_path.exists():
        return usdz_path
    return None
