"""Application configuration and constants."""

import os
from pathlib import Path

# Storage
DATA_DIR = Path(__file__).parent.parent / "data" / "scans"
DATA_DIR.mkdir(parents=True, exist_ok=True)

ASSETS_DIR = Path(__file__).parent.parent / "assets"
TOOL_IMAGE_PATH = ASSETS_DIR / "tool_image.png"

# Limits
MAX_FILE_SIZE = 500 * 1024 * 1024  # 500 MB
MAX_STL_SIZE = 100 * 1024 * 1024   # 100 MB per STL

# CORS origins
CORS_ORIGINS = [
    "http://localhost:5173",      # Vite dev server
    "http://localhost:3000",       # Docker dev
    "https://ar4ct.com",           # Production
    "https://www.ar4ct.com",       # Production with www
]

# Base URL used in QR codes / PDFs
PUBLIC_BASE_URL = os.environ.get("PUBLIC_BASE_URL", "https://ar4ct.com")

# Public API base URL (used to build callback URLs for RunPod)
API_BASE_URL = os.environ.get("API_BASE_URL", "https://api.ar4ct.com")

# RunPod configuration
RUNPOD_API_KEY = os.environ.get("RUNPOD_API_KEY", "")
RUNPOD_ENDPOINT_ID = os.environ.get("RUNPOD_ENDPOINT_ID", "")

# Default organs to segment
DEFAULT_ORGANS = [
    "liver", "heart",
    "lung_upper_lobe_left", "lung_upper_lobe_right",
    "lung_lower_lobe_left", "lung_lower_lobe_right",
    "lung_middle_lobe_right",
]
