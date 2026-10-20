#!/usr/bin/env python3
"""Build/package pinned Stockfish 18 for Android ARM64; never run Unity or a device."""

import argparse
import gzip
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import struct
import subprocess
import tarfile
import tempfile
import urllib.request
import uuid


RELEASE = 'sf_18'
ARCHIVE_NAME = 'stockfish-android-armv8.tar'
ARCHIVE_SHA256 = 'e2eca54b0e3189ec7de338133c2b34fa8f5cdec3d2473519b414a5cb6815e768'
ARCHIVE_SIZE = 115578880
ARCHIVE_URL = f'https://github.com/official-stockfish/Stockfish/releases/download/{RELEASE}/{ARCHIVE_NAME}'
SOURCE_URL = f'https://github.com/official-stockfish/Stockfish/tree/{RELEASE}'
UPSTREAM_BINARY_SHA256 = 'cc3b3b74466b6d85cdc0f8cfdad4111c6a44bd0698f4eab96fea7b359a54d3d4'
NDK_REVISION = '27.2.12479018'
NETWORKS = {
    'nn-c288c895ea92.nnue': 'c288c895ea924429ea9092e3f36b2b3c1f00f2a3a4c759ff7e57e79e3b43e4a7',
    'nn-37f18f62d772.nnue': '37f18f62d772f3107e1d6aaca3898c130c3c86f2ab63e6555fbbca20635a899d',
}
LINK_FLAGS = '-Wl,-z,max-page-size=16384 -Wl,-z,common-page-size=16384 -Wl,--build-id=sha1'
BUILD_ARGS = ['-j2', 'build', 'ARCH=armv8', 'COMP=ndk', 'OS=Android', 'KERNEL=Linux',
              'GIT_SHA=', 'GIT_DATE=', f'EXTRALDFLAGS={LINK_FLAGS}']
PLUGIN_PATH = Path('game/Assets/Plugins/Android/Stockfish/arm64-v8a/libstockfish.so')
NOTICE_PATH = Path('game/Assets/StreamingAssets/ThirdParty/Stockfish')
GUID_NAMESPACE = uuid.UUID('f5ff9e77-7598-4a61-9f08-15ee6409da93')


class PackageError(RuntimeError):
    pass


def digest(path):
    value = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def json_bytes(value):
    return (json.dumps(value, indent=2, sort_keys=True) + '\n').encode('utf-8')


def fetch(url, path, expected, offline=False, size=None):
    """Never accept or silently replace a corrupt pre-existing download."""
    path = Path(path)
    if path.is_symlink():
        raise PackageError(f'Refusing symlink: {path}')
    if not path.exists():
        if offline:
            raise PackageError(f'Offline cache missing: {path}')
        path.parent.mkdir(parents=True, exist_ok=True)
        print(f'Downloading {url}', flush=True)
        with tempfile.NamedTemporaryFile(dir=path.parent, suffix='.download', delete=False) as target:
            temporary = Path(target.name)
            try:
                with urllib.request.urlopen(url, timeout=120) as source:
                    shutil.copyfileobj(source, target)
            except Exception:
                temporary.unlink(missing_ok=True)
                raise
        if digest(temporary) != expected or (size is not None and temporary.stat().st_size != size):
            temporary.unlink()
            raise PackageError(f'Download checksum/size mismatch: {url}')
        temporary.replace(path)
    if digest(path) != expected or (size is not None and path.stat().st_size != size):
        raise PackageError(f'Cache checksum/size mismatch; preserve/remove it explicitly: {path}')
    return path


def safe_extract(archive, destination):
    """Extract only regular files/directories after validating the complete index."""
    destination = Path(destination).resolve()
    if any(destination.iterdir()):
        raise PackageError('Extraction destination must be empty.')
    with tarfile.open(archive) as package:
        members = package.getmembers()
        seen = set()
        total = 0
        for member in members:
            path = PurePosixPath(member.name)
            if (path.is_absolute() or '..' in path.parts or '\\' in member.name
                    or not path.parts or path.parts[0] != 'stockfish'
                    or not (member.isdir() or member.isfile()) or str(path) in seen):
                raise PackageError(f'Unsafe/duplicate archive entry: {member.name}')
            seen.add(str(path))
            total += member.size
            if member.size > 256 * 1024 * 1024 or total > 512 * 1024 * 1024:
                raise PackageError('Archive exceeds expected extraction limits.')
        for member in members:
            target = destination.joinpath(*PurePosixPath(member.name).parts)
            if member.isdir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with package.extractfile(member) as source, target.open('xb') as output:
                    shutil.copyfileobj(source, output)
                target.chmod(0o644)


def elf_information(binary):
    """Read ELF headers directly as well as preserving llvm-readelf evidence."""
    with Path(binary).open('rb') as stream:
        header = stream.read(64)
        if len(header) != 64 or header[:7] != b'\x7fELF\x02\x01\x01':
            raise PackageError('Expected little-endian ELF64.')
        kind, machine = struct.unpack_from('<HH', header, 16)
        entry, phoff = struct.unpack_from('<QQ', header, 24)
        phsize, phcount = struct.unpack_from('<HH', header, 54)
        if phsize != 56 or phcount > 128:
            raise PackageError('Unexpected ELF program-header table.')
        loads, interpreter = [], None
        for index in range(phcount):
            stream.seek(phoff + index * phsize)
            raw = stream.read(phsize)
            if len(raw) != phsize:
                raise PackageError('Truncated ELF program header.')
            ptype, flags, offset, address, _, filesz, memsz, alignment = struct.unpack('<IIQQQQQQ', raw)
            if ptype == 1:
                loads.append({'offset': offset, 'virtual_address': address, 'file_size': filesz,
                              'memory_size': memsz, 'flags': flags, 'alignment': alignment})
            elif ptype == 3:
                if filesz > 1024:
                    raise PackageError('Unexpected ELF interpreter length.')
                stream.seek(offset)
                interpreter = stream.read(filesz).rstrip(b'\0').decode('ascii')
    return {'elf_class': 64, 'machine': machine, 'type': kind, 'entry_point': entry,
            'interpreter': interpreter, 'load_segments': loads}


def inspect_elf(binary, readelf):
    info = elf_information(binary)
    evidence = subprocess.run([str(readelf), '-h', '-l', '-d', '--wide', str(binary)],
                              check=True, capture_output=True, text=True).stdout
    info['needed'] = sorted(re.findall(r'\(NEEDED\).*?\[([^\]]+)\]', evidence))
    if info['machine'] != 183 or info['type'] != 3 or not info['entry_point']:
        raise PackageError('Expected AArch64 ET_DYN PIE executable, not ET_EXEC or a dlopen library.')
    if info['interpreter'] != '/system/bin/linker64' or 'FLAGS_1' not in evidence or 'PIE' not in evidence:
        raise PackageError('Expected Android linker64 and DF_1_PIE.')
    if not info['load_segments'] or any(segment['alignment'] < 16384 or
            segment['offset'] % 16384 != segment['virtual_address'] % 16384
            for segment in info['load_segments']):
        raise PackageError('Every LOAD segment must support 16 KiB Android pages.')
    if not info['needed'] or set(info['needed']) - {'libc.so', 'libm.so', 'libdl.so'}:
        raise PackageError(f'Unexpected dynamic dependencies: {info["needed"]}')
    return info, evidence


def verify_networks(binary, networks):
    """Verify the exact standard NNUE bytes are embedded, not just their filenames."""
    data = Path(binary).read_bytes()
    result = {}
    for name, path in sorted(networks.items()):
        network = Path(path).read_bytes()
        offset = data.find(network)
        if offset < 0 or hashlib.sha256(network).hexdigest() != NETWORKS[name]:
            raise PackageError(f'Exact default NNUE is not embedded: {name}')
        result[name] = {'sha256': NETWORKS[name], 'size': len(network), 'embedded_offset': offset}
    return result


def find_ndk(project_root, explicit=None):
    version_file = project_root / 'game/ProjectSettings/ProjectVersion.txt'
    version = re.search(r'm_EditorVersion: (\S+)', version_file.read_text()).group(1)
    ndk = Path(explicit).expanduser().resolve() if explicit else Path(
        f'/Applications/Unity/Hub/Editor/{version}/PlaybackEngines/AndroidPlayer/NDK')
    properties = ndk / 'source.properties'
    if not properties.is_file() or f'Pkg.Revision = {NDK_REVISION}' not in properties.read_text():
        raise PackageError(f'Expected Android NDK r27c ({NDK_REVISION}): {ndk}')
    bins = list((ndk / 'toolchains/llvm/prebuilt').glob('*/bin'))
    if len(bins) != 1:
        raise PackageError(f'Ambiguous/missing NDK host toolchain: {ndk}')
    return ndk, bins[0]


def guarded_write(path, data):
    path = Path(path)
    if path.is_symlink() or (path.exists() and path.read_bytes() != data):
        raise PackageError(f'Existing asset differs; preserve/reconcile it before replacing: {path}')
    if not path.exists():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)


def meta_bytes(relative, folder=False, plugin=False):
    guid = uuid.uuid5(GUID_NAMESPACE, Path(relative).as_posix()).hex
    text = f'fileFormatVersion: 2\nguid: {guid}\n'
    if folder:
        text += 'folderAsset: yes\n'
    if plugin:
        text += '''PluginImporter:
  externalObjects: {}
  serializedVersion: 3
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Android: Android
    second:
      enabled: 1
      settings:
        CPU: ARM64
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: AnyOS
'''
    else:
        text += 'DefaultImporter:\n  externalObjects: {}\n'
    text += '  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    return text.encode('utf-8')


def deterministic_tar(destination, files):
    """Source bundle has no host paths, current timestamps, uid/gid or random order."""
    temporary = destination.with_suffix('.tmp')
    with temporary.open('wb') as output, gzip.GzipFile(filename='', mode='wb', fileobj=output, mtime=0) as zipped:
        with tarfile.open(fileobj=zipped, mode='w') as archive:
            for name, path in sorted(files.items()):
                entry = tarfile.TarInfo(name)
                entry.size = path.stat().st_size
                entry.mode = 0o755 if name.endswith('.sh') else 0o644
                with path.open('rb') as stream:
                    archive.addfile(entry, stream)
    temporary.replace(destination)


def build_recipe(tool_bin):
    compiler = tool_bin / 'clang++'
    return {'ndk_revision': NDK_REVISION, 'compiler_sha256': digest(compiler.resolve()),
            'make_args': BUILD_ARGS, 'source_date_epoch': '1700000000',
            'upstream_archive_sha256': ARCHIVE_SHA256, 'networks': NETWORKS}


def prepare(project_root, cache, ndk_override=None, offline=False, rebuild=False, verify_only=False):
    ndk, tool_bin = find_ndk(project_root, ndk_override)
    readelf = tool_bin / 'llvm-readelf'
    archive = fetch(ARCHIVE_URL, cache / ARCHIVE_NAME, ARCHIVE_SHA256,
                    offline or verify_only, ARCHIVE_SIZE)
    networks = {name: fetch('https://github.com/official-stockfish/networks/raw/master/' + name,
                           cache / 'networks' / name, checksum, offline or verify_only)
                for name, checksum in NETWORKS.items()}
    recipe = build_recipe(tool_bin)
    recipe_id = hashlib.sha256(json_bytes(recipe)).hexdigest()
    build_dir = cache / 'builds' / recipe_id
    binary = build_dir / 'libstockfish.so'
    record_path = build_dir / 'build-record.json'
    if verify_only:
        installed = project_root / PLUGIN_PATH
        manifest = json.loads((project_root / NOTICE_PATH / 'manifest.json').read_text())
        if digest(installed) != manifest['binary_sha256'] or manifest['build']['recipe_sha256'] != recipe_id:
            raise PackageError('Installed binary/recipe does not match manifest.')
        info, _ = inspect_elf(installed, readelf)
        verify_networks(installed, networks)
        source_bundle = cache / manifest['source_bundle']['file']
        if digest(source_bundle) != manifest['source_bundle']['sha256']:
            raise PackageError('Offline corresponding-source bundle checksum mismatch.')
        for name in ['Copying.txt', 'AUTHORS', 'SOURCE.txt']:
            path = project_root / NOTICE_PATH / name
            if digest(path) != manifest['notices'][name]:
                raise PackageError(f'Notice checksum mismatch: {name}')
        if (installed.with_suffix('.so.meta')).read_bytes() != meta_bytes(PLUGIN_PATH, plugin=True):
            raise PackageError('PluginImporter must select only Android ARM64 and disable preloading.')
        print(json.dumps({'verified': True, 'binary_sha256': digest(installed), 'elf': info}, indent=2))
        return
    cache.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='package-', dir=cache) as scratch:
        safe_extract(archive, Path(scratch))
        upstream = Path(scratch) / 'stockfish'
        original = upstream / 'stockfish-android-armv8'
        if digest(original) != UPSTREAM_BINARY_SHA256:
            raise PackageError('Upstream executable identity changed.')
        defaults = (upstream / 'src/evaluate.h').read_text()
        if any(f'"{name}"' not in defaults for name in NETWORKS):
            raise PackageError('Pinned networks differ from release source defaults.')
        for name, network in networks.items():
            shutil.copyfile(network, upstream / 'src' / name)
        source_files = {path.relative_to(upstream).as_posix(): digest(path)
                        for path in upstream.rglob('*') if path.is_file() and path != original}
        source_id = hashlib.sha256(json_bytes(source_files)).hexdigest()
        if rebuild or not binary.exists() or not record_path.exists():
            build_dir.mkdir(parents=True, exist_ok=True)
            env = {'PATH': str(tool_bin) + os.pathsep + '/usr/bin:/bin',
                   'SOURCE_DATE_EPOCH': recipe['source_date_epoch'], 'LC_ALL': 'C',
                   'HOME': os.environ.get('HOME', '/tmp'), 'TMPDIR': str(Path(scratch).resolve())}
            with (build_dir / 'compile.log').open('w') as log:
                subprocess.run(['/usr/bin/make'] + BUILD_ARGS, cwd=upstream / 'src', env=env,
                               stdout=log, stderr=subprocess.STDOUT, check=True)
            compiled = upstream / 'src/stockfish'
            subprocess.run([str(tool_bin / 'llvm-strip'), '--strip-all', str(compiled)], check=True)
            inspect_elf(compiled, readelf)
            verify_networks(compiled, networks)
            shutil.copyfile(compiled, binary)
            binary.chmod(0o755)
            record_path.write_bytes(json_bytes({'recipe_sha256': recipe_id,
                                               'source_inventory_sha256': source_id,
                                               'binary_sha256': digest(binary)}))
        record = json.loads(record_path.read_text())
        if (record['recipe_sha256'] != recipe_id or record['source_inventory_sha256'] != source_id
                or digest(binary) != record['binary_sha256']):
            raise PackageError('Cached build record mismatch; preserve it and explicitly rebuild.')
        info, evidence = inspect_elf(binary, readelf)
        embedded = verify_networks(binary, networks)
        (build_dir / 'llvm-readelf.txt').write_text(evidence)
        rebuild_text = ('Stockfish 18 corresponding source; unmodified upstream algorithms.\n'
                        f'Upstream archive: {ARCHIVE_URL}\nSHA-256: {ARCHIVE_SHA256}\n'
                        f'NDK r27c ({NDK_REVISION}); add its toolchains/llvm/prebuilt/<host>/bin to PATH.\n'
                        'From stockfish/src, with SOURCE_DATE_EPOCH=1700000000 and LC_ALL=C:\n'
                        'make ' + ' '.join(f"'{arg}'" if ' ' in arg else arg for arg in BUILD_ARGS) + '\n'
                        'llvm-strip --strip-all stockfish\n'
                        'The Makefile supplies -fPIE -pie and -static-libstdc++.\n'
                        'No profile build, source modification, runtime download or external NNUE is required.\n')
        (build_dir / 'REBUILD.txt').write_text(rebuild_text)
        inventory = {'stockfish/' + name: upstream / name for name in source_files}
        inventory['REBUILD.txt'] = build_dir / 'REBUILD.txt'
        (build_dir / 'source-inventory.json').write_bytes(json_bytes(source_files))
        inventory['source-inventory.json'] = build_dir / 'source-inventory.json'
        bundle = cache / 'stockfish-18-android-corresponding-source.tar.gz'
        bundle_candidate = build_dir / bundle.name
        # Exclude build outputs; source_files was captured before make.
        deterministic_tar(bundle_candidate, inventory)
        bundle_sha256 = digest(bundle_candidate)
        source_notice = (f'Stockfish 18 ({RELEASE}); GPL-3.0-or-later.\n'
                         f'Unmodified source: {SOURCE_URL}\nOfficial release: {ARCHIVE_URL}\n'
                         f'Official archive SHA-256: {ARCHIVE_SHA256}\n'
                         'The official Android binary is ET_EXEC; this app packages a local PIE build\n'
                         'from the same source and the exact default embedded NNUE networks.\n'
                         'The manifest records source, network, compiler, build and executable hashes.\n'
                         'The distributable local artifact must travel with its offline corresponding-source bundle:\n'
                         f'{bundle.name}\nSHA-256: {bundle_sha256}\n'
                         'The bundle includes all release source, scripts, GPL, authors, both NNUE files,\n'
                         'a file hash inventory and REBUILD.txt. Keep it beside every distributed APK.\n')
        notices = {'Copying.txt': (upstream / 'Copying.txt').read_bytes(),
                   'AUTHORS': (upstream / 'AUTHORS').read_bytes(),
                   'SOURCE.txt': source_notice.encode('utf-8')}
        manifest = {'engine': 'Stockfish 18', 'release': RELEASE, 'platform': 'Android',
                    'architecture': 'arm64-v8a', 'license': 'GPL-3.0-or-later', 'source': SOURCE_URL,
                    'archive': ARCHIVE_NAME, 'archive_sha256': ARCHIVE_SHA256, 'download': ARCHIVE_URL,
                    'upstream_binary_sha256': UPSTREAM_BINARY_SHA256, 'binary_sha256': digest(binary),
                    'binary_size': binary.stat().st_size, 'binary_asset': PLUGIN_PATH.as_posix(),
                    'runtime_filename': 'libstockfish.so', 'execution': 'ProcessBuilder (not dlopen)',
                    'elf': info, 'networks': embedded, 'source_inventory_sha256': source_id,
                    'source_bundle': {'file': bundle.name, 'sha256': bundle_sha256,
                                      'size': bundle_candidate.stat().st_size},
                    'build': dict(recipe, recipe_sha256=recipe_id),
                    'notices': {name: hashlib.sha256(data).hexdigest() for name, data in notices.items()}}
        assets = {project_root / PLUGIN_PATH: binary.read_bytes(),
                  project_root / NOTICE_PATH / 'manifest.json': json_bytes(manifest)}
        assets.update({project_root / NOTICE_PATH / name: data for name, data in notices.items()})
        for path in list(assets):
            relative = path.relative_to(project_root)
            assets[Path(str(path) + '.meta')] = meta_bytes(relative, plugin=relative == PLUGIN_PATH)
        for relative in ['game/Assets/Plugins/Android', 'game/Assets/Plugins/Android/Stockfish',
                         'game/Assets/Plugins/Android/Stockfish/arm64-v8a', 'game/Assets/StreamingAssets',
                         'game/Assets/StreamingAssets/ThirdParty', str(NOTICE_PATH)]:
            assets[project_root / (relative + '.meta')] = meta_bytes(relative, folder=True)
        # Preflight all existing assets before writing any of them.
        for path, data in assets.items():
            if path.is_symlink() or (path.exists() and path.read_bytes() != data):
                raise PackageError(f'Existing asset differs; reconcile before replacing: {path}')
        if bundle.is_symlink() or (bundle.exists() and digest(bundle) != bundle_sha256):
            raise PackageError(f'Existing source bundle differs; preserve/reconcile it: {bundle}')
        if not bundle.exists():
            shutil.copyfile(bundle_candidate, bundle)
        for path, data in assets.items():
            guarded_write(path, data)
        (project_root / PLUGIN_PATH).chmod(0o755)
        (cache / 'manifest.json').write_bytes(json_bytes(manifest))
        print(json.dumps(manifest, indent=2, sort_keys=True))
        print(f'Offline source bundle: {bundle}\nELF evidence: {build_dir / "llvm-readelf.txt"}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--cache-root', type=Path, help='Defaults to <project>/.local/stockfish/android')
    parser.add_argument('--ndk-root', type=Path, help='NDK r27c; default is the project Unity Editor NDK')
    parser.add_argument('--offline', action='store_true', help='Require the verified local release/networks')
    parser.add_argument('--rebuild', action='store_true', help='Recompile cached verified source instead of reusing the build')
    parser.add_argument('--verify-only', action='store_true', help='Verify installed binary, metadata, notices and offline bundle')
    arguments = parser.parse_args()
    project = arguments.project_root.expanduser().resolve()
    cache = (arguments.cache_root or project / '.local/stockfish/android').expanduser().resolve()
    try:
        prepare(project, cache, arguments.ndk_root, arguments.offline, arguments.rebuild, arguments.verify_only)
    except (PackageError, OSError, subprocess.CalledProcessError, ValueError, KeyError) as error:
        parser.exit(1, f'Stockfish Android package failed: {error}\n')


if __name__ == '__main__':
    main()
