import json
import unittest
from pathlib import Path


class CharacterQualityManifestTests(unittest.TestCase):
    def test_mathwidu_v3a_manifest_preserves_fallback_until_import_approval(self):
        manifest_path = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json")
        self.assertTrue(manifest_path.exists())

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

        self.assertEqual("Pawn_Mathwidu_v3a", manifest["candidateId"])
        self.assertEqual("Mathwidu", manifest["personName"])
        self.assertEqual("Pawn", manifest["pieceKind"])
        self.assertEqual("approved_current_visual_baseline", manifest["visualStatus"])
        self.assertEqual("rig_candidate", manifest["rigStatus"])
        self.assertFalse(manifest["approvedForUnity"])
        self.assertFalse(manifest["replacesActivePrefab"])
        self.assertIn("ginger curly short hair", manifest["identityChecklist"])
        self.assertIn("light ginger beard and mustache", manifest["identityChecklist"])
        self.assertIn("not primitive-built", manifest["technicalChecklist"])


if __name__ == "__main__":
    unittest.main()
