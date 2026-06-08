#!/usr/bin/env python3
"""Render standard review previews for a character candidate.

Run with:
/opt/homebrew/bin/blender --background --python tools/blender/render_character_review.py -- \
  --manifest game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json
"""

import argparse
import json
import os
import sys

import bpy
from mathutils import Vector


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser(description="Render front, 3/4, and board-scale character previews.")
    parser.add_argument("--manifest", required=True, help="Path to character_quality_manifest.json.")
    return parser.parse_args(argv)


def project_path(path):
    return os.path.normpath(os.path.abspath(path))


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def import_model(path):
    resolved = project_path(path)
    if not os.path.exists(resolved):
        raise FileNotFoundError(resolved)

    bpy.ops.import_scene.gltf(filepath=resolved)
    objects = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and not any(collection.name == "glTF_not_exported" for collection in obj.users_collection)
    ]
    if not objects:
        raise RuntimeError(f"Imported model has no mesh objects: {resolved}")
    return objects


def resolve_review_model_path(manifest):
    for key in ("reviewModelPath", "candidateModelPath", "sourceAssetPath"):
        path = manifest.get(key)
        if path and os.path.exists(project_path(path)):
            return path
    raise FileNotFoundError("Manifest has no existing review, candidate, or source model path")


def calculate_bounds(objects):
    bounds_min = None
    bounds_max = None
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            if bounds_min is None:
                bounds_min = world.copy()
                bounds_max = world.copy()
                continue

            bounds_min.x = min(bounds_min.x, world.x)
            bounds_min.y = min(bounds_min.y, world.y)
            bounds_min.z = min(bounds_min.z, world.z)
            bounds_max.x = max(bounds_max.x, world.x)
            bounds_max.y = max(bounds_max.y, world.y)
            bounds_max.z = max(bounds_max.z, world.z)

    if bounds_min is None or bounds_max is None:
        raise RuntimeError("Could not calculate bounds for candidate model")
    return bounds_min, bounds_max


def create_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.7
    return material


def add_board_scale_square(bounds_min, bounds_max):
    width = max(bounds_max.x - bounds_min.x, bounds_max.y - bounds_min.y, 0.8)
    square_size = max(width * 1.75, 1.0)
    center_x = (bounds_min.x + bounds_max.x) * 0.5
    center_y = (bounds_min.y + bounds_max.y) * 0.5
    floor_z = bounds_min.z - 0.015

    material = create_material("ReviewBoardSquare", (0.73, 0.60, 0.44, 1.0))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center_x, center_y, floor_z))
    square = bpy.context.object
    square.name = "ReviewBoardScaleSquare"
    square.scale = (square_size, square_size, 0.025)
    square.data.materials.append(material)
    return square


def setup_lighting(target):
    key_data = bpy.data.lights.new("ReviewKey", "AREA")
    key = bpy.data.objects.new("ReviewKey", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (-2.8, -4.0, 4.2)
    key.data.energy = 700
    key.data.size = 4.5
    look_at(key, target)

    fill_data = bpy.data.lights.new("ReviewFill", "POINT")
    fill = bpy.data.objects.new("ReviewFill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (2.2, -1.5, 2.4)
    fill.data.energy = 95


def setup_camera(location, target, lens):
    camera_data = bpy.data.cameras.new("ReviewCamera")
    camera = bpy.data.objects.new("ReviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.camera = camera
    return camera


def clear_camera():
    if bpy.context.scene.camera is not None:
        bpy.data.objects.remove(bpy.context.scene.camera, do_unlink=True)
        bpy.context.scene.camera = None


def render(path):
    output_path = project_path(path)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1200
    bpy.context.scene.render.resolution_y = 1400
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    with open(project_path(args.manifest), "r", encoding="utf-8") as handle:
        manifest = json.load(handle)

    clear_scene()
    objects = import_model(resolve_review_model_path(manifest))
    bounds_min, bounds_max = calculate_bounds(objects)
    center = (bounds_min + bounds_max) * 0.5
    height = max(bounds_max.z - bounds_min.z, 0.1)
    target = Vector((center.x, center.y, bounds_min.z + height * 0.55))

    setup_lighting(target)
    previews = manifest["previewImages"]

    setup_camera((center.x, center.y - height * 2.8, bounds_min.z + height * 0.55), target, 60)
    render(previews["front"])

    clear_camera()
    setup_camera((center.x + height * 1.5, center.y - height * 2.4, bounds_min.z + height * 0.7), target, 58)
    render(previews["threeQuarter"])

    add_board_scale_square(bounds_min, bounds_max)
    clear_camera()
    setup_camera((center.x + height * 2.1, center.y - height * 2.5, bounds_min.z + height * 1.35), target, 42)
    render(previews["boardScale"])


if __name__ == "__main__":
    main()
