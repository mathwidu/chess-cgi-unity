#!/usr/bin/env python3
"""Create white/black side variants for the approved Mathwidu pawn.

The script derives from the current v3b pawn GLB, recolors existing clothing
texture areas for team identity, adds invisible combat sockets, then exports
two GLBs for Unity import. It must not add visible outfit props over the model.
"""

import json
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
SOURCE_GLB = PROJECT_ROOT / "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b_Assets/selected.glb"
OUTPUT_ROOT = PROJECT_ROOT / "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants"

VARIANTS = {
    "White": {
        "shirt_texture": (0.94, 0.94, 0.90),
        "pants_texture": (0.84, 0.81, 0.72),
        "shoe": (0.96, 0.95, 0.9, 1.0),
        "sole": (0.26, 0.28, 0.3, 1.0),
        "description": "white-side existing clothing texture recolor: white shirt, light pants, and white sneakers",
    },
    "Black": {
        "shirt_texture": (0.025, 0.027, 0.032),
        "pants_texture": (0.035, 0.036, 0.040),
        "shoe": (0.025, 0.027, 0.03, 1.0),
        "sole": (0.16, 0.17, 0.18, 1.0),
        "description": "black-side existing clothing texture recolor: black shirt, dark pants, and black sneakers",
    },
}


def create_material(name, color, roughness=0.72, metallic=0.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return material


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


def import_source():
    if not SOURCE_GLB.exists():
        raise FileNotFoundError(SOURCE_GLB)

    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    return prune_imported_source_artifacts([obj for obj in bpy.context.scene.objects])


def is_character_source_object(obj):
    if obj.type == "ARMATURE":
        return obj.name.startswith("Pawn_Mathwidu_v3b")
    if obj.type == "MESH":
        return obj.name.startswith("Pawn_Mathwidu_v3b_")
    return obj.type == "EMPTY" and obj.name.startswith("Pawn_Mathwidu_v3b")


def prune_imported_source_artifacts(imported):
    kept = []
    for obj in imported:
        if obj.type == "MESH" and "TeamOutfit" in obj.name:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue

        if is_character_source_object(obj):
            kept.append(obj)
            continue

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


def parent_top_level_to_root(root):
    for obj in list(bpy.context.scene.objects):
        if obj == root or obj.parent is not None:
            continue
        obj.parent = root
        obj.matrix_parent_inverse = root.matrix_world.inverted()


def make_root(name):
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.18
    bpy.context.collection.objects.link(root)
    return root


def add_socket(root, name, location):
    existing = bpy.data.objects.get(name)
    if existing is not None:
        existing.location = location
        existing.parent = root
        return existing

    socket = bpy.data.objects.new(name, None)
    socket.empty_display_type = "PLAIN_AXES"
    socket.empty_display_size = 0.08
    socket.location = location
    socket.parent = root
    bpy.context.collection.objects.link(socket)
    return socket


def recolor_existing_materials(side, palette):
    replacements = {
        "Pawn_Mathwidu_v3b_WhiteSneaker": create_material(f"Pawn_Mathwidu_{side}_Shoes", palette["shoe"], 0.68),
        "Pawn_Mathwidu_v3b_OffWhiteSole": create_material(f"Pawn_Mathwidu_{side}_Soles", palette["sole"], 0.78),
    }

    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue

        for index, material in enumerate(obj.data.materials):
            if material is None:
                continue
            material_key = material.name.split(".", 1)[0]
            if material_key in replacements:
                obj.data.materials[index] = replacements[material_key]


def body_mesh():
    body = next(
        (obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name.startswith("Pawn_Mathwidu_v3b_Body")),
        None,
    )
    if body is None:
        raise RuntimeError("Pawn body mesh was not found for clothing texture recolor.")
    return body


def body_color_texture_node(material):
    if material is None or material.node_tree is None:
        return None

    for node in material.node_tree.nodes:
        if node.bl_idname == "ShaderNodeTexImage" and node.image is not None and node.image.name.startswith("Color_"):
            return node

    return None


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
    total = 0.0
    vertex_count = max(len(polygon.vertices), 1)
    for vertex_index in polygon.vertices:
        vertex = obj.data.vertices[vertex_index]
        for group_weight in vertex.groups:
            group_name = obj.vertex_groups[group_weight.group].name
            if any(group_name.startswith(prefix) for prefix in group_prefixes):
                total += group_weight.weight
    return total / vertex_count


def rasterize_uniform_geometry_masks(body, image_size, bounds_min, bounds_max):
    if body.data.uv_layers.active is None:
        raise RuntimeError("Pawn body mesh has no active UV layer for clothing texture recolor.")

    texture_width, texture_height = image_size
    shirt_mask = np.zeros((texture_height, texture_width), dtype=bool)
    pants_mask = np.zeros((texture_height, texture_width), dtype=bool)
    uv_data = body.data.uv_layers.active.data

    height = max(bounds_max.z - bounds_min.z, 0.001)
    width = max(bounds_max.x - bounds_min.x, 0.001)

    for polygon in body.data.polygons:
        center = body.matrix_world @ polygon.center
        normalized_z = (center.z - bounds_min.z) / height
        normalized_abs_x = abs(center.x) / width

        upper_body_weight = polygon_vertex_group_weight(body, polygon, ("Chest", "Spine", "UpperArm"))
        leg_weight = polygon_vertex_group_weight(body, polygon, ("Thigh", "Shin", "Foot"))

        is_shirt = upper_body_weight > 0.18 and 0.40 <= normalized_z <= 0.82 and normalized_abs_x <= 0.82
        is_pants = leg_weight > 0.18 and 0.02 <= normalized_z < 0.54 and normalized_abs_x <= 0.82
        if not is_shirt and not is_pants:
            continue

        polygon_uvs = [uv_data[loop_index].uv.copy() for loop_index in polygon.loop_indices]
        if len(polygon_uvs) < 3:
            continue

        target_mask = shirt_mask if is_shirt else pants_mask
        for index in range(1, len(polygon_uvs) - 1):
            rasterize_triangle(target_mask, (polygon_uvs[0], polygon_uvs[index], polygon_uvs[index + 1]))

    return shirt_mask.reshape(-1), pants_mask.reshape(-1)


def clothing_color_masks(rgb):
    r = rgb[:, 0]
    g = rgb[:, 1]
    b = rgb[:, 2]
    value = np.max(rgb, axis=1)
    minimum = np.min(rgb, axis=1)
    saturation = np.where(value > 1e-5, (value - minimum) / value, 0.0)

    shirt_color = (b > r * 1.04) & (b > g * 0.98) & (value > 0.20) & (saturation < 0.58)
    skin_color = ((r - g) > 0.19) & ((g - b) > 0.03) & (value > 0.45)
    hair_color = (r > g * 1.10) & (g > b * 1.12) & (value < 0.34)
    pants_color = (
        (r > b * 1.10)
        & (g > b * 1.02)
        & ((r - g) < 0.24)
        & ((g - b) < 0.24)
        & ((r - b) > 0.035)
        & (value > 0.14)
        & (value < 0.88)
        & (saturation < 0.58)
        & ~skin_color
        & ~hair_color
    )

    return shirt_color, pants_color


def shade_preserving_recolor(rgb, mask, target_color, strength):
    if not np.any(mask):
        return

    target = np.array(target_color, dtype=np.float32)
    luma = rgb[mask] @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)

    if float(target.mean()) < 0.18:
        shaded_target = np.clip(target + luma[:, None] * 0.12, 0.0, 1.0)
    else:
        shaded_target = np.clip(target * (0.72 + luma[:, None] * 0.56), 0.0, 1.0)

    rgb[mask] = rgb[mask] * (1.0 - strength) + shaded_target * strength


def recolor_existing_clothing_texture(side, palette, bounds_min, bounds_max):
    body = body_mesh()
    material = body.active_material
    texture_node = body_color_texture_node(material)
    if texture_node is None:
        raise RuntimeError("Pawn body material does not expose a base color texture for clothing recolor.")

    source_image = texture_node.image
    uniform_image = source_image.copy()
    uniform_image.name = f"Pawn_Mathwidu_{side}_UniformTexture"
    texture_node.image = uniform_image

    pixels = np.empty(len(uniform_image.pixels), dtype=np.float32)
    uniform_image.pixels.foreach_get(pixels)
    rgba = pixels.reshape((-1, 4))
    rgb = rgba[:, :3]

    geometry_shirt_mask, geometry_pants_mask = rasterize_uniform_geometry_masks(
        body,
        uniform_image.size,
        bounds_min,
        bounds_max,
    )
    shirt_color_mask, pants_color_mask = clothing_color_masks(rgb)
    shirt_mask = geometry_shirt_mask & shirt_color_mask
    pants_mask = geometry_pants_mask & pants_color_mask

    shade_preserving_recolor(rgb, shirt_mask, palette["shirt_texture"], 0.92)
    shade_preserving_recolor(rgb, pants_mask, palette["pants_texture"], 0.88)

    uniform_image.pixels.foreach_set(rgba.reshape(-1))
    uniform_image.update()
    material.name = f"Pawn_Mathwidu_{side}_BodyTextureUniform"

    return {
        "shirtPixels": int(shirt_mask.sum()),
        "pantsPixels": int(pants_mask.sum()),
    }


def add_combat_sockets(root, bounds_min, bounds_max):
    height = bounds_max.z - bounds_min.z
    z0 = bounds_min.z
    add_socket(root, "EffectsSocket", (0.0, 0.0, z0 + height * 0.92))
    add_socket(root, "HitSocket", (0.0, -0.02, z0 + height * 0.58))
    add_socket(root, "GroundSocket", (0.0, 0.0, z0))
    add_socket(root, "WeaponSocket", (0.28, -0.04, z0 + height * 0.46))
    add_socket(root, "RightHandSocket", (0.34, -0.03, z0 + height * 0.52))
    add_socket(root, "LeftHandSocket", (-0.34, -0.03, z0 + height * 0.52))
    add_socket(root, "CastSocket", (0.0, -0.16, z0 + height * 0.78))


def write_manifest(side, palette, texture_stats):
    side_dir = OUTPUT_ROOT / side
    side_dir.mkdir(parents=True, exist_ok=True)
    manifest = {
        "candidateId": f"Pawn_Mathwidu_{side}",
        "personName": "Mathwidu",
        "pieceKind": "Pawn",
        "side": side,
        "sourceAssetPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b_Assets/selected.glb",
        "candidateModelPath": f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/Pawn_Mathwidu_{side}.glb",
        "importedPrefabPath": f"game/Assets/Resources/CustomPieces/Pawn_Mathwidu_{side}.prefab",
        "visualStatus": "side_variant_candidate",
        "rigStatus": "rigged_candidate_initial",
        "approvedForUnity": True,
        "replacesActivePrefab": False,
        "outfitIntent": palette["description"],
        "textureRecolorOnly": True,
        "textureRecolorStats": texture_stats,
        "requiredSockets": [
            "EffectsSocket",
            "HitSocket",
            "GroundSocket",
            "WeaponSocket",
            "RightHandSocket",
            "LeftHandSocket",
            "CastSocket",
        ],
        "combatPreparation": {
            "futureCaptureClip": "Capture_Pawn_DaggerLunge",
            "weaponConcept": "socket-only dagger concept; no visible idle weapon prop",
        },
        "reviewImages": {
            "front": f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/preview_front.png",
            "threeQuarter": f"game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/preview_three_quarter.png",
        },
    }
    (side_dir / "character_quality_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_preview(side, root, suffix, camera_location):
    side_dir = OUTPUT_ROOT / side
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

    bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.filepath = str(side_dir / f"preview_{suffix}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(key_light, do_unlink=True)


def export_variant(side):
    side_dir = OUTPUT_ROOT / side
    side_dir.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(
        filepath=str(side_dir / f"Pawn_Mathwidu_{side}.glb"),
        export_format="GLB",
        use_selection=True,
        export_skins=True,
        export_animations=True,
        export_yup=True,
        export_apply=True,
    )


def create_variant(side, palette):
    clear_scene()
    imported = import_source()
    root = make_root(f"Pawn_Mathwidu_{side}")
    parent_top_level_to_root(root)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    bounds_min, bounds_max = world_bounds(mesh_objects)
    armature = next((obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"), None)

    recolor_existing_materials(side, palette)
    texture_stats = recolor_existing_clothing_texture(side, palette, bounds_min, bounds_max)
    add_combat_sockets(root, bounds_min, bounds_max)

    root["character_id"] = "mathwidu_pawn"
    root["side_variant"] = side
    root["future_capture_clip"] = "Capture_Pawn_DaggerLunge"

    export_variant(side)
    render_preview(side, root, "front", (0.0, -4.8, 0.9))
    render_preview(side, root, "three_quarter", (2.4, -4.4, 1.1))
    write_manifest(side, palette, texture_stats)
    print(f"Created Pawn_Mathwidu_{side} from {len(imported)} source objects")


def main():
    for side, palette in VARIANTS.items():
        create_variant(side, palette)


if __name__ == "__main__":
    main()
