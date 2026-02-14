"""Application configuration and constants."""

import os
from pathlib import Path

# Storage
DATA_DIR = Path(__file__).parent.parent / "data" / "scans"
DATA_DIR.mkdir(parents=True, exist_ok=True)

ASSETS_DIR = Path(__file__).parent.parent / "assets"
TOOL_IMAGE_PATH = ASSETS_DIR / "tool_image.png"

# Limits
MAX_FILE_SIZE = 500 * 1024 * 1024
MAX_STL_SIZE = 100 * 1024 * 1024

# CORS origins
CORS_ORIGINS = [
    "http://localhost:5173",
    "http://localhost:3000",
    "https://ar4ct.com",
    "https://www.ar4ct.com",
]

# Base URL used in QR codes / PDFs
PUBLIC_BASE_URL = os.environ.get("PUBLIC_BASE_URL", "https://ar4ct.com")

# Public API base URL (used to build callback URLs for RunPod)
API_BASE_URL = os.environ.get("API_BASE_URL", "https://api.ar4ct.com")

# RunPod configuration
RUNPOD_API_KEY = os.environ.get("RUNPOD_API_KEY", "")
RUNPOD_ENDPOINT_ID = os.environ.get("RUNPOD_ENDPOINT_ID", "")

# Default organs to segment – all 117 TotalSegmentator v2 "total" classes + body surface
DEFAULT_ORGANS = [
    "body",  # custom: extracted from raw CT via HU thresholding, not a TS label
    # ── Abdominal / pelvic organs ──
    "spleen", "kidney_right", "kidney_left", "gallbladder", "liver",
    "stomach", "pancreas", "adrenal_gland_right", "adrenal_gland_left",
    "esophagus", "small_bowel", "duodenum", "colon",
    "urinary_bladder", "prostate",
    "kidney_cyst_left", "kidney_cyst_right",
    # ── Lung lobes ──
    "lung_upper_lobe_left", "lung_lower_lobe_left",
    "lung_upper_lobe_right", "lung_middle_lobe_right", "lung_lower_lobe_right",
    # ── Airway / neck ──
    "trachea", "thyroid_gland",
    # ── Heart & great vessels ──
    "heart", "aorta", "pulmonary_vein",
    "brachiocephalic_trunk", "subclavian_artery_right", "subclavian_artery_left",
    "common_carotid_artery_right", "common_carotid_artery_left",
    "brachiocephalic_vein_left", "brachiocephalic_vein_right",
    "atrial_appendage_left", "superior_vena_cava", "inferior_vena_cava",
    "portal_vein_and_splenic_vein",
    "iliac_artery_left", "iliac_artery_right",
    "iliac_vena_left", "iliac_vena_right",
    # ── Vertebrae ──
    "sacrum", "vertebrae_S1",
    "vertebrae_L5", "vertebrae_L4", "vertebrae_L3", "vertebrae_L2", "vertebrae_L1",
    "vertebrae_T12", "vertebrae_T11", "vertebrae_T10", "vertebrae_T9",
    "vertebrae_T8", "vertebrae_T7", "vertebrae_T6", "vertebrae_T5",
    "vertebrae_T4", "vertebrae_T3", "vertebrae_T2", "vertebrae_T1",
    "vertebrae_C7", "vertebrae_C6", "vertebrae_C5", "vertebrae_C4",
    "vertebrae_C3", "vertebrae_C2", "vertebrae_C1",
    # ── Upper limb / shoulder bones ──
    "humerus_left", "humerus_right",
    "scapula_left", "scapula_right",
    "clavicula_left", "clavicula_right",
    # ── Lower limb / hip bones ──
    "femur_left", "femur_right",
    "hip_left", "hip_right",
    # ── Ribs & sternum ──
    "rib_left_1", "rib_left_2", "rib_left_3", "rib_left_4",
    "rib_left_5", "rib_left_6", "rib_left_7", "rib_left_8",
    "rib_left_9", "rib_left_10", "rib_left_11", "rib_left_12",
    "rib_right_1", "rib_right_2", "rib_right_3", "rib_right_4",
    "rib_right_5", "rib_right_6", "rib_right_7", "rib_right_8",
    "rib_right_9", "rib_right_10", "rib_right_11", "rib_right_12",
    "sternum", "costal_cartilages",
    # ── Muscles ──
    "gluteus_maximus_left", "gluteus_maximus_right",
    "gluteus_medius_left", "gluteus_medius_right",
    "gluteus_minimus_left", "gluteus_minimus_right",
    "autochthon_left", "autochthon_right",
    "iliopsoas_left", "iliopsoas_right",
    # ── CNS / skull ──
    "spinal_cord", "brain", "skull",
    # ── Face ──
    "face",
]
