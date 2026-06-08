#!/usr/bin/env python3
"""Create white/black texture-authored side variants for all custom chess pieces.

The variants are intentionally texture-only for team identity. This avoids
floating props or pasted-on geometric clothing and keeps the assets ready for
later rigging/animation passes.
"""

import json
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
CUSTOM_PIECES_ROOT = PROJECT_ROOT / "game/Assets/Resources/CustomPieces"
OUTPUT_ROOT = PROJECT_ROOT / "game/Assets/Art/CharacterCandidates"

PIECES = [
    {
        "id": "Pawn_Mathwidu",
        "person": "Mathwidu",
        "kind": "Pawn",
        "source": CUSTOM_PIECES_ROOT / "Pawn_Mathwidu_v3b_Assets/selected.glb",
        "rig_status": "rigged_candidate_initial",
        "future_capture_clip": "Capture_Pawn_DaggerLunge",
        "weapon_concept": "socket-only dagger concept; no visible idle weapon prop",
        "upper": (0.40, 0.82),
        "lower": (0.02, 0.56),
        "source_prefix": "Pawn_Mathwidu_v3b",
    },
    {
        "id": "Bishop_Rafael",
        "person": "Rafael",
        "kind": "Bishop",
        "source": CUSTOM_PIECES_ROOT / "Bishop_Rafael_Assets/selected.glb",
        "rig_status": "static_mesh_candidate",
        "future_capture_clip": "Capture_Bishop_PrayerBeam",
        "weapon_concept": "CastSocket for future prayer/laser effect",
        "upper": (0.42, 0.86),
        "lower": (0.08, 0.58),
    },
    {
        "id": "Rook_Alex",
        "person": "Alex",
        "kind": "Rook",
        "source": CUSTOM_PIECES_ROOT / "Rook_Alex_Assets/selected.glb",
        "rig_status": "static_mesh_candidate",
        "future_capture_clip": "Capture_Rook_TowerCrush",
        "weapon_concept": "Impact sockets for future tower-drop hit",
        "upper": (0.48, 0.91),
        "lower": (0.20, 0.62),
    },
    {
        "id": "Knight_Gustavo",
        "person": "Gustavo",
        "kind": "Knight",
        "source": CUSTOM_PIECES_ROOT / "Knight_Gustavo_Assets/selected.glb",
        "rig_status": "static_mesh_candidate",
        "future_capture_clip": "Capture_Knight_HorseCharge",
        "weapon_concept": "HitSocket and CastSocket for future horse neigh/charge",
        "upper": (0.50, 0.92),
        "lower": (0.20, 0.58),
    },
    {
        "id": "Queen_Marta",
        "person": "Marta",
        "kind": "Queen",
        "source": CUSTOM_PIECES_ROOT / "Queen_Marta_Assets/selected.glb",
        "rig_status": "static_mesh_candidate",
        "future_capture_clip": "Capture_Queen_SwordCleave",
        "weapon_concept": "WeaponSocket for future sword apparition",
        "upper": (0.36, 0.86),
        "lower": (0.04, 0.58),
    },
    {
        "id": "King_Ricardo_Carioca",
        "person": "Ricardo Carioca",
        "kind": "King",
        "source": CUSTOM_PIECES_ROOT / "King_Ricardo_Carioca_Assets/selected.glb",
        "rig_status": "static_mesh_candidate",
        "future_capture_clip": "Capture_King_OpenHandStrike",
        "weapon_concept": "RightHandSocket for future open-hand strike",
        "upper": (0.36, 0.88),
        "lower": (0.04, 0.58),
    },
]

VARIANTS = {
    "White": {
        "upper": (0.94, 0.94, 0.90),
        "lower": (0.82, 0.82, 0.76),
        "strength_upper": 0.88,
        "strength_lower": 0.80,
        "description": "white-side texture recolor over existing clothing surfaces",
    },
    "Black": {
        "upper": (0.025, 0.027, 0.032),
        "lower": (0.035, 0.036, 0.040),
        "strength_upper": 0.90,
        "strength_lower": 0.84,
        "description": "black-side texture recolor over existing clothing surfaces",
    },
}

SOCKET_NAMES = [
    "EffectsSocket",
    "HitSocket",
    "GroundSocket",
    "WeaponSocket",
    "RightHandSocket",
    "LeftHandSocket",
    "CastSocket",
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    purge_orphan_data()


def purge_orphan_data():
    for collection in [
        bpy.data.actions,
        bpy.data.armatures,
        bpy.data.images,
        bpy.data.materials,
        bpy.data.meshes,
        bpy.data.textures,
    ]:
        for data_block in list(collection):
            collection.remove(data_block)


def import_source(piece):
    if not piece["source"].exists():
        raise FileNotFoundError(piece["source"])

    bpy.ops.import_scene.gltf(filepath=str(piece["source"]))
    imported = [obj for obj in bpy.context.scene.objects]
    return prune_imported_source_artifacts(imported, piece)


def prune_imported_source_artifacts(imported, piece):
    kept = []
    source_prefix = piece.get("source_prefix")

    for obj in imported:
        if obj.type == "MESH" and "TeamOutfit" in obj.name:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue

        if source_prefix is None:
            kept.append(obj)
            continue

        if obj.name.startswith(source_prefix):
            kept.append(obj)
        else:
            bpy.data.objects.remove(obj, do_unlink=True)

    return kept


def world_bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        for corner in obj.bound_box:
            points.append(obj.matrix_world @ Vector(corner))

    if not points:
        return Vector((-0.5, -0.5, 0.0)), Vector((0.5, 0.5, 1.2))

    return (
        Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points))),
        Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points))),
    )


def make_root(name):
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.18
    bpy.context.collection.objects.link(root)
    return root


def parent_top_level_to_root(root):
    for obj in list(bpy.context.scene.objects):
        if obj == root or obj.parent is not None:
            continue
        obj.parent = root
        obj.matrix_parent_inverse = root.matrix_world.inverted()


def add_socket(root, name, location):
    socket = bpy.data.objects.new(name, None)
    socket.empty_display_type = "PLAIN_AXES"
    socket.empty_display_size = 0.08
    socket.location = location
    socket.parent = root
    bpy.context.collection.objects.link(socket)
    return socket


def add_combat_sockets(root, bounds_min, bounds_max):
    height = max(bounds_max.z - bounds_min.z, 0.001)
    z0 = bounds_min.z
    add_socket(root, "EffectsSocket", (0.0, 0.0, z0 + height * 0.92))
    add_socket(root, "HitSocket", (0.0, -0.02, z0 + height * 0.58))
    add_socket(root, "GroundSocket", (0.0, 0.0, z0))
    add_socket(root, "WeaponSocket", (0.28, -0.04, z0 + height * 0.46))
    add_socket(root, "RightHandSocket", (0.34, -0.03, z0 + height * 0.52))
    add_socket(root, "LeftHandSocket", (-0.34, -0.03, z0 + height * 0.52))
    add_socket(root, "CastSocket", (0.0, -0.16, z0 + height * 0.78))


def find_color_texture_node(material):
    if material is None or material.node_tree is None:
        return None

    preferred = []
    fallback = []
    for node in material.node_tree.nodes:
        if node.bl_idname != "ShaderNodeTexImage" or node.image is None:
            continue
        if node.image.name.startswith("Color_"):
            preferred.append(node)
        else:
            fallback.append(node)

    return (preferred or fallback or [None])[0]


def rasterize_triangle(mask, triangle_uvs):
    height, width = mask.shape
    points = np.array(
        [
            [max(0.0, min(1.0, uv.x)) * (width - 1), max(0.0, min(1.0, uv.y)) * (height - 1)]
            for uv in triangle_uvs
        ],
        dtype=np.float32,
    )
    min_x = max(int(np.floor(points[:, 0].min())), 0)
    max_x = min(int(np.ceil(points[:, 0].max())), width - 1)
    min_y = max(int(np.floor(points[:, 1].min())), 0)
    max_y = min(int(np.ceil(points[:, 1].max())), height - 1)

    if min_x > max_x or min_y > max_y:
        return

    xs = np.arange(min_x, max_x + 1, dtype=np.float32) + 0.5
    ys = np.arange(min_y, max_y + 1, dtype=np.float32) + 0.5
    px, py = np.meshgrid(xs, ys)

    a, b, c = points
    denominator = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
    if abs(float(denominator)) < 1e-6:
        return

    w0 = ((b[1] - c[1]) * (px - c[0]) + (c[0] - b[0]) * (py - c[1])) / denominator
    w1 = ((c[1] - a[1]) * (px - c[0]) + (a[0] - c[0]) * (py - c[1])) / denominator
    w2 = 1.0 - w0 - w1

    inside = (w0 >= -0.001) & (w1 >= -0.001) & (w2 >= -0.001)
    mask[min_y : max_y + 1, min_x : max_x + 1] |= inside


def polygon_vertex_group_weight(obj, polygon, group_prefixes):
    if len(obj.vertex_groups) == 0:
        return 0.0

    total = 0.0
    vertex_count = max(len(polygon.vertices), 1)
    for vertex_index in polygon.vertices:
        vertex = obj.data.vertices[vertex_index]
        for group_weight in vertex.groups:
            group_name = obj.vertex_groups[group_weight.group].name
            if any(group_name.startswith(prefix) for prefix in group_prefixes):
                total += group_weight.weight
    return total / vertex_count


def clothing_color_masks(rgb):
    r = rgb[:, 0]
    g = rgb[:, 1]
    b = rgb[:, 2]
    value = np.max(rgb, axis=1)
    minimum = np.min(rgb, axis=1)
    saturation = np.divide(value - minimum, value, out=np.zeros_like(value), where=value > 1e-5)

    skin_color = ((r - g) > 0.16) & ((g - b) > 0.02) & (value > 0.42) & (saturation > 0.14)
    ginger_hair = (r > g * 1.08) & (g > b * 1.05) & (value < 0.48)
    dark_hair_or_glasses = (value < 0.12) & (saturation < 0.45)
    white_hair = (value > 0.72) & (saturation < 0.16)

    clothing_like = ~skin_color & ~ginger_hair & ~dark_hair_or_glasses
    light_clothing = (value > 0.15) & (saturation < 0.72) & clothing_like
    colorful_clothing = (value > 0.12) & (saturation >= 0.18) & clothing_like
    white_side_safe = light_clothing | colorful_clothing

    return white_side_safe, clothing_like, white_hair


def shade_preserving_recolor(rgb, mask, target_color, strength):
    if not np.any(mask):
        return

    target = np.array(target_color, dtype=np.float32)
    luma = rgb[mask] @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)

    if float(target.mean()) < 0.18:
        shaded_target = np.clip(target + luma[:, None] * 0.12, 0.0, 1.0)
    else:
        shaded_target = np.clip(target * (0.74 + luma[:, None] * 0.52), 0.0, 1.0)

    rgb[mask] = rgb[mask] * (1.0 - strength) + shaded_target * strength


def material_texture_records(piece, side):
    records = {}
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue

        for material in obj.data.materials:
            node = find_color_texture_node(material)
            if node is None:
                continue

            if material.name in records:
                continue

            uniform_image = node.image.copy()
            uniform_image.name = f"{piece['id']}_{side}_UniformTexture"
            node.image = uniform_image
            material.name = f"{piece['id']}_{side}_BodyTextureUniform"

            width, height = uniform_image.size
            records[material.name] = {
                "image": uniform_image,
                "shirt_mask": np.zeros((height, width), dtype=bool),
                "pants_mask": np.zeros((height, width), dtype=bool),
            }

    return records


def rasterize_uniform_geometry_masks(piece, records, bounds_min, bounds_max):
    height = max(bounds_max.z - bounds_min.z, 0.001)
    width = max(bounds_max.x - bounds_min.x, 0.001)
    upper_min, upper_max = piece["upper"]
    lower_min, lower_max = piece["lower"]

    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or obj.data.uv_layers.active is None:
            continue

        uv_data = obj.data.uv_layers.active.data
        for polygon in obj.data.polygons:
            if polygon.material_index >= len(obj.data.materials):
                continue

            material = obj.data.materials[polygon.material_index]
            if material is None or material.name not in records:
                continue

            center = obj.matrix_world @ polygon.center
            normalized_z = (center.z - bounds_min.z) / height
            normalized_abs_x = abs(center.x) / max(width, 0.001)

            upper_body_weight = polygon_vertex_group_weight(obj, polygon, ("Chest", "Spine", "UpperArm"))
            leg_weight = polygon_vertex_group_weight(obj, polygon, ("Thigh", "Shin", "Foot"))
            has_rig_weights = len(obj.vertex_groups) > 0

            if has_rig_weights:
                is_shirt = upper_body_weight > 0.18 and upper_min <= normalized_z <= upper_max and normalized_abs_x <= 0.9
                is_pants = leg_weight > 0.18 and lower_min <= normalized_z < lower_max and normalized_abs_x <= 0.9
            else:
                is_shirt = upper_min <= normalized_z <= upper_max and normalized_abs_x <= 1.1
                is_pants = lower_min <= normalized_z < lower_max and normalized_abs_x <= 1.1

            if not is_shirt and not is_pants:
                continue

            polygon_uvs = [uv_data[loop_index].uv.copy() for loop_index in polygon.loop_indices]
            if len(polygon_uvs) < 3:
                continue

            target_mask = records[material.name]["shirt_mask"] if is_shirt else records[material.name]["pants_mask"]
            for index in range(1, len(polygon_uvs) - 1):
                rasterize_triangle(target_mask, (polygon_uvs[0], polygon_uvs[index], polygon_uvs[index + 1]))


def recolor_record_textures(records, palette):
    total_shirt = 0
    total_pants = 0

    for record in records.values():
        image = record["image"]
        pixels = np.empty(len(image.pixels), dtype=np.float32)
        image.pixels.foreach_get(pixels)
        rgba = pixels.reshape((-1, 4))
        rgb = rgba[:, :3]

        white_side_safe, clothing_like, white_hair = clothing_color_masks(rgb)
        geometry_shirt = record["shirt_mask"].reshape(-1)
        geometry_pants = record["pants_mask"].reshape(-1)

        shirt_mask = geometry_shirt & white_side_safe & ~white_hair
        pants_mask = geometry_pants & clothing_like & ~shirt_mask & ~white_hair

        if int(shirt_mask.sum()) < 1000:
            shirt_mask = geometry_shirt & clothing_like & ~white_hair

        shade_preserving_recolor(rgb, shirt_mask, palette["upper"], palette["strength_upper"])
        shade_preserving_recolor(rgb, pants_mask, palette["lower"], palette["strength_lower"])

        image.pixels.foreach_set(rgba.reshape(-1))
        image.update()

        total_shirt += int(shirt_mask.sum())
        total_pants += int(pants_mask.sum())

    return {
        "shirtPixels": total_shirt,
        "pantsPixels": total_pants,
    }


def write_manifest(piece, side, palette, texture_stats):
    side_dir = OUTPUT_ROOT / piece["id"] / "side_variants" / side
    side_dir.mkdir(parents=True, exist_ok=True)
    manifest = {
        "candidateId": f"{piece['id']}_{side}",
        "personName": piece["person"],
        "pieceKind": piece["kind"],
        "side": side,
        "sourceAssetPath": str(piece["source"].relative_to(PROJECT_ROOT)),
        "candidateModelPath": f"game/Assets/Art/CharacterCandidates/{piece['id']}/side_variants/{side}/{piece['id']}_{side}.glb",
        "importedPrefabPath": f"game/Assets/Resources/CustomPieces/{piece['id']}_{side}.prefab",
        "visualStatus": "side_variant_candidate",
        "rigStatus": piece["rig_status"],
        "approvedForUnity": True,
        "replacesActivePrefab": False,
        "outfitIntent": palette["description"],
        "textureRecolorOnly": True,
        "textureRecolorStats": texture_stats,
        "requiredSockets": SOCKET_NAMES,
        "combatPreparation": {
            "futureCaptureClip": piece["future_capture_clip"],
            "weaponConcept": piece["weapon_concept"],
        },
        "reviewImages": {
            "front": f"game/Assets/Art/CharacterCandidates/{piece['id']}/side_variants/{side}/preview_front.png",
            "threeQuarter": f"game/Assets/Art/CharacterCandidates/{piece['id']}/side_variants/{side}/preview_three_quarter.png",
        },
    }
    (side_dir / "character_quality_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def eevee_engine_name():
    available = [item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    return "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in available else "BLENDER_EEVEE"


def render_preview(piece, side, suffix, camera_location):
    side_dir = OUTPUT_ROOT / piece["id"] / "side_variants" / side
    bounds_min, bounds_max = world_bounds([obj for obj in bpy.context.scene.objects if obj.type == "MESH"])
    center = (bounds_min + bounds_max) * 0.5

    camera_data = bpy.data.cameras.new(f"PreviewCamera_{suffix}")
    camera = bpy.data.objects.new(f"PreviewCamera_{suffix}", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = camera_location
    look_at(camera, (center.x, center.y, center.z))
    camera.data.lens = 70
    bpy.context.scene.camera = camera

    key_data = bpy.data.lights.new(f"PreviewKeyLight_{suffix}", "AREA")
    key_light = bpy.data.objects.new(f"PreviewKeyLight_{suffix}", key_data)
    bpy.context.collection.objects.link(key_light)
    key_light.location = (-2.3, -3.0, 4.0)
    look_at(key_light, (center.x, center.y, center.z))
    key_light.data.energy = 650
    key_light.data.size = 4.0

    bpy.context.scene.render.engine = eevee_engine_name()
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.filepath = str(side_dir / f"preview_{suffix}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(key_light, do_unlink=True)


def export_variant(piece, side):
    side_dir = OUTPUT_ROOT / piece["id"] / "side_variants" / side
    side_dir.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(
        filepath=str(side_dir / f"{piece['id']}_{side}.glb"),
        export_format="GLB",
        use_selection=True,
        export_skins=True,
        export_animations=True,
        export_yup=True,
        export_apply=True,
    )


def create_variant(piece, side, palette):
    clear_scene()
    imported = import_source(piece)
    root = make_root(f"{piece['id']}_{side}")
    parent_top_level_to_root(root)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bounds_min, bounds_max = world_bounds(mesh_objects)

    records = material_texture_records(piece, side)
    if not records:
        raise RuntimeError(f"{piece['id']} has no texture-backed material to recolor.")

    rasterize_uniform_geometry_masks(piece, records, bounds_min, bounds_max)
    texture_stats = recolor_record_textures(records, palette)
    add_combat_sockets(root, bounds_min, bounds_max)

    root["character_id"] = piece["id"]
    root["person_name"] = piece["person"]
    root["piece_kind"] = piece["kind"]
    root["side_variant"] = side
    root["future_capture_clip"] = piece["future_capture_clip"]

    export_variant(piece, side)
    render_preview(piece, side, "front", (0.0, -4.8, 0.9))
    render_preview(piece, side, "three_quarter", (2.4, -4.4, 1.1))
    write_manifest(piece, side, palette, texture_stats)
    print(f"Created {piece['id']}_{side} from {len(imported)} source objects with {texture_stats}")


def main():
    for piece in PIECES:
        for side, palette in VARIANTS.items():
            create_variant(piece, side, palette)


if __name__ == "__main__":
    main()
