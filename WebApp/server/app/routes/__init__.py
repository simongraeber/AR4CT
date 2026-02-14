"""Aggregate all route modules into a single list for easy inclusion."""

from app.routes.scans import router as scans_router
from app.routes.files import router as files_router
from app.routes.points import router as points_router
from app.routes.qr import router as qr_router
from app.routes.bundle import router as bundle_router
from app.routes.processing import router as processing_router

all_routers = [
    scans_router,
    files_router,
    points_router,
    qr_router,
    bundle_router,
    processing_router,
]
