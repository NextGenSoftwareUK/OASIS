#!/usr/bin/env python3
"""Smoke-test a published OASIS MCP executable over its real stdio protocol."""

from __future__ import annotations

import argparse
import json
import queue
import subprocess
import threading
from pathlib import Path


def read_line(stream, timeout_seconds: int) -> str:
    result: queue.Queue[str] = queue.Queue(maxsize=1)
    threading.Thread(target=lambda: result.put(stream.readline()), daemon=True).start()
    try:
        line = result.get(timeout=timeout_seconds)
    except queue.Empty as exc:
        raise RuntimeError("Timed out waiting for the MCP server response") from exc
    if not line:
        raise RuntimeError("MCP server closed stdout before returning a response")
    return line


def send(stream, message: dict[str, object]) -> None:
    stream.write(json.dumps(message, separators=(",", ":")) + "\n")
    stream.flush()


def receive(stream, request_id: int, timeout_seconds: int) -> dict[str, object]:
    response = json.loads(read_line(stream, timeout_seconds))
    if response.get("id") != request_id:
        raise RuntimeError(f"Expected response id {request_id}, received {response.get('id')!r}")
    if "error" in response:
        raise RuntimeError(f"MCP request {request_id} failed: {response['error']}")
    return response


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("binary")
    parser.add_argument("--timeout-seconds", type=int, default=30)
    parser.add_argument("--minimum-tools", type=int, default=500)
    args = parser.parse_args()

    binary = Path(args.binary).resolve(strict=True)
    process = subprocess.Popen(
        [str(binary)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
    )
    assert process.stdin is not None
    assert process.stdout is not None
    assert process.stderr is not None

    try:
        send(
            process.stdin,
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": "2025-06-18",
                    "capabilities": {},
                    "clientInfo": {"name": "oasis-release-smoke", "version": "1.0"},
                },
            },
        )
        initialized = receive(process.stdout, 1, args.timeout_seconds)
        server_name = initialized["result"]["serverInfo"]["name"]
        if server_name != "oasis-web4-to-web10-mcp":
            raise RuntimeError(f"Unexpected MCP server name: {server_name!r}")

        send(process.stdin, {"jsonrpc": "2.0", "method": "notifications/initialized"})
        send(process.stdin, {"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
        tools_response = receive(process.stdout, 2, args.timeout_seconds)
        tools = tools_response["result"]["tools"]
        if len(tools) < args.minimum_tools:
            raise RuntimeError(
                f"Expected at least {args.minimum_tools} MCP tools, received {len(tools)}"
            )
        print(f"MCP release smoke test passed: server={server_name}, tools={len(tools)}")
        return 0
    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)


if __name__ == "__main__":
    raise SystemExit(main())
