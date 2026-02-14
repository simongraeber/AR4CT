"""Entry point – thin wrapper that exposes the app for uvicorn."""

from app import app  # noqa: F401

# Run with:  uvicorn main:app --host 0.0.0.0 --port 8000 --reload