#!/usr/bin/env python3
"""Prepare pinned Stockfish 18 lite single-thread assets, with no npm hooks."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import urllib.request

VERSION = '18.0.0'
SOURCE_COMMIT = '3f5d4d17bee569cbec41d5f6abed23717eb17c01'
RELEASE = f'https://github.com/nmrugg/stockfish.js/releases/download/v{VERSION}'
RAW = f'https://raw.githubusercontent.com/nmrugg/stockfish.js/{SOURCE_COMMIT}'
SOURCE_URL = f'https://codeload.github.com/nmrugg/stockfish.js/tar.gz/{SOURCE_COMMIT}'
# Release JS/WASM are byte-identical to npm stockfish@18.0.0. The registry
# records this source commit; neither latest nor a system/npm installation is used.
FILES = {
    'stockfish-18-lite-single.js': (f'{RELEASE}/stockfish-18-lite-single.js',
        '2278005057f381491f1c9bb3e44c9f5920b3a00bef9759e33cc6582769a1f1fe'),
    'stockfish-18-lite-single.wasm': (f'{RELEASE}/stockfish-18-lite-single.wasm',
        'a8fbc05ec6920b56d7485826dcb02c5ffd2826bcbf751cf973046f237a9096f1'),
    'Copying.txt': (f'{RAW}/Copying.txt',
        '0b383d5a63da644f628d99c33976ea6487ed89aaa59f0b3257992deac1171e6b'),
    'AUTHORS': (f'{RAW}/AUTHORS',
        '8a3322e37f144896d724ef6b8f4395bf30e4e996c636d38d72789ff812fb33cc'),
    'README.upstream.md': (f'{RAW}/README.md',
        '4bfe213442b4e8f488814ef121f7680e1094e04a65a7b91ca13db05fa85bf6a3'),
    'stockfish.js-18.0.0-source.tar.gz': (SOURCE_URL,
        '3eff01c34587e0dbb1604f383faa50820dd99d5037184a1eef3577790a110cff'),
}


def digest(path):
    checksum = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            checksum.update(block)
    return checksum.hexdigest()


def fetch_verified(url, target, checksum):
    target = Path(target)
    if target.exists():
        if target.is_symlink() or digest(target) != checksum:
            raise ValueError(f'Checksum mismatch; preserve/remove the existing file before retrying: {target}')
        return target
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + '.download')
    if temporary.exists():
        raise ValueError(f'Existing partial download; preserve/remove before retrying: {temporary}')
    try:
        request = urllib.request.Request(url, headers={'User-Agent': 'ChessCGI-StockfishSetup/1'})
        with urllib.request.urlopen(request, timeout=60) as response, temporary.open('xb') as output:
            shutil.copyfileobj(response, output)
        if digest(temporary) != checksum:
            raise ValueError(f'Download checksum mismatch: {url}')
        temporary.replace(target)
    finally:
        if temporary.exists():
            temporary.unlink()
    return target


def install_file(source, target, checksum):
    if target.exists():
        if target.is_symlink() or digest(target) != checksum:
            raise ValueError(f'Another asset exists; preserve it before replacing: {target}')
        return
    shutil.copy2(source, target)


def prepare(root=None):
    root = Path(root) if root else Path(__file__).resolve().parents[1] / '.local' / 'stockfish-web'
    root = root.expanduser().resolve()
    cache = root / 'downloads'
    output = root / 'engine'
    output.mkdir(parents=True, exist_ok=True)
    manifest = {
        'engine': 'Stockfish.js 18 Lite', 'package': f'stockfish@{VERSION}',
        'variant': 'lite-single', 'threads': 1, 'cross_origin_isolation_required': False,
        'source_commit': SOURCE_COMMIT, 'source': SOURCE_URL, 'license': 'GPL-3.0',
        'registry': f'https://registry.npmjs.org/stockfish/{VERSION}',
        'embedded_network': 'nn-9067e33176e8.nnue',
        'files': {},
    }
    for name, (url, checksum) in FILES.items():
        downloaded = fetch_verified(url, cache / name, checksum)
        install_file(downloaded, output / name, checksum)
        manifest['files'][name] = {'url': url, 'sha256': checksum, 'bytes': downloaded.stat().st_size}
    # Generated metadata is reproducible and contains no local machine paths.
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2, sort_keys=True) + '\n')
    (output / 'SOURCE.txt').write_text(
        'Stockfish.js 18.0.0 Lite single-threaded, unmodified upstream release.\n'
        'Copyright (c) 2026 Chess.com, LLC and Stockfish contributors; GPLv3.\n'
        'See Copying.txt and AUTHORS. Source archive is included beside this file.\n'
        f'Corresponding source: {SOURCE_URL}\n'
        f'Commit: {SOURCE_COMMIT}\n'
        'Build instructions: README.upstream.md; build.js --lite --single-threaded --no-split.\n'
        'The NNUE evaluation network is embedded in the WASM release. The source\n'
        'build obtains nn-9067e33176e8.nnue via its upstream network download step.\n'
        'Serve JS and WASM together over HTTP(S), with text/javascript and\n'
        'application/wasm MIME types. No COOP/COEP headers are required.\n')
    print(f'Stockfish web verified: {output}')
    return output


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, help='Local cache/output root (default: .local/stockfish-web)')
    arguments = parser.parse_args()
    try:
        prepare(arguments.root)
    except (ValueError, OSError) as error:
        parser.exit(1, f'{error}\n')
