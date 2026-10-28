"""Offline release-package regressions with an isolated clean Git fixture."""
import gzip
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import unittest
from unittest.mock import patch


SPEC = importlib.util.spec_from_file_location("web_release", Path(__file__).resolve().parents[1] / "web_release.py")
RELEASE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RELEASE)


class WebReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="chess-web-release-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.build = self.root / "Builds/Web"
        self.output = self.root / ".local/release"
        self.write(self.root / ".gitignore", b"/Builds/\n/.local/\n")
        self.write(self.root / "tracked.txt", b"reviewed source\n")
        self.write(self.root / "deploy/web/Dockerfile", b"FROM scratch\nCOPY web/ /srv/chess-web/\n")
        self.write(self.root / "deploy/web/nginx.conf", b"events {}\nhttp {}\n")
        for recipe in RELEASE.RECIPES:
            if recipe.startswith("tools/"):
                self.write(self.root / recipe, (Path(__file__).resolve().parents[2] / recipe).read_bytes())
        self.write(self.root / "game/ProjectSettings/ProjectVersion.txt", b"m_EditorVersion: 6000.3.16f1\n")
        self.git("init", "--quiet")
        commit = "3" * 40
        source_url = "https://codeload.github.com/nmrugg/stockfish.js/tar.gz/" + commit
        assets = {
            "stockfish-18-lite-single.js": b'globalThis.postMessage("uciok");\n',
            "stockfish-18-lite-single.wasm": b"\0asm\x01\0\0\0",
            "Copying.txt": b"GNU GENERAL PUBLIC LICENSE\nVersion 3\n",
            "AUTHORS": b"Stockfish contributors\n",
            "README.upstream.md": b"Build the corresponding source with --lite --single-threaded\n",
            "stockfish.js-18.0.0-source.tar.gz": gzip.compress(b"pinned fixture source", mtime=0),
        }
        self.pins = {
            name: (source_url if name.endswith(".tar.gz") else "https://example.invalid/pinned/" + name,
                   hashlib.sha256(data).hexdigest())
            for name, data in assets.items()
        }
        self.patch_pins = patch.object(RELEASE, "stockfish_pins", return_value=(self.pins, "18.0.0", commit))
        self.patch_pins.start()
        self.addCleanup(self.patch_pins.stop)
        for name, data in assets.items():
            self.write(self.build / "stockfish" / name, data)
        metadata = {
            "engine": "Stockfish.js 18 Lite", "package": "stockfish@18.0.0",
            "source_commit": commit, "threads": 1, "variant": "lite-single",
            "cross_origin_isolation_required": False, "license": "GPL-3.0", "source": source_url,
            "registry": "https://registry.npmjs.org/stockfish/18.0.0",
            "embedded_network": "nn-9067e33176e8.nnue",
            "files": {name: {"url": self.pins[name][0], "sha256": self.pins[name][1], "bytes": len(data)}
                      for name, data in assets.items()},
        }
        self.write(self.build / "stockfish/manifest.json", RELEASE.canonical_json(metadata))
        self.write(self.build / "stockfish/SOURCE.txt", f"Commit: {commit}\nSource: {source_url}\n".encode())
        for name in RELEASE.NOTICES:
            notice = (b"Native-engine notice: " + name.encode() + b"\n")
            self.write(self.build / "StreamingAssets/ThirdParty/Stockfish" / name, notice)
            self.write(self.root / "game/Assets/StreamingAssets/ThirdParty/Stockfish" / name, notice)
        self.write(self.build / "index.html", (
            b'<!doctype html><canvas id="unity-canvas"></canvas><script>'
            b"const config={dataUrl:'Build/Web.data.gz',frameworkUrl:'Build/Web.framework.js.gz',"
            b"codeUrl:'Build/Web.wasm.gz'};script.src='Build/Web.loader.js';createUnityInstance();</script>"
        ))
        self.write(self.build / "Build/Web.loader.js", b"function createUnityInstance() {}\n")
        self.write(self.build / "Build/Web.data.gz", gzip.compress(b"UnityWebData1.0\0fixture", mtime=0))
        self.write(self.build / "Build/Web.framework.js.gz", gzip.compress(b"function unityFramework() {}", mtime=0))
        self.write(self.build / "Build/Web.wasm.gz", gzip.compress(b"\0asm\x01\0\0\0", mtime=0))
        self.git("add", ".")
        self.git("-c", "user.name=Release Fixture", "-c", "user.email=release-fixture@localhost",
                 "commit", "--quiet", "-m", "Isolated release fixture")
        self.revision = self.git("rev-parse", "HEAD").strip()

    @staticmethod
    def write(path, data):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def git(self, *args):
        return subprocess.check_output(["git", "-C", str(self.root), *args], text=True, stderr=subprocess.DEVNULL)

    def pack(self, output=None, **kwargs):
        return RELEASE.pack(self.build, output or self.output, self.revision, repo_root=self.root, **kwargs)

    def runtime_root(self):
        manifest = json.loads((self.output / "context/manifest.json").read_text())
        return self.output / "context/web/jogar/runtime" / manifest["build_id"]

    def verify(self, context=None, revision=None):
        return RELEASE.verify(context or self.output / "context", revision or self.revision, repo_root=self.root)

    def custom_archive(self, members):
        path = self.root / ".local/custom.tar.gz"
        path.parent.mkdir(exist_ok=True)
        with tarfile.open(path, "w:gz", format=tarfile.USTAR_FORMAT) as bundle:
            for info, data in members:
                bundle.addfile(info, io.BytesIO(data) if data is not None else None)
        return path

    def reject_archive(self, members, pattern):
        path = self.custom_archive(members)
        target = self.root / ".local/extracted"
        with self.assertRaisesRegex(RELEASE.ReleaseError, pattern):
            RELEASE.unpack(path, RELEASE.sha256(path), target, self.revision, repo_root=self.root)
        self.assertFalse(target.exists(), "Unsafe archives must be rejected before creating output")

    @staticmethod
    def member(name, data=b"content"):
        info = tarfile.TarInfo(name)
        info.size = len(data)
        return info, data

    def test_pack_verify_unpack_round_trip_with_exact_public_metadata(self):
        archive, checksum, manifest = self.pack()
        self.assertEqual(checksum, RELEASE.sha256(archive))
        self.assertEqual(archive.name, f"chess-web-{self.revision[:12]}.tar.gz")
        public = json.loads((self.output / "context/web/jogar/runtime/release.json").read_text())
        self.assertEqual(set(public), RELEASE.PUBLIC_KEYS)
        self.assertEqual(public["source_revision"], self.revision)
        self.assertEqual(public["unity_version"], "6000.3.16f1")
        self.assertEqual(public["initial_download_bytes"], RELEASE.launch_manifest(manifest["build_id"], manifest["runtimes"][manifest["build_id"]])["initialBytes"])
        self.assertEqual(public["retained_build_ids"], [manifest["build_id"]])
        self.assertNotIn(str(self.root), json.dumps(manifest))
        self.assertEqual(len(manifest["files"]), len(RELEASE.context_files(manifest)) - 1)
        self.assertEqual(set(manifest["recipes"]), set(RELEASE.RECIPES))
        self.assertEqual(self.verify(), manifest)
        target = self.root / ".local/unpacked"
        self.assertEqual(RELEASE.unpack(archive, checksum, target, self.revision, repo_root=self.root), target)
        self.assertEqual(self.verify(target), manifest)
        with tarfile.open(archive, "r:gz") as bundle:
            self.assertEqual(set(bundle.getnames()), RELEASE.context_files(manifest))
            self.assertTrue(all(member.isfile() and member.uid == 0 and member.mtime == 0 for member in bundle))

    def test_archives_are_reproducible_without_local_timestamps_or_paths(self):
        _, first, _ = self.pack()
        _, second, _ = self.pack(self.root / ".local/second-release")
        self.assertEqual(first, second)

    def test_existing_output_is_rejected_and_preserved(self):
        self.output.mkdir(parents=True)
        sentinel = self.output / "preserve.txt"
        sentinel.write_text("preserve")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "already exists"):
            self.pack()
        self.assertEqual(sentinel.read_text(), "preserve")

    def test_wrong_revision_and_dirty_checkout_are_rejected(self):
        with self.assertRaisesRegex(RELEASE.ReleaseError, "does not match"):
            RELEASE.pack(self.build, self.output, "0" * 40, repo_root=self.root)
        (self.root / "tracked.txt").write_text("unreviewed mutation")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "dirty"):
            self.pack()
        self.assertFalse(self.output.exists())

    def test_untracked_source_is_also_rejected(self):
        (self.root / "new-source.cs").write_text("unreviewed")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "dirty"):
            self.pack()

    def test_build_outside_checkout_and_output_inside_build_are_rejected(self):
        with self.assertRaisesRegex(RELEASE.ReleaseError, "inside this checkout"):
            RELEASE.pack(self.root.parent, self.output, self.revision, repo_root=self.root)
        with self.assertRaisesRegex(RELEASE.ReleaseError, "outside the input build"):
            self.pack(self.build / "release-output")

    def test_unexpected_private_file_and_missing_asset_are_rejected(self):
        secret = self.build / ".env"
        secret.write_text("SECRET=do-not-package")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Unexpected file"):
            self.pack()
        secret.unlink()
        (self.build / "Build/Web.wasm.gz").unlink()
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Missing files"):
            self.pack()

    def test_payload_symlinks_and_symlinked_build_ancestors_are_rejected(self):
        loader = self.build / "Build/Web.loader.js"
        loader.unlink()
        loader.symlink_to(self.root / "tracked.txt")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Symbolic links"):
            self.pack()
        alias = self.root / "Builds/Alias"
        alias.symlink_to(self.build, target_is_directory=True)
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Symbolic links"):
            RELEASE.pack(alias, self.output, self.revision, repo_root=self.root)

    def test_unity_template_placeholder_and_private_key_marker_are_rejected(self):
        index = self.build / "index.html"
        original = index.read_bytes()
        index.write_bytes(original + b"{{{ PRODUCT_NAME }}}")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "placeholders"):
            self.pack()
        index.write_bytes(original + b"-----BEGIN PRIVATE KEY-----")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "private-key"):
            self.pack()

    def test_invalid_truncated_empty_and_oversized_gzip_are_rejected(self):
        path = self.build / "Build/Web.wasm.gz"
        original = path.read_bytes()
        path.write_bytes(original[:-4])
        with self.assertRaisesRegex(RELEASE.ReleaseError, "truncated gzip"):
            self.pack()
        path.write_bytes(gzip.compress(b"", mtime=0))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "empty"):
            self.pack()
        path.write_bytes(original)
        with patch.object(RELEASE, "MAX_GZIP_BYTES", 4):
            with self.assertRaisesRegex(RELEASE.ReleaseError, "decoded limit"):
                self.pack()

    def test_gzip_wasm_requires_magic_and_version(self):
        (self.build / "Build/Web.wasm.gz").write_bytes(gzip.compress(b"wrong WASM", mtime=0))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "WebAssembly magic"):
            self.pack()

    def test_stockfish_pins_cannot_be_replaced_by_a_self_consistent_manifest(self):
        path = self.build / "stockfish/stockfish-18-lite-single.js"
        path.write_bytes(b"substituted worker")
        manifest_path = self.build / "stockfish/manifest.json"
        manifest = json.loads(manifest_path.read_text())
        manifest["files"][path.name].update(RELEASE.file_record(path))
        manifest_path.write_bytes(RELEASE.canonical_json(manifest))
        shutil.copyfile(manifest_path, self.build / "StreamingAssets/ThirdParty/Stockfish/manifest.json")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "pinned checksum"):
            self.pack()

    def test_stockfish_sources_and_tracked_streaming_notices_are_required(self):
        notice = self.build / "StreamingAssets/ThirdParty/Stockfish/SOURCE.txt"
        notice.write_text("incorrect corresponding source")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "notice differs"):
            self.pack()

    def test_verify_rejects_runtime_tampering_and_manifest_revision_change(self):
        self.pack()
        loader = self.runtime_root() / "Build/Web.loader.js"
        original = loader.read_bytes()
        loader.write_bytes(original + b"tampering")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "metadata|Manifest"):
            self.verify()
        loader.write_bytes(original)
        manifest_path = self.output / "context/manifest.json"
        manifest = json.loads(manifest_path.read_text())
        manifest["source_revision"] = "0" * 40
        manifest_path.write_bytes(RELEASE.canonical_json(manifest))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Manifest"):
            self.verify()

    def test_verify_rejects_recipe_tampering_even_with_updated_manifest(self):
        self.pack()
        context = self.output / "context"
        recipe = context / "deploy/web/Dockerfile"
        recipe.write_text("FROM untrusted-image:latest\n")
        manifest_path = context / "manifest.json"
        manifest = json.loads(manifest_path.read_text())
        manifest["recipes"]["deploy/web/Dockerfile"] = RELEASE.file_record(recipe)
        manifest_path.write_bytes(RELEASE.canonical_json(manifest))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "recipe differs|Manifest differs"):
            self.verify()

    def test_verify_rejects_extra_missing_symlink_and_private_public_metadata(self):
        self.pack()
        context = self.output / "context"
        extra = context / "web/private.env"
        extra.write_text("do not ship")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Unexpected file"):
            self.verify()
        extra.unlink()
        loader = self.runtime_root() / "Build/Web.loader.js"
        original = loader.read_bytes()
        loader.unlink()
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Missing files"):
            self.verify()
        loader.symlink_to(self.build / "Build/Web.loader.js")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Symbolic links"):
            self.verify()
        loader.unlink()
        loader.write_bytes(original)
        public_path = context / "web/jogar/runtime/release.json"
        public = json.loads(public_path.read_text())
        public["private_local_path"] = str(self.root)
        public_path.write_bytes(RELEASE.canonical_json(public))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Manifest|Public release metadata"):
            self.verify()

    def test_duplicate_json_keys_and_boolean_schema_version_are_rejected(self):
        self.pack()
        path = self.output / "context/manifest.json"
        original = path.read_bytes()
        path.write_bytes(b'{"schema_version": 99, ' + original[1:])
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Duplicate JSON key"):
            self.verify()
        manifest = json.loads(original)
        manifest["schema_version"] = True
        path.write_bytes(RELEASE.canonical_json(manifest))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Manifest"):
            self.verify()

    def test_archive_checksum_is_checked_before_tar_parsing_or_output_creation(self):
        path = self.root / ".local/not-a-tar.gz"
        self.write(path, b"not an archive")
        with patch.object(RELEASE.tarfile, "open", side_effect=AssertionError("Parsed before hash verification")):
            with self.assertRaisesRegex(RELEASE.ReleaseError, "SHA256 mismatch"):
                RELEASE.unpack(path, "0" * 64, self.output, self.revision, repo_root=self.root)
        self.assertFalse(self.output.exists())

    def test_archive_path_traversal_absolute_path_and_unknown_file_are_rejected(self):
        for name in ("../escaped.txt", "/tmp/escaped.txt", "web/../manifest.json", "web/.env", "web//index.html"):
            with self.subTest(name=name):
                self.reject_archive([self.member(name)], "unsafe|unexpected|exact release file set")
        self.assertFalse((self.root / "escaped.txt").exists())

    def test_archive_symlinks_hardlinks_special_files_and_duplicates_are_rejected(self):
        for kind in (tarfile.SYMTYPE, tarfile.LNKTYPE, tarfile.FIFOTYPE):
            with self.subTest(kind=kind):
                info = tarfile.TarInfo("web/jogar/runtime/" + "a" * 64 + "/Build/Web.loader.js")
                info.type = kind
                info.linkname = "../../outside"
                self.reject_archive([(info, None)], "links/special")
        self.reject_archive([self.member("web/jogar/runtime/" + "a" * 64 + "/Build/Web.loader.js"), self.member("web/jogar/runtime/" + "a" * 64 + "/Build/Web.loader.js")], "duplicate")

    def test_archive_directories_cannot_hide_oversized_data(self):
        info = tarfile.TarInfo("web")
        info.type = tarfile.DIRTYPE
        info.size = 4
        self.reject_archive([(info, b"data")], "zero size")

    def test_archive_size_limits_and_missing_members_are_rejected(self):
        self.reject_archive([self.member("web/jogar/runtime/" + "a" * 64 + "/Build/Web.loader.js")], "exact release file set")
        with patch.object(RELEASE, "MAX_CONTEXT_BYTES", 2):
            self.reject_archive([self.member("web/jogar/runtime/" + "a" * 64 + "/Build/Web.loader.js")], "unpacked limit")
        archive, checksum, _ = self.pack()
        with patch.object(RELEASE, "MAX_ARCHIVE_BYTES", 2):
            with self.assertRaisesRegex(RELEASE.ReleaseError, "compressed limit"):
                RELEASE.unpack(archive, checksum, self.root / ".local/too-large", self.revision, repo_root=self.root)

    def test_unpack_rejects_wrong_revision_and_preserves_existing_output(self):
        archive, checksum, _ = self.pack()
        with self.assertRaisesRegex(RELEASE.ReleaseError, "does not match"):
            RELEASE.unpack(archive, checksum, self.root / ".local/wrong-revision", "0" * 40, repo_root=self.root)
        target = self.root / ".local/preserved"
        target.mkdir()
        sentinel = target / "sentinel"
        sentinel.write_text("preserve")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "already exists"):
            RELEASE.unpack(archive, checksum, target, self.revision, repo_root=self.root)
        self.assertEqual(sentinel.read_text(), "preserve")

    def test_brotli_roundtrip_and_original_input_inventory_are_recorded(self):
        _, _, manifest = self.pack()
        runtime = manifest["runtimes"][manifest["build_id"]]
        self.assertRegex(manifest["build_id"], r"^[0-9a-f]{64}$")
        self.assertEqual(runtime["source_revision"], self.revision)
        self.assertEqual(runtime["compression"], {"encoding": "br", "quality": 9, "roundtrip_verified": True})
        for source, name in RELEASE.RUNTIME_MAP.items():
            with self.subTest(name=name):
                raw = (self.build / source).read_bytes()
                decoded = gzip.decompress(raw) if source.startswith("Build/") and source.endswith(".gz") else raw
                self.assertEqual(runtime["source_files"][source], RELEASE.file_record(self.build / source))
                self.assertEqual(runtime["files"][name]["decoded"], {
                    "bytes": len(decoded), "sha256": hashlib.sha256(decoded).hexdigest()})
                if name in RELEASE.BROTLI_FILES:
                    self.assertEqual(runtime["files"][name]["encoding"], "br")
        self.assertEqual(runtime["files"]["Build/Web.wasm.br"]["content_type"], "application/wasm")
        self.assertEqual(runtime["files"]["stockfish/stockfish.js-18.0.0-source.tar.gz"]["encoding"], "identity")

    def test_namespace_commits_to_encoded_bytes_and_headers(self):
        _, _, manifest = self.pack()
        runtime = manifest["runtimes"][manifest["build_id"]]
        item = runtime["files"]["Build/Web.loader.js"]
        for key, changed in (("encoding", "identity"), ("content_type", "text/plain"), ("sha256", "0" * 64)):
            original = item[key]
            item[key] = changed
            self.assertNotEqual(RELEASE.runtime_id(runtime), manifest["build_id"])
            item[key] = original

    def test_previous_requires_authenticated_manifest_and_valid_payload(self):
        self.pack()
        previous = self.output / "context"
        digest = RELEASE.sha256(previous / "manifest.json")
        for kwargs, error in (({"previous_context": previous}, "provided together"),
                              ({"previous_sha256": digest}, "provided together"),
                              ({"previous_context": previous, "previous_sha256": "0" * 64}, "SHA256 mismatch")):
            with self.assertRaisesRegex(RELEASE.ReleaseError, error):
                self.pack(self.root / ".local/second", **kwargs)
        (self.runtime_root() / "Build/Web.wasm.br").write_bytes(b"mutated previous")
        with self.assertRaisesRegex(RELEASE.ReleaseError, "Manifest differs"):
            self.pack(self.root / ".local/second", previous_context=previous, previous_sha256=digest)

    def test_retention_keeps_only_active_previous_and_supports_old_source_revision(self):
        _, _, first = self.pack()
        first_context = self.output / "context"
        self.write(self.build / "Build/Web.loader.js", b"function createUnityInstance() { return 2; }\n")
        (self.root / "tracked.txt").write_text("next reviewed revision\n")
        self.git("add", "tracked.txt")
        self.git("-c", "user.name=Release Fixture", "-c", "user.email=release-fixture@localhost",
                 "commit", "--quiet", "-m", "Second source revision")
        self.revision = self.git("rev-parse", "HEAD").strip()
        second_output = self.root / ".local/second"
        _, _, second = self.pack(second_output, previous_context=first_context,
            previous_sha256=RELEASE.sha256(first_context / "manifest.json"))
        self.assertEqual(set(second["runtimes"]), {first["build_id"], second["build_id"]})
        self.assertNotEqual(first["source_revision"], second["source_revision"])
        self.write(self.build / "Build/Web.loader.js", b"function createUnityInstance() { return 3; }\n")
        third_output = self.root / ".local/third"
        _, _, third = self.pack(third_output, previous_context=second_output / "context",
            previous_sha256=RELEASE.sha256(second_output / "context/manifest.json"))
        self.assertEqual(set(third["runtimes"]), {second["build_id"], third["build_id"]})
        self.assertNotIn(first["build_id"], third["runtimes"])
        self.assertEqual(self.verify(third_output / "context"), third)

    def test_identical_runtime_is_deduplicated_when_reusing_previous(self):
        _, _, first = self.pack()
        _, _, second = self.pack(self.root / ".local/second", previous_context=self.output / "context",
            previous_sha256=RELEASE.sha256(self.output / "context/manifest.json"))
        self.assertEqual(first["build_id"], second["build_id"])
        self.assertEqual(len(second["runtimes"]), 1)

    def test_generated_whitelist_cannot_be_replaced_even_with_updated_file_record(self):
        _, _, manifest = self.pack()
        context = self.output / "context"
        path = context / RELEASE.GENERATED_RECIPE
        path.write_bytes(path.read_bytes() + b"location / { try_files $uri /index.html; }\n")
        manifest["files"][RELEASE.GENERATED_RECIPE] = RELEASE.file_record(path)
        (context / "manifest.json").write_bytes(RELEASE.canonical_json(manifest))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "whitelist"):
            self.verify()

    def test_runtime_hash_or_decoded_digest_tampering_is_rejected(self):
        _, _, manifest = self.pack()
        context = self.output / "context"
        runtime = manifest["runtimes"][manifest["build_id"]]
        runtime["files"]["Build/Web.wasm.br"]["decoded"]["sha256"] = "0" * 64
        (context / "manifest.json").write_bytes(RELEASE.canonical_json(manifest))
        with self.assertRaisesRegex(RELEASE.ReleaseError, "decoded/roundtrip mismatch"):
            self.verify()

    def test_brotli_decode_limit_is_bounded(self):
        self.pack()
        with patch.object(RELEASE, "MAX_GZIP_BYTES", 4):
            with self.assertRaisesRegex(RELEASE.ReleaseError, "Brotli inspect/roundtrip"):
                RELEASE.brotli("inspect", self.runtime_root() / "Build/Web.loader.js", repo_root=self.root)


if __name__ == "__main__":
    unittest.main()
