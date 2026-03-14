"""
Blockade Labs API client for skybox generation.
Use from a backend only; do not expose API key to the frontend.

API docs: https://api-documentation.blockadelabs.com/api/
"""

import os
import time
from typing import Any

try:
    import requests
except ImportError:
    requests = None

BLOCKADE_BASE = "https://backend.blockadelabs.com/api/v1"
# Default style: M3 Photoreal (id 67). Required by API; override with skybox_style_id. List: GET .../skybox/styles?model_version=3
DEFAULT_SKYBOX_STYLE_ID = 67


def _require_requests() -> None:
    if requests is None:
        raise RuntimeError("Install requests: pip install requests")


def create_skybox(
    prompt: str,
    api_key: str | None = None,
    skybox_style_id: int | None = None,
    webhook_url: str | None = None,
) -> dict[str, Any]:
    """
    Submit a skybox generation request to Blockade Labs.
    Returns the initial response with id, status, pusher_channel, pusher_event, etc.
    """
    _require_requests()
    key = api_key or os.environ.get("BLOCKADE_LABS_API_KEY")
    if not key:
        raise ValueError("Blockade Labs API key required (BLOCKADE_LABS_API_KEY or api_key=...)")

    url = f"{BLOCKADE_BASE}/skybox"
    style_id = skybox_style_id if skybox_style_id is not None else DEFAULT_SKYBOX_STYLE_ID
    payload: dict[str, Any] = {"prompt": prompt, "skybox_style_id": style_id}
    if webhook_url:
        payload["webhook_url"] = webhook_url

    r = requests.post(
        url,
        json=payload,
        headers={"x-api-key": key, "Content-Type": "application/json"},
        timeout=30,
    )
    r.raise_for_status()
    return r.json()


def _unwrap_response(data: dict[str, Any]) -> dict[str, Any]:
    """GET imagine/requests returns payload under 'response'; obfuscated-id under 'request'."""
    if isinstance(data.get("response"), dict):
        return data["response"]
    if isinstance(data.get("request"), dict):
        return data["request"]
    return data


def get_skybox_status(request_id: int, api_key: str | None = None) -> dict[str, Any]:
    """Poll status of a skybox generation (GET imagine/requests/{id})."""
    _require_requests()
    key = api_key or os.environ.get("BLOCKADE_LABS_API_KEY")
    if not key:
        raise ValueError("Blockade Labs API key required")

    r = requests.get(
        f"{BLOCKADE_BASE}/imagine/requests/{request_id}",
        headers={"x-api-key": key},
        timeout=15,
    )
    r.raise_for_status()
    return _unwrap_response(r.json())


def get_skybox_status_by_obfuscated_id(
    obfuscated_id: str, api_key: str | None = None
) -> dict[str, Any]:
    """Fetch skybox status by obfuscated_id (hash from skybox URL, e.g. d7403868f2345bdb27199b8ff64c9f31)."""
    _require_requests()
    key = api_key or os.environ.get("BLOCKADE_LABS_API_KEY")
    if not key:
        raise ValueError("Blockade Labs API key required")

    r = requests.get(
        f"{BLOCKADE_BASE}/imagine/requests/obfuscated-id/{obfuscated_id.strip()}",
        headers={"x-api-key": key},
        timeout=15,
    )
    r.raise_for_status()
    return _unwrap_response(r.json())


def wait_for_skybox(
    request_id: int,
    api_key: str | None = None,
    poll_interval: float = 5.0,
    timeout_seconds: float = 300.0,
) -> dict[str, Any]:
    """
    Poll until status is complete, error, or abort.
    Returns the final status payload with file_url, thumb_url, depth_map_url when complete.
    """
    key = api_key or os.environ.get("BLOCKADE_LABS_API_KEY")
    start = time.monotonic()
    while True:
        data = get_skybox_status(request_id, api_key=key)
        status = (data.get("status") or "").lower()
        if status == "complete":
            return data
        if status in ("error", "abort"):
            raise RuntimeError(
                f"Blockade skybox {status}: {data.get('error_message') or data}"
            )
        if time.monotonic() - start > timeout_seconds:
            raise TimeoutError(
                f"Blockade skybox still {status} after {timeout_seconds}s"
            )
        time.sleep(poll_interval)


def generate_skybox_sync(
    prompt: str,
    api_key: str | None = None,
    skybox_style_id: int | None = None,
    poll_interval: float = 5.0,
    timeout_seconds: float = 300.0,
) -> dict[str, Any]:
    """
    Create a skybox and block until complete. Returns final payload with:
    - file_url: equirectangular image (8192x4096)
    - thumb_url: thumbnail (720x360)
    - depth_map_url: depth map (2048x1024)
    """
    resp = create_skybox(
        prompt,
        api_key=api_key,
        skybox_style_id=skybox_style_id,
    )
    # POST /skybox may return id at top level or under "response"
    request_id = resp.get("id") or (resp.get("response") or {}).get("id")
    if request_id is None:
        raise RuntimeError(f"No id in Blockade response: {resp}")
    return wait_for_skybox(
        request_id,
        api_key=api_key,
        poll_interval=poll_interval,
        timeout_seconds=timeout_seconds,
    )


def skybox_to_worldgen_pano_url(payload: dict[str, Any]) -> str:
    """Return the equirectangular image URL suitable for WorldGen _generate_world input."""
    url = payload.get("file_url") or ""
    if not url:
        raise ValueError("Blockade payload has no file_url (generation may not be complete)")
    return url
