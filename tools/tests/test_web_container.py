#!/usr/bin/env python3
"""Verify a running static release against its local Unity Web payload.

python3 tools/tests/test_web_container.py --url http://127.0.0.1:8091 \
    --web-root .local/web-release/context/web

TEST_URL and WEB_ROOT provide the same inputs for unittest discovery. These
checks make HTTP reads only; they do not build, start, or publish containers.
"""
import argparse
import gzip
import hashlib
import json
import os
from pathlib import Path
import re
import unittest
import urllib.error
import urllib.parse
import urllib.request


TEST_URL = os.environ.get("TEST_URL", "")
WEB_ROOT = Path(os.environ.get("WEB_ROOT", Path(__file__).resolve().parents[2] / "Builds/Web"))
HTTP_TIMEOUT = 30


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None


class WebContainerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not TEST_URL:
            raise unittest.SkipTest("Set TEST_URL or run this file with --url for live HTTP checks")
        parsed = urllib.parse.urlsplit(TEST_URL)
        if parsed.scheme not in ("http", "https") or not parsed.netloc or parsed.query or parsed.fragment:
            raise ValueError("TEST_URL must be an HTTP(S) base URL without query or fragment")
        cls.base_url = TEST_URL.rstrip("/") + "/"
        cls.web_root = WEB_ROOT.resolve()
        cls.index = (cls.web_root / "index.html").read_bytes()
        cls.manifest = json.loads((cls.web_root / "stockfish/manifest.json").read_text())
        cls.opener = urllib.request.build_opener(NoRedirect())

    def fetch(self, path, *, method="GET", headers=None):
        request = urllib.request.Request(
            urllib.parse.urljoin(self.base_url, path.lstrip("/")),
            method=method,
            headers={"Accept-Encoding": "gzip", **(headers or {})},
        )
        try:
            response = self.opener.open(request, timeout=HTTP_TIMEOUT)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.code, response.headers, response.read()

    def assert_revalidation(self, headers):
        directives = {part.strip().lower() for part in headers.get("Cache-Control", "").split(",")}
        self.assertIn("no-cache", directives)
        self.assertNotIn("immutable", directives)
        self.assertEqual(headers.get("X-Content-Type-Options"), "nosniff")

    def assert_payload(self, path, expected_type=None):
        code, headers, body = self.fetch(path)
        self.assertEqual(code, 200, path)
        self.assert_revalidation(headers)
        if expected_type:
            self.assertEqual(headers.get_content_type(), expected_type, path)
        local = (self.web_root / path.lstrip("/")).read_bytes()
        self.assertEqual(hashlib.sha256(body).digest(), hashlib.sha256(local).digest(), path)
        self.assertEqual(int(headers["Content-Length"]), len(local), path)
        return headers, body

    def test_entry_document_matches_release_at_both_urls(self):
        for path in ("/", "/index.html"):
            with self.subTest(path=path):
                code, headers, body = self.fetch(path)
                self.assertEqual(code, 200)
                self.assertEqual(headers.get_content_type(), "text/html")
                self.assertEqual(body, self.index)
                self.assertIn(b"unity-canvas", body)
                self.assertNotIn("Content-Encoding", headers)
                self.assert_revalidation(headers)

    def test_health_endpoint(self):
        code, headers, body = self.fetch("/healthz")
        self.assertEqual(code, 200)
        self.assertEqual(headers.get_content_type(), "text/plain")
        self.assertEqual(body, b"ok\n")
        self.assert_revalidation(headers)

    def test_unity_runtime_mime_gzip_and_exact_transferred_bytes(self):
        references = dict(re.findall(
            r"\b(dataUrl|frameworkUrl|codeUrl)\s*:\s*['\"]([^'\"]+)['\"]",
            self.index.decode("utf-8"),
        ))
        self.assertEqual(set(references), {"dataUrl", "frameworkUrl", "codeUrl"})
        for key, path in references.items():
            with self.subTest(path=path):
                self.assertFalse(urllib.parse.urlsplit(path).netloc, "Runtime must remain same-origin")
                self.assertTrue(path.endswith(".gz"))
                expected = {"dataUrl": "application/gzip", "frameworkUrl": "application/javascript",
                            "codeUrl": "application/wasm"}[key]
                headers, raw = self.assert_payload(path, expected)
                self.assertEqual(headers.get_all("Content-Encoding"), ["gzip"])
                self.assertTrue(raw.startswith(b"\x1f\x8b"), "Transfer must retain the original gzip bytes")
                decoded = gzip.decompress(raw)
                self.assertTrue(decoded)
                self.assertFalse(decoded.startswith(b"\x1f\x8b"), "Payload was compressed twice")
                if key == "codeUrl":
                    self.assertTrue(decoded.startswith(b"\0asm"))
                if key == "dataUrl":
                    self.assertTrue(decoded.startswith(b"UnityWebData"))

    def test_loader_is_executable_javascript_without_recompression(self):
        match = re.search(r"script\.src\s*=\s*['\"]([^'\"]+)['\"]", self.index.decode("utf-8"))
        self.assertIsNotNone(match)
        path = match.group(1)
        self.assertFalse(urllib.parse.urlsplit(path).netloc)
        headers, body = self.assert_payload(path, "application/javascript")
        self.assertNotIn("Content-Encoding", headers)
        self.assertIn(b"createUnityInstance", body)

    def test_stockfish_worker_wasm_and_source_match_manifest(self):
        self.assertEqual(self.manifest["threads"], 1)
        self.assertFalse(self.manifest["cross_origin_isolation_required"])
        for name, metadata in self.manifest["files"].items():
            with self.subTest(asset=name):
                headers, body = self.assert_payload("stockfish/" + name)
                self.assertEqual(hashlib.sha256(body).hexdigest(), metadata["sha256"])
                self.assertEqual(len(body), metadata["bytes"])
                self.assertNotIn("Content-Encoding", headers, "Source archives must download intact")
                if name.endswith(".wasm"):
                    self.assertEqual(headers.get_content_type(), "application/wasm")
                    self.assertTrue(body.startswith(b"\0asm"))
                if name.endswith(".js"):
                    self.assertEqual(headers.get_content_type(), "application/javascript")

    def test_stockfish_licenses_and_notices_remain_accessible(self):
        for directory in ("stockfish", "StreamingAssets/ThirdParty/Stockfish"):
            for name in ("Copying.txt", "AUTHORS", "SOURCE.txt", "manifest.json"):
                with self.subTest(directory=directory, name=name):
                    self.assert_payload(directory + "/" + name)
        _, copying = self.assert_payload("stockfish/Copying.txt")
        self.assertIn(b"GNU GENERAL PUBLIC LICENSE", copying)
        self.assertIn(self.manifest["source_commit"].encode(),
                      self.fetch("stockfish/SOURCE.txt")[2])

    def test_etag_revalidates_index_runtime_and_worker(self):
        for path in ("index.html", "Build/Web.framework.js.gz", "stockfish/stockfish-18-lite-single.wasm"):
            with self.subTest(path=path):
                code, headers, _ = self.fetch(path, method="HEAD")
                self.assertEqual(code, 200)
                self.assertIn("ETag", headers)
                self.assertIn("Last-Modified", headers)
                self.assert_revalidation(headers)
                code, cached, body = self.fetch(path, headers={"If-None-Match": headers["ETag"]})
                self.assertEqual(code, 304)
                self.assertEqual(body, b"")
                self.assert_revalidation(cached)

    def test_compressed_wasm_range_has_original_bytes(self):
        path = "Build/Web.wasm.gz"
        code, headers, body = self.fetch(path, headers={"Range": "bytes=0-31"})
        self.assertEqual(code, 206)
        self.assertEqual(headers.get_content_type(), "application/wasm")
        self.assertEqual(headers.get("Content-Encoding"), "gzip")
        local = self.web_root / path
        self.assertEqual(headers["Content-Range"], f"bytes 0-31/{local.stat().st_size}")
        with local.open("rb") as stream:
            self.assertEqual(body, stream.read(32))
        self.assert_revalidation(headers)

    def test_missing_assets_and_routes_are_real_non_gzip_404(self):
        for path in ("Build/missing.data.gz", "Build/missing.framework.js.gz", "Build/missing.wasm.gz",
                     "Build/missing.loader.js", "stockfish/missing.wasm", "stockfish/missing.js",
                     "stockfish/missing.tar.gz", "missing-client-route", "50x.html"):
            with self.subTest(path=path):
                code, headers, body = self.fetch(path)
                self.assertEqual(code, 404, path)
                self.assertNotEqual(body, self.index)
                self.assertNotIn("Content-Encoding", headers)
                self.assert_revalidation(headers)

    def test_directory_redirect_is_relative_behind_https_proxy(self):
        code, headers, _ = self.fetch("stockfish", headers={"X-Forwarded-Proto": "https"})
        self.assertEqual(code, 301)
        self.assertEqual(headers["Location"], "/stockfish/")

    def test_single_threaded_runtime_has_no_unneeded_isolation_headers(self):
        for path in ("index.html", "Build/Web.loader.js", "stockfish/stockfish-18-lite-single.js"):
            with self.subTest(path=path):
                code, headers, _ = self.fetch(path, method="HEAD")
                self.assertEqual(code, 200)
                self.assertNotIn("Cross-Origin-Opener-Policy", headers)
                self.assertNotIn("Cross-Origin-Embedder-Policy", headers)
                self.assertNotIn("Content-Security-Policy", headers)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default=TEST_URL, help="Running release base URL (or TEST_URL)")
    parser.add_argument("--web-root", type=Path, default=WEB_ROOT, help="Exact local release payload")
    parser.add_argument("--timeout", type=float, default=HTTP_TIMEOUT, help="Per-request timeout in seconds")
    args, remaining = parser.parse_known_args()
    if not args.url:
        parser.error("--url or TEST_URL is required")
    TEST_URL, WEB_ROOT, HTTP_TIMEOUT = args.url, args.web_root, args.timeout
    unittest.main(argv=[__file__, *remaining], verbosity=2)
