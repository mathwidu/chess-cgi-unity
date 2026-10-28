"""The Unity repo exports runtime identity; only the site owns deployment."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("runtime_plan", ROOT / "tools/deploy_web.py")
plan = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plan)
IMAGE = "ghcr.io/mathwidu/chess-cgi-unity-web@sha256:" + "1" * 64
SHA = "2" * 40
BUILD = "3" * 64


class RuntimePlanTests(unittest.TestCase):
    def test_plan_scopes_runtime_to_existing_site_domain_and_coordinated_owner(self):
        value = plan.render(IMAGE, SHA, BUILD)
        self.assertEqual(value["runtime_image"], IMAGE)
        self.assertEqual(value["service"], "cicochess-site_game")
        self.assertEqual(value["deployment_owner"], "mathwidu/cicochess-site")
        self.assertEqual(value["route"], "/jogar/runtime/")
        self.assertFalse(value["dns_changes"])
        self.assertTrue(value["site_release_required"])

    def test_tags_wrong_namespace_and_path_injection_are_rejected(self):
        for image, sha, build in ((IMAGE.replace("@sha256:", ":"), SHA, BUILD),
                                  (IMAGE.replace("mathwidu", "other"), SHA, BUILD),
                                  (IMAGE, "main", BUILD), (IMAGE, SHA, "../public")):
            with self.subTest(image=image, sha=sha, build=build), self.assertRaises(ValueError):
                plan.render(image, sha, build)

    def test_legacy_apply_is_refused_without_connecting_anywhere(self):
        result = subprocess.run([sys.executable, str(ROOT / "tools/deploy_web.py"), "apply",
                                 "--image", IMAGE, "--revision", SHA, "--build-id", BUILD],
                                capture_output=True, text=True, timeout=10)
        self.assertEqual(result.returncode, 2)
        self.assertIn("invalid choice", result.stderr)
        self.assertFalse((ROOT / "deploy/web/activate_after_dns.py").exists())
