# Blender MCP local setup

Date: 2026-06-06

## What was installed

The Blender MCP code was pinned locally instead of being launched from a floating package name:

```text
/Users/mathwidu/.codex/mcp-vendor/blender-mcp
```

Pinned commit:

```text
f76420613e5abb7c965df7ca84a1c52f3a211c5b
```

The Blender addon was copied to:

```text
/Users/mathwidu/Library/Application Support/Blender/5.1/scripts/addons/blender_mcp.py
```

The addon was enabled and Blender preferences were saved.

## Codex MCP configuration

The following MCP server was added to `/Users/mathwidu/.codex/config.toml`:

```toml
[mcp_servers.blender]
command = "/Users/mathwidu/Library/Python/3.9/bin/uvx"
args = ["--from", "/Users/mathwidu/.codex/mcp-vendor/blender-mcp", "blender-mcp"]
startup_timeout_sec = 120

[mcp_servers.blender.env]
DISABLE_TELEMETRY = "true"
BLENDER_HOST = "localhost"
BLENDER_PORT = "9876"
```

This means Codex should load Blender MCP after restart.

## Safe startup flow

1. Open Blender.
2. Use a disposable/default scene first.
3. Confirm the Blender MCP addon is enabled.
4. Keep optional integrations disabled:
   - Poly Haven;
   - Sketchfab;
   - Hyper3D Rodin;
   - Hunyuan3D.
5. Restart Codex so it reloads `/Users/mathwidu/.codex/config.toml`.
6. Ask Codex to search for Blender MCP tools.
7. Run a read-only health check before modifying any scene.

## Local health check

With Blender open, run:

```bash
cd /Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity
python3 tools/blender/check_blender_mcp.py
```

Expected behavior:

- it prints the current Blender scene name;
- it lists a few objects;
- it reports optional integrations as disabled;
- it does not execute arbitrary Python;
- it does not modify files or the scene.

## Security rules

Allowed immediately:

- `get_scene_info`;
- `get_object_info`;
- viewport screenshot;
- small inspected Blender Python snippets that only inspect objects/materials.

Requires review before execution:

- any `execute_blender_code` snippet that writes files;
- any script that imports `os`, `subprocess`, `socket`, `requests`, `urllib`, or `shutil`;
- any operation that saves, exports, deletes, downloads, or opens network URLs.

Blocked unless explicitly approved:

- entering API keys;
- enabling Sketchfab, Hyper3D, Hunyuan3D, or any paid/external generation service;
- running unreviewed code copied from a third-party skill;
- using the MCP against the Unity project assets before it passes on a disposable scene.

## Current validation

Validated on 2026-06-06:

- Blender 5.1.2 is installed;
- addon can auto-start in Blender;
- local socket at `localhost:9876` responds to `get_scene_info`;
- Codex config TOML parses with the new Blender MCP section;
- native Codex MCP tools still require restarting Codex to appear in the active tool registry.
