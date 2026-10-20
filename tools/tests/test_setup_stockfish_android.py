"""Focused packaging checks; no network, Unity, Android device or large binary."""

import hashlib
import importlib.util
import io
from pathlib import Path
import struct
import tarfile
import tempfile
import unittest
from unittest import mock


SPEC = importlib.util.spec_from_file_location('setup_stockfish_android',
    Path(__file__).resolve().parents[1] / 'setup_stockfish_android.py')
PACKAGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PACKAGE)


class StockfishAndroidPackageTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def archive(self, names):
        path = self.root / 'archive.tar'
        with tarfile.open(path, 'w') as archive:
            for name, kind in names:
                entry = tarfile.TarInfo(name)
                entry.type = kind
                entry.linkname = '../../outside'
                entry.size = 1 if kind == tarfile.REGTYPE else 0
                archive.addfile(entry, io.BytesIO(b'x') if entry.size else None)
        return path

    def test_safe_extraction_rejects_paths_links_and_duplicates_before_writing(self):
        for names in [[('stockfish/../outside', tarfile.REGTYPE)],
                      [('/stockfish/outside', tarfile.REGTYPE)],
                      [('stockfish\\outside', tarfile.REGTYPE)],
                      [('stockfish/link', tarfile.SYMTYPE)],
                      [('stockfish/link', tarfile.LNKTYPE)],
                      [('stockfish/fifo', tarfile.FIFOTYPE)],
                      [('stockfish/a', tarfile.REGTYPE), ('stockfish/a', tarfile.REGTYPE)]]:
            with self.subTest(names=names):
                destination = self.root / 'extract'
                destination.mkdir(exist_ok=True)
                with self.assertRaises(PACKAGE.PackageError):
                    PACKAGE.safe_extract(self.archive(names), destination)
                self.assertEqual(list(destination.iterdir()), [])

    def test_safe_extraction_accepts_release_layout(self):
        destination = self.root / 'extract'
        destination.mkdir()
        PACKAGE.safe_extract(self.archive([('stockfish/src/a.cpp', tarfile.REGTYPE)]), destination)
        self.assertEqual((destination / 'stockfish/src/a.cpp').read_bytes(), b'x')

    def test_offline_cache_verifies_hash_without_download(self):
        path = self.root / 'cached'
        path.write_bytes(b'release')
        expected = hashlib.sha256(b'release').hexdigest()
        with mock.patch.object(PACKAGE.urllib.request, 'urlopen') as network:
            self.assertEqual(PACKAGE.fetch('https://invalid', path, expected, True), path)
            path.write_bytes(b'tampered')
            with self.assertRaises(PACKAGE.PackageError):
                PACKAGE.fetch('https://invalid', path, expected, True)
            with self.assertRaises(PACKAGE.PackageError):
                PACKAGE.fetch('https://invalid', self.root / 'missing', expected, True)
            network.assert_not_called()

    def test_existing_asset_is_preserved_on_mismatch(self):
        path = self.root / 'asset'
        path.write_bytes(b'owned elsewhere')
        with self.assertRaises(PACKAGE.PackageError):
            PACKAGE.guarded_write(path, b'replacement')
        self.assertEqual(path.read_bytes(), b'owned elsewhere')

    def test_source_bundle_is_reproducible_when_file_timestamps_change(self):
        source = self.root / 'source'
        source.write_bytes(b'corresponding source')
        first, second = self.root / 'first.tar.gz', self.root / 'second.tar.gz'
        PACKAGE.deterministic_tar(first, {'stockfish/a.cpp': source})
        source.touch()
        PACKAGE.deterministic_tar(second, {'stockfish/a.cpp': source})
        self.assertEqual(first.read_bytes(), second.read_bytes())

    def test_only_exact_network_data_counts_as_embedded(self):
        net = self.root / 'nn-example.nnue'
        net.write_bytes(b'network bytes')
        binary = self.root / 'engine'
        binary.write_bytes(b'prefix network bytes suffix')
        expected = hashlib.sha256(net.read_bytes()).hexdigest()
        with mock.patch.dict(PACKAGE.NETWORKS, {net.name: expected}, clear=True):
            info = PACKAGE.verify_networks(binary, {net.name: net})
            self.assertEqual(info[net.name]['embedded_offset'], 7)
            binary.write_bytes(net.name.encode())
            with self.assertRaises(PACKAGE.PackageError):
                PACKAGE.verify_networks(binary, {net.name: net})

    def fake_elf(self, kind=3, machine=183, alignment=16384, interpreter='/system/bin/linker64'):
        data = bytearray(512)
        data[:7] = b'\x7fELF\x02\x01\x01'
        struct.pack_into('<HH', data, 16, kind, machine)
        struct.pack_into('<QQ', data, 24, 4096, 64)
        struct.pack_into('<HH', data, 54, 56, 2)
        struct.pack_into('<IIQQQQQQ', data, 64, 1, 5, 0, 0, 0, 512, 512, alignment)
        raw = interpreter.encode() + b'\0'
        struct.pack_into('<IIQQQQQQ', data, 120, 3, 4, 256, 256, 256, len(raw), len(raw), 1)
        data[256:256 + len(raw)] = raw
        binary = self.root / 'engine'
        binary.write_bytes(data)
        return binary

    def test_elf_gate_checks_pie_android_linker_dependencies_and_pages(self):
        report = '(NEEDED) Shared library: [libc.so]\n(FLAGS_1) Flags: NOW PIE\n'
        with mock.patch.object(PACKAGE.subprocess, 'run', return_value=mock.Mock(stdout=report)):
            info, _ = PACKAGE.inspect_elf(self.fake_elf(), 'readelf')
            self.assertEqual(info['needed'], ['libc.so'])
            for overrides in [{'kind': 2}, {'machine': 62}, {'alignment': 4096},
                              {'interpreter': '/lib/ld-linux-aarch64.so.1'}]:
                with self.subTest(overrides=overrides), self.assertRaises(PACKAGE.PackageError):
                    PACKAGE.inspect_elf(self.fake_elf(**overrides), 'readelf')
        with mock.patch.object(PACKAGE.subprocess, 'run', return_value=mock.Mock(
                stdout=report + '(NEEDED) Shared library: [libc++_shared.so]\n')):
            with self.assertRaises(PACKAGE.PackageError):
                PACKAGE.inspect_elf(self.fake_elf(), 'readelf')

    def test_plugin_metadata_is_stable_android_arm64_and_not_preloaded(self):
        meta = PACKAGE.meta_bytes(PACKAGE.PLUGIN_PATH, plugin=True)
        self.assertEqual(meta, PACKAGE.meta_bytes(PACKAGE.PLUGIN_PATH, plugin=True))
        text = meta.decode()
        self.assertIn('isPreloaded: 0', text)
        self.assertIn('CPU: ARM64', text)
        self.assertEqual(text.count('enabled: 1'), 1)
        self.assertNotIn('Standalone:', text)


if __name__ == '__main__':
    unittest.main()
