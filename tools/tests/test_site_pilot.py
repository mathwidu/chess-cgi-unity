import gzip
import http.client
import http.server
import json
from pathlib import Path
import sys
import tempfile
import threading
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from serve_site_pilot import Build, handler_for


class SitePilotTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        (root / 'Build').mkdir()
        (root / 'stockfish').mkdir()
        (root / 'index.html').write_text("dataUrl: 'Build/Web.data.gz', frameworkUrl: 'Build/Web.framework.js.gz', codeUrl: 'Build/Web.wasm.gz'; script.src = 'Build/Web.loader.js';")
        for name in ('data', 'framework.js', 'wasm'):
            (root / f'Build/Web.{name}.gz').write_bytes(gzip.compress(b'payload', mtime=0))
        (root / 'Build/Web.loader.js').write_text('loader')
        (root / 'stockfish/engine.wasm').write_bytes(b'wasm')
        self.build = Build(root)
        self.server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), handler_for(self.build, 'http://127.0.0.1:1'))
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.addCleanup(self.stop)

    def stop(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    def request(self, path, method='GET', headers=None):
        connection = http.client.HTTPConnection(*self.server.server_address)
        connection.request(method, path, headers=headers or {})
        response = connection.getresponse()
        result = response.status, dict(response.getheaders()), response.read()
        connection.close()
        return result

    def test_manifest_and_version_are_content_based(self):
        status, headers, body = self.request('/jogar/runtime/manifest.json')
        self.assertEqual(status, 200)
        self.assertEqual(headers['Cache-Control'], 'no-store')
        manifest = json.loads(body)
        self.assertEqual(manifest['buildId'], Build(self.build.root).id)
        expected = len(b'loader') + sum((self.build.root / ('Build/Web.' + suffix + '.gz')).stat().st_size for suffix in ('data', 'framework.js', 'wasm'))
        self.assertEqual(manifest['initialBytes'], expected)
        (self.build.root / 'stockfish/engine.wasm').write_bytes(b'new wasm')
        self.assertNotEqual(manifest['buildId'], Build(self.build.root).id)

    def test_wasm_encoding_head_and_revalidation(self):
        path = self.build.manifest['assetBase'] + 'Build/Web.wasm.gz'
        status, headers, body = self.request(path)
        self.assertEqual(status, 200)
        self.assertEqual(headers['Content-Type'], 'application/wasm')
        self.assertEqual(headers['Content-Encoding'], 'gzip')
        self.assertEqual(gzip.decompress(body), b'payload')
        self.assertIn('immutable', headers['Cache-Control'])
        head_status, head_headers, head_body = self.request(path, 'HEAD')
        self.assertEqual((head_status, head_headers['Content-Length'], head_body), (200, str(len(body)), b''))
        self.assertEqual(self.request(path, headers={'If-None-Match': headers['ETag']})[0], 304)

    def test_missing_assets_and_traversal_never_receive_gzip_headers(self):
        for name in ('Build/missing.gz', '../index.html', '%2e%2e/index.html', 'Build/', 'stockfish/../../index.html'):
            status, headers, _ = self.request(self.build.manifest['assetBase'] + name)
            self.assertEqual(status, 404)
            self.assertNotIn('Content-Encoding', headers)

    def test_changed_build_cannot_be_served_under_old_id(self):
        (self.build.root / 'Build/Web.loader.js').write_text('different')
        status, headers, _ = self.request(self.build.manifest['assetBase'] + 'Build/Web.loader.js')
        self.assertEqual(status, 503)
        self.assertEqual(headers['Cache-Control'], 'no-store')

    def test_unavailable_site_returns_helpful_gateway_error(self):
        self.assertEqual(self.request('/jogar/')[0], 502)

    def test_external_upstream_rejected(self):
        with self.assertRaises(ValueError):
            handler_for(self.build, 'http://example.com')


if __name__ == '__main__':
    unittest.main()
