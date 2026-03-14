#!/usr/bin/env python3
"""
Register an existing Blockade skybox as an OASIS world so you can load it.

Use when you already have a completed skybox (e.g. from the Blockade site).
Pass either the numeric request id or the obfuscated_id from the skybox URL:

  https://skybox.blockadelabs.com/d7403868f2345bdb27199b8ff64c9f31
                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                    use as: --obfuscated-id d7403868f2345bdb27199b8ff64c9f31

Requires: BLOCKADE_LABS_API_KEY in .env, OASIS_USER/OASIS_PASS or --username/--password.
"""

import argparse
import os
import sys
from pathlib import Path

def _load_dotenv() -> None:
    if os.environ.get("BLOCKADE_LABS_API_KEY"):
        return
    env_file = Path(__file__).resolve().parent / ".env"
    if not env_file.exists():
        return
    for line in env_file.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _, value = line.partition("=")
        key, value = key.strip(), value.strip().strip('"').strip("'")
        if key and key not in os.environ:
            os.environ[key] = value

_load_dotenv()

from demo_worldgen_oasis import auth, save_world_holon
from blockade_client import get_skybox_status, get_skybox_status_by_obfuscated_id


def _find_world_id(result: dict) -> str:
    holon_result = result.get("result") or result.get("Result") or {}
    inner = holon_result.get("result") or holon_result.get("Result") or holon_result
    world_id = inner.get("id") or inner.get("Id") or holon_result.get("id") or holon_result.get("Id") or ""
    if isinstance(world_id, dict):
        world_id = world_id.get("id") or world_id.get("Id") or ""
    return str(world_id) if world_id else ""


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Register an existing Blockade skybox as an OASIS world (by id or obfuscated-id from URL)"
    )
    parser.add_argument("--id", type=int, default=None, help="Blockade numeric request id")
    parser.add_argument(
        "--obfuscated-id",
        type=str,
        default=None,
        help="Blockade obfuscated_id (hash from skybox URL, e.g. d7403868f2345bdb27199b8ff64c9f31)",
    )
    parser.add_argument("--api-url", default="http://localhost:5003", help="ONODE API URL")
    parser.add_argument("--username", default=os.environ.get("OASIS_USER"), help="OASIS avatar username")
    parser.add_argument("--password", default=os.environ.get("OASIS_PASS"), help="OASIS avatar password")
    parser.add_argument("--name", default=None, help="World name (default: from prompt or 'Blockade Skybox')")
    args = parser.parse_args()

    if not args.id and not args.obfuscated_id:
        print("Error: pass --id <numeric> or --obfuscated-id <hash from skybox URL>", file=sys.stderr)
        sys.exit(1)
    if not args.username or not args.password:
        print("Error: --username and --password required (or set OASIS_USER, OASIS_PASS)", file=sys.stderr)
        sys.exit(1)

    api_url = args.api_url.rstrip("/")

    print("=== Register skybox as OASIS world ===\n")

    print("1. Fetching skybox from Blockade Labs...")
    if args.obfuscated_id:
        payload = get_skybox_status_by_obfuscated_id(args.obfuscated_id)
    else:
        payload = get_skybox_status(args.id)

    status = (payload.get("status") or "").lower()
    if status != "complete":
        print(f"   Status: {status}. Skybox must be complete to register.", file=sys.stderr)
        if status in ("error", "abort"):
            print(f"   {payload.get('error_message') or payload}", file=sys.stderr)
        sys.exit(1)

    file_url = payload.get("file_url") or ""
    thumb_url = payload.get("thumb_url") or ""
    depth_map_url = payload.get("depth_map_url") or ""
    prompt = (
        (payload.get("generator_data") or {}).get("prompt")
        or payload.get("prompt")
        or payload.get("title")
        or ""
    )

    if not file_url:
        print("Error: Blockade response has no file_url", file=sys.stderr)
        sys.exit(1)

    print(f"   Skybox: {file_url[:60]}...")
    print()

    name = args.name or (prompt[:50] + "..." if len(prompt) > 50 else prompt) if prompt else "Blockade Skybox"
    description = prompt or "Blockade Labs skybox"

    print("2. Authenticating with OASIS...")
    token = auth(api_url, args.username, args.password)
    print("   Got JWT\n")

    print("3. Saving STAR world holon (sceneAssetType=skybox)...")
    result = save_world_holon(
        api_url,
        token,
        name,
        description,
        scene_asset_url=file_url,
        scene_image_url=thumb_url or None,
        scene_asset_type="skybox",
        depth_map_url=depth_map_url or None,
    )
    if result.get("isError"):
        print(f"Error: {result.get('message')}", file=sys.stderr)
        sys.exit(1)

    world_id = _find_world_id(result)
    print(f"   World ID: {world_id}\n")

    print("=== Success — you can access the world at ===")
    print(f"  Load holon: GET {api_url}/api/data/load-holon/{world_id}")
    print(f"  World ID:  {world_id}")
    print(f"  Name:      {name}")


if __name__ == "__main__":
    main()
