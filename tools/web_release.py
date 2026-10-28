#!/usr/bin/env python3
"""Package and verify the already-built Unity Web release; never build or publish it."""
import argparse
import gzip
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import tarfile
import tempfile


REPO_ROOT = Path(__file__).resolve().parents[1]
CHUNK = 1024 * 1024
MAX_GZIP_BYTES = 1024 * 1024 * 1024
MAX_ARCHIVE_BYTES = 256 * 1024 * 1024
MAX_CONTEXT_BYTES = 512 * 1024 * 1024
RECIPES = ("deploy/web/Dockerfile", "deploy/web/nginx.conf", "tools/web_release.py",
           "tools/web_brotli.cjs", "tools/setup_stockfish_web.py")
RUNTIME_FILES = (
    "index.html", "Build/Web.loader.js", "Build/Web.data.gz",
    "Build/Web.framework.js.gz", "Build/Web.wasm.gz",
)
STOCKFISH_FILES = (
    "stockfish-18-lite-single.js", "stockfish-18-lite-single.wasm",
    "Copying.txt", "AUTHORS", "README.upstream.md", "stockfish.js-18.0.0-source.tar.gz",
)
NOTICES = ("Copying.txt", "AUTHORS", "SOURCE.txt", "manifest.json")
BUILD_FILES = frozenset(RUNTIME_FILES) | {
    "stockfish/" + name for name in (*STOCKFISH_FILES, "SOURCE.txt", "manifest.json")
} | {"StreamingAssets/ThirdParty/Stockfish/" + name for name in NOTICES}
PREFIX = "/jogar/runtime/"
IMMUTABLE = "public, max-age=31536000, immutable"
GENERATED_RECIPE = "deploy/web/runtime-assets.conf"
RUNTIME_MAP = {name: name[:-3] + ".br" if name.endswith(".gz") and name.startswith("Build/") else name
               for name in BUILD_FILES if name != "index.html"}
BROTLI_FILES = frozenset(name for name in RUNTIME_MAP.values()
                        if name.endswith((".br", ".js", ".wasm")))
RUNTIME_ASSETS = frozenset(RUNTIME_MAP.values())
PUBLIC_KEYS = {"source_revision", "build_id", "initial_download_bytes", "ai_download_bytes",
               "unity_version", "retained_build_ids", "manifest_sha256"}
SECRET_PATTERN = re.compile(
    rb"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|"
    rb"\bAKIA[0-9A-Z]{16}\b|\bgh[pousr]_[A-Za-z0-9]{36,}\b|"
    rb"\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,}\b"
)


class ReleaseError(ValueError):
    """A rejected release input, safe to show as a concise CLI error."""


def sha256(path):
    result = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(CHUNK), b""):
            result.update(block)
    return result.hexdigest()


def file_record(path):
    return {"bytes": Path(path).stat().st_size, "sha256": sha256(path)}


def canonical_json(value):
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8")


def read_json(path):
    if path.stat().st_size > CHUNK:
        raise ReleaseError(f"JSON metadata exceeds 1 MiB: {path.name}")
    try:
        def unique_object(pairs):
            result = {}
            for key, value in pairs:
                if key in result:
                    raise ReleaseError(f"Duplicate JSON key in {path.name}")
                result[key] = value
            return result
        return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_object)
    except (UnicodeError, json.JSONDecodeError) as error:
        raise ReleaseError(f"Invalid JSON metadata: {path.name}") from error


def validate_revision(revision):
    if not re.fullmatch(r"[0-9a-f]{40}", revision):
        raise ReleaseError("revision must be exactly 40 lowercase hexadecimal characters")


def checkout_guard(repo_root, revision):
    validate_revision(revision)
    try:
        head = subprocess.check_output(
            ["git", "-C", str(repo_root), "rev-parse", "HEAD"], text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
        dirty = subprocess.check_output(
            ["git", "-C", str(repo_root), "status", "--porcelain", "--untracked-files=normal"],
            text=True, stderr=subprocess.DEVNULL,
        )
    except subprocess.CalledProcessError as error:
        raise ReleaseError("A valid local Git checkout is required") from error
    if head != revision:
        raise ReleaseError("revision does not match the checked-out HEAD")
    if dirty:
        raise ReleaseError("Checkout is dirty; review and commit the intended files before packaging/verifying")


def repo_path(value, repo_root):
    path = Path(value).expanduser()
    if not path.is_absolute():
        path = repo_root / path
    path = Path(os.path.abspath(path))
    try:
        relative = path.relative_to(repo_root)
    except ValueError as error:
        raise ReleaseError("Build and recipe inputs must stay inside this checkout") from error
    cursor = repo_root
    for component in relative.parts:
        cursor /= component
        if cursor.is_symlink():
            raise ReleaseError(f"Symbolic links are not allowed: {relative.as_posix()}")
    return path


def allowed_directories(files):
    return {str(parent) for name in files for parent in PurePosixPath(name).parents if str(parent) != "."}


def inventory(root, expected):
    if root.is_symlink() or not root.is_dir():
        raise ReleaseError("Payload/context must be a real directory, not a symbolic link")
    directories = allowed_directories(expected)
    found = set()
    size = 0
    for current, subdirs, files in os.walk(root, followlinks=False):
        for name in (*subdirs, *files):
            path = Path(current) / name
            relative = path.relative_to(root).as_posix()
            mode = path.lstat().st_mode
            if stat.S_ISLNK(mode):
                raise ReleaseError(f"Symbolic links are not allowed: {relative}")
            if stat.S_ISDIR(mode):
                if relative not in directories:
                    raise ReleaseError(f"Unexpected directory: {relative}")
            elif stat.S_ISREG(mode):
                if relative not in expected:
                    raise ReleaseError(f"Unexpected file (including private/config files): {relative}")
                if not path.stat().st_size:
                    raise ReleaseError(f"Empty file: {relative}")
                size += path.stat().st_size
                if size > MAX_CONTEXT_BYTES:
                    raise ReleaseError("Payload/context exceeds the 512 MiB unpacked limit")
                found.add(relative)
            else:
                raise ReleaseError(f"Only regular files and directories are allowed: {relative}")
    missing = set(expected) - found
    if missing:
        raise ReleaseError("Missing files: " + ", ".join(sorted(missing)))


def stockfish_pins(repo_root):
    path = repo_path("tools/setup_stockfish_web.py", repo_root)
    spec = importlib.util.spec_from_file_location("_web_release_stockfish_pins", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.FILES, module.VERSION, module.SOURCE_COMMIT


def scan_text(path):
    previous = b""
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(CHUNK), b""):
            if SECRET_PATTERN.search(previous + block):
                raise ReleaseError(f"Credential/private-key marker detected in {path.name}")
            previous = block[-256:]


def validate_gzip(path, *, wasm=False, unity_data=False, scan_credentials=False):
    count = 0
    beginning = b""
    previous = b""
    try:
        with gzip.open(path, "rb") as stream:
            while True:
                block = stream.read(min(CHUNK, MAX_GZIP_BYTES + 1 - count))
                if not block:
                    break
                count += len(block)
                if count > MAX_GZIP_BYTES:
                    raise ReleaseError(f"Gzip exceeds the 1 GiB decoded limit: {path.name}")
                if len(beginning) < 32:
                    beginning += block[:32 - len(beginning)]
                if scan_credentials and SECRET_PATTERN.search(previous + block):
                    raise ReleaseError(f"Credential/private-key marker detected in {path.name}")
                previous = block[-256:]
    except (gzip.BadGzipFile, EOFError, OSError) as error:
        raise ReleaseError(f"Invalid/truncated gzip: {path.name}") from error
    if not count:
        raise ReleaseError(f"Decoded gzip is empty: {path.name}")
    if wasm and not beginning.startswith(b"\0asm\x01\0\0\0"):
        raise ReleaseError(f"Invalid WebAssembly magic/version: {path.name}")
    if unity_data and not beginning.startswith(b"UnityWebData"):
        raise ReleaseError(f"Invalid Unity data header: {path.name}")


def validate_payload(root, repo_root, *, release=False):
    inventory(root, BUILD_FILES)
    if (root / "index.html").stat().st_size > CHUNK:
        raise ReleaseError("index.html exceeds the 1 MiB text limit")
    index = (root / "index.html").read_bytes()
    if b"{{{" in index or b"}}}" in index:
        raise ReleaseError("index.html still contains Unity template placeholders")
    if b"unity-canvas" not in index or b"createUnityInstance" not in index:
        raise ReleaseError("index.html is not the expected Unity entry document")
    references = {
        "dataUrl": "Build/Web.data.gz", "frameworkUrl": "Build/Web.framework.js.gz",
        "codeUrl": "Build/Web.wasm.gz",
    }
    for key, value in references.items():
        pattern = rb"\b" + key.encode() + rb"\s*:\s*['\"]" + re.escape(value.encode()) + rb"['\"]"
        if not re.search(pattern, index):
            raise ReleaseError(f"index.html must reference the expected same-origin {key}")
    if not re.search(rb"script\.src\s*=\s*['\"]Build/Web\.loader\.js['\"]", index):
        raise ReleaseError("index.html must reference the expected same-origin loader")
    for name in ("index.html", "Build/Web.loader.js", "stockfish/SOURCE.txt", "stockfish/manifest.json"):
        scan_text(root / name)
    validate_gzip(root / "Build/Web.data.gz", unity_data=True)
    validate_gzip(root / "Build/Web.framework.js.gz", scan_credentials=True)
    validate_gzip(root / "Build/Web.wasm.gz", wasm=True)
    pins, version, commit = stockfish_pins(repo_root)
    if set(pins) != set(STOCKFISH_FILES):
        raise ReleaseError("Stockfish pin list differs from the release allowlist")
    metadata = read_json(root / "stockfish/manifest.json")
    required_metadata = {
        "engine": "Stockfish.js 18 Lite",
        "package": f"stockfish@{version}", "source_commit": commit, "threads": 1,
        "variant": "lite-single", "cross_origin_isolation_required": False, "license": "GPL-3.0",
        "source": pins["stockfish.js-18.0.0-source.tar.gz"][0],
        "registry": f"https://registry.npmjs.org/stockfish/{version}",
        "embedded_network": "nn-9067e33176e8.nnue",
    }
    if (not isinstance(metadata, dict) or set(metadata) != set(required_metadata) | {"files"}
            or canonical_json({key: metadata.get(key) for key in required_metadata}) != canonical_json(required_metadata)):
        raise ReleaseError("Stockfish metadata does not match the pinned single-threaded release")
    if not isinstance(metadata.get("files"), dict) or set(metadata["files"]) != set(pins):
        raise ReleaseError("Stockfish manifest file list does not match its pins")
    for name, (url, digest) in pins.items():
        path = root / "stockfish" / name
        record = file_record(path)
        if record["sha256"] != digest:
            raise ReleaseError(f"Stockfish pinned checksum mismatch: {name}")
        if metadata["files"][name] != {**record, "url": url}:
            raise ReleaseError(f"Stockfish manifest metadata mismatch: {name}")
    with (root / "stockfish/stockfish-18-lite-single.wasm").open("rb") as stream:
        if stream.read(8) != b"\0asm\x01\0\0\0":
            raise ReleaseError("Stockfish WASM has an invalid magic/version")
    validate_gzip(root / "stockfish/stockfish.js-18.0.0-source.tar.gz")
    if (root / "stockfish/SOURCE.txt").stat().st_size > CHUNK:
        raise ReleaseError("Stockfish source notice exceeds the 1 MiB text limit")
    source = (root / "stockfish/SOURCE.txt").read_text(encoding="utf-8")
    if commit not in source or pins["stockfish.js-18.0.0-source.tar.gz"][0] not in source:
        raise ReleaseError("Stockfish source notice does not identify the pinned source")
    for name in NOTICES:
        # Unity also copies the tracked native-engine notices into StreamingAssets.
        # Preserve them separately: the Web engine's corresponding source is above.
        tracked = repo_path("game/Assets/StreamingAssets/ThirdParty/Stockfish/" + name, repo_root)
        shipped = root / "StreamingAssets/ThirdParty/Stockfish" / name
        if sha256(tracked) != sha256(shipped):
            raise ReleaseError(f"Stockfish StreamingAssets notice differs from this checkout: {name}")
        scan_text(shipped)


def unity_version(repo_root):
    text = repo_path("game/ProjectSettings/ProjectVersion.txt", repo_root).read_text(encoding="utf-8")
    match = re.search(r"^m_EditorVersion: ([0-9]+\.[0-9]+\.[0-9]+[a-z][0-9]+)$", text, re.MULTILINE)
    if not match:
        raise ReleaseError("Cannot identify the pinned Unity editor version")
    return match.group(1)


def content_type(name):
    base = name[:-3] if name.endswith((".br", ".gz")) else name
    return ("application/wasm" if base.endswith(".wasm") else
            "application/javascript" if base.endswith(".js") else
            "application/gzip" if name.endswith(".tar.gz") or name.endswith(".data.gz") else
            "application/json" if base.endswith(".json") else
            "application/octet-stream" if base.endswith(".data") else "text/plain")


def brotli(operation, path, *, repo_root=REPO_ROOT, output=None, gzip_input=False):
    helper = repo_path("tools/web_brotli.cjs", Path(repo_root))
    command = ["node", str(helper), operation, str(path)]
    if operation == "encode":
        command += [str(output), "gzip" if gzip_input else "identity", str(MAX_GZIP_BYTES)]
    else:
        command += [str(MAX_GZIP_BYTES)]
    try:
        result = subprocess.run(command, check=True, text=True, capture_output=True, timeout=300)
        return json.loads(result.stdout)
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired, json.JSONDecodeError) as error:
        raise ReleaseError(f"Brotli {operation}/roundtrip failed: {Path(path).name}") from error


def contract_files(runtime):
    return {name: {key: item[key] for key in ("bytes", "sha256", "encoding", "content_type")}
            for name, item in sorted(runtime["files"].items())}


def runtime_id(runtime):
    # The namespace commits to transferred bytes AND their decoding/MIME contract.
    return hashlib.sha256(canonical_json({"files": contract_files(runtime), "cache_control": IMMUTABLE})).hexdigest()


def launch_manifest(build_id, runtime):
    entries = {"loader": "Build/Web.loader.js", "data": "Build/Web.data.br",
               "framework": "Build/Web.framework.js.br", "code": "Build/Web.wasm.br"}
    return {"schema": 1, "buildId": build_id, "assetBase": f"{PREFIX}{build_id}/", **entries,
            "initialBytes": sum(runtime["files"][name]["bytes"] for name in entries.values()),
            "aiBytes": sum(item["bytes"] for name, item in runtime["files"].items()
                           if name.startswith("stockfish/") and name.endswith((".js", ".wasm")))}


def public_metadata(manifest):
    active = manifest["runtimes"][manifest["build_id"]]
    launch = launch_manifest(manifest["build_id"], active)
    return {"source_revision": manifest["source_revision"], "build_id": manifest["build_id"],
            "initial_download_bytes": launch["initialBytes"], "ai_download_bytes": launch["aiBytes"],
            "unity_version": active["unity_version"],
            "retained_build_ids": [manifest["build_id"], *sorted(set(manifest["runtimes"]) - {manifest["build_id"]})],
            "manifest_sha256": hashlib.sha256(canonical_json(launch)).hexdigest()}


def context_files(manifest):
    return set(RECIPES) | {"manifest.json", GENERATED_RECIPE,
        "web" + PREFIX + "manifest.json", "web" + PREFIX + "release.json"} | {
        "web" + PREFIX + build_id + "/" + name
        for build_id in manifest["runtimes"] for name in RUNTIME_ASSETS}


def nginx_assets(manifest):
    lines = ["# Generated and verified by tools/web_release.py; exact immutable whitelist."]
    for build_id, runtime in sorted(manifest["runtimes"].items()):
        for name, item in sorted(runtime["files"].items()):
            lines += [f"location = {PREFIX}{build_id}/{name} {{",
                      "    proxy_pass http://127.0.0.1:8081;",
                      "    add_header Cache-Control $runtime_cache always;",
                      '    add_header X-Content-Type-Options "nosniff" always;']
            if item["encoding"] != "identity":
                # Without always: never label 4xx/5xx HTML error bodies as compressed.
                lines += [f"    add_header Content-Encoding {item['encoding']};"]
            lines += ["}"]
    return ("\n".join(lines) + "\n").encode()


def validate_record(record):
    return (isinstance(record, dict) and set(record) == {"bytes", "sha256"}
            and type(record["bytes"]) is int and 0 < record["bytes"] <= MAX_GZIP_BYTES
            and isinstance(record["sha256"], str) and re.fullmatch(r"[0-9a-f]{64}", record["sha256"]))


def validate_manifest_shape(manifest):
    expected = {"schema_version", "source_revision", "build_id", "runtimes", "recipes", "files"}
    if not isinstance(manifest, dict) or set(manifest) != expected or type(manifest["schema_version"]) is not int or manifest["schema_version"] != 2:
        raise ReleaseError("Manifest has an unsupported schema or fields")
    validate_revision(manifest["source_revision"])
    runtimes = manifest["runtimes"]
    if not isinstance(runtimes, dict) or not 1 <= len(runtimes) <= 2 or manifest["build_id"] not in runtimes:
        raise ReleaseError("Manifest must retain only the current and at most one previous runtime")
    for build_id, runtime in runtimes.items():
        if not re.fullmatch(r"[0-9a-f]{64}", build_id):
            raise ReleaseError("Manifest runtime ID must be a SHA256")
        if not isinstance(runtime, dict) or set(runtime) != {"source_revision", "unity_version", "source_files", "files", "compression"}:
            raise ReleaseError("Manifest runtime metadata differs from the allowlist")
        validate_revision(runtime["source_revision"])
        if not isinstance(runtime["unity_version"], str) or not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+[a-z][0-9]+", runtime["unity_version"]):
            raise ReleaseError("Manifest Unity version is invalid")
        if runtime["compression"] != {"encoding": "br", "quality": 9, "roundtrip_verified": True} or type(runtime["compression"].get("quality")) is not int:
            raise ReleaseError("Manifest compression must attest Brotli q9 and byte roundtrip")
        source = runtime["source_files"]
        if not isinstance(source, dict) or set(source) != BUILD_FILES or not all(validate_record(item) for item in source.values()):
            raise ReleaseError("Manifest input inventory differs from the allowlist")
        if not isinstance(runtime["files"], dict) or set(runtime["files"]) != RUNTIME_ASSETS:
            raise ReleaseError("Manifest runtime file list differs from the allowlist")
        for name, item in runtime["files"].items():
            if (not isinstance(item, dict) or set(item) != {"bytes", "sha256", "decoded", "encoding", "content_type"}
                    or not validate_record({key: item[key] for key in ("bytes", "sha256")})
                    or not validate_record(item["decoded"])
                    or item["encoding"] != ("br" if name in BROTLI_FILES else "identity")
                    or item["content_type"] != content_type(name)):
                raise ReleaseError("Manifest runtime decoding/header contract is invalid")
        if runtime_id(runtime) != build_id:
            raise ReleaseError("Manifest runtime ID differs from encoded bytes/header contract")
    active = runtimes[manifest["build_id"]]
    if active["source_revision"] != manifest["source_revision"]:
        raise ReleaseError("Manifest active source revision differs")
    if (not isinstance(manifest["recipes"], dict) or set(manifest["recipes"]) != set(RECIPES)
            or not all(validate_record(item) for item in manifest["recipes"].values())):
        raise ReleaseError("Manifest recipe inventory differs")
    expected_files = context_files(manifest) - {"manifest.json"}
    if (not isinstance(manifest["files"], dict) or set(manifest["files"]) != expected_files
            or not all(validate_record(item) for item in manifest["files"].values())):
        raise ReleaseError("Manifest context file inventory differs")


def verify_runtime(root, runtime, repo_root, *, current):
    pins, _, _ = stockfish_pins(repo_root)
    for source, name in sorted(RUNTIME_MAP.items()):
        path = root / name
        item = runtime["files"][name]
        if file_record(path) != {key: item[key] for key in ("bytes", "sha256")}:
            raise ReleaseError(f"Manifest runtime bytes differ: {name}")
        if item["encoding"] == "br":
            decoded = brotli("inspect", path, repo_root=repo_root)
            beginning = bytes.fromhex(decoded.pop("beginning"))
        else:
            decoded = file_record(path)
            with path.open("rb") as stream:
                beginning = stream.read(32)
            if not name.endswith(".tar.gz"):
                scan_text(path)
        if decoded != item["decoded"]:
            raise ReleaseError(f"Manifest decoded/roundtrip mismatch: {name}")
        if not source.endswith(".gz") or not source.startswith("Build/"):
            if decoded != runtime["source_files"][source]:
                raise ReleaseError(f"Manifest input and decoded bytes differ: {name}")
        if name.endswith((".wasm", ".wasm.br")) and not beginning.startswith(b"\0asm\x01\0\0\0"):
            raise ReleaseError(f"Invalid WebAssembly magic/version: {name}")
        if name.endswith(".data.br") and not beginning.startswith(b"UnityWebData"):
            raise ReleaseError("Invalid Unity data header")
        if name.startswith("stockfish/") and name.split("/")[-1] in pins:
            if decoded["sha256"] != pins[name.split("/")[-1]][1]:
                raise ReleaseError(f"Stockfish pinned checksum mismatch: {name}")
        if current and name.startswith("StreamingAssets/"):
            tracked = repo_path("game/Assets/" + name, repo_root)
            if sha256(tracked) != decoded["sha256"]:
                raise ReleaseError(f"Stockfish notice differs from this checkout: {name}")
    metadata = read_json(root / "stockfish/manifest.json")
    for name, (url, digest) in pins.items():
        expected = {**runtime["files"]["stockfish/" + name]["decoded"], "url": url}
        if metadata.get("files", {}).get(name) != expected or expected["sha256"] != digest:
            raise ReleaseError("Stockfish manifest does not match decoded pinned files")
    validate_gzip(root / "stockfish/stockfish.js-18.0.0-source.tar.gz")


def verify_context(context, *, repo_root=REPO_ROOT, revision=None):
    context = Path(context).expanduser().absolute()
    if context.is_symlink() or not context.is_dir() or (context / "manifest.json").is_symlink():
        raise ReleaseError("Payload/context and manifest must not be symbolic links")
    manifest = read_json(context / "manifest.json")
    validate_manifest_shape(manifest)
    inventory(context, context_files(manifest))
    for name, expected in manifest["files"].items():
        if file_record(context / name) != expected:
            raise ReleaseError(f"Manifest differs from file bytes: {name}")
    for name, record in manifest["recipes"].items():
        if file_record(context / name) != record:
            raise ReleaseError("Manifest recipe differs from bundled files")
    if revision is not None:
        if revision != manifest["source_revision"]:
            raise ReleaseError("Manifest source_revision does not match requested revision")
        local_recipes = {name: file_record(repo_path(name, repo_root)) for name in RECIPES}
        if manifest["recipes"] != local_recipes:
            raise ReleaseError("Container/packaging recipe differs from this checkout")
        if manifest["runtimes"][manifest["build_id"]]["unity_version"] != unity_version(repo_root):
            raise ReleaseError("Unity version differs from this checkout")
    for build_id, runtime in manifest["runtimes"].items():
        verify_runtime(context / ("web" + PREFIX + build_id), runtime, repo_root,
                       current=revision is not None and build_id == manifest["build_id"])
    # A retained context is authenticated by the caller's accepted manifest hash.
    # Its recipe files are hash-checked above but never executed or imported:
    # older releases may use an earlier generator. The new/current context must
    # always reproduce its whitelist with the checked-out generator.
    if revision is not None and (context / GENERATED_RECIPE).read_bytes() != nginx_assets(manifest):
        raise ReleaseError("Generated Nginx whitelist differs from verified runtimes")
    launch = launch_manifest(manifest["build_id"], manifest["runtimes"][manifest["build_id"]])
    if canonical_json(read_json(context / ("web" + PREFIX + "manifest.json"))) != canonical_json(launch):
        raise ReleaseError("Public launcher manifest differs from the verified runtime")
    if canonical_json(read_json(context / ("web" + PREFIX + "release.json"))) != canonical_json(public_metadata(manifest)):
        raise ReleaseError("Public release metadata differs from verified runtime")
    return manifest


def verify(context, revision, *, repo_root=REPO_ROOT):
    repo_root = Path(repo_root).resolve()
    checkout_guard(repo_root, revision)
    return verify_context(context, repo_root=repo_root, revision=revision)


def new_output(path):
    path = Path(path).expanduser().absolute()
    if os.path.lexists(path):
        raise ReleaseError("Output directory already exists; choose a new directory")
    path = path.parent.resolve() / path.name
    path.parent.mkdir(parents=True, exist_ok=True)
    try:
        path.mkdir()
    except FileExistsError as error:
        raise ReleaseError("Output directory already exists; choose a new directory") from error
    return path


def pack(build_dir, output_dir, revision, *, repo_root=REPO_ROOT,
         previous_context=None, previous_sha256=None):
    repo_root = Path(repo_root).resolve()
    checkout_guard(repo_root, revision)
    build = repo_path(build_dir, repo_root)
    target = Path(output_dir).expanduser().absolute().resolve()
    if target == build or build in target.parents:
        raise ReleaseError("Output directory must be outside the input build directory")
    validate_payload(build, repo_root)
    previous = None
    if bool(previous_context) != bool(previous_sha256):
        raise ReleaseError("--previous-context and --previous-sha256 must be provided together")
    if previous_context:
        previous_context = Path(previous_context).expanduser().absolute()
        if not re.fullmatch(r"[0-9a-f]{64}", previous_sha256) or sha256(previous_context / "manifest.json") != previous_sha256:
            raise ReleaseError("Previous context manifest SHA256 mismatch")
        previous = verify_context(previous_context, repo_root=repo_root)
    recipes = {name: file_record(repo_path(name, repo_root)) for name in RECIPES}
    output = new_output(output_dir)
    context = output / "context"
    context.mkdir()
    runtime = {"source_revision": revision, "unity_version": unity_version(repo_root),
               "compression": {"encoding": "br", "quality": 9, "roundtrip_verified": True},
               "source_files": {name: file_record(build / name) for name in sorted(BUILD_FILES)}, "files": {}}
    staging = output / ".runtime-staging"
    staging.mkdir()
    for source, name in sorted(RUNTIME_MAP.items()):
        destination = staging / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        encoding = "br" if name in BROTLI_FILES else "identity"
        if encoding == "br":
            decoded = brotli("encode", build / source, repo_root=repo_root, output=destination,
                             gzip_input=source.endswith(".gz"))
        else:
            shutil.copyfile(build / source, destination)
            decoded = file_record(destination)
        runtime["files"][name] = {**file_record(destination), "decoded": decoded,
                                  "encoding": encoding, "content_type": content_type(name)}
    # Refuse input or recipe mutations during compression; clean HEAD alone is insufficient.
    validate_payload(build, repo_root)
    if runtime["source_files"] != {name: file_record(build / name) for name in sorted(BUILD_FILES)}:
        raise ReleaseError("Input build changed during packaging; partial output preserved")
    checkout_guard(repo_root, revision)
    if recipes != {name: file_record(repo_path(name, repo_root)) for name in RECIPES}:
        raise ReleaseError("Recipes changed during packaging; partial output preserved")
    build_id = runtime_id(runtime)
    root = context / ("web" + PREFIX)
    root.mkdir(parents=True)
    staging.rename(root / build_id)
    manifest = {"schema_version": 2, "source_revision": revision, "build_id": build_id,
                "runtimes": {build_id: runtime}, "recipes": recipes, "files": {}}
    if previous:
        # Check every prior namespace, even the one that would be pruned.
        if build_id in previous["runtimes"] and contract_files(previous["runtimes"][build_id]) != contract_files(runtime):
            raise ReleaseError("Refusing to reuse a runtime ID for different bytes/headers")
        previous_id = previous["build_id"]
        if previous_id != build_id:
            manifest["runtimes"][previous_id] = previous["runtimes"][previous_id]
            shutil.copytree(previous_context / ("web" + PREFIX + previous_id), root / previous_id)
    for name in RECIPES:
        destination = context / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(repo_root / name, destination)
    (context / GENERATED_RECIPE).write_bytes(nginx_assets(manifest))
    (root / "manifest.json").write_bytes(canonical_json(launch_manifest(build_id, runtime)))
    (root / "release.json").write_bytes(canonical_json(public_metadata(manifest)))
    manifest["files"] = {name: file_record(context / name) for name in sorted(context_files(manifest) - {"manifest.json"})}
    (context / "manifest.json").write_bytes(canonical_json(manifest))
    verify(context, revision, repo_root=repo_root)
    archive = output / f"chess-web-{revision[:12]}.tar.gz"
    with archive.open("xb") as raw:
        with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0) as compressed:
            with tarfile.open(fileobj=compressed, mode="w|", format=tarfile.USTAR_FORMAT) as bundle:
                for name in sorted(context_files(manifest)):
                    path = context / name
                    info = tarfile.TarInfo(name)
                    info.size = path.stat().st_size
                    info.mode = 0o644
                    info.mtime = 0
                    with path.open("rb") as stream:
                        bundle.addfile(info, stream)
    if archive.stat().st_size > MAX_ARCHIVE_BYTES:
        raise ReleaseError("Archive exceeds the 256 MiB compressed limit; partial output was preserved")
    return archive, sha256(archive), manifest


def archive_members(bundle):
    # Read a bounded manifest without extracting anything; its dynamic whitelist is
    # then checked against every archive entry before creating the output directory.
    members, seen, total, manifest = [], set(), 0, None
    max_members = 2 * len(RUNTIME_ASSETS) + len(RECIPES) + 32
    for member in bundle:
        name = member.name.rstrip("/") if member.isdir() else member.name
        if (not name or "\\" in name or name.startswith("/") or name != PurePosixPath(name).as_posix()
                or ".." in PurePosixPath(name).parts):
            raise ReleaseError("Archive contains an unsafe/non-canonical path")
        if name in seen:
            raise ReleaseError(f"Archive contains a duplicate path: {name}")
        seen.add(name)
        if member.pax_headers:
            raise ReleaseError("Archive extended headers are not allowed")
        if member.isdir():
            if member.size != 0:
                raise ReleaseError("Archive directory headers must have zero size")
        elif member.type in (tarfile.REGTYPE, tarfile.AREGTYPE):
            if member.size <= 0:
                raise ReleaseError(f"Archive contains an empty file: {name}")
            total += member.size
            if total > MAX_CONTEXT_BYTES:
                raise ReleaseError("Archive exceeds the 512 MiB unpacked limit")
            if name == "manifest.json":
                if member.size > CHUNK:
                    raise ReleaseError("Archive manifest exceeds 1 MiB")
                try:
                    with tempfile.TemporaryDirectory() as temp:
                        path = Path(temp) / "manifest.json"
                        path.write_bytes(bundle.extractfile(member).read(CHUNK + 1))
                        manifest = read_json(path)
                        validate_manifest_shape(manifest)
                except (UnicodeError, json.JSONDecodeError) as error:
                    raise ReleaseError("Invalid archive manifest") from error
        else:
            raise ReleaseError(f"Archive links/special files are not allowed: {name}")
        members.append(member)
        if len(members) > max_members:
            raise ReleaseError("Archive has too many members")
    if manifest is None:
        raise ReleaseError("Archive does not contain the exact release file set (manifest missing)")
    expected = context_files(manifest)
    directories = allowed_directories(expected)
    for member in members:
        name = member.name.rstrip("/") if member.isdir() else member.name
        if name not in (directories if member.isdir() else expected):
            raise ReleaseError(f"Archive contains an unexpected file/directory: {name}")
    if {member.name for member in members if member.isfile()} != expected:
        raise ReleaseError("Archive does not contain the exact release file set")
    return members


def unpack(archive, checksum, output_dir, revision, *, repo_root=REPO_ROOT):
    validate_revision(revision)
    if not re.fullmatch(r"[0-9a-f]{64}", checksum):
        raise ReleaseError("sha256 must be exactly 64 lowercase hexadecimal characters")
    archive = Path(archive).expanduser().absolute()
    if archive.is_symlink() or not archive.is_file():
        raise ReleaseError("Archive must be a regular file, not a symbolic link")
    if archive.stat().st_size > MAX_ARCHIVE_BYTES:
        raise ReleaseError("Archive exceeds the 256 MiB compressed limit")
    if sha256(archive) != checksum:
        raise ReleaseError("Archive SHA256 mismatch; nothing was extracted")
    checkout_guard(Path(repo_root).resolve(), revision)
    try:
        with tarfile.open(archive, "r:gz") as bundle:
            members = archive_members(bundle)
            output = new_output(output_dir)
            for member in members:
                destination = output / member.name
                if member.isdir():
                    destination.mkdir(parents=True, exist_ok=True)
                    continue
                destination.parent.mkdir(parents=True, exist_ok=True)
                with bundle.extractfile(member) as source, destination.open("xb") as target:
                    shutil.copyfileobj(source, target, length=CHUNK)
                destination.chmod(0o644)
    except (tarfile.TarError, EOFError, gzip.BadGzipFile) as error:
        raise ReleaseError("Invalid/truncated release archive") from error
    verify(output, revision, repo_root=repo_root)
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    pack_parser = commands.add_parser("pack", help="Package local Unity Web assets from clean HEAD with Brotli q9")
    pack_parser.add_argument("--build-dir", type=Path, required=True)
    pack_parser.add_argument("--output-dir", type=Path, required=True)
    pack_parser.add_argument("--revision", required=True)
    pack_parser.add_argument("--previous-context", type=Path, help="Previously verified schema-2 context; retain its active runtime")
    pack_parser.add_argument("--previous-sha256", help="SHA256 of that context's manifest.json, recorded at its acceptance")
    verify_parser = commands.add_parser("verify", help="Verify an existing context against the clean checkout")
    verify_parser.add_argument("--context", type=Path, required=True)
    verify_parser.add_argument("--revision", required=True)
    unpack_parser = commands.add_parser("unpack", help="Authenticate, safely unpack and verify a release context")
    unpack_parser.add_argument("--archive", type=Path, required=True)
    unpack_parser.add_argument("--sha256", required=True)
    unpack_parser.add_argument("--output-dir", type=Path, required=True)
    unpack_parser.add_argument("--revision", required=True)
    args = parser.parse_args()
    try:
        if args.command == "pack":
            archive, checksum, manifest = pack(args.build_dir, args.output_dir, args.revision,
                previous_context=args.previous_context, previous_sha256=args.previous_sha256)
            print(f"context={archive.parent / 'context'}")
            print(f"archive={archive}")
            print(f"sha256={checksum}")
            print(f"build_id={manifest['build_id']}")
        elif args.command == "verify":
            manifest = verify(args.context, args.revision)
            print(f"verified={manifest['build_id']}")
        else:
            print(f"context={unpack(args.archive, args.sha256, args.output_dir, args.revision)}")
    except (ReleaseError, OSError, UnicodeError, KeyError, TypeError) as error:
        parser.exit(1, f"web-release: {error}\n")


if __name__ == "__main__":
    main()
