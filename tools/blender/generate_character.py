#!/usr/bin/env python3
"""Generate a modular stylized chess character in Blender.

Run with:
blender --background --python tools/blender/generate_character.py -- \
  --definition tools/blender/definitions/mathwidu_pawn.json \
  --output game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb
"""

import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Generate a modular chess character GLB.")
    parser.add_argument("--definition", required=True, help="Path to character JSON definition.")
    parser.add_argument("--output", required=True, help="Output GLB path.")
    parser.add_argument("--preview", help="Optional PNG preview path.")
    return parser.parse_args(argv)


def hex_to_rgba(value):
    value = value.strip().lstrip("#")
    if len(value) != 6:
        raise ValueError(f"Expected #RRGGBB color, got {value!r}")
    red = int(value[0:2], 16) / 255.0
    green = int(value[2:4], 16) / 255.0
    blue = int(value[4:6], 16) / 255.0
    return (red, green, blue, 1.0)


def make_material(name, color, roughness=0.72):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def material_from_palette(palette, key, fallback, name=None, roughness=0.72):
    return make_material(name or key, hex_to_rgba(palette.get(key, fallback)), roughness)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def add_empty(name, parent=None, location=(0.0, 0.0, 0.0)):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = "PLAIN_AXES"
    empty.empty_display_size = 0.08
    empty.location = location
    bpy.context.collection.objects.link(empty)
    if parent is not None:
        empty.parent = parent
    return empty


def shade_smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_smooth()
    finally:
        obj.select_set(False)


def add_uv_sphere(name, parent, location, scale, material, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.scale = scale
    obj.data.materials.append(material)
    shade_smooth(obj)
    return obj


def add_cube(name, parent, location, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.scale = scale
    obj.data.materials.append(material)
    if bevel > 0.0:
        modifier = obj.modifiers.new("SoftEdges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 5
        obj.modifiers.new("WeightedNormals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder(name, parent, location, radius, depth, material, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.data.materials.append(material)
    shade_smooth(obj)
    return obj


def add_limb(name, parent, location, rotation_y, length, radius, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=length, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.rotation_euler[1] = rotation_y
    obj.data.materials.append(material)
    shade_smooth(obj)
    return obj


def add_flat_detail(name, parent, location, scale, material, bevel=0.002):
    return add_cube(name, parent, location, scale, material, bevel)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_hair_cluster(parent, material, accent_material=None, premium=False):
    accent = accent_material or material
    add_uv_sphere("HairCap", parent, (0.0, 0.0, 0.132), (0.158, 0.14, 0.075), accent, 32, 12)
    curl_positions = [
        (-0.105, -0.04, 0.178),
        (-0.064, -0.067, 0.207),
        (-0.018, -0.072, 0.225),
        (0.034, -0.064, 0.212),
        (0.086, -0.044, 0.184),
        (-0.096, 0.015, 0.203),
        (-0.042, 0.04, 0.229),
        (0.022, 0.043, 0.222),
        (0.078, 0.018, 0.198),
    ]
    if premium:
        curl_positions.extend(
            [
                (-0.13, -0.004, 0.145),
                (0.125, -0.002, 0.145),
                (-0.078, -0.092, 0.162),
                (0.074, -0.088, 0.156),
                (0.0, -0.103, 0.19),
                (-0.02, 0.072, 0.188),
                (0.045, 0.066, 0.176),
            ]
        )
    for index, position in enumerate(curl_positions, start=1):
        scale = (0.037, 0.033, 0.031) if premium else (0.036, 0.032, 0.03)
        curl_material = material if index % 3 else accent
        add_uv_sphere(f"GingerCurl_{index:02d}", parent, position, scale, curl_material, 16, 8)


def add_face(parent, skin, beard, eyes, brow, cheek, mouth, premium=False):
    add_uv_sphere("LeftEye", parent, (-0.052, -0.135, 0.035), (0.018, 0.01, 0.018), eyes, 12, 6)
    add_uv_sphere("RightEye", parent, (0.052, -0.135, 0.035), (0.018, 0.01, 0.018), eyes, 12, 6)
    add_uv_sphere("Nose", parent, (0.0, -0.151, 0.0), (0.019, 0.031, 0.026), skin, 12, 6)
    add_cube("Mustache", parent, (0.0, -0.151, -0.042), (0.058, 0.007, 0.01), beard, 0.004)
    add_uv_sphere("SmallBeard", parent, (0.0, -0.13, -0.087), (0.076, 0.012, 0.035), beard, 16, 6)

    if not premium:
        return

    add_uv_sphere("LeftEar", parent, (-0.14, -0.015, 0.005), (0.024, 0.014, 0.036), skin, 12, 6)
    add_uv_sphere("RightEar", parent, (0.14, -0.015, 0.005), (0.024, 0.014, 0.036), skin, 12, 6)
    add_flat_detail("LeftBrow", parent, (-0.052, -0.143, 0.073), (0.04, 0.005, 0.007), brow, 0.002)
    add_flat_detail("RightBrow", parent, (0.052, -0.143, 0.073), (0.04, 0.005, 0.007), brow, 0.002)
    add_uv_sphere("LeftCheek", parent, (-0.066, -0.142, -0.022), (0.024, 0.006, 0.017), cheek, 12, 6)
    add_uv_sphere("RightCheek", parent, (0.066, -0.142, -0.022), (0.024, 0.006, 0.017), cheek, 12, 6)
    add_flat_detail("Smile", parent, (0.0, -0.158, -0.067), (0.046, 0.005, 0.005), mouth, 0.002)


def create_character(definition):
    palette = definition["palette"]
    premium = definition.get("styleTarget") == "premium_stylized_cartoon"
    skin = material_from_palette(palette, "skin", "#f2c7ab", "Skin")
    skin_warm = material_from_palette(palette, "skinWarm", palette.get("skin", "#f2c7ab"), "SkinWarm")
    cheek = material_from_palette(palette, "cheek", "#e9a991", "Cheek", 0.78)
    hair = material_from_palette(palette, "hair", "#c76a2d", "GingerHair")
    hair_dark = material_from_palette(palette, "hairDark", palette.get("hair", "#c76a2d"), "GingerHairShadow")
    beard = material_from_palette(palette, "beard", "#b85c29", "GingerBeard")
    shirt = material_from_palette(palette, "shirt", "#cfd5d8", "LightGrayShirt")
    shirt_shadow = material_from_palette(palette, "shirtShadow", palette.get("shirt", "#cfd5d8"), "ShirtSoftShadow")
    pants = material_from_palette(palette, "pants", "#b89768", "BeigeCargoPants")
    pants_shadow = material_from_palette(palette, "pantsShadow", palette.get("pants", "#b89768"), "PantsShadow")
    shoes = material_from_palette(palette, "shoes", "#f2f0e8", "WhiteSneakers")
    sole = material_from_palette(palette, "sole", "#1d2328", "DarkSoles")
    eyes = material_from_palette(palette, "eyes", "#654a32", "Eyes")
    brow = material_from_palette(palette, "brow", palette.get("beard", "#b85c29"), "Brows")
    mouth = material_from_palette(palette, "mouth", "#a96558", "Mouth")
    stitch = material_from_palette(palette, "stitch", "#e9dec8", "Stitch")

    root = add_empty(definition.get("outputName", "GeneratedCharacter"))
    body_root = add_empty("BodyRoot", root)
    prop_root = add_empty("PropRoot", root)
    add_empty("EffectsSocket", root, (0.0, 0.0, 1.58))
    add_empty("HitSocket", root, (0.0, -0.02, 0.95))
    add_empty("GroundSocket", root, (0.0, 0.0, 0.0))
    add_empty("WeaponSocket", root, (0.28, -0.02, 0.9))
    add_empty("RightHandSocket", root, (0.34, -0.02, 0.82))
    add_empty("LeftHandSocket", root, (-0.34, -0.02, 0.82))
    add_empty("CastSocket", root, (0.0, -0.16, 1.22))

    torso = add_empty("TorsoRoot", body_root, (0.0, 0.0, 0.88))
    add_uv_sphere("Torso", torso, (0.0, 0.0, 0.0), (0.195, 0.124, 0.315), shirt, 40, 18)
    add_uv_sphere("ChestSoftForm", torso, (0.0, -0.012, 0.08), (0.17, 0.026, 0.17), shirt_shadow, 24, 10)
    add_uv_sphere("ShirtHem", torso, (0.0, 0.0, -0.292), (0.205, 0.132, 0.03), shirt, 32, 8)
    add_cube("BeltHint", torso, (0.0, -0.118, -0.315), (0.18, 0.008, 0.012), pants_shadow, 0.003)
    add_flat_detail("LeftCollarFold", torso, (-0.04, -0.125, 0.255), (0.038, 0.006, 0.035), shirt_shadow, 0.003)
    add_flat_detail("RightCollarFold", torso, (0.04, -0.125, 0.255), (0.038, 0.006, 0.035), shirt_shadow, 0.003)

    neck = add_empty("Neck", torso, (0.0, 0.0, 0.34))
    add_cylinder("NeckMesh", neck, (0.0, 0.0, 0.0), 0.055, 0.12, skin, 20)

    head_root = add_empty("HeadRoot", neck, (0.0, -0.01, 0.18))
    head_scale = (0.147, 0.128, 0.17) if premium else (0.135, 0.12, 0.16)
    add_uv_sphere("Head", head_root, (0.0, 0.0, 0.0), head_scale, skin, 48, 20)
    add_uv_sphere("FaceWarmth", head_root, (0.0, -0.105, -0.012), (0.106, 0.018, 0.115), skin_warm, 24, 10)
    add_hair_cluster(head_root, hair, hair_dark, premium)
    add_face(head_root, skin, beard, eyes, brow, cheek, mouth, premium)

    left_arm = add_empty("LeftArmRoot", torso, (-0.245, 0.0, 0.18))
    right_arm = add_empty("RightArmRoot", torso, (0.245, 0.0, 0.18))
    for side_name, side_root, side_sign in [("Left", left_arm, -1.0), ("Right", right_arm, 1.0)]:
        add_limb(f"{side_name}ShortSleeve", side_root, (side_sign * 0.023, 0.0, -0.055), math.radians(8.0 * -side_sign), 0.15, 0.058, shirt)
        add_limb(f"{side_name}SleeveTrim", side_root, (side_sign * 0.033, -0.004, -0.115), math.radians(8.0 * -side_sign), 0.035, 0.06, shirt_shadow)
        add_limb(f"{side_name}UpperArm", side_root, (side_sign * 0.047, 0.0, -0.145), math.radians(8.0 * -side_sign), 0.29, 0.041, skin)
        forearm_root = add_empty(f"{side_name}ForearmRoot", side_root, (side_sign * 0.075, 0.0, -0.285))
        add_limb(f"{side_name}Forearm", forearm_root, (side_sign * 0.03, 0.0, -0.105), math.radians(5.0 * -side_sign), 0.23, 0.038, skin)
        add_uv_sphere(f"{side_name}Hand", forearm_root, (side_sign * 0.055, -0.005, -0.235), (0.044, 0.034, 0.047), skin, 16, 8)
        add_uv_sphere(f"{side_name}ThumbHint", forearm_root, (side_sign * 0.088, -0.033, -0.221), (0.014, 0.013, 0.025), skin, 10, 6)

    left_leg = add_empty("LeftLegRoot", body_root, (-0.09, 0.0, 0.47))
    right_leg = add_empty("RightLegRoot", body_root, (0.09, 0.0, 0.47))
    for side_name, side_root, side_sign in [("Left", left_leg, -1.0), ("Right", right_leg, 1.0)]:
        add_limb(f"{side_name}Thigh", side_root, (0.0, 0.0, -0.15), 0.0, 0.31, 0.058, pants)
        add_flat_detail(f"{side_name}PantsFold", side_root, (side_sign * 0.025, -0.052, -0.17), (0.01, 0.006, 0.09), pants_shadow, 0.002)
        knee_root = add_empty(f"{side_name}KneeRoot", side_root, (0.0, 0.0, -0.315))
        add_limb(f"{side_name}Shin", knee_root, (0.0, 0.0, -0.16), 0.0, 0.32, 0.048, pants)
        foot_root = add_empty(f"{side_name}FootRoot", knee_root, (0.0, -0.035, -0.325))
        add_cube(f"{side_name}Shoe", foot_root, (0.0, -0.025, 0.0), (0.068, 0.14, 0.038), shoes, 0.018)
        add_cube(f"{side_name}ShoeToe", foot_root, (0.0, -0.103, 0.006), (0.068, 0.04, 0.032), shoes, 0.016)
        add_cube(f"{side_name}ShoeSole", foot_root, (0.0, -0.035, -0.034), (0.073, 0.145, 0.012), sole, 0.01)
        add_flat_detail(f"{side_name}ShoeLaceA", foot_root, (-0.019, -0.071, 0.041), (0.004, 0.026, 0.003), stitch, 0.001)
        add_flat_detail(f"{side_name}ShoeLaceB", foot_root, (0.019, -0.071, 0.041), (0.004, 0.026, 0.003), stitch, 0.001)

    add_cube("LeftCargoPocket", left_leg, (-0.066, -0.05, -0.18), (0.027, 0.013, 0.067), pants_shadow, 0.004)
    add_cube("RightCargoPocket", right_leg, (0.066, -0.05, -0.18), (0.027, 0.013, 0.067), pants_shadow, 0.004)
    add_flat_detail("LeftCargoFlap", left_leg, (-0.066, -0.06, -0.112), (0.031, 0.006, 0.01), stitch, 0.002)
    add_flat_detail("RightCargoFlap", right_leg, (0.066, -0.06, -0.112), (0.031, 0.006, 0.01), stitch, 0.002)

    root["character_id"] = definition.get("id", "")
    root["style_target"] = definition.get("styleTarget", "modular_stylized")
    root["has_integrated_base"] = bool(definition.get("presentation", {}).get("hasIntegratedBase", False))

    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    root.select_set(True)
    bpy.context.view_layer.objects.active = root
    return root


def export_glb(output_path):
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=output_path,
        export_format="GLB",
        use_selection=False,
        export_apply=True,
        export_yup=True,
    )


def render_preview(preview_path):
    os.makedirs(os.path.dirname(preview_path), exist_ok=True)
    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (0.0, -5.2, 0.92)
    look_at(camera, (0.0, 0.0, 0.85))
    camera.data.lens = 58
    bpy.context.scene.camera = camera

    key_data = bpy.data.lights.new("PreviewKeyLight", "AREA")
    key_light = bpy.data.objects.new("PreviewKeyLight", key_data)
    bpy.context.collection.objects.link(key_light)
    key_light.location = (-2.0, -3.0, 4.0)
    look_at(key_light, (0.0, 0.0, 0.85))
    key_light.data.energy = 550
    key_light.data.size = 4.5

    fill_data = bpy.data.lights.new("PreviewFillLight", "POINT")
    fill_light = bpy.data.objects.new("PreviewFillLight", fill_data)
    bpy.context.collection.objects.link(fill_light)
    fill_light.location = (1.8, 1.8, 2.6)
    fill_light.data.energy = 80

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.filepath = preview_path
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    with open(args.definition, "r", encoding="utf-8") as handle:
        definition = json.load(handle)

    clear_scene()
    create_character(definition)
    export_glb(args.output)
    print(f"Exported modular character: {args.output}")
    if args.preview:
        render_preview(args.preview)
        print(f"Rendered modular character preview: {args.preview}")


if __name__ == "__main__":
    main()
