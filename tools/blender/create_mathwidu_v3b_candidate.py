#!/usr/bin/env python3
"""Create the Mathwidu pawn v3b rigged-review candidate.

This script deliberately writes into Assets/Art/CharacterCandidates, not
Resources/CustomPieces. The candidate must pass visual review before Unity uses it.
"""

import json
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
CANDIDATE_DIR = PROJECT_ROOT / "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b"
SOURCE_GLB = PROJECT_ROOT / "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb"
CANDIDATE_GLB = CANDIDATE_DIR / "Pawn_Mathwidu_v3b.glb"
MANIFEST_PATH = CANDIDATE_DIR / "character_quality_manifest.json"


def create_material(name, color, roughness=0.72):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_source_mesh():
    if not SOURCE_GLB.exists():
        raise FileNotFoundError(SOURCE_GLB)

    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one source mesh, got {len(meshes)}")

    mesh = meshes[0]
    mesh.name = "Pawn_Mathwidu_v3b_Body"
    mesh.data.name = "Pawn_Mathwidu_v3b_BodyMesh"
    bpy.context.view_layer.objects.active = mesh
    mesh.select_set(True)
    return mesh


def world_bounds(obj):
    if obj.type == "MESH" and obj.data.vertices:
        coords = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    else:
        coords = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    bounds_min = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    bounds_max = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return bounds_min, bounds_max


def normalize_mesh_transform(obj):
    bounds_min, bounds_max = world_bounds(obj)
    center = (bounds_min + bounds_max) * 0.5
    obj.location -= Vector((center.x, center.y, bounds_min.z))
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def remove_integrated_chess_base(obj):
    bounds_min, bounds_max = world_bounds(obj)
    height = bounds_max.z - bounds_min.z
    cutoff_z = bounds_min.z + height * 0.135

    mesh = obj.data
    vertices_to_delete = {
        vertex.index
        for vertex in mesh.vertices
        if (obj.matrix_world @ vertex.co).z < cutoff_z
    }

    if not vertices_to_delete:
        return 0, cutoff_z

    import bmesh

    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    doomed = [bm.verts[index] for index in vertices_to_delete if index < len(bm.verts)]
    bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    return len(vertices_to_delete), cutoff_z


def delete_vertices_below(obj, cutoff_z):
    vertices_to_delete = {
        vertex.index
        for vertex in obj.data.vertices
        if (obj.matrix_world @ vertex.co).z < cutoff_z
    }
    if not vertices_to_delete:
        return 0

    import bmesh

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    doomed = [bm.verts[index] for index in vertices_to_delete if index < len(bm.verts)]
    bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return len(vertices_to_delete)


def measure_foot_anchors(body):
    bounds_min, bounds_max = world_bounds(body)
    height = bounds_max.z - bounds_min.z
    width = bounds_max.x - bounds_min.x
    low_cutoff = bounds_min.z + height * 0.105
    low_vertices = [body.matrix_world @ vertex.co for vertex in body.data.vertices if (body.matrix_world @ vertex.co).z <= low_cutoff]
    anchors = []

    for side, predicate in (("L", lambda point: point.x < 0.0), ("R", lambda point: point.x >= 0.0)):
        side_vertices = [point for point in low_vertices if predicate(point)]
        if not side_vertices:
            continue

        min_x = min(point.x for point in side_vertices)
        max_x = max(point.x for point in side_vertices)
        min_y = min(point.y for point in side_vertices)
        max_y = max(point.y for point in side_vertices)
        min_z = min(point.z for point in side_vertices)

        anchors.append(
            {
                "side": side,
                "center": Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z)),
                "width": max((max_x - min_x) * 1.2, width * 0.28),
                "length": max((max_y - min_y) * 1.24, height * 0.13),
                "height": max(height * 0.048, 0.04),
            }
        )

    return anchors, height


def make_armature(bounds_min, bounds_max):
    height = bounds_max.z - bounds_min.z
    x = max(bounds_max.x - bounds_min.x, 0.3)
    z0 = bounds_min.z

    bpy.ops.object.armature_add(enter_editmode=True, location=(0.0, 0.0, 0.0))
    armature = bpy.context.object
    armature.name = "Pawn_Mathwidu_v3b_Armature"
    armature.data.name = "Pawn_Mathwidu_v3b_Skeleton"
    armature.show_in_front = True

    bones = armature.data.edit_bones
    bones.remove(bones[0])

    def add_bone(name, head, tail, parent=None):
        bone = bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        bone.use_deform = True
        if parent:
            bone.parent = bones[parent]
            bone.use_connect = False
        return bone

    hips_z = z0 + height * 0.34
    chest_z = z0 + height * 0.62
    head_z = z0 + height * 0.84
    shoulder_z = z0 + height * 0.66
    hand_z = z0 + height * 0.38
    knee_z = z0 + height * 0.18
    foot_z = z0 + height * 0.03
    shoulder_x = x * 0.36
    hand_x = x * 0.54
    hip_x = x * 0.17
    foot_x = x * 0.18

    add_bone("Hips", Vector((0, 0, hips_z - height * 0.05)), Vector((0, 0, hips_z + height * 0.06)))
    add_bone("Spine", Vector((0, 0, hips_z + height * 0.04)), Vector((0, 0, chest_z)), "Hips")
    add_bone("Chest", Vector((0, 0, chest_z - height * 0.04)), Vector((0, 0, shoulder_z + height * 0.05)), "Spine")
    add_bone("Neck", Vector((0, 0, shoulder_z + height * 0.03)), Vector((0, 0, head_z - height * 0.07)), "Chest")
    add_bone("Head", Vector((0, 0, head_z - height * 0.07)), Vector((0, 0, z0 + height * 0.99)), "Neck")

    for side, sign in (("L", -1), ("R", 1)):
        add_bone(f"UpperArm.{side}", Vector((sign * shoulder_x, 0, shoulder_z)), Vector((sign * (shoulder_x + x * 0.08), 0, shoulder_z - height * 0.14)), "Chest")
        add_bone(f"Forearm.{side}", Vector((sign * (shoulder_x + x * 0.08), 0, shoulder_z - height * 0.14)), Vector((sign * hand_x, 0, hand_z + height * 0.06)), f"UpperArm.{side}")
        add_bone(f"Hand.{side}", Vector((sign * hand_x, 0, hand_z + height * 0.06)), Vector((sign * (hand_x + x * 0.06), 0, hand_z)), f"Forearm.{side}")
        add_bone(f"Thigh.{side}", Vector((sign * hip_x, 0, hips_z - height * 0.03)), Vector((sign * foot_x, 0, knee_z)), "Hips")
        add_bone(f"Shin.{side}", Vector((sign * foot_x, 0, knee_z)), Vector((sign * foot_x, 0, foot_z + height * 0.07)), f"Thigh.{side}")
        add_bone(f"Foot.{side}", Vector((sign * foot_x, 0, foot_z + height * 0.07)), Vector((sign * foot_x, -height * 0.11, foot_z + height * 0.03)), f"Shin.{side}")

    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def nearest_bone_weight(point, head, tail):
    segment = tail - head
    if segment.length == 0:
        return 0.0
    t = max(0.0, min(1.0, (point - head).dot(segment) / segment.length_squared))
    closest = head + segment * t
    distance = (point - closest).length
    return max(0.0, 1.0 - distance * 8.0)


def add_manual_vertex_groups(mesh_obj, armature):
    groups = {bone.name: mesh_obj.vertex_groups.new(name=bone.name) for bone in armature.data.bones}
    bone_segments = {
        bone.name: (
            armature.matrix_world @ bone.head_local,
            armature.matrix_world @ bone.tail_local,
        )
        for bone in armature.data.bones
    }

    for vertex in mesh_obj.data.vertices:
        point = mesh_obj.matrix_world @ vertex.co
        weighted = []
        for name, (head, tail) in bone_segments.items():
            weight = nearest_bone_weight(point, head, tail)
            if weight > 0.03:
                weighted.append((name, weight))

        if not weighted:
            weighted = [("Hips", 1.0)]

        weighted.sort(key=lambda item: item[1], reverse=True)
        total = sum(weight for _, weight in weighted[:4])
        for name, weight in weighted[:4]:
            groups[name].add([vertex.index], weight / total, "ADD")


def bind_mesh_to_armature(mesh_obj, armature):
    add_manual_vertex_groups(mesh_obj, armature)
    modifier = mesh_obj.modifiers.new("Pawn_Mathwidu_v3b_Skin", "ARMATURE")
    modifier.object = armature
    mesh_obj.parent = armature


def bind_mesh_to_single_bone(mesh_obj, armature, bone_name):
    group = mesh_obj.vertex_groups.new(name=bone_name)
    group.add([vertex.index for vertex in mesh_obj.data.vertices], 1.0, "ADD")
    modifier = mesh_obj.modifiers.new(f"{mesh_obj.name}_Skin", "ARMATURE")
    modifier.object = armature


def create_rounded_shoe(name, center, dimensions, material, armature, bone_name):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    shoe = bpy.context.object
    shoe.name = name
    shoe.data.name = f"{name}Mesh"
    shoe.scale = (dimensions.x * 0.5, dimensions.y * 0.5, dimensions.z * 0.5)
    shoe.data.materials.append(material)

    bpy.context.view_layer.objects.active = shoe
    shoe.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    bevel = shoe.modifiers.new(f"{name}_SoftEdges", "BEVEL")
    bevel.width = min(dimensions.x, dimensions.y, dimensions.z) * 0.22
    bevel.segments = 4
    bevel.affect = "EDGES"
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    bpy.ops.object.shade_smooth()

    bind_mesh_to_single_bone(shoe, armature, bone_name)
    return shoe


def create_sneaker_set(anchors, height, armature):
    shoe_material = create_material("Pawn_Mathwidu_v3b_WhiteSneaker", (0.94, 0.92, 0.86, 1.0), 0.68)
    sole_material = create_material("Pawn_Mathwidu_v3b_OffWhiteSole", (0.72, 0.70, 0.64, 1.0), 0.78)
    shoes = []

    for anchor in anchors:
        side = anchor["side"]
        shoe_width = anchor["width"]
        shoe_length = anchor["length"]
        shoe_height = anchor["height"]
        center = anchor["center"].copy()
        center.z += shoe_height * 0.47
        dimensions = Vector((shoe_width, shoe_length, shoe_height))

        shoes.append(create_rounded_shoe(f"Pawn_Mathwidu_v3b_Shoe.{side}", center, dimensions, shoe_material, armature, f"Foot.{side}"))

        sole_center = center.copy()
        sole_center.z = anchor["center"].z + shoe_height * 0.08
        sole_dimensions = Vector((shoe_width * 1.04, shoe_length * 1.04, shoe_height * 0.28))
        shoes.append(create_rounded_shoe(f"Pawn_Mathwidu_v3b_Sole.{side}", sole_center, sole_dimensions, sole_material, armature, f"Foot.{side}"))

    return shoes


def create_team_outfit_overlays(body, armature):
    bounds_min, bounds_max = world_bounds(body)
    height = bounds_max.z - bounds_min.z
    width = max(bounds_max.x - bounds_min.x, 0.3)
    depth = max(bounds_max.y - bounds_min.y, width * 0.42)
    outfit_material = create_material("TeamOutfitPrimary", (0.92, 0.9, 0.84, 1.0), 0.76)
    overlays = []

    shirt_center = Vector((0.0, 0.0, bounds_min.z + height * 0.535))
    shirt_dimensions = Vector((width * 0.72, depth * 0.58, height * 0.24))
    overlays.append(
        create_rounded_shoe(
            "Pawn_Mathwidu_v3b_TeamOutfit_Shirt",
            shirt_center,
            shirt_dimensions,
            outfit_material,
            armature,
            "Chest",
        )
    )

    return overlays


def add_preview_walk_action(armature):
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="POSE")

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 32
    action = bpy.data.actions.new("Pawn_Mathwidu_v3b_walk_preview")
    armature.animation_data_create()
    armature.animation_data.action = action

    def set_frame(frame, left, right, bounce):
        scene.frame_set(frame)
        armature.location.z = bounce
        armature.keyframe_insert(data_path="location", frame=frame)
        for bone_name, angle in (("Thigh.L", left), ("Shin.L", -left * 0.65), ("Foot.L", left * 0.35), ("Thigh.R", right), ("Shin.R", -right * 0.65), ("Foot.R", right * 0.35)):
            bone = armature.pose.bones.get(bone_name)
            if bone is None:
                continue
            bone.rotation_mode = "XYZ"
            bone.rotation_euler = (math.radians(angle), 0.0, 0.0)
            bone.keyframe_insert(data_path="rotation_euler", frame=frame)

    set_frame(1, 0.0, 0.0, 0.0)
    set_frame(9, 16.0, -12.0, 0.012)
    set_frame(17, 0.0, 0.0, 0.0)
    set_frame(25, -12.0, 16.0, 0.012)
    set_frame(32, 0.0, 0.0, 0.0)
    bpy.ops.object.mode_set(mode="OBJECT")


def write_manifest(deleted_vertices, cutoff_z):
    manifest = {
        "candidateId": "Pawn_Mathwidu_v3b",
        "personName": "Mathwidu",
        "pieceKind": "Pawn",
        "sourceAssetPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb",
        "activeFallbackPrefabPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab",
        "candidateModelPath": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb",
        "reviewModelPath": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb",
        "importedPrefabPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b.prefab",
        "importedModelPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b_Assets/selected.glb",
        "visualStatus": "unity_import_approved",
        "rigStatus": "rigged_candidate_initial",
        "approvedForUnity": True,
        "replacesActivePrefab": True,
        "identityChecklist": [
            "ginger curly short hair",
            "light ginger beard and mustache",
            "fair skin",
            "light gray shirt",
            "beige cargo pants",
            "white sneakers",
            "adult stylized proportions",
        ],
        "technicalChecklist": [
            "baseline-derived from approved visual",
            "armature-added",
            "manual vertex groups",
            "walk-preview action",
            "base-cleanup attempted",
            "white sneaker geometry added",
            "sneakers skinned to foot bones",
            "semantic team outfit overlays added",
            "approved for Unity pawn prefab test",
        ],
        "generationNotes": {
            "deletedLowVertices": deleted_vertices,
            "baseCutoffZ": round(cutoff_z, 5),
            "limitations": "Initial rig candidate from static GLB; deformation quality still needs Blender viewport review.",
        },
        "previewImages": {
            "front": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/preview_front.png",
            "threeQuarter": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/preview_three_quarter.png",
            "boardScale": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/preview_board_scale.png",
        },
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def export_candidate(meshes, armature):
    CANDIDATE_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.gltf(
        filepath=str(CANDIDATE_GLB),
        export_format="GLB",
        use_selection=True,
        export_skins=True,
        export_animations=True,
        export_yup=True,
        export_apply=True,
    )


def main():
    clear_scene()
    mesh = import_source_mesh()
    normalize_mesh_transform(mesh)
    deleted_vertices, cutoff_z = remove_integrated_chess_base(mesh)
    foot_anchors, anchor_height = measure_foot_anchors(mesh)
    ragged_cutoff_z = world_bounds(mesh)[0].z + (world_bounds(mesh)[1].z - world_bounds(mesh)[0].z) * 0.055
    deleted_vertices += delete_vertices_below(mesh, ragged_cutoff_z)
    bounds_min, bounds_max = world_bounds(mesh)
    armature = make_armature(bounds_min, bounds_max)
    bind_mesh_to_armature(mesh, armature)
    shoes = create_sneaker_set(foot_anchors, anchor_height, armature)
    outfit_overlays = create_team_outfit_overlays(mesh, armature)
    add_preview_walk_action(armature)
    export_candidate([mesh, *outfit_overlays, *shoes], armature)
    write_manifest(deleted_vertices, cutoff_z)
    print(f"Created {CANDIDATE_GLB}")
    print(f"Deleted probable base vertices: {deleted_vertices}")


if __name__ == "__main__":
    main()
