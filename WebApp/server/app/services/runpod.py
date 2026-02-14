"""RunPod integration – submit segmentation jobs and poll for results."""

import logging
from typing import Optional

import httpx

from app.config import RUNPOD_API_KEY, RUNPOD_ENDPOINT_ID

logger = logging.getLogger(__name__)

RUNPOD_BASE = "https://api.runpod.ai/v2"


def _headers() -> dict:
    return {
        "Authorization": f"Bearer {RUNPOD_API_KEY}",
        "Content-Type": "application/json",
    }


async def submit_segmentation_job(
    file_url: str,
    organs: list[str],
    callback_url: Optional[str] = None,
    fast: bool = False,
) -> dict:
    """
    Submit an async segmentation job to RunPod.

    Args:
        file_url:     Public URL where RunPod can download the CT scan.
        organs:       List of organ names to segment.
        callback_url: Base scan URL; RunPod uploads STLs to {callback_url}/stl/{organ}.
        fast:         Use fast (3 mm) mode.

    Returns:
        {"job_id": "...", "status": "IN_QUEUE"} on success,
        {"error": "..."} on failure.
    """
    if not RUNPOD_API_KEY or not RUNPOD_ENDPOINT_ID:
        logger.warning("RunPod credentials not configured – skipping job submission")
        return {"error": "RunPod is not configured on this server"}

    payload: dict = {
        "input": {
            "file_url": file_url,
            "organs": organs,
            "fast": fast,
        }
    }
    if callback_url:
        payload["input"]["callback_url"] = callback_url

    url = f"{RUNPOD_BASE}/{RUNPOD_ENDPOINT_ID}/run"
    logger.info("Submitting RunPod job to %s", url)

    async with httpx.AsyncClient(timeout=30) as client:
        resp = await client.post(url, headers=_headers(), json=payload)

    body = resp.json()
    if resp.status_code != 200 or "id" not in body:
        logger.error("RunPod submission failed: %s", body)
        return {"error": body.get("error", "Failed to submit RunPod job"), "details": body}

    return {"job_id": body["id"], "status": body.get("status", "IN_QUEUE")}


async def poll_job_status(job_id: str) -> dict:
    """
    Poll RunPod for the status of a previously submitted job.

    Returns the raw status payload, e.g.:
        {"status": "COMPLETED", "output": {...}}
        {"status": "IN_PROGRESS"}
        {"status": "FAILED", "error": "..."}
    """
    if not RUNPOD_API_KEY or not RUNPOD_ENDPOINT_ID:
        return {"error": "RunPod is not configured on this server"}

    url = f"{RUNPOD_BASE}/{RUNPOD_ENDPOINT_ID}/status/{job_id}"

    async with httpx.AsyncClient(timeout=30) as client:
        resp = await client.get(url, headers=_headers())

    return resp.json()
