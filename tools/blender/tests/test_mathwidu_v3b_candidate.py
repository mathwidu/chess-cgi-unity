import json
import unittest
from pathlib import Path


class MathwiduV3bCandidateTests(unittest.TestCase):
    def test_v3b_manifest_tracks_approved_unity_import_candidate(self):
        manifest_path = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/character_quality_manifest.json")
        self.assertTrue(manifest_path.exists())

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

        self.assertEqual("Pawn_Mathwidu_v3b", manifest["candidateId"])
        self.assertEqual("Mathwidu", manifest["personName"])
        self.assertEqual("Pawn", manifest["pieceKind"])
        self.assertEqual("unity_import_approved", manifest["visualStatus"])
        self.assertEqual("rigged_candidate_initial", manifest["rigStatus"])
        self.assertTrue(manifest["approvedForUnity"])
        self.assertTrue(manifest["replacesActivePrefab"])
        self.assertEqual(
            "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb",
            manifest["candidateModelPath"],
        )
        self.assertEqual(
            "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b.prefab",
            manifest["importedPrefabPath"],
        )
        self.assertIn("armature-added", manifest["technicalChecklist"])
        self.assertIn("approved for Unity pawn prefab test", manifest["technicalChecklist"])

    def test_v3b_imported_resource_prefab_exists(self):
        prefab = Path("game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b.prefab")
        model = Path("game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b_Assets/selected.glb")

        self.assertTrue(prefab.exists())
        self.assertTrue(model.exists())
        self.assertGreater(prefab.stat().st_size, 100)
        self.assertGreater(model.stat().st_size, 10_000)

    def test_v3b_candidate_glb_is_generated_for_blender_review(self):
        candidate_model = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb")

        self.assertTrue(candidate_model.exists())
        self.assertGreater(candidate_model.stat().st_size, 10_000)

    def test_v3b_candidate_glb_does_not_export_debug_geometry(self):
        candidate_model = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb")

        self.assertTrue(candidate_model.exists())
        raw = candidate_model.read_bytes()
        self.assertNotIn(b"Icosphere", raw)

    def test_v3b_candidate_exports_skinned_white_sneakers(self):
        manifest_path = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/character_quality_manifest.json")
        candidate_model = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb")

        self.assertTrue(manifest_path.exists())
        self.assertTrue(candidate_model.exists())

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        self.assertIn("white sneaker geometry added", manifest["technicalChecklist"])
        self.assertIn("sneakers skinned to foot bones", manifest["technicalChecklist"])

        raw = candidate_model.read_bytes()
        self.assertTrue(b"Pawn_Mathwidu_v3b_Shoe.L" in raw, "left sneaker mesh name was not exported")
        self.assertTrue(b"Pawn_Mathwidu_v3b_Shoe.R" in raw, "right sneaker mesh name was not exported")

    def test_v3b_candidate_exports_semantic_team_outfit_materials(self):
        candidate_model = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb")

        self.assertTrue(candidate_model.exists())

        raw = candidate_model.read_bytes()
        self.assertTrue(b"TeamOutfitPrimary" in raw, "team outfit primary material was not exported")
        self.assertTrue(b"Pawn_Mathwidu_v3b_TeamOutfit_Shirt" in raw, "team outfit shirt overlay was not exported")

    def test_side_variant_glbs_do_not_export_debug_geometry(self):
        for side in ("White", "Black"):
            with self.subTest(side=side):
                candidate_model = Path(
                    f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/Pawn_Mathwidu_{side}.glb"
                )

                self.assertTrue(candidate_model.exists())
                raw = candidate_model.read_bytes()

                self.assertNotIn(b"Icosphere", raw)

    def test_side_variant_glbs_recolor_existing_clothing_textures_only(self):
        for side in ("White", "Black"):
            with self.subTest(side=side):
                candidate_model = Path(
                    f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/Pawn_Mathwidu_{side}.glb"
                )
                manifest_path = Path(
                    f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/character_quality_manifest.json"
                )

                self.assertTrue(candidate_model.exists())
                self.assertTrue(manifest_path.exists())

                manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
                raw = candidate_model.read_bytes()

                self.assertEqual(side, manifest["side"])
                self.assertTrue(bytes(f"Pawn_Mathwidu_{side}_BodyTextureUniform", "utf-8") in raw)
                self.assertTrue(bytes(f"Pawn_Mathwidu_{side}_UniformTexture", "utf-8") in raw)
                self.assertTrue(b"WeaponSocket" in raw, "combat preparation socket was not exported")

                for pasted_or_floating_token in (
                    b"Cube",
                    b"TeamOutfit_Shirt",
                    b"TeamOutfitPrimary",
                    b"IntegratedShirt",
                    b"IntegratedPants",
                    b"Integrated",
                    b"AuthoredJacket",
                    b"LeftLapel",
                    b"RightLapel",
                    b"SideAccentStripe",
                    b"PocketDagger",
                    b"BeltLine",
                    b"IntegratedCollar",
                    b"IntegratedSleeve",
                    b"IntegratedSideMark",
                ):
                    self.assertNotIn(
                        pasted_or_floating_token,
                        raw,
                        f"{pasted_or_floating_token!r} should not be exported as pasted-on outfit geometry",
                    )


if __name__ == "__main__":
    unittest.main()
