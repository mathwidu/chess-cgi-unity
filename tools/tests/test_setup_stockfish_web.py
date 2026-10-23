"""Local deterministic setup checks; no downloads, installs or engine processes."""
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('setup_stockfish_web', Path(__file__).resolve().parents[1] / 'setup_stockfish_web.py')
SETUP = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SETUP)


class WebSetupTests(unittest.TestCase):
    def test_pinned_variant_source_and_integrity_are_complete(self):
        self.assertEqual(SETUP.VERSION, '18.0.0')
        self.assertEqual(len(SETUP.SOURCE_COMMIT), 40)
        self.assertEqual(len(SETUP.FILES), 6)
        for name, (url, checksum) in SETUP.FILES.items():
            self.assertTrue(url.startswith('https://'))
            self.assertEqual(len(checksum), 64, name)
            self.assertNotIn('/master/', url)
            self.assertNotIn('/latest/', url)

    def test_verified_cache_does_not_touch_network(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / 'asset'
            target.write_bytes(b'verified')
            checksum = hashlib.sha256(target.read_bytes()).hexdigest()
            with patch.object(SETUP.urllib.request, 'urlopen', side_effect=AssertionError('Network used')):
                self.assertEqual(SETUP.fetch_verified('https://unused', target, checksum), target)

    def test_modified_cache_is_preserved_and_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / 'asset'
            target.write_bytes(b'preserve')
            with self.assertRaisesRegex(ValueError, 'Checksum mismatch'):
                SETUP.fetch_verified('https://unused', target, '0' * 64)
            self.assertEqual(target.read_bytes(), b'preserve')

    def test_modified_destination_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as temporary:
            source, target = Path(temporary) / 'source', Path(temporary) / 'target'
            source.write_bytes(b'new')
            target.write_bytes(b'old')
            with self.assertRaisesRegex(ValueError, 'Another asset'):
                SETUP.install_file(source, target, hashlib.sha256(source.read_bytes()).hexdigest())
            self.assertEqual(target.read_bytes(), b'old')

    def test_failed_download_is_rejected_and_partial_removed(self):
        import io
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / 'asset'
            with patch.object(SETUP.urllib.request, 'urlopen', return_value=io.BytesIO(b'corrupt')):
                with self.assertRaisesRegex(ValueError, 'Download checksum mismatch'):
                    SETUP.fetch_verified('https://unused', target, '0' * 64)
            self.assertFalse(target.exists())
            self.assertFalse(target.with_suffix('.download').exists())


if __name__ == '__main__':
    unittest.main()
