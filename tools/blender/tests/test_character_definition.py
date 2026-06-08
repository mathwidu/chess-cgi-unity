import json
import unittest
from pathlib import Path


class CharacterDefinitionTests(unittest.TestCase):
    def test_mathwidu_pawn_v2_declares_premium_rig_ready_contract(self):
        definition_path = Path('tools/blender/definitions/mathwidu_pawn_v2.json')
        self.assertTrue(definition_path.exists(), 'Mathwidu pawn v2 definition should be versioned separately')

        definition = json.loads(definition_path.read_text(encoding='utf-8'))

        self.assertEqual('mathwidu_pawn_v2', definition['id'])
        self.assertEqual('MathwiduPawnV2', definition['outputName'])
        self.assertEqual('premium_stylized_cartoon', definition['styleTarget'])
        self.assertFalse(definition['presentation']['hasIntegratedBase'])
        self.assertTrue(definition['rigging']['requiresSeparatedFeet'])
        self.assertTrue(definition['rigging']['requiresSeparatedHands'])
        self.assertIn('ginger/red curly short hair', definition['identityCues'])
        self.assertIn('light ginger beard and mustache', definition['identityCues'])

        required_parts = {
            'TorsoRoot',
            'HeadRoot',
            'LeftArmRoot',
            'RightArmRoot',
            'LeftLegRoot',
            'RightLegRoot',
            'LeftFootRoot',
            'RightFootRoot',
            'EffectsSocket',
            'HitSocket',
            'GroundSocket',
            'WeaponSocket',
            'RightHandSocket',
            'LeftHandSocket',
            'CastSocket',
        }
        self.assertTrue(required_parts.issubset(set(definition['rigging']['requiredNodes'])))
        self.assertIn('White', definition['sideVariants'])
        self.assertIn('Black', definition['sideVariants'])
        self.assertEqual('Capture_Pawn_DaggerLunge', definition['combatPreparation']['futureCaptureClip'])

    def test_side_variant_combat_preset_declares_all_pieces_and_sockets(self):
        definition_path = Path('tools/blender/definitions/side_variant_combat_preset.json')
        self.assertTrue(definition_path.exists(), 'Side variant combat preset should be versioned')

        preset = json.loads(definition_path.read_text(encoding='utf-8'))

        self.assertEqual('side_variant_combat_preset', preset['id'])
        self.assertTrue(preset['assetNaming']['genericFallbackIsTemporary'])
        self.assertIn('White', preset['sideVariants'])
        self.assertIn('Black', preset['sideVariants'])

        required_sockets = {
            'EffectsSocket',
            'HitSocket',
            'GroundSocket',
            'WeaponSocket',
            'RightHandSocket',
            'LeftHandSocket',
            'CastSocket',
        }
        self.assertTrue(required_sockets.issubset(set(preset['requiredSockets'])))

        expected_pieces = {'Pawn', 'Rook', 'Knight', 'Bishop', 'Queen', 'King'}
        self.assertEqual(expected_pieces, set(preset['pieceCombatPresets'].keys()))
        self.assertEqual('Capture_Rook_TowerCrush', preset['pieceCombatPresets']['Rook']['captureClip'])
        self.assertEqual('Capture_Knight_HorseLeap', preset['pieceCombatPresets']['Knight']['captureClip'])
        self.assertEqual('Capture_Bishop_PrayerBeam', preset['pieceCombatPresets']['Bishop']['captureClip'])


if __name__ == '__main__':
    unittest.main()
