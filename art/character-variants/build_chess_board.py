"""Manufactured wooden board frame; the 64 interactive squares remain in Unity.

blender -b --python art/character-variants/build_chess_board.py
Blender units match BoardView (1.25 per square), Z up, white seated at -Y.
"""
from pathlib import Path
from collections import defaultdict
import bpy, math, json

HERE = Path(__file__).resolve().parent
OUT = HERE / 'tabletop-polish-20260926' / 'board-source'
ART = HERE.parents[1] / 'game/Assets/Art/Environment/ChessBoard'
OUT.mkdir(parents=True, exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 1

def solid(name, color, roughness=.45, metallic=0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = roughness
    p.inputs['Metallic'].default_value = metallic
    return m

def wood(name, low, high):
    m = solid(name, low)
    nodes, links = m.node_tree.nodes, m.node_tree.links
    p = nodes.get('Principled BSDF')
    uv = nodes.new('ShaderNodeTexCoord')
    stretch = nodes.new('ShaderNodeVectorMath'); stretch.operation = 'MULTIPLY'
    stretch.inputs[1].default_value = (3, 110, 1)
    links.new(uv.outputs['UV'], stretch.inputs[0])
    warp = nodes.new('ShaderNodeTexNoise'); warp.inputs['Scale'].default_value = 3
    links.new(uv.outputs['UV'], warp.inputs[0])
    amount = nodes.new('ShaderNodeVectorMath'); amount.operation = 'SCALE'
    amount.inputs[3].default_value = 1.8
    links.new(warp.outputs['Color'], amount.inputs[0])
    add = nodes.new('ShaderNodeVectorMath'); add.operation = 'ADD'
    links.new(stretch.outputs[0], add.inputs[0]); links.new(amount.outputs[0], add.inputs[1])
    grain = nodes.new('ShaderNodeTexNoise'); grain.inputs['Scale'].default_value = 1.7
    grain.inputs['Detail'].default_value = 3
    links.new(add.outputs[0], grain.inputs[0])
    colour = nodes.new('ShaderNodeMixRGB')
    colour.inputs[1].default_value = (*low,1); colour.inputs[2].default_value = (*high,1)
    links.new(grain.outputs['Fac'], colour.inputs[0])
    rough = nodes.new('ShaderNodeMapRange')
    rough.inputs['To Min'].default_value = .36; rough.inputs['To Max'].default_value = .48
    links.new(grain.outputs['Fac'], rough.inputs[0])
    bump = nodes.new('ShaderNodeBump'); bump.inputs['Distance'].default_value = .002
    bump.inputs['Strength'].default_value = .16
    links.new(grain.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], p.inputs['Normal'])
    bpy.ops.mesh.primitive_plane_add(size=1)
    plane = bpy.context.object; plane.data.materials.append(m)
    scene.render.bake.use_pass_direct = False; scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True; scene.render.bake.margin = 8
    maps = {}
    for suffix, output in [('Albedo',colour.outputs[0]),('Roughness',rough.outputs[0]),('Normal',None)]:
        image = bpy.data.images.new(name+'_'+suffix, 1024, 1024)
        if suffix != 'Albedo': image.colorspace_settings.name = 'Non-Color'
        dest = nodes.new('ShaderNodeTexImage'); dest.image = image; nodes.active = dest
        if output is not None: links.new(output, p.inputs['Base Color'])
        bpy.ops.object.bake(type='NORMAL' if suffix == 'Normal' else 'DIFFUSE')
        image.filepath_raw = str(OUT/(image.name+'.png')); image.file_format='PNG'; image.save()
        maps[suffix] = dest
    bpy.data.objects.remove(plane, do_unlink=True)
    links.new(maps['Albedo'].outputs['Color'],p.inputs['Base Color'])
    links.new(maps['Roughness'].outputs['Color'],p.inputs['Roughness'])
    normal = nodes.new('ShaderNodeNormalMap'); normal.inputs['Strength'].default_value=.35
    links.new(maps['Normal'].outputs['Color'],normal.inputs['Color'])
    links.new(normal.outputs['Normal'],p.inputs['Normal'])
    return m

maple = wood('Board_Maple', (.49,.375,.225), (.64,.52,.35))
walnut = wood('Board_Walnut', (.075,.036,.019), (.145,.081,.043))
frame = wood('Board_FrameWalnut', (.043,.020,.013), (.095,.049,.027))
ivory = solid('Board_CoordinateIvory',(.83,.74,.52),.65)
inlay = solid('Board_Inlay',(.38,.23,.075),.40,.3)
rubber = solid('Board_Underside',(.025,.023,.021),.92)

def finish(o, mat, bevel=0):
    o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new('Soft manufactured edge','BEVEL');mod.width=bevel;mod.segments=3
        mod=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    return o

def box(name, loc, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,mat,bevel)

box('Board body',(0,0,-.28),(11.28,11.28,.50),frame,.045)
# The underside reaches exactly -0.54, matching the tabletop at 0.7557 m.
box('Non-slip underside',(0,0,-.5325),(11.05,11.05,.015),rubber,.004)

# Four mitred rails, flush with the tops of the interactive square cubes.
for index in range(4):
    outline=[(-5,-5),(5,-5),(5.64,-5.64),(-5.64,-5.64)]
    verts=[(x,y,z) for z in [-.055,.04] for x,y in outline]
    faces=[(0,1,2,3),(7,6,5,4),(4,5,1,0),(5,6,2,1),(6,7,3,2),(7,4,0,3)]
    mesh=bpy.data.meshes.new('Mitred rail');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Frame rail '+str(index),mesh);scene.collection.objects.link(o)
    o.rotation_euler.z=index*math.pi/2
    finish(o,frame,.012)

for x in [-5.045,5.045]:box('Inlaid pinstripe',(x,0,.039),( .025,10.115,.006),inlay)
for y in [-5.045,5.045]:box('Inlaid pinstripe',(0,y,.039),(10.115,.025,.006),inlay)

for i in range(8):
    coordinate=(i-3.5)*1.25
    for label, x,y, angle,side in [
        (chr(65+i),coordinate,-5.34,0,'WhiteFile'),
        (chr(65+i),coordinate,5.34,math.pi,'BlackFile'),
        (str(i+1),-5.34,coordinate,0,'WhiteRank'),
        (str(i+1),5.34,coordinate,math.pi,'BlackRank')]:
        c=bpy.data.curves.new(side+label,'FONT');c.body=label;c.size=.31
        c.align_x='CENTER';c.align_y='CENTER';c.resolution_u=4;c.extrude=0
        o=bpy.data.objects.new('Coordinate_'+side+'_'+label,c);scene.collection.objects.link(o)
        o.location=(x,y,.044);o.rotation_euler.z=angle;c.materials.append(ivory)

# The importer uses these materials for BoardView's existing 64 square renderers.
# Sampling meshes are removed from the runtime prefab by ChessBoardImport.
box('MaterialSample_Maple',(0,0,-2),(.1,.1,.1),maple)
box('MaterialSample_Walnut',(.2,0,-2),(.1,.1,.1),walnut)

for o in scene.objects:
    if o.type!='MESH':continue
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='WoodGrain')
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda a:abs(poly.normal[a]))
        axes=(0,1) if axis==2 else ((0,2) if axis==1 else (1,2))
        for li in poly.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(v[axes[0]]/5.5+.5,v[axes[1]]/2+.5)

bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ChessBoard.blend'),compress=True)
# Keep the 32 named coordinate nodes as review markers, but combine their geometry
# into one draw. Named empty markers retain the authored orientation for tests.
groups=defaultdict(list)
for o in list(scene.objects):
    if o.name.startswith('MaterialSample_'):continue
    if o.type=='FONT':
        marker_name=o.name;o.name='Glyph_'+o.name
        marker=bpy.data.objects.new(marker_name,None);scene.collection.objects.link(marker)
        marker.matrix_world=o.matrix_world.copy()
    groups[o.data.materials[0].name].append(o)
for material,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
    bpy.context.object.name=material
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ART/'ChessBoard.glb'),export_format='GLB',use_selection=True,
    export_yup=True,export_apply=True,export_animations=False,export_cameras=False,export_lights=False)
(OUT/'model-report.json').write_text(json.dumps({'playing_width_units':10,'frame_width_units':11.28,
    'top_units':.04,'bottom_units':-.54,'vr_thickness_m':.0261,'coordinates':32,
    'interactive_squares_in_asset':0,'colliders_in_asset':0,'headset_tested':False},indent=2)+'\n')
print('CHESS_BOARD_EXPORTED',flush=True)
