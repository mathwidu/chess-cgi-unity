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


REPO_ROOT = Path(__file__).resolve().parents[1]
CHUNK = 1024 * 1024
MAX_GZIP_BYTES = 1024 * 1024 * 1024
MAX_ARCHIVE_BYTES = 256 * 1024 * 1024
MAX_CONTEXT_BYTES = 512 * 1024 * 1024
RECIPES = ("deploy/web/Dockerfile", "deploy/web/nginx.conf")
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
WEB_FILES = BUILD_FILES | {"release.json"}
CONTEXT_FILES = {"web/" + name for name in WEB_FILES} | set(RECIPES) | {"manifest.json"}
PUBLIC_KEYS = {"source_revision", "build_id", "initial_download_bytes", "unity_version"}
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
    inventory(root, WEB_FILES if release else BUILD_FILES)
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


def metadata_for(root, repo_root, revision, recipes):
    records = {name: file_record(root / name) for name in sorted(BUILD_FILES)}
    fingerprint = hashlib.sha256(canonical_json({"files": records, "recipes": recipes})).hexdigest()[:12]
    return {
        "source_revision": revision,
        "build_id": f"{revision[:12]}-{fingerprint}",
        "initial_download_bytes": sum(records[name]["bytes"] for name in RUNTIME_FILES),
        "unity_version": unity_version(repo_root),
    }


def verify(context, revision, *, repo_root=REPO_ROOT):
    repo_root = Path(repo_root).resolve()
    checkout_guard(repo_root, revision)
    context = Path(context).expanduser().absolute()
    inventory(context, CONTEXT_FILES)
    validate_payload(context / "web", repo_root, release=True)
    local_recipes = {name: file_record(repo_path(name, repo_root)) for name in RECIPES}
    bundled_recipes = {name: file_record(context / name) for name in RECIPES}
    if bundled_recipes != local_recipes:
        raise ReleaseError("Container recipe differs from this checkout")
    public = metadata_for(context / "web", repo_root, revision, local_recipes)
    release = read_json(context / "web/release.json")
    if not isinstance(release, dict) or set(release) != PUBLIC_KEYS or canonical_json(release) != canonical_json(public):
        raise ReleaseError("Public release metadata differs from the verified payload/revision")
    expected = {
        "schema_version": 1, **public,
        "files": {name: file_record(context / "web" / name) for name in sorted(WEB_FILES)},
        "recipes": local_recipes,
    }
    if canonical_json(read_json(context / "manifest.json")) != canonical_json(expected):
        raise ReleaseError("Manifest differs from files, recipes, or requested revision")
    return expected


def new_output(path):
    path = Path(path).expanduser().absolute()
    if os.path.lexists(path):
        raise ReleaseError("Output directory already exists; choose a new directory")
    # Resolve existing ancestors before creation, without following an output symlink.
    path = path.parent.resolve() / path.name
    path.parent.mkdir(parents=True, exist_ok=True)
    try:
        path.mkdir()
    except FileExistsError as error:
        raise ReleaseError("Output directory already exists; choose a new directory") from error
    return path


def pack(build_dir, output_dir, revision, *, repo_root=REPO_ROOT):
    repo_root = Path(repo_root).resolve()
    checkout_guard(repo_root, revision)
    build = repo_path(build_dir, repo_root)
    target = Path(output_dir).expanduser().absolute().resolve()
    if target == build or build in target.parents:
        raise ReleaseError("Output directory must be outside the input build directory")
    validate_payload(build, repo_root)
    recipes = {name: file_record(repo_path(name, repo_root)) for name in RECIPES}
    public = metadata_for(build, repo_root, revision, recipes)
    output = new_output(output_dir)
    context = output / "context"
    (context / "web").mkdir(parents=True)
    for name in sorted(BUILD_FILES):
        destination = context / "web" / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(build / name, destination)
    for name in RECIPES:
        destination = context / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(repo_root / name, destination)
    (context / "web/release.json").write_bytes(canonical_json(public))
    manifest = {
        "schema_version": 1, **public,
        "files": {name: file_record(context / "web" / name) for name in sorted(WEB_FILES)},
        "recipes": recipes,
    }
    (context / "manifest.json").write_bytes(canonical_json(manifest))
    verify(context, revision, repo_root=repo_root)
    archive = output / f"chess-web-{revision[:12]}.tar.gz"
    with archive.open("xb") as raw:
        with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0) as compressed:
            with tarfile.open(fileobj=compressed, mode="w|", format=tarfile.USTAR_FORMAT) as bundle:
                for name in sorted(CONTEXT_FILES):
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
    directories = allowed_directories(CONTEXT_FILES)
    seen = set()
    total = 0
    members = []
    for member in bundle:
        name = member.name.rstrip("/") if member.isdir() else member.name
        if not name or "\\" in name or name != PurePosixPath(name).as_posix() or ".." in PurePosixPath(name).parts:
            raise ReleaseError("Archive contains an unsafe/non-canonical path")
        if name in seen:
            raise ReleaseError(f"Archive contains a duplicate path: {name}")
        seen.add(name)
        if member.pax_headers:
            raise ReleaseError("Archive extended headers are not allowed")
        if member.isdir():
            if name not in directories:
                raise ReleaseError(f"Archive contains an unexpected directory: {name}")
            if member.size != 0:
                raise ReleaseError("Archive directory headers must have zero size")
        elif member.type in (tarfile.REGTYPE, tarfile.AREGTYPE):
            if name not in CONTEXT_FILES:
                raise ReleaseError(f"Archive contains an unexpected file: {name}")
            if member.size <= 0:
                raise ReleaseError(f"Archive contains an empty file: {name}")
            total += member.size
            if total > MAX_CONTEXT_BYTES:
                raise ReleaseError("Archive exceeds the 512 MiB unpacked limit")
        else:
            raise ReleaseError(f"Archive links/special files are not allowed: {name}")
        members.append(member)
        if len(members) > len(CONTEXT_FILES) + len(directories):
            raise ReleaseError("Archive has too many members")
    present = {member.name for member in members if member.isfile()}
    if present != CONTEXT_FILES:
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
    # Authenticate the entire archive before inspecting or extracting any member.
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
    pack_parser = commands.add_parser("pack", help="Package a local Unity Web build from a clean HEAD")
    pack_parser.add_argument("--build-dir", type=Path, required=True)
    pack_parser.add_argument("--output-dir", type=Path, required=True)
    pack_parser.add_argument("--revision", required=True)
    verify_parser = commands.add_parser("verify", help="Verify an existing release context against this checkout")
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
            archive, checksum, manifest = pack(args.build_dir, args.output_dir, args.revision)
            print(f"context={archive.parent / 'context'}")
            print(f"archive={archive}")
            print(f"sha256={checksum}")
            print(f"build_id={manifest['build_id']}")
        elif args.command == "verify":
            manifest = verify(args.context, args.revision)
            print(f"verified={manifest['build_id']}")
        else:
            output = unpack(args.archive, args.sha256, args.output_dir, args.revision)
            print(f"context={output}")
    except (ReleaseError, OSError, UnicodeError, KeyError, TypeError) as error:
        parser.exit(1, f"web-release: {error}\n")


if __name__ == "__main__":
    main()
