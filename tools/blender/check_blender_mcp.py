#!/usr/bin/env python3
"""Safe local health check for the Blender MCP addon socket.

This script talks directly to the Blender addon socket on localhost. It does not
execute arbitrary Python in Blender and does not modify the current scene.
"""

from __future__ import annotations

import json
import socket
import sys
from typing import Any


HOST = "localhost"
PORT = 9876


def send_command(command_type: str, params: dict[str, Any] | None = None) -> dict[str, Any]:
    payload = {"type": command_type, "params": params or {}}
    with socket.create_connection((HOST, PORT), timeout=5) as sock:
        sock.sendall(json.dumps(payload).encode("utf-8"))
        sock.settimeout(15)
        chunks: list[bytes] = []
        while True:
            chunk = sock.recv(65536)
            if not chunk:
                break
            chunks.append(chunk)
            try:
                return json.loads(b"".join(chunks).decode("utf-8"))
            except json.JSONDecodeError:
                continue
    raise RuntimeError("Blender MCP socket closed before a complete JSON response.")


def require_success(command_type: str) -> dict[str, Any]:
    response = send_command(command_type)
    if response.get("status") != "success":
        raise RuntimeError(f"{command_type} failed: {response}")
    result = response.get("result", {})
    if not isinstance(result, dict):
        raise RuntimeError(f"{command_type} returned an unexpected result: {result!r}")
    return result


def main() -> int:
    print(f"Checking Blender MCP addon at {HOST}:{PORT}...")
    scene = require_success("get_scene_info")

    print(f"Scene: {scene.get('name')} ({scene.get('object_count')} objects)")
    for obj in scene.get("objects", []):
        print(f"- {obj.get('name')} [{obj.get('type')}] at {obj.get('location')}")

    optional_checks = {
        "Poly Haven": "get_polyhaven_status",
        "Hyper3D": "get_hyper3d_status",
        "Sketchfab": "get_sketchfab_status",
        "Hunyuan3D": "get_hunyuan3d_status",
    }
    for label, command in optional_checks.items():
        result = require_success(command)
        enabled = result.get("enabled")
        print(f"{label}: {'enabled' if enabled else 'disabled'}")
        if enabled:
            print(f"WARNING: {label} is enabled. Disable it for the no-cost local pipeline.")

    print("Blender MCP addon health check passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        print(
            "Open Blender, ensure the Blender MCP addon is enabled, and confirm "
            "the server is connected on localhost:9876.",
            file=sys.stderr,
        )
        raise SystemExit(1)
