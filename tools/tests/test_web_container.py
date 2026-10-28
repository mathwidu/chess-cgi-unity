#!/usr/bin/env python3
"""Read-only HTTP checks for a verified runtime-only image.

python3 tools/tests/test_web_container.py --url http://127.0.0.1:8091 \
    --web-root .local/web-release/context/web

The local manifest is the reviewed expectation, not data fetched from the server.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import unittest
import urllib.error
import urllib.parse
import urllib.request

SPEC = importlib.util.spec_from_file_location('web_release', Path(__file__).resolve().parents[1] / 'web_release.py')
RELEASE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RELEASE)
TEST_URL = os.environ.get('TEST_URL', '')
WEB_ROOT = Path(os.environ.get('WEB_ROOT', Path(__file__).resolve().parents[2] / '.local/web-release/context/web'))
HTTP_TIMEOUT = 30


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None


class WebContainerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not TEST_URL:
            raise unittest.SkipTest('Set TEST_URL or --url for live HTTP checks')
        parsed = urllib.parse.urlsplit(TEST_URL)
        if parsed.scheme not in ('http', 'https') or not parsed.netloc or parsed.path not in ('', '/') or parsed.query or parsed.fragment:
            raise ValueError('TEST_URL must be an HTTP(S) origin without path, query or fragment')
        cls.base_url = TEST_URL.rstrip('/') + '/'
        cls.web_root = WEB_ROOT.resolve()
        cls.context = json.loads((cls.web_root.parent / 'manifest.json').read_text())
        RELEASE.validate_manifest_shape(cls.context)
        cls.launch = RELEASE.launch_manifest(cls.context['build_id'], cls.context['runtimes'][cls.context['build_id']])
        cls.release = RELEASE.public_metadata(cls.context)
        cls.opener = urllib.request.build_opener(NoRedirect())

    def fetch(self, path, *, method='GET', headers=None):
        request = urllib.request.Request(urllib.parse.urljoin(self.base_url, path.lstrip('/')),
            method=method, headers={'Accept-Encoding': 'br, gzip', **(headers or {})})
        try:
            response = self.opener.open(request, timeout=HTTP_TIMEOUT)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.code, response.headers, response.read()

    def assert_cache(self, headers, immutable=False):
        self.assertEqual(headers.get('Cache-Control'), RELEASE.IMMUTABLE if immutable else 'no-store')
        self.assertEqual(headers.get('X-Content-Type-Options'), 'nosniff')

    def assert_asset(self, build_id, name, item):
        path = f'{RELEASE.PREFIX}{build_id}/{name}'
        code, headers, body = self.fetch(path)
        self.assertEqual(code, 200, path)
        self.assert_cache(headers, immutable=True)
        self.assertEqual(headers.get_content_type(), item['content_type'], path)
        self.assertEqual(headers.get('Content-Encoding', 'identity'), item['encoding'], path)
        local = self.web_root / path.lstrip('/')
        self.assertEqual(hashlib.sha256(body).hexdigest(), item['sha256'], path)
        self.assertEqual(body, local.read_bytes(), path)
        self.assertEqual(int(headers['Content-Length']), len(body), path)
        return path, headers, body

    def test_mutable_manifest_and_public_release_match_local_expectation(self):
        for name, expected in (('manifest.json', self.launch), ('release.json', self.release)):
            path = RELEASE.PREFIX + name
            code, headers, body = self.fetch(path)
            self.assertEqual(code, 200)
            self.assertEqual(body, RELEASE.canonical_json(expected))
            self.assertEqual(headers.get_content_type(), 'application/json')
            self.assertNotIn('Content-Encoding', headers)
            self.assert_cache(headers)
            self.assertNotIn('ETag', headers)
            code, headers, head_body = self.fetch(path, method='HEAD')
            self.assertEqual(code, 200)
            self.assertEqual(head_body, b'')
            self.assertEqual(int(headers['Content-Length']), len(body))
            code, headers, body_again = self.fetch(path, headers={
                'If-None-Match': '*', 'If-Modified-Since': 'Thu, 31 Dec 2099 23:59:59 GMT'})
            self.assertEqual(code, 200, 'Mutable pointers must not become conditional 304 responses')
            self.assertEqual(body_again, body)
            self.assert_cache(headers)
        self.assertEqual(self.launch['schema'], 1)
        self.assertEqual(self.launch['assetBase'], RELEASE.PREFIX + self.launch['buildId'] + '/')
        self.assertEqual(self.release['retained_build_ids'][0], self.launch['buildId'])
        self.assertLessEqual(len(self.release['retained_build_ids']), 2)

    def test_health_endpoint(self):
        code, headers, body = self.fetch('/healthz')
        self.assertEqual((code, body), (200, b'ok\n'))
        self.assertEqual(headers.get_content_type(), 'text/plain')
        self.assertNotIn('Content-Encoding', headers)
        self.assert_cache(headers)

    def test_all_current_and_retained_runtime_bytes_mime_and_encoding(self):
        for build_id, runtime in self.context['runtimes'].items():
            for name, item in runtime['files'].items():
                with self.subTest(build=build_id, asset=name):
                    self.assert_asset(build_id, name, item)
        runtime = self.context['runtimes'][self.context['build_id']]
        for key in ('loader', 'data', 'framework', 'code'):
            item = runtime['files'][self.launch[key]]
            self.assertEqual(item['encoding'], 'br')
        self.assertEqual(runtime['files'][self.launch['code']]['content_type'], 'application/wasm')

    def test_stockfish_worker_and_wasm_keep_conventional_urls_and_decoded_pins(self):
        runtime = self.context['runtimes'][self.context['build_id']]
        pins, _, _ = RELEASE.stockfish_pins(RELEASE.REPO_ROOT)
        root = self.web_root / self.launch['assetBase'].lstrip('/')
        for name in ('stockfish-18-lite-single.js', 'stockfish-18-lite-single.wasm'):
            item = runtime['files']['stockfish/' + name]
            path, headers, body = self.assert_asset(self.context['build_id'], 'stockfish/' + name, item)
            self.assertFalse(path.endswith('.br'), 'Worker resolves its conventional companion WASM URL')
            self.assertEqual(headers['Content-Encoding'], 'br')
            decoded = RELEASE.brotli('inspect', root / 'stockfish' / name)
            self.assertEqual(decoded['sha256'], pins[name][1])

    def test_stockfish_sources_licenses_and_notices_are_accessible_intact(self):
        runtime = self.context['runtimes'][self.context['build_id']]
        for name, item in runtime['files'].items():
            if name in RELEASE.BROTLI_FILES:
                continue
            path, headers, body = self.assert_asset(self.context['build_id'], name, item)
            self.assertNotIn('Content-Encoding', headers, path)
            if name == 'stockfish/Copying.txt':
                self.assertIn(b'GNU GENERAL PUBLIC LICENSE', body)
            if name.endswith('.tar.gz'):
                self.assertTrue(body.startswith(b'\x1f\x8b'), 'Source tar.gz must download intact')

    def test_head_304_and_range_preserve_encoded_asset_contract(self):
        for build_id, runtime in self.context['runtimes'].items():
            for name in ('Build/Web.wasm.br', 'Build/Web.loader.js', 'stockfish/stockfish-18-lite-single.wasm'):
                with self.subTest(build=build_id, asset=name):
                    item = runtime['files'][name]
                    path = f'{RELEASE.PREFIX}{build_id}/{name}'
                    code, headers, body = self.fetch(path, method='HEAD')
                    self.assertEqual(code, 200)
                    self.assertEqual(body, b'')
                    self.assertEqual(headers.get('Content-Encoding'), 'br')
                    self.assertEqual(headers.get_content_type(), item['content_type'])
                    self.assertEqual(int(headers['Content-Length']), item['bytes'])
                    self.assert_cache(headers, immutable=True)
                    code, cached, body = self.fetch(path, headers={'If-None-Match': headers['ETag']})
                    self.assertEqual(code, 304)
                    self.assertEqual(body, b'')
                    self.assertEqual(cached.get('Content-Encoding'), 'br')
                    self.assert_cache(cached, immutable=True)
                    code, ranged, body = self.fetch(path, headers={'Range': 'bytes=0-7'})
                    self.assertEqual(code, 206)
                    self.assertEqual(ranged.get('Content-Encoding'), 'br')
                    self.assertEqual(ranged['Content-Range'], f"bytes 0-7/{item['bytes']}")
                    self.assertEqual(body, (self.web_root / path.lstrip('/')).read_bytes()[:8])
                    self.assert_cache(ranged, immutable=True)

    def test_unknown_routes_files_and_build_ids_are_uncompressed_404(self):
        base = self.launch['assetBase']
        for path in ('/', '/index.html', '/jogar/', '/release.json', '/50x.html', '/stockfish/',
                     RELEASE.PREFIX, RELEASE.PREFIX + 'private.env',
                     RELEASE.PREFIX + '0' * 64 + '/Build/Web.wasm.br',
                     base, base + 'index.html', base + 'Build/missing.wasm.br',
                     base + 'Build/Web.wasm.gz', base + 'stockfish/missing.js', base + 'stockfish/'):
            with self.subTest(path=path):
                code, headers, body = self.fetch(path)
                self.assertEqual(code, 404, path)
                self.assertNotIn('Content-Encoding', headers, path)
                self.assert_cache(headers)

    def test_invalid_range_is_uncompressed_error_without_immutable_cache(self):
        path = self.launch['assetBase'] + self.launch['code']
        code, headers, body = self.fetch(path, headers={'Range': 'bytes=999999999999-'})
        self.assertEqual(code, 416)
        self.assertNotIn('Content-Encoding', headers)
        self.assert_cache(headers)

    def test_runtime_is_read_only_and_single_thread_does_not_add_isolation(self):
        for path in (RELEASE.PREFIX + 'manifest.json', self.launch['assetBase'] + self.launch['loader']):
            for method in ('POST', 'PUT', 'DELETE', 'OPTIONS'):
                with self.subTest(path=path, method=method):
                    code, headers, body = self.fetch(path, method=method)
                    self.assertEqual(code, 405)
                    self.assertNotIn('Content-Encoding', headers)
                    self.assert_cache(headers)
            code, headers, _ = self.fetch(path, method='HEAD')
            self.assertEqual(code, 200)
            for header in ('Cross-Origin-Opener-Policy', 'Cross-Origin-Embedder-Policy', 'Content-Security-Policy'):
                self.assertNotIn(header, headers)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', default=TEST_URL)
    parser.add_argument('--web-root', type=Path, default=WEB_ROOT)
    parser.add_argument('--timeout', type=float, default=HTTP_TIMEOUT)
    args, remaining = parser.parse_known_args()
    if not args.url:
        parser.error('--url or TEST_URL is required')
    TEST_URL, WEB_ROOT, HTTP_TIMEOUT = args.url, args.web_root, args.timeout
    unittest.main(argv=[__file__, *remaining], verbosity=2)
