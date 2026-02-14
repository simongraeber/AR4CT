"""CT viewer routes – serve CT slices as PNG images for the web viewer.

Endpoints
---------
GET /scans/{scan_id}/ct/info
    Volume metadata (dimensions, spacing, origin, value range).

GET /scans/{scan_id}/ct/slice/{axis}/{index}?wc=40&ww=400
    A single 2-D slice rendered as an 8-bit grayscale PNG with HU windowing.

The volume is loaded once and cached in-process (LRU, max 2 volumes) so
subsequent slice requests are fast.
"""

import io
import zlib
import zipfile
import tempfile
import logging
from pathlib import Path
from typing import Optional

import numpy as np
from fastapi import APIRouter, HTTPException, Query
from fastapi.responses import Response

from app.storage import get_scan_dir, scan_exists

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/scans", tags=["ct_viewer"])

# ---------------------------------------------------------------------------
# In-memory volume cache
# ---------------------------------------------------------------------------

_volume_cache: dict[str, dict] = {}
_MAX_CACHE = 2


# ---------------------------------------------------------------------------
# Loaders
# ---------------------------------------------------------------------------

_MHD_DTYPE_MAP = {
    "MET_SHORT": np.int16,
    "MET_USHORT": np.uint16,
    "MET_INT": np.int32,
    "MET_UINT": np.uint32,
    "MET_FLOAT": np.float32,
    "MET_DOUBLE": np.float64,
    "MET_UCHAR": np.uint8,
    "MET_CHAR": np.int8,
}


def _load_mhd(mhd_path: Path) -> tuple[np.ndarray, tuple, tuple]:
    """Load a MetaImage (.mhd + .raw/.zraw) file pair."""
    meta: dict[str, str] = {}
    with open(mhd_path, "r") as f:
        for line in f:
            line = line.strip()
            if "=" in line:
                key, value = line.split("=", 1)
                meta[key.strip()] = value.strip()

    dims = tuple(int(x) for x in meta["DimSize"].split())
    spacing = tuple(float(x) for x in meta.get("ElementSpacing", "1 1 1").split())
    origin = tuple(float(x) for x in meta.get("Offset", "0 0 0").split())
    dtype = _MHD_DTYPE_MAP.get(meta.get("ElementType", "MET_SHORT"), np.int16)

    data_file = meta.get("ElementDataFile", "")
    data_path = mhd_path.parent / data_file
    if not data_path.exists():
        raise FileNotFoundError(f"Raw data file not found: {data_path}")

    logger.info("Reading raw data from %s …", data_path.name)
    with open(data_path, "rb") as f:
        raw_bytes = f.read()

    compressed = meta.get("CompressedData", "False").lower() in ("true", "1", "yes")
    if compressed:
        logger.info("Decompressing zlib data (%d bytes compressed) …", len(raw_bytes))
        raw_bytes = zlib.decompress(raw_bytes)

    total_voxels = dims[0] * dims[1] * dims[2]
    expected = total_voxels * np.dtype(dtype).itemsize
    if len(raw_bytes) < expected:
        raise ValueError(
            f"Data file too small: got {len(raw_bytes)} bytes, expected {expected}"
        )

    volume = np.frombuffer(raw_bytes, dtype=dtype, count=total_voxels)
    # MHD stores x-fastest (column-major). In C-order that is (z, y, x).
    volume = volume.reshape((dims[2], dims[1], dims[0]))
    volume = np.transpose(volume, (2, 1, 0))  # → (x, y, z)

    return volume, spacing, origin


def _load_nifti(nii_path: Path) -> tuple[np.ndarray, tuple, tuple]:
    """Load a NIfTI (`.nii` or `.nii.gz`) file."""
    import nibabel as nib

    img = nib.load(str(nii_path))
    data = np.asarray(img.dataobj)
    # Keep a reasonable dtype
    if data.dtype == np.float64:
        data = data.astype(np.float32)
    spacing = tuple(float(s) for s in img.header.get_zooms()[:3])
    origin = tuple(float(o) for o in img.affine[:3, 3])
    return data, spacing, origin


# ---------------------------------------------------------------------------
# Volume loading & caching
# ---------------------------------------------------------------------------


def _find_and_load(scan_id: str) -> dict:
    """Locate the CT file for *scan_id*, load and return volume+meta dict."""
    scan_dir = get_scan_dir(scan_id)
    ct_files = list(scan_dir.glob("ct_original_*"))
    if not ct_files:
        raise HTTPException(status_code=404, detail="No CT file found for this scan")

    ct_path = ct_files[0]
    temp_dir: Optional[tempfile.TemporaryDirectory] = None

    try:
        load_path = ct_path

        # ── Handle ZIP archives ──────────────────────────────────
        if ct_path.suffix.lower() == ".zip":
            temp_dir = tempfile.TemporaryDirectory()
            logger.info("Extracting ZIP %s …", ct_path.name)
            with zipfile.ZipFile(ct_path, "r") as z:
                z.extractall(temp_dir.name)

            candidates = [
                f
                for f in Path(temp_dir.name).rglob("*")
                if f.suffix.lower() in (".nii", ".gz", ".mhd", ".nrrd")
            ]
            if not candidates:
                raise HTTPException(
                    status_code=422,
                    detail="No supported image file (.nii, .mhd, .nrrd) in ZIP",
                )
            mhds = [f for f in candidates if f.suffix.lower() == ".mhd"]
            niftis = [f for f in candidates if ".nii" in f.name.lower()]
            load_path = (mhds or niftis or candidates)[0]

        # ── Dispatch by extension ────────────────────────────────
        ext = load_path.suffix.lower()
        name_lower = load_path.name.lower()

        if ext == ".mhd":
            volume, spacing, origin = _load_mhd(load_path)
        elif ".nii" in name_lower:
            volume, spacing, origin = _load_nifti(load_path)
        else:
            # Last resort: try SimpleITK if installed
            try:
                import SimpleITK as sitk

                reader = sitk.ImageFileReader()
                reader.SetFileName(str(load_path))
                img = reader.Execute()
                spacing = img.GetSpacing()
                origin = img.GetOrigin()
                arr = sitk.GetArrayFromImage(img)
                del img
                volume = np.transpose(arr, (2, 1, 0))
            except ImportError:
                raise HTTPException(
                    status_code=422,
                    detail=f"Unsupported format '{ext}'. Supported: .mhd, .nii, .nii.gz, .zip",
                )

        return {
            "volume": volume,
            "spacing": tuple(float(s) for s in spacing),
            "origin": tuple(float(o) for o in origin),
            "dimensions": (int(volume.shape[0]), int(volume.shape[1]), int(volume.shape[2])),
            "min_value": float(np.nanmin(volume)),
            "max_value": float(np.nanmax(volume)),
        }
    finally:
        if temp_dir:
            temp_dir.cleanup()


def _get_volume(scan_id: str) -> dict:
    """Return cached volume or load + cache it."""
    if scan_id in _volume_cache:
        return _volume_cache[scan_id]

    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")

    try:
        vol = _find_and_load(scan_id)
    except HTTPException:
        raise
    except Exception as exc:
        logger.exception("Failed to load CT volume for scan %s", scan_id)
        raise HTTPException(status_code=500, detail=f"Failed to load CT data: {exc}")

    logger.info(
        "Cached CT volume for %s — dims=%s, spacing=%s, HU range [%.0f, %.0f]",
        scan_id,
        vol["dimensions"],
        vol["spacing"],
        vol["min_value"],
        vol["max_value"],
    )

    while len(_volume_cache) >= _MAX_CACHE:
        evict = next(iter(_volume_cache))
        logger.info("Evicting cached volume %s", evict)
        del _volume_cache[evict]

    _volume_cache[scan_id] = vol
    return vol


# ---------------------------------------------------------------------------
# Slice rendering
# ---------------------------------------------------------------------------


def _render_slice_jpeg(
    volume: np.ndarray, axis: str, index: int, wc: float, ww: float
) -> bytes:
    """Render a 2-D slice as a windowed 8-bit grayscale JPEG (fast)."""
    from PIL import Image

    dims = volume.shape  # (x, y, z)

    if axis == "axial":
        if not (0 <= index < dims[2]):
            raise HTTPException(400, f"Index {index} out of range [0, {dims[2] - 1}]")
        s = volume[:, :, index]  # shape (x, y)
    elif axis == "sagittal":
        if not (0 <= index < dims[0]):
            raise HTTPException(400, f"Index {index} out of range [0, {dims[0] - 1}]")
        s = volume[index, :, :]  # shape (y, z)
    elif axis == "coronal":
        if not (0 <= index < dims[1]):
            raise HTTPException(400, f"Index {index} out of range [0, {dims[1] - 1}]")
        s = volume[:, index, :]  # shape (x, z)
    else:
        raise HTTPException(400, f"Unknown axis '{axis}'. Use axial, sagittal, or coronal.")

    # Window / Level to 0–255
    lo = wc - ww / 2.0
    hi = wc + ww / 2.0
    arr = np.clip(s.astype(np.float32), lo, hi)
    arr = np.nan_to_num(arr, nan=lo)
    arr = ((arr - lo) / max(hi - lo, 1e-6) * 255.0).astype(np.uint8)

    # Transpose so that the first numpy axis (rows) = second spatial axis
    # i.e. image rows = y or z, columns = x or y.
    arr = arr.T

    img = Image.fromarray(arr, mode="L")
    buf = io.BytesIO()
    img.save(buf, format="JPEG", quality=85)
    return buf.getvalue()


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------


@router.get("/{scan_id}/ct/info")
async def ct_info(scan_id: str):
    """Return CT volume metadata for the viewer."""
    vol = _get_volume(scan_id)
    d = vol["dimensions"]
    return {
        "scan_id": scan_id,
        "dimensions": d,
        "spacing": vol["spacing"],
        "origin": vol["origin"],
        "min_value": vol["min_value"],
        "max_value": vol["max_value"],
        "slice_sizes": {
            # [width, height] of the resulting PNG for each axis
            "axial": [d[0], d[1]],
            "sagittal": [d[1], d[2]],
            "coronal": [d[0], d[2]],
        },
    }


@router.get("/{scan_id}/ct/slice/{axis}/{index}")
async def ct_slice(
    scan_id: str,
    axis: str,
    index: int,
    wc: float = Query(40, description="Window centre (HU)"),
    ww: float = Query(400, description="Window width (HU)"),
):
    """Return a single 2-D CT slice as a grayscale JPEG."""
    vol = _get_volume(scan_id)
    jpeg = _render_slice_jpeg(vol["volume"], axis, index, wc, ww)
    return Response(
        content=jpeg,
        media_type="image/jpeg",
        headers={"Cache-Control": "public, max-age=3600"},
    )
