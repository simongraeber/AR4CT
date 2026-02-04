import os
import uuid
import json
from datetime import datetime
from pathlib import Path
from typing import Optional
from fastapi import FastAPI, UploadFile, File, HTTPException, Form
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel

app = FastAPI(title="AR4CT API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",      # Vite dev server
        "http://localhost:3000",       # Docker dev
        "https://ar4ct.com",           # Production
        "https://www.ar4ct.com",       # Production with www
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Storage configuration
DATA_DIR = Path(__file__).parent / "data" / "scans"
DATA_DIR.mkdir(parents=True, exist_ok=True)
MAX_FILE_SIZE = 500 * 1024 * 1024  # 500 MB


# --- Pydantic Models ---

class Point3D(BaseModel):
    x: float
    y: float
    z: float
    label: Optional[str] = "target"


class ScanMetadata(BaseModel):
    scan_id: str
    created_at: str
    original_filename: Optional[str] = None
    status: str  # "uploaded", "processing", "completed", "error"
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


# --- Helper Functions ---

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


# --- API Endpoints ---

@app.get("/")
async def root():
    return {"message": "AR4CT API", "version": "1.0.0"}


@app.get("/hello")
async def hello():
    return {"message": "Hello from AR4CT API"}


# --- Scan Upload ---

@app.post("/scans/upload")
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
                    # Clean up
                    import shutil
                    shutil.rmtree(scan_dir)
                    raise HTTPException(
                        status_code=413,
                        detail=f"File too large. Maximum size is {MAX_FILE_SIZE / (1024**2):.0f} MB"
                    )
                buffer.write(chunk)
    except HTTPException:
        raise
    except Exception as e:
        import shutil
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
        "has_fbx": lower_filename.endswith(".fbx")
    }
    save_metadata(scan_id, metadata)
    
    return {
        "scan_id": scan_id,
        "filename": original_filename,
        "size": total_size,
        "status": status,
        "has_fbx": metadata["has_fbx"],
        "message": "FBX uploaded successfully" if metadata["has_fbx"] else "CT scan uploaded. Processing not yet implemented."
    }


# --- Get Scan Metadata ---

@app.get("/scans/{scan_id}")
async def get_scan(scan_id: str):
    """Get scan metadata and status."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")
    
    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")
    
    metadata["has_fbx"] = get_fbx_path(scan_id) is not None
    
    return metadata


# --- Set Point ---

@app.post("/scans/{scan_id}/point")
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
        "set_at": datetime.utcnow().isoformat() + "Z"
    }
    save_metadata(scan_id, metadata)
    
    return {
        "scan_id": scan_id,
        "point": metadata["point"],
        "message": "Point saved successfully"
    }


# --- Get Point ---

@app.get("/scans/{scan_id}/point")
async def get_point(scan_id: str):
    """Get the annotation point for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")
    
    metadata = load_metadata(scan_id)
    if not metadata:
        raise HTTPException(status_code=404, detail="Scan metadata not found")
    
    return {
        "scan_id": scan_id,
        "point": metadata.get("point")
    }


# --- Download FBX ---

@app.get("/scans/{scan_id}/fbx")
async def download_fbx(scan_id: str):
    """Download the FBX model file for a scan."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")
    
    fbx_path = get_fbx_path(scan_id)
    if not fbx_path:
        raise HTTPException(status_code=404, detail="FBX file not found. Scan may not be processed yet.")
    
    return FileResponse(
        path=fbx_path,
        filename=f"{scan_id}.fbx",
        media_type="application/octet-stream"
    )


# --- Get Bundle (FBX URL + Point + Metadata) ---

@app.get("/scans/{scan_id}/bundle")
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
        "has_fbx": has_fbx
    }


# --- List All Scans ---

@app.get("/scans")
async def list_scans():
    """List all scans."""
    scans = []
    
    for scan_dir in DATA_DIR.iterdir():
        if scan_dir.is_dir():
            scan_id = scan_dir.name
            metadata = load_metadata(scan_id)
            if metadata:
                metadata["has_fbx"] = get_fbx_path(scan_id) is not None
                scans.append({
                    "scan_id": scan_id,
                    "created_at": metadata.get("created_at"),
                    "status": metadata.get("status"),
                    "has_point": metadata.get("point") is not None,
                    "has_fbx": metadata["has_fbx"],
                    "original_filename": metadata.get("original_filename")
                })
    
    scans.sort(key=lambda x: x.get("created_at", ""), reverse=True)
    
    return {"scans": scans, "count": len(scans)}


# --- Delete Scan ---

@app.delete("/scans/{scan_id}")
async def delete_scan(scan_id: str):
    """Delete a scan and all its files."""
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")
    
    import shutil
    scan_dir = get_scan_dir(scan_id)
    shutil.rmtree(scan_dir)
    
    return {"message": "Scan deleted successfully", "scan_id": scan_id}


# --- Upload FBX for existing scan (manual upload) ---

@app.post("/scans/{scan_id}/fbx")
async def upload_fbx_for_scan(scan_id: str, file: UploadFile = File(...)):
    """
    Upload/replace the FBX file for an existing scan.
    Use this for manual FBX uploads after processing externally.
    """
    if not scan_exists(scan_id):
        raise HTTPException(status_code=404, detail="Scan not found")
    
    if not file.filename.lower().endswith(".fbx"):
        raise HTTPException(status_code=400, detail="File must be an FBX file")
    
    scan_dir = get_scan_dir(scan_id)
    fbx_path = scan_dir / "model.fbx"
    
    total_size = 0
    try:
        with open(fbx_path, "wb") as buffer:
            while chunk := await file.read(1024 * 1024):
                total_size += len(chunk)
                if total_size > MAX_FILE_SIZE:
                    buffer.close()
                    if fbx_path.exists():
                        os.remove(fbx_path)
                    raise HTTPException(
                        status_code=413,
                        detail=f"File too large. Maximum size is {MAX_FILE_SIZE / (1024**2):.0f} MB"
                    )
                buffer.write(chunk)
    except HTTPException:
        raise
    except Exception as e:
        if fbx_path.exists():
            os.remove(fbx_path)
        raise HTTPException(status_code=500, detail=f"Failed to save FBX: {str(e)}")
    
    metadata = load_metadata(scan_id)
    metadata["status"] = "completed"
    metadata["has_fbx"] = True
    metadata["fbx_uploaded_at"] = datetime.utcnow().isoformat() + "Z"
    save_metadata(scan_id, metadata)
    
    return {
        "scan_id": scan_id,
        "message": "FBX uploaded successfully",
        "size": total_size,
        "fbx_url": f"/scans/{scan_id}/fbx"
    }
