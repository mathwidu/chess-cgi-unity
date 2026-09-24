#!/usr/bin/env python3
"""Prepare a pinned local engine outside Unity Assets. No system installation."""
import argparse
import hashlib
import json
from pathlib import Path
import platform
import shutil
import tarfile
import urllib.request
import zipfile

RELEASE = 'sf_18'
ARTIFACTS = {
    ('Darwin', 'arm64'): ('stockfish-macos-m1-apple-silicon.tar', '4d77c4aa3ad9bd1ea8111f2ac5a4620fe7ebf998d6893bf828d49ccd579c8cb0'),
    ('Darwin', 'x86_64'): ('stockfish-macos-x86-64.tar', 'e7d7a2bca13915419d41ac6cb8cedb123dd2ba1c39a22c574df7a2aa3f526592'),
    ('Windows', 'AMD64'): ('stockfish-windows-x86-64.zip', '40cc975817e7eee270b03f354810d20956df565420d320f6dd37d454dc81a139'),
    ('Linux', 'x86_64'): ('stockfish-ubuntu-x86-64.tar', '5c6f38b02a4da5f3ffe763f27da6c3e743eebefd92b50cb3661623b96696adff'),
}


def digest(path):
    with path.open('rb') as stream:
        checksum = hashlib.sha256()
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            checksum.update(block)
        return checksum.hexdigest()


def prepare(player_directory=None):
    key = (platform.system(), platform.machine())
    if key not in ARTIFACTS:
        raise SystemExit(f'No verified desktop artifact for {key}; Quest requires its own adapter.')
    archive_name, expected = ARTIFACTS[key]
    root = Path(__file__).resolve().parents[1] / '.local' / 'stockfish'
    root.mkdir(parents=True, exist_ok=True)
    archive = root / archive_name
    # Reuse a previously verified local download made during development.
    old_archive = root / 'sf18.tar'
    if not archive.exists() and old_archive.exists() and digest(old_archive) == expected:
        shutil.copy2(old_archive, archive)
    url = f'https://github.com/official-stockfish/Stockfish/releases/download/{RELEASE}/{archive_name}'
    if not archive.exists():
        print(f'Downloading {url}', flush=True)
        temporary = archive.with_suffix('.download')
        urllib.request.urlretrieve(url, temporary)
        if digest(temporary) != expected:
            temporary.unlink()
            raise SystemExit('Archive checksum mismatch.')
        temporary.replace(archive)
    if digest(archive) != expected:
        raise SystemExit(f'Archive checksum mismatch: {archive}. Remove it and retry.')

    distribution = root / 'verified-package'
    distribution.mkdir(exist_ok=True)
    if archive.suffix == '.zip':
        with zipfile.ZipFile(archive) as package:
            for member in package.infolist():
                target = (distribution / member.filename).resolve()
                if not target.is_relative_to(distribution.resolve()):
                    raise SystemExit('Unsafe archive path.')
            package.extractall(distribution)
    else:
        with tarfile.open(archive) as package:
            for member in package.getmembers():
                target = (distribution / member.name).resolve()
                if not target.is_relative_to(distribution.resolve()) or member.issym() or member.islnk():
                    raise SystemExit('Unsafe archive entry.')
            package.extractall(distribution)
    binary_name = archive_name.rsplit('.', 1)[0] + ('.exe' if key[0] == 'Windows' else '')
    binary = next(distribution.rglob(binary_name))
    binary.chmod(binary.stat().st_mode | 0o111)
    executable_name = 'stockfish.exe' if key[0] == 'Windows' else 'stockfish'
    executable = root / executable_name
    if executable.is_symlink():
        executable.unlink()
    shutil.copy2(binary, executable)
    license_path = next(distribution.rglob('Copying.txt'))
    shutil.copy2(license_path, root / 'Copying.txt')
    manifest = {
        'engine': 'Stockfish 18', 'platform': key[0], 'architecture': key[1],
        'archive': archive_name, 'archive_sha256': expected,
        'binary_sha256': digest(executable), 'download': url,
        'source': f'https://github.com/official-stockfish/Stockfish/tree/{RELEASE}',
        'license': 'GPL-3.0',
    }
    (root / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    if player_directory:
        destination = Path(player_directory).expanduser().resolve() / 'Engines'
        destination.mkdir(parents=True, exist_ok=True)
        target = destination / executable_name
        if target.exists() and digest(target) != manifest['binary_sha256']:
            raise SystemExit(f'Another engine exists at {target}; preserve it before replacing.')
        for source in (executable, root / 'Copying.txt', root / 'manifest.json'):
            shutil.copy2(source, destination / source.name)
        print(f'Local player engine: {target}')
    print(f'Editor engine: {executable}\nSHA-256: {manifest["binary_sha256"]}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player-directory', help='Unity persistentDataPath for a local standalone player')
    prepare(parser.parse_args().player_directory)
