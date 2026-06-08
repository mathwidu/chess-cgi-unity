from pathlib import Path
import unittest

from tools.character_pipeline.audit_custom_pieces import build_audit


class CustomPieceAuditTests(unittest.TestCase):
    def test_audit_lists_all_six_current_custom_prefabs(self):
        root = Path(__file__).resolve().parents[3]
        audit = build_audit(root)
        names = {entry["prefab"] for entry in audit["pieces"]}

        self.assertEqual(
            {
                "Pawn_Mathwidu_v3b",
                "Rook_Alex",
                "Knight_Gustavo",
                "Bishop_Rafael",
                "Queen_Marta",
                "King_Ricardo_Carioca",
            },
            names,
        )

    def test_audit_marks_credit_spend_as_blocked(self):
        root = Path(__file__).resolve().parents[3]
        audit = build_audit(root)

        self.assertEqual("blocked_without_user_confirmation", audit["creditSpendPolicy"])

    def test_audit_confirms_active_prefabs_and_selected_glbs_exist(self):
        root = Path(__file__).resolve().parents[3]
        audit = build_audit(root)

        for entry in audit["pieces"]:
            self.assertTrue(entry["prefabExists"], entry["prefab"])
            self.assertTrue(entry["selectedGlbExists"], entry["prefab"])


if __name__ == "__main__":
    unittest.main()

