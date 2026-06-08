from __future__ import annotations

import json
from pathlib import Path


CUSTOM_PREFABS = [
    ("Pawn_Mathwidu_v3b", "Pawn", "Mathwidu"),
    ("Rook_Alex", "Rook", "Alex"),
    ("Knight_Gustavo", "Knight", "Gustavo"),
    ("Bishop_Rafael", "Bishop", "Rafael"),
    ("Queen_Marta", "Queen", "Marta"),
    ("King_Ricardo_Carioca", "King", "Ricardo Carioca"),
]


def build_audit(repo_root: Path) -> dict:
    resources = repo_root / "game" / "Assets" / "Resources" / "CustomPieces"
    pieces = []

    for prefab, kind, person in CUSTOM_PREFABS:
        prefab_path = resources / f"{prefab}.prefab"
        asset_dir = resources / f"{prefab}_Assets"
        glb_path = asset_dir / "selected.glb"
        text = prefab_path.read_text(errors="ignore") if prefab_path.exists() else ""
        pieces.append(
            {
                "prefab": prefab,
                "kind": kind,
                "person": person,
                "prefabExists": prefab_path.exists(),
                "selectedGlbExists": glb_path.exists(),
                "hasTeamBaseToken": "TeamBase" in text,
                "hasTeamOutfitToken": _has_team_outfit_token(text),
                "hasAnimatorToken": "Animator" in text,
            }
        )

    return {
        "creditSpendPolicy": "blocked_without_user_confirmation",
        "pieces": pieces,
    }


def _has_team_outfit_token(text: str) -> bool:
    return "TeamOutfit" in text or "TeamClothes" in text or "TeamUniform" in text


def main() -> int:
    repo_root = Path(__file__).resolve().parents[2]
    print(json.dumps(build_audit(repo_root), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

