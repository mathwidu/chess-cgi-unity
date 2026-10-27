#!/usr/bin/env python3
"""Local-only site + Unity pilot. No production configuration is changed."""
import argparse
import hashlib
import http.client
import http.server
import json
import mimetypes
import re
from pathlib import Path
from urllib.parse import unquote, urlsplit

PREFIX = '/jogar/runtime/'
HOP_HEADERS = {'connection', 'keep-alive', 'proxy-authenticate', 'proxy-authorization',
               'te', 'trailer', 'transfer-encoding', 'upgrade'}


class Build:
    def __init__(self, root):
        self.root = Path(root).resolve()
        html = (self.root / 'index.html').read_text()
        self.files = {}
        for folder in ('Build', 'StreamingAssets', 'stockfish'):
            for path in sorted((self.root / folder).rglob('*')):
                if path.is_file():
                    if path.is_symlink() or not path.resolve().is_relative_to(self.root):
                        raise ValueError('Build must not contain symlinks outside its root')
                    name = path.relative_to(self.root).as_posix()
                    self.files[name] = (path, hashlib.sha256(path.read_bytes()).hexdigest(), path.stat().st_size)
        entries = {}
        for key, pattern in {'loader': r"script.src\s*=\s*['\"](Build/[\w.-]+)['\"]",
                             'data': r"dataUrl:\s*['\"](Build/[\w.-]+)['\"]",
                             'framework': r"frameworkUrl:\s*['\"](Build/[\w.-]+)['\"]",
                             'code': r"codeUrl:\s*['\"](Build/[\w.-]+)['\"]"}.items():
            match = re.search(pattern, html)
            if not match or match[1] not in self.files:
                raise ValueError(f'Missing built {key} entry; run ChessCgiWebBuild.Build')
            entries[key] = match[1]
        inventory = json.dumps({name: value[1] for name, value in self.files.items()}, sort_keys=True).encode()
        self.id = hashlib.sha256(inventory).hexdigest()
        self.manifest = {'schema': 1, 'buildId': self.id, 'assetBase': f'{PREFIX}{self.id}/', **entries,
                         'initialBytes': sum(self.files[name][2] for name in entries.values()),
                         'aiBytes': sum(info[2] for name, info in self.files.items() if name.startswith('stockfish/') and name.endswith(('.js', '.wasm')))}


def handler_for(build, upstream):
    destination = urlsplit(upstream)
    if destination.scheme != 'http' or destination.hostname not in ('127.0.0.1', 'localhost') or destination.username or destination.password or destination.path not in ('', '/') or destination.query or destination.fragment:
        raise ValueError('Upstream must be a local HTTP site root')

    class Handler(http.server.BaseHTTPRequestHandler):
        def reply(self, status, body, headers=None, head=False):
            self.send_response(status)
            for key, value in (headers or {}).items():
                self.send_header(key, value)
            if status != 304:
                self.send_header('Content-Length', str(len(body)))
            self.send_header('X-Content-Type-Options', 'nosniff')
            self.end_headers()
            if not head:
                self.wfile.write(body)

        def route(self, head=False):
            path = unquote(urlsplit(self.path).path)
            if path == PREFIX + 'manifest.json':
                self.reply(200, json.dumps(build.manifest).encode(), {'Content-Type': 'application/json', 'Cache-Control': 'no-store'}, head)
                return
            if path.startswith(PREFIX):
                version_prefix = build.manifest['assetBase']
                name = path[len(version_prefix):] if path.startswith(version_prefix) else ''
                item = build.files.get(name)
                if not item:
                    self.reply(404, b'Build asset not found', {'Cache-Control': 'no-store'}, head)
                    return
                file, expected_hash, _ = item
                try:
                    data = file.read_bytes()
                except OSError:
                    data = None
                if data is None or hashlib.sha256(data).hexdigest() != expected_hash:
                    self.reply(503, b'Build changed. Restart the local pilot server.', {'Cache-Control': 'no-store'}, head)
                    return
                tag = f'"{expected_hash}"'
                headers = {'Cache-Control': 'public, max-age=31536000, immutable', 'ETag': tag}
                if self.headers.get('If-None-Match') == tag:
                    self.reply(304, b'', headers, head=True)
                    return
                base = name[:-3] if name.endswith('.gz') else name
                headers['Content-Type'] = {'.wasm': 'application/wasm', '.js': 'application/javascript', '.data': 'application/octet-stream'}.get(Path(base).suffix, mimetypes.guess_type(base)[0] or 'application/octet-stream')
                if name.endswith('.gz'):
                    headers['Content-Encoding'] = 'gzip'
                    if name.endswith('.data.gz'):
                        headers['Content-Type'] = 'application/gzip'  # Unity's Safari compatibility requirement.
                self.reply(200, data, headers, head)
                return
            self.proxy(head)

        def proxy(self, head=False):
            if self.headers.get('Transfer-Encoding'):
                self.reply(400, b'Chunked requests are not supported by the local pilot')
                return
            try:
                length = int(self.headers.get('Content-Length', '0'))
                if not 0 <= length <= 1024 * 1024:
                    raise ValueError()
            except ValueError:
                self.reply(413, b'Request body too large')
                return
            body = self.rfile.read(length) if length else None
            headers = {key: value for key, value in self.headers.items() if key.lower() not in HOP_HEADERS}
            # Keep original Host: Astro form origin checks must see the pilot's origin.
            connection = http.client.HTTPConnection(destination.hostname, destination.port or 80, timeout=30)
            responded = False
            try:
                connection.request(self.command, self.path, body, headers)
                response = connection.getresponse()
                responded = True
                self.send_response(response.status)
                for key, value in response.getheaders():
                    if key.lower() not in HOP_HEADERS:
                        self.send_header(key, value)
                self.end_headers()
                if not head:
                    while chunk := response.read(65536):
                        self.wfile.write(chunk)
            except (OSError, http.client.HTTPException):
                if not responded:
                    self.reply(502, b'Local site unavailable. Start the Astro server first.', {'Cache-Control': 'no-store'}, head)
                self.close_connection = True
            finally:
                connection.close()

        def do_GET(self):
            self.route()

        def do_HEAD(self):
            self.route(head=True)

        def do_POST(self):
            if unquote(urlsplit(self.path).path).startswith(PREFIX):
                self.reply(405, b'Read-only build assets', {'Allow': 'GET, HEAD'})
            else:
                self.proxy()

    return Handler


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8090)
    parser.add_argument('--site', default='http://127.0.0.1:4321')
    parser.add_argument('--directory', type=Path, default=Path(__file__).resolve().parents[1] / 'Builds/Web')
    args = parser.parse_args()
    try:
        build = Build(args.directory)
        handler = handler_for(build, args.site)
    except (OSError, ValueError) as error:
        parser.error(str(error))
    with http.server.ThreadingHTTPServer(('127.0.0.1', args.port), handler) as server:
        print(f'Local CiCoChess: http://127.0.0.1:{args.port}/jogar/ | build {build.id}', flush=True)
        server.serve_forever()


if __name__ == '__main__':
    main()
