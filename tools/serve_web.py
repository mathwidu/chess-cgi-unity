#!/usr/bin/env python3
"""Serve the Unity prototype locally with correct precompressed WASM headers."""
import argparse
import functools
import http.server
import mimetypes
from pathlib import Path


class WebHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        # Unity's Safari workaround: precompressed data must advertise gzip,
        # while WASM retains its MIME type for streaming compilation.
        if path.endswith('.data.gz'):
            return 'application/gzip'
        base = path[:-3] if path.endswith('.gz') else path
        return {'.wasm': 'application/wasm', '.js': 'application/javascript',
                '.data': 'application/octet-stream'}.get(Path(base).suffix,
                mimetypes.guess_type(base)[0] or 'application/octet-stream')

    def end_headers(self):
        if self.path.split('?', 1)[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-cache')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8090)
    parser.add_argument('--host', default='127.0.0.1')
    parser.add_argument('--directory', type=Path, default=Path(__file__).resolve().parents[1] / 'Builds/Web')
    args = parser.parse_args()
    root = args.directory.resolve()
    if not (root / 'index.html').is_file():
        parser.error(f'No Web build at {root}; run ChessCgiWebBuild.Build first.')
    handler = functools.partial(WebHandler, directory=str(root))
    with http.server.ThreadingHTTPServer((args.host, args.port), handler) as server:
        print(f'Chess CGI: http://{args.host}:{args.port}/ — {root}', flush=True)
        server.serve_forever()


if __name__ == '__main__':
    main()
