import json
import unittest
from pathlib import Path


class AllPieceSideVariantTests(unittest.TestCase):
    PIECES = {
        "Pawn_Mathwidu": "Pawn",
        "Bishop_Rafael": "Bishop",
        "Rook_Alex": "Rook",
        "Knight_Gustavo": "Knight",
        "Queen_Marta": "Queen",
        "King_Ricardo_Carioca": "King",
    }

    def test_all_custom_pieces_export_texture_only_side_variants(self):
        for piece_name, piece_kind in self.PIECES.items():
            for side in ("White", "Black"):
                with self.subTest(piece_name=piece_name, side=side):
                    side_dir = Path(f"game/Assets/Art/CharacterCandidates/{piece_name}/side_variants/{side}")
                    model = side_dir / f"{piece_name}_{side}.glb"
                    manifest_path = side_dir / "character_quality_manifest.json"

                    self.assertTrue(model.exists(), f"{model} should exist")
                    self.assertTrue(manifest_path.exists(), f"{manifest_path} should exist")

                    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
                    raw = model.read_bytes()

                    self.assertEqual(f"{piece_name}_{side}", manifest["candidateId"])
                    self.assertEqual(piece_kind, manifest["pieceKind"])
                    self.assertEqual(side, manifest["side"])
                    self.assertTrue(manifest["approvedForUnity"])
                    self.assertTrue(manifest["textureRecolorOnly"])
                    self.assertGreater(manifest["textureRecolorStats"]["shirtPixels"], 1000)
                    self.assertTrue(bytes(f"{piece_name}_{side}_BodyTextureUniform", "utf-8") in raw)
                    self.assertTrue(bytes(f"{piece_name}_{side}_UniformTexture", "utf-8") in raw)
                    self.assertTrue(b"WeaponSocket" in raw)

                    for pasted_or_floating_token in (
                        b"TeamOutfit_Shirt",
                        b"IntegratedShirt",
                        b"IntegratedPants",
                        b"AuthoredJacket",
                        b"SideAccentStripe",
                        b"RuntimeUniform",
                    ):
                        self.assertNotIn(
                            pasted_or_floating_token,
                            raw,
                            f"{pasted_or_floating_token!r} should not be exported as pasted-on outfit geometry",
                        )


if __name__ == "__main__":
    unittest.main()
