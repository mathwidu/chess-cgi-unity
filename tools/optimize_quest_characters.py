"""Run with Blender 5.2 in background; derive geometry without replacing Direction02 sources."""
import bpy
import argparse
import hashlib
import json
import math
import struct
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'art/quest3-optimization-20261026'
KINDS = ['Pawn', 'Rook', 'Knight', 'Bishop', 'Queen', 'King']
PROFILE = 'inspection'
EXPORT_NAME = 'Geometry.glb'

def reduction_ratio(name, count):
    # The table profile is only used at a distance. Bases retain their full silhouette.
    ratios = {'inspection': (.25, .5, .5, .16, .28),
              'table': (.10, .25, .20, .05, .08)}
    ratio, hand, patch, inlay, body = ratios[PROFILE]
    if 'HandSkin' in name: ratio = hand
    if 'FeevalePatch' in name: ratio = patch
    if 'Side_Inlay' in name: ratio = inlay
    if 'Body_White' in name: ratio = body
    if 'Side_Surface' in name or count < 200: ratio = 1
    return ratio

def triangles(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)

def repair_exported_tangents(path):
    """Give tiny triangles a valid tangent when the glTF exporter emits a zero vector."""
    data = bytearray(path.read_bytes())
    json_length = struct.unpack_from('<I', data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    binary_start = 20 + json_length + 8

    def offset(index, vertex, width):
        accessor = gltf['accessors'][index]
        view = gltf['bufferViews'][accessor['bufferView']]
        assert accessor['componentType'] == 5126 and 'sparse' not in accessor
        return binary_start + view.get('byteOffset', 0) + accessor.get('byteOffset', 0) + vertex * view.get('byteStride', width * 4)

    repaired = {'zero': 0, 'orthogonalized': 0}
    for mesh in gltf['meshes']:
        for primitive in mesh['primitives']:
            attributes = primitive['attributes']
            if 'TANGENT' not in attributes:
                continue
            for i in range(gltf['accessors'][attributes['TANGENT']]['count']):
                address = offset(attributes['TANGENT'], i, 4)
                tangent = struct.unpack_from('<4f', data, address)
                normal = Vector(struct.unpack_from('<3f', data, offset(attributes['NORMAL'], i, 3)))
                assert all(math.isfinite(v) for v in tangent) and abs(abs(tangent[3]) - 1) < .001
                assert all(math.isfinite(v) for v in normal) and abs(normal.length - 1) < .002
                normal.normalize()
                direction = Vector(tangent[:3])
                if direction.length_squared < 1e-12:
                    # A perpendicular unit vector is preferable to an undefined normal-map basis.
                    axis = min((Vector((1,0,0)), Vector((0,1,0)), Vector((0,0,1))), key=lambda a: abs(a.dot(normal)))
                    direction = (axis - normal * axis.dot(normal)).normalized()
                    struct.pack_into('<4f', data, address, *direction, tangent[3])
                    repaired['zero'] += 1
                elif abs(direction.dot(normal)) > 1e-5 or abs(direction.length - 1) > 1e-5:
                    # Split-normal transfer may leave the exported tangent outside the final normal plane.
                    direction = (direction - normal * direction.dot(normal)).normalized()
                    assert direction.length_squared > .99
                    struct.pack_into('<4f', data, address, *direction, tangent[3])
                    repaired['orthogonalized'] += 1
                assert abs(direction.length - 1) < .002
                assert abs(direction.dot(normal)) < .002
    path.write_bytes(data)
    return repaired

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def render(scene, camera, objects, path):
    scene.camera = camera
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)

def process(kind):
    source = ROOT / 'game/Assets/Art/Characters/Direction02' / kind / 'Model.glb'
    directory = OUTPUT / kind
    directory.mkdir(parents=True, exist_ok=False)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # UV/normal seams in glTF duplicate vertices; join compatible vertices before
    # the stronger reduction so adjacent triangles do not collapse independently.
    bpy.ops.import_scene.gltf(filepath=str(source), merge_vertices=PROFILE == 'table')
    objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    corners = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = Vector(tuple(min(point[i] for point in corners) for i in range(3)))
    high = Vector(tuple(max(point[i] for point in corners) for i in range(3)))
    height = high.z - low.z
    centre = (high + low) * .5
    assert height > 0
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 640
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.world = bpy.data.worlds.new('Comparison studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.18, .18, .18, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .5
    camera_data = bpy.data.cameras.new('ComparisonCamera')
    camera = bpy.data.objects.new('ComparisonCamera', camera_data)
    scene.collection.objects.link(camera)
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = height * 1.15
    camera.location = centre + Vector((height * 1.7, -height * 3.2, height * .75))
    aim(camera, centre)
    for name, offset, power in [('Key', (-2,-3,4), 750), ('Fill', (3,-1,2), 450), ('Rim',(0,3,3),650)]:
        data = bpy.data.lights.new(name, 'AREA')
        data.energy = power * height * height
        data.shape = 'DISK'
        data.size = height * 2
        light = bpy.data.objects.new(name, data)
        scene.collection.objects.link(light)
        light.location = centre + Vector(offset) * height
        aim(light, centre)
    render(scene, camera, objects, directory / 'before.png')
    report = {'kind': kind, 'source': str(source.relative_to(ROOT)),
              'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
              'blender': bpy.app.version_string, 'profile': PROFILE,
              'parts': [], 'source_height': height}
    for obj in objects:
        mesh = obj.data
        original_count = triangles(mesh)
        normal_source = obj.copy()
        normal_source.data = mesh.copy()
        normal_source.name = obj.name + '_NormalReference'
        scene.collection.objects.link(normal_source)
        normal_source.hide_render = True
        original_positions = [vertex.co.copy() for vertex in mesh.vertices]
        original_bounds = [Vector(corner) for corner in obj.bound_box]
        uv_count = len(mesh.uv_layers)
        material_names = [material.name for material in mesh.materials]
        ratio = reduction_ratio(obj.name, original_count)
        validated = False
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if ratio < 1:
            modifier = obj.modifiers.new('Quest derived mesh', 'DECIMATE')
            modifier.decimate_type = 'COLLAPSE'
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            # Merging seams can make duplicate faces coincide after collapse.
            # Clean them before transferring normals, rendering and counting;
            # otherwise the glTF exporter would silently change the saved mesh.
            if PROFILE == 'table':
                validated = obj.data.validate(clean_customdata=False)
            # Transfer the authored split normals; recomputing them on the reduced topology visibly facets faces/clothes.
            transfer = obj.modifiers.new('Preserve authored normals', 'DATA_TRANSFER')
            transfer.object = normal_source
            transfer.use_loop_data = True
            transfer.data_types_loops = {'CUSTOM_NORMAL'}
            transfer.loop_mapping = 'POLYINTERP_NEAREST'
            bpy.ops.object.modifier_apply(modifier=transfer.name)
        reference_mesh = normal_source.data
        bpy.data.objects.remove(normal_source, do_unlink=True)
        bpy.data.meshes.remove(reference_mesh)
        obj.select_set(False)
        mesh = obj.data
        mesh.calc_loop_triangles()
        assert len(mesh.uv_layers) == uv_count
        if 'Body_White' in obj.name or 'FeevalePatch' in obj.name:
            assert uv_count > 0
        assert [material.name for material in mesh.materials] == material_names
        assert all(math.isfinite(value) for vertex in mesh.vertices for value in vertex.co)
        tree = BVHTree.FromPolygons([v.co for v in mesh.vertices],
                                   [tuple(t.vertices) for t in mesh.loop_triangles], all_triangles=True)
        step = max(1, len(original_positions) // 5000)
        distances = [tree.find_nearest(point)[3] for point in original_positions[::step]]
        report['parts'].append({'node': obj.name, 'mesh': mesh.name,
             'original_triangles': original_count, 'optimized_triangles': triangles(mesh),
             'ratio_requested': ratio, 'uv_layers': len(mesh.uv_layers), 'materials': material_names,
             'validation_repaired': validated,
             'sampled_surface_error_max': max(distances),
             'sampled_surface_error_mm_at_13cm': max(distances) / height * 130})
    report['original_triangles'] = sum(x['original_triangles'] for x in report['parts'])
    report['optimized_triangles'] = sum(x['optimized_triangles'] for x in report['parts'])
    render(scene, camera, objects, directory / 'after.png')
    # Keep a material-complete editable derivation for visual review; the Unity export carries geometry only.
    bpy.ops.wm.save_as_mainfile(filepath=str(directory / (kind + '-quest.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(directory / EXPORT_NAME), export_format='GLB',
        use_selection=True, export_materials='PLACEHOLDER', export_normals=True,
        export_tangents=True, export_texcoords=True, export_yup=True, export_apply=False)
    report['tangent_repairs'] = repair_exported_tangents(directory / EXPORT_NAME)
    report['geometry_sha256'] = hashlib.sha256((directory / EXPORT_NAME).read_bytes()).hexdigest()
    (directory / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print('QUEST_GEOMETRY', kind, report['original_triangles'], report['optimized_triangles'], flush=True)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--profile', choices=['inspection', 'table'], default='inspection')
    parser.add_argument('--output', type=Path)
    parser.add_argument('kinds', nargs='*', choices=KINDS)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    PROFILE = args.profile
    if args.output:
        OUTPUT = args.output.resolve()
    elif PROFILE == 'table':
        OUTPUT = ROOT / 'art/quest3-table-detail-20261026'
    EXPORT_NAME = 'Table.glb' if PROFILE == 'table' else 'Geometry.glb'
    for kind in args.kinds or KINDS:
        process(kind)
