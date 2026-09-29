"""Editable, metre-scale lab based on the user's Feevale classroom recording.

blender -b --python art/character-variants/build_feevale_lab.py
Exports environment only. The chessboard and characters remain owned by Unity.
"""
from pathlib import Path
from collections import defaultdict
import bpy, math, json, random
from mathutils import Vector, Matrix

HERE = Path(__file__).resolve().parent
OUT = HERE / 'feevale-room-v3-20260928' / 'lab-source'
ART = HERE.parents[1] / 'game/Assets/Art/Environment/FeevaleLab'
OUT.mkdir(parents=True, exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)
random.seed(24)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'Feevale_Lab_Authored_Metres'
scene.unit_settings.system = 'METRIC'

def material(name, color, rough=.65, metal=0, emission=0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    if emission:
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = emission
    return m

wall = material('ClassroomGreyPaint', (.34, .345, .355), .9)
ceiling = material('CeilingPaint', (.67, .68, .67), .9, emission=.08)
blue_wall = material('ClassroomBluePaint', (.075, .25, .70), .9)
orange_wall = material('WindowOchrePaint', (.82, .37, .050), .88)
underlay = material('FloorUnderlay', (.17, .105, .054), .9)
laminate = material('IvoryLaminate', (.63, .64, .61), .62)
edge = material('LaminateEdge', (.25, .18, .10), .61)
white = material('BoardEnamel', (.82, .84, .81), .32)
alum = material('BrushedAluminium', (.43, .47, .49), .38, .65)
metal = material('GraphiteSteel', (.045, .053, .055), .45, .35)
plastic = material('PCGraphite', (.024, .029, .032), .48)
keys = material('KeyboardKeys', (.060, .065, .070), .57)
fabric = material('ChairFabric', (.034, .040, .043), .95)
screen = material('SleepingDisplay', (.011, .019, .024), .22, .18)
blind = material('WovenVerticalBlinds', (.32, .335, .33), .90)
glass = material('NightWindowGlass', (.016, .021, .027), .16, .48)
light = material('CeilingDiffuser', (.91, .94, .96), .8, emission=1.3)
ink = material('GraphiteLettering', (.045, .055, .057), .8)
wood = material('OakLaminate', (.39, .27, .15), .63)
deskwood = material('NaturalAshDesk', (.44, .32, .20), .58)
storage_teal = material('StorageTeal', (.028, .25, .29), .72)
storage_ochre = material('StorageOchre', (.63, .43, .045), .72)
storage_rose = material('StorageRose', (.41, .09, .20), .72)
legacy_beige = material('LegacyEquipmentBeige', (.39, .38, .29), .70)
display = material('NotebookDisplay', (.045, .07, .085), .52, emission=.08)

# Shared, physically scaled surface maps. Baking actual shader values preserves
# detail in glTF; no lighting or shadows are painted into the base colour.
def bake_surface(mat, kind, low, high, resolution=1024):
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    p = nodes.get('Principled BSDF')
    coord = nodes.new('ShaderNodeTexCoord')
    scale = nodes.new('ShaderNodeVectorMath'); scale.operation = 'MULTIPLY'
    scale.inputs[1].default_value = (3.5, 145, 1) if kind == 'wood' else (155, 155, 1)
    links.new(coord.outputs['UV'], scale.inputs[0])
    noise = nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 2
    noise.inputs['Detail'].default_value = 3; noise.inputs['Roughness'].default_value = .68
    if kind == 'wood':
        # Low-frequency warp breaks the perfectly straight procedural stripes.
        warp = nodes.new('ShaderNodeTexNoise'); warp.inputs['Scale'].default_value = 2.7
        warp.inputs['Detail'].default_value = 2
        links.new(coord.outputs['UV'], warp.inputs['Vector'])
        amount = nodes.new('ShaderNodeVectorMath'); amount.operation = 'SCALE'; amount.inputs[3].default_value = 1.15
        links.new(warp.outputs['Color'], amount.inputs[0])
        add = nodes.new('ShaderNodeVectorMath'); add.operation = 'ADD'
        links.new(scale.outputs[0], add.inputs[0]); links.new(amount.outputs[0], add.inputs[1])
        links.new(add.outputs[0], noise.inputs['Vector'])
    else:
        links.new(scale.outputs['Vector'], noise.inputs['Vector'])
    colour = nodes.new('ShaderNodeMixRGB')
    colour.inputs[1].default_value = (*low, 1); colour.inputs[2].default_value = (*high, 1)
    links.new(noise.outputs['Fac'], colour.inputs[0])
    rough = nodes.new('ShaderNodeMapRange')
    rough.inputs['From Min'].default_value = 0; rough.inputs['From Max'].default_value = 1
    rough.inputs['To Min'].default_value = .48 if kind == 'wood' else .74
    rough.inputs['To Max'].default_value = .60 if kind == 'wood' else .93
    links.new(noise.outputs['Fac'], rough.inputs['Value'])
    bump = nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .20
    bump.inputs['Distance'].default_value = .0003 if kind == 'wood' else .00035
    links.new(noise.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], p.inputs['Normal'])
    bpy.ops.mesh.primitive_plane_add(size=1)
    plane = bpy.context.object; plane.data.materials.append(mat)
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 1
    scene.render.bake.use_pass_direct = False; scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True; scene.render.bake.margin = 8
    maps = {}
    for suffix, output in [('Albedo', colour.outputs[0]), ('Roughness', rough.outputs[0]), ('Normal', None)]:
        image = bpy.data.images.new(mat.name+'_'+suffix, resolution, resolution)
        if suffix != 'Albedo': image.colorspace_settings.name = 'Non-Color'
        dest = nodes.new('ShaderNodeTexImage'); dest.image = image; nodes.active = dest
        if output is not None: links.new(output, p.inputs['Base Color'])
        bpy.ops.object.bake(type='NORMAL' if suffix == 'Normal' else 'DIFFUSE')
        image.filepath_raw = str(OUT/(image.name+'.png')); image.file_format = 'PNG'; image.save()
        maps[suffix] = dest
    bpy.data.objects.remove(plane, do_unlink=True)
    links.new(maps['Albedo'].outputs['Color'], p.inputs['Base Color'])
    links.new(maps['Roughness'].outputs['Color'], p.inputs['Roughness'])
    normal = nodes.new('ShaderNodeNormalMap'); normal.inputs['Strength'].default_value = .65
    links.new(maps['Normal'].outputs['Color'], normal.inputs['Color'])
    links.new(normal.outputs['Normal'], p.inputs['Normal'])

bake_surface(deskwood, 'wood', (.335,.268,.183), (.46,.385,.277), 2048)
bake_surface(wall, 'paint', (.325,.332,.342), (.355,.362,.372))
bake_surface(fabric, 'fabric', (.020,.025,.027), (.043,.049,.051))
bake_surface(blind, 'fabric', (.28,.29,.28), (.36,.37,.36))

def box(name, loc, size, mat, bevel=.005, group='Furniture'):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object; o.name = name; o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat); o['batch'] = group
    if bevel:
        b = o.modifiers.new('Manufactured edge radius', 'BEVEL'); b.width = bevel; b.segments = 3
        n = o.modifiers.new('Face normals', 'WEIGHTED_NORMAL'); n.keep_sharp = True
    return o

def rod(name, a, b, radius, mat, vertices=12, group='Furniture'):
    a, b = Vector(a), Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=(b-a).length, location=(a+b)/2)
    o = bpy.context.object; o.name = name
    o.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    o.data.materials.append(mat); o['batch'] = group
    for p in o.data.polygons: p.use_smooth = len(p.vertices) == 4
    return o

def label(text, loc, size, mat, group='Signage'):
    c = bpy.data.curves.new(text, 'FONT'); c.body = text; c.size = size; c.extrude = 0
    c.resolution_u = 4
    o = bpy.data.objects.new(text, c); scene.collection.objects.link(o)
    o.location = loc; o.rotation_euler = (math.pi/2, 0, 0)
    c.materials.append(mat); o['batch'] = group
    return o

def marker(name, loc):
    o = bpy.data.objects.new(name, None); scene.collection.objects.link(o); o.location = loc
    return o

# Bake Blender's procedural wood to an ordinary albedo texture for glTF/URP.
# This is a material bake, not a generated picture of a classroom.
nodes, links = wood.node_tree.nodes, wood.node_tree.links
p = nodes.get('Principled BSDF')
texcoord = nodes.new('ShaderNodeTexCoord')
scale = nodes.new('ShaderNodeVectorMath'); scale.operation = 'MULTIPLY'
scale.inputs[1].default_value = (3, 110, 1); links.new(texcoord.outputs['UV'], scale.inputs[0])
noise = nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 2.5
noise.inputs['Detail'].default_value = 3; noise.inputs['Roughness'].default_value = .6
links.new(scale.outputs['Vector'], noise.inputs['Vector'])
mix = nodes.new('ShaderNodeMixRGB'); mix.inputs[1].default_value = (.285, .205, .115, 1); mix.inputs[2].default_value = (.46, .35, .22, 1)
links.new(noise.outputs['Fac'], mix.inputs[0]); links.new(mix.outputs[0], p.inputs['Base Color'])
image = bpy.data.images.new('OakLaminate_Albedo', 1024, 1024)
dest = nodes.new('ShaderNodeTexImage'); dest.image = image; nodes.active = dest
bpy.ops.mesh.primitive_plane_add(size=1)
bake_plane = bpy.context.object; bake_plane.data.materials.append(wood)
scene.render.engine = 'CYCLES'; scene.cycles.samples = 1
scene.render.bake.use_pass_direct = False; scene.render.bake.use_pass_indirect = False
scene.render.bake.use_pass_color = True; scene.render.bake.margin = 8
bpy.ops.object.bake(type='DIFFUSE')
image.filepath_raw = str(OUT/'OakLaminate_Albedo.png'); image.file_format = 'PNG'; image.save()
bpy.data.objects.remove(bake_plane, do_unlink=True)
links.new(dest.outputs['Color'], p.inputs['Base Color'])

# The reference establishes materials and proportions, not measured dimensions.
# Preserve the metre-scale room envelope and all interaction anchors.
box('Subfloor', (0, .1, -.067), (6.8, 9.4, .11), underlay, 0, 'Shell')
for j in range(52):
    y = -4.6 + (j+.5)*9.4/52
    offset = [0, .38, .75][j%3]
    edges = [-3.4] + [x for x in [-3.4+i*1.13+offset for i in range(8)] if -3.399 < x < 3.399] + [3.4]
    for i,(start,end) in enumerate(zip(edges, edges[1:])):
        tile = box('Oak floor plank', ((start+end)/2, y, -.006), (end-start-.0012, 9.4/52-.0012, .012), wood, 0, 'Floor')
        for poly in tile.data.polygons:
            for li in poly.loop_indices:
                v = tile.data.vertices[tile.data.loops[li].vertex_index].co
                tile.data.uv_layers.active.data[li].uv = (v.x/1.13+.5+i*.071, v.y/.181+.5+j*.027)

box('Projection wall', (0, 4.87, 1.65), (6.94, .14, 3.3), wall, .004, 'Shell')
box('Blue teaching wall', (0, -4.67, 1.65), (6.94, .14, 3.3), blue_wall, .004, 'Shell')
box('Right ochre sill wall', (3.47, .1, .415), (.14, 9.4, .83), orange_wall, .004, 'Shell')
box('Right ochre lintel', (3.47, .1, 3.06), (.14, 9.4, .48), orange_wall, .004, 'Shell')
for y in [-4.5, -1.425, 1.625, 4.7]:
    box('Ochre window pier', (3.47,y,1.82), (.14,.29,1.99), orange_wall,.004,'Shell')
for y in [-2.95,.1,3.15]:
    width, bottom, top = 2.76, .83, 2.82
    box('Dark evening glass', (3.505,y,1.825), (.018,width,top-bottom), glass,0,'Shell')
    box('Window sill', (3.38,y,.829), (.22,width+.10,.035), laminate,.004,'Shell')
    for offset in [-width/2,-width/4,0,width/4,width/2]:
        box('White vertical window frame', (3.39,y+offset,1.825), (.08,.035,top-bottom), laminate,.002,'Shell')
    for z in [bottom,1.47,2.37,top]:
        box('White horizontal window frame', (3.39,y,z), (.08,width,.035), laminate,.002,'Shell')
    for offset in [-width/4,width/4]:
        box('Sliding window catch', (3.344,y+offset,1.67), (.020,.023,.105), metal,.003,'Shell')

box('Left sill wall', (-3.47,.1,.42), (.14,9.4,.84), wall,.004,'Shell')
box('Left lintel', (-3.47,.1,3.13), (.14,9.4,.34), wall,.004,'Shell')
box('Glass behind blinds', (-3.49,.1,1.93), (.018,9.4,2.06), glass,0,'Shell')
for y in [-4.59,-2.25,.1,2.45,4.79]:
    box('Blind wall window mullion', (-3.385,y,1.93), (.11,.035,2.06), laminate,.002,'Shell')
for z in [.90,1.80,2.96]:
    box('Blind wall window rail', (-3.39,.1,z), (.11,9.4,.035), laminate,.002,'Shell')
for j in range(76):
    y = -4.50+j*.122
    slat = box('Grey woven vertical blind', (-3.27,y,1.91), (.008,.109,2.04), blind,.001,'Shell')
    slat.rotation_euler.z = -.27
    box('Blind bottom weight', (-3.27,y,.902), (.010,.095,.022), blind,.002,'Shell')
    if j < 75:
        rod('Blind retaining chain',(-3.258,y,.891),(-3.23,y+.061,.867),.002,laminate,6,'Shell')
        rod('Blind retaining chain',(-3.23,y+.061,.867),(-3.258,y+.122,.891),.002,laminate,6,'Shell')
box('Blind headrail', (-3.27,.1,2.96), (.065,9.3,.045), laminate,.002,'Shell')

# White low storage: alternating open cubbies and the coloured doors in the video.
for i in range(10):
    y = -3.88+i*.84
    box('Storage back', (-3.33,y,.435), (.023,.832,.69), laminate,.002)
    for z in [.09,.79]: box('Storage horizontal', (-3.12,y,z), (.44,.836,.025), laminate,.003)
    for dy in [-.418,.418]: box('Storage divider', (-3.12,y+dy,.44), (.44,.018,.70), laminate,.002)
    if i%3 == 1:
        box('Open cubby shelf', (-3.12,y,.438), (.43,.817,.018), laminate,.002)
    else:
        colour = [storage_ochre,storage_teal,storage_rose,laminate][i%4]
        box('Coloured cabinet door', (-2.889,y,.441), (.020,.794,.642), colour,.003)
        rod('Small cabinet pull', (-2.874,y+.26,.47), (-2.860,y+.26,.47), .012, alum,12)
# Three low-profile teaching/network devices; no logos or student information.
for i,y in enumerate([-2.62,-1.71,2.51]):
    box('Legacy equipment housing', (-3.105,y,.877), (.335,.43,.15), legacy_beige,.009)
    for n in range(12):
        box('Equipment ventilation', (-3.11,y-.165+n*.029,.954), (.24,.012,.002), metal,0)
    box('Equipment front panel',(-2.932,y,.88),(.004,.39,.112),legacy_beige,.003)
    for n in range(3):
        box('Equipment connector', (-2.928,y-.10+n*.066,.875), (.008,.027,.019), plastic,.001)

# Concrete beams cross the room. Single linear lamps run along the window
# wall, perpendicular to the beams, as corrected by the user from the video.
box('Ceiling slab', (0,.1,3.35), (6.8,9.4,.10), ceiling,0,'Shell')
for y in [-3.10,.10,3.30]:
    box('Exposed ceiling beam', (0,y,3.15), (6.8,.23,.30), ceiling,.004,'Shell')
for x in [-3.29,3.29]:
    box('Ceiling perimeter beam', (x,.1,3.15), (.22,9.4,.30), ceiling,.004,'Shell')
for x in [-2.25,0,2.25]:
    box('Luminaire suspension rail', (x,.1,2.955), (.035,8.95,.035), alum,.003,'Shell')
    for y in [-3.65,-.65,2.35]:
        rod('Suspension rod',(x,y,2.97),(x,y,3.30),.005,alum,8,'Shell')
    for y in [-3.65,-2.15,-.65,.85,2.35,3.85]:
        box('Single linear luminaire',(x,y,2.916),(.043,1.37,.030),laminate,.003,'Shell')
        rod('Single white light tube',(x,y-.644,2.887),(x,y+.644,2.887),.011,light,12,'Shell')
        for dy in [-.656,.656]:
            box('Tube end cap',(x,y+dy,2.890),(.044,.035,.029),laminate,.003,'Shell')
# Cable trays follow the visible room perimeter; short rungs preserve openness.
for x in [-3.08,3.10]:
    for dx in [-.082,.082]: box('Cable tray side',(x+dx,.1,3.005),(.012,9.25,.054),alum,.002,'Shell')
    for j in range(64): box('Cable tray rung',(x,-4.48+j*.145,2.983),(.17,.015,.008),alum,0,'Shell')
for y in [-4.38,4.58]:
    for dy in [-.082,.082]: box('Cable tray end',(0,y+dy,3.005),(6.22,.012,.054),alum,.002,'Shell')
    for j in range(43): box('End tray rung',(-3.06+j*.145,y,2.983),(.015,.17,.008),alum,0,'Shell')
for x in [-3.395,3.395]: box('Skirting',(x,.1,.045),(.02,9.4,.09),laminate,.002,'Shell')
for y in [-4.595,4.795]: box('Skirting',(0,y,.045),(6.8,.02,.09),laminate,.002,'Shell')

def desk(name, x, y, width, depth, top=.75, grommet=True, facing=0):
    before = set(bpy.data.objects)
    # Thin oak laminate, T-legs and black cable grommets match the ruler photograph.
    # The structural top must stay below the veneer: coplanar faces flicker
    # and replace the wood texture with the solid edge-band colour in Unity.
    box(name+' edge', (x,y,top-.018), (width,depth,.028), edge,.005)
    box(name+' laminate', (x,y,top-.004), (width-.006,depth-.006,.008), deskwood,.004)
    for dx in [-width/2+.105,width/2-.105]:
        box(name+' upright', (x+dx,y,(top-.04)/2+.02), (.055,.115,top-.04), alum,.005)
        box(name+' T foot', (x+dx,y,.035), (.10,depth-.105,.052), alum,.013)
        for dy in [-depth/2+.08,depth/2-.08]:
            box(name+' levelling pad',(x+dx,y+dy,.010),(.073,.052,.017),plastic,.005)
    box(name+' modesty panel', (x,y+depth*.26,.53), (width-.22,.022,.29), laminate,.003)
    if grommet:
        rod(name+' cable grommet',(x+width*.30,y+depth*.26,top),(x+width*.30,y+depth*.26,top+.0028),.031,plastic,24)
        box(name+' grommet recess',(x+width*.30,y+depth*.26-.017,top+.003),(.025,.010,.002),metal,.002)
    if facing:
        from mathutils import Matrix
        transform = Matrix.Translation((x,y,0)) @ Matrix.Rotation(facing,4,'Z') @ Matrix.Translation((-x,-y,0))
        for o in set(bpy.data.objects)-before: o.matrix_world = transform @ o.matrix_world

def chair(name, x, y, facing=0):
    before = set(bpy.data.objects)
    box(name+' cushion', (x,y,.47), (.43,.405,.065), fabric, .042)
    box(name+' seat shell', (x,y,.431), (.421,.40,.023), plastic, .025)
    # Back has a curved outline, a shallow wrap, and a real thickness.
    verts=[]
    for side in [-1,1]:
        for row in range(7):
            t=row/6; z=.57+t*.38; half=.178+.04*math.sin(t*math.pi)
            for col in range(9):
                u=col/8*2-1
                verts.append((x+u*half,y-.19-.045*t+.035*u*u+side*.014,z))
    faces=[]; n=63
    for side in range(2):
        for row in range(6):
            for col in range(8):
                k=side*n+row*9+col
                q=(k,k+1,k+10,k+9); faces.append(q if side else tuple(reversed(q)))
    perimeter=list(range(9))+[r*9+8 for r in range(1,7)]+list(range(61,53,-1))+[r*9 for r in range(5,0,-1)]
    for a,b in zip(perimeter,perimeter[1:]+perimeter[:1]): faces.append((a,b,b+n,a+n))
    mesh=bpy.data.meshes.new(name+' curved back'); mesh.from_pydata(verts,[],faces); mesh.materials.append(fabric)
    o=bpy.data.objects.new(name+' curved back',mesh); scene.collection.objects.link(o); o['batch']='Furniture'
    for poly in mesh.polygons: poly.use_smooth=True
    rod(name+' back support',(x,y-.20,.39),(x,y-.23,.69),.021,plastic)
    rod(name+' lift',(x,y,.12),(x,y,.425),.027,metal,16)
    for i in range(5):
        a=i*math.tau/5; dx,dy=math.cos(a)*.255,math.sin(a)*.255
        rod(name+' spoke',(x,y,.16),(x+dx,y+dy,.082),.021,plastic)
        rod(name+' caster',(x+dx-.021,y+dy,.056),(x+dx+.021,y+dy,.056),.042,plastic,12)
    for dx in [-.255,.255]:
        rod(name+' arm support',(x+dx,y+.015,.41),(x+dx,y+.015,.65),.013,metal)
        box(name+' armrest',(x+dx,y-.018,.66),(.049,.245,.035),plastic,.015)
    if facing:
        from mathutils import Matrix
        transform=Matrix.Translation((x,y,0)) @ Matrix.Rotation(facing,4,'Z') @ Matrix.Translation((-x,-y,0))
        for o in set(bpy.data.objects)-before: o.matrix_world=transform @ o.matrix_world

def notebook(x, y, top=.75, facing=0, opened=True, number=0):
    from mathutils import Matrix
    before = set(bpy.data.objects)
    base = top+.008
    box('Notebook chassis',(x,y,base),(.343,.235,.016),plastic,.005)
    if opened:
        for row in range(5):
            for col in range(12):
                box('Notebook key',(x-.144+col*.026,y-.025+row*.022,top+.018),(.022,.017,.003),keys,0)
        box('Notebook space bar',(x,y-.047,top+.018),(.105,.014,.003),keys,0)
        box('Trackpad outline',(x,y-.083,top+.017),(.112,.050,.0015),metal,.003)
        box('Trackpad',(x,y-.083,top+.018),(.108,.046,.001),keys,.002)
        lid_objects = set(bpy.data.objects)
        hinge_y, hinge_z = y+.109, top+.017
        box('Notebook lid',(x,hinge_y,hinge_z+.111),(.343,.012,.222),plastic,.005)
        box('Notebook glass',(x,hinge_y-.0068,hinge_z+.114),(.319,.0015,.193),display if number%3 else screen,.002)
        if number%3:
            # Abstract application blocks only. Never reproduce classmates' screens.
            box('Notebook application header',(x,hinge_y-.008,hinge_z+.198),(.311,.0008,.014),keys,0)
            box('Notebook application sidebar',(x-.127,hinge_y-.008,hinge_z+.11),(.046,.0008,.154),metal,0)
            for row in range(4):
                box('Notebook abstract line',(x+.02,hinge_y-.0085,hinge_z+.163-row*.027),(.19-row*.018,.0008,.004),keys,0)
        tilt = Matrix.Translation((x,hinge_y,hinge_z)) @ Matrix.Rotation(math.radians(-13),4,'X') @ Matrix.Translation((-x,-hinge_y,-hinge_z))
        for o in set(bpy.data.objects)-lid_objects: o.matrix_world = tilt @ o.matrix_world
    else:
        box('Closed notebook lid',(x,y,top+.024),(.343,.235,.015),metal,.005)
    for side in [-1,1]:
        box('Notebook USB port',(x+side*.172,y+.031,base),(.0015,.019,.005),screen,0)
    if opened and number%4 == 0:
        rod('Notebook power lead',(x+.174,y+.072,base),(x+.32,y+.15,base),.0027,plastic,6)
        rod('Notebook cable drop',(x+.32,y+.15,base),(x+.32,y+.15,top-.15),.0027,plastic,6)
    transform = Matrix.Translation((x,y,0)) @ Matrix.Rotation(facing,4,'Z') @ Matrix.Translation((-x,-y,0))
    for o in set(bpy.data.objects)-before: o.matrix_world = transform @ o.matrix_world

# The chess table remains clear and its top stays in contact with the board base.
desk('Chess table',0,0,1.30,.90,.7557)
marker('ChessTableSurface',(0,0,.7557))
marker('BoardAnchor',(0,0,.78))
marker('SeatedEyeWhite',(0,-.6,1.2))
marker('SeatedEyeBlack',(0,.6,1.2))
marker('WindowSide',(-3.4,.1,1.8))
marker('RightWindows',(3.4,.1,1.825))
chair('Player chair',0,-.91)
chair('Opponent chair',0,.91,math.pi)
# Four groups of four individual desks, with seams and paired T-frames.
# Count/layout are a game adaptation; the video is not a measured floor plan.
notebook_count = 0
for island_y in [-2.63,2.63]:
    for island_x in [-1.45,1.56]:
        island_objects = set(bpy.data.objects)
        for dx in [-.557,.557]:
            for dy in [-.329,.329]:
                x,y = island_x+dx,island_y+dy
                facing = 0 if dy < 0 else math.pi
                desk('Student desk',x,y,1.106,.650,facing=facing)
                notebook(x,y,.75,facing,notebook_count%5 != 2,notebook_count)
                chair('Student chair',x,y+(-.60 if dy < 0 else .60),facing)
                notebook_count += 1
        # Confirmed by the user: students face blinds/windows, with the
        # projection board to their side. Rotate the complete furniture group.
        turn = Matrix.Translation((island_x,island_y,0)) @ Matrix.Rotation(math.pi/2,4,'Z') @ Matrix.Translation((-island_x,-island_y,0))
        for o in set(bpy.data.objects)-island_objects: o.matrix_world = turn @ o.matrix_world

desk('Teaching desk',0,4.35,1.50,.64)
notebook(0,4.35,.75,0,True,notebook_count)
notebook_count += 1
chair('Teaching chair',0,3.76)

# Two real whiteboard positions, rather than a large invented branded wall sign.
def whiteboard(name, x, y, reverse=False):
    direction = 1 if reverse else -1
    box(name+' frame',(x,y,1.80),(3.45,.045,1.18),alum,.007,'Shell')
    box(name+' enamel',(x,y+direction*.028,1.80),(3.40,.014,1.13),white,.003,'Shell')
    box(name+' tray',(x,y+direction*.082,1.202),(3.43,.135,.018),alum,.003,'Shell')
    box(name+' eraser',(x-.90,y+direction*.082,1.226),(.115,.042,.026),plastic,.004,'Signage')
    for dx in [-1.35,-1.17]:
        rod(name+' marker',(x+dx,y+direction*.085,1.224),(x+dx+.11,y+direction*.085,1.224),.006,ink,10,'Signage')
whiteboard('Projection whiteboard',-.35,4.748)
whiteboard('Blue wall whiteboard',.27,-4.553,True)
# A restrained generic teaching diagram gives the front board a live-room cue.
label('Computação Gráfica',(-1.77,4.705,2.15),.082,ink)
label('Modelos  /  transformação  /  luz',(-1.77,4.705,1.99),.052,ink)
for a,b in [((-1.50,1.51),(-1.07,1.51)),((-1.50,1.51),(-1.50,1.82)),((-1.50,1.51),(-1.30,1.66))]:
    rod('Whiteboard diagram', (a[0],4.709,a[1]),(b[0],4.709,b[1]),.0025,ink,6,'Signage')

box('Projector mount',(0,2.04,2.93),(.047,.047,.34),alum,.003,'Shell')
box('Ceiling projector',(0,2.04,2.729),(.34,.29,.115),laminate,.020,'Shell')
rod('Projector lens',(-.089,2.18,2.73),(-.089,2.205,2.73),.036,screen,20,'Shell')
# Broad under-ceiling AC by the tall windows, as opposed to a wall split.
box('Ceiling air conditioner',(2.965,1.95,2.74),(.70,1.47,.25),laminate,.025,'Shell')
box('AC underside return',(2.91,1.95,2.607),(.43,1.18,.015),metal,.005,'Shell')
for n in range(16):
    box('AC intake slat',(2.91,1.421+n*.07,2.594),(.425,.016,.022),laminate,.001,'Shell')
for n in range(4):
    box('AC outlet louvre',(2.605,1.95,2.675+n*.021),(.018,1.29,.009),laminate,.001,'Shell')
box('Tall grey storage cabinet',(2.89,4.40,1.01),(.88,.57,2.02),wall,.006)
for x in [2.677,3.103]:
    box('Tall cabinet door',(x,4.106,1.02),(.419,.025,1.94),laminate,.004)
    rod('Cabinet pull',(x+(-.13 if x>2.89 else .13),4.083,.94),(x+(-.13 if x>2.89 else .13),4.083,1.09),.006,alum)
box('Entrance frame',(-2.57,-4.574,1.075),(1.105,.072,2.15),laminate,.005,'Shell')
box('Light entrance door',(-2.57,-4.525,1.045),(1.015,.044,2.07),laminate,.007,'Shell')
box('Door lower kickplate',(-2.57,-4.500,.195),(.935,.003,.26),alum,.001,'Shell')
rod('Door handle',(-2.23,-4.478,1.015),(-2.35,-4.478,1.015),.012,alum,12,'Shell')
for x,y in [(-3.375,4.35),(-3.375,-4.15)]:
    box('Surface electrical box',(x,y,.37),(.04,.12,.12),laminate,.004,'Shell')
    rod('Surface conduit',(x,y,.43),(x,y,2.98),.009,laminate,10,'Shell')
box('Small access point',(-2.82,4.744,2.55),(.23,.05,.16),laminate,.022,'Shell')

# Surface UVs use metres, not the stretched default cube atlas. A shared map
# retains the same grain size on the chess table, benches and storage shelves.
surface_scales = {deskwood.name:(1.6,.55), wall.name:(1,1), fabric.name:(.45,.45), blind.name:(.55,.55)}
for o in scene.objects:
    if o.type != 'MESH' or not o.data.materials: continue
    mat_name = o.data.materials[0].name
    if mat_name not in surface_scales: continue
    uv = o.data.uv_layers.active or o.data.uv_layers.new(name='SurfaceMetres')
    su, sv = surface_scales[mat_name]
    for poly in o.data.polygons:
        normal = poly.normal
        axis = max(range(3), key=lambda a: abs(normal[a]))
        axes = (0,1) if axis == 2 else ((0,2) if axis == 1 else (1,2))
        for li in poly.loop_indices:
            v = o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv = (v[axes[0]]/su+.5, v[axes[1]]/sv+.5)

# Keep a fully editable source; combine only a disposable export copy.
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.6,.7,.8,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.3
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FeevaleComputerLab.blend'),compress=True)
groups=defaultdict(list)
for o in list(scene.objects):
    if o.type in {'MESH','FONT'}:
        groups[(o.get('batch','Furniture'),o.data.materials[0].name)].append(o)
if len(groups) > 48:
    raise RuntimeError('Room material batches exceed the existing 48-renderer budget')
print('FEEVALE_MATERIAL_BATCHES',len(groups),flush=True)
for (group, mat), objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.join()
    bpy.context.object.name=group+'_'+mat

# Shared-material UVs cannot hold scene-wide contact occlusion. Bake it into
# vertex colour on the export copy instead: inexpensive in URP and identical
# in desktop/VR. No dynamic lights, shadows or reflections are baked here.
from mathutils.bvhtree import BVHTree
meshes=[o for o in scene.objects if o.type=='MESH']
positions=[]; polygons=[]
for o in meshes:
    start=len(positions)
    positions.extend(o.matrix_world @ v.co for v in o.data.vertices)
    polygons.extend(tuple(start+i for i in p.vertices) for p in o.data.polygons)
bvh=BVHTree.FromPolygons(positions,polygons,all_triangles=False)
samples=[]
for i in range(20):
    radius=math.sqrt((i+.5)/20); angle=i*2.39996323
    samples.append(Vector((radius*math.cos(angle),radius*math.sin(angle),math.sqrt(1-radius*radius))))
cache={}
for o in meshes:
    if o.name.startswith('Signage_') or 'WindowGlass' in o.name or 'Diffuser' in o.name: continue
    mesh=o.data; colours=mesh.color_attributes.new(name='ContactOcclusion',type='FLOAT_COLOR',domain='CORNER')
    matrix=o.matrix_world; normal_matrix=matrix.to_3x3().inverted().transposed()
    # Snapshot the geometry before writing any colours; per-loop RNA writes
    # invalidate the evaluated normals and make a large batch quadratic.
    normals=[(normal_matrix @ n.vector).normalized() for n in mesh.corner_normals]
    vertices=[matrix @ v.co for v in mesh.vertices]
    colour_values=[]
    print('FEEVALE_AO_BATCH',o.name,len(mesh.loops),flush=True)
    for loop in mesh.loops:
        position=vertices[loop.vertex_index]
        normal=normals[loop.index]
        key=tuple(round(v,4) for v in (*position,*normal))
        value=cache.get(key)
        if value is None:
            tangent=normal.cross(Vector((0,0,1)) if abs(normal.z)<.95 else Vector((0,1,0))).normalized()
            bitangent=normal.cross(tangent)
            occlusion=0
            for sample in samples:
                direction=tangent*sample.x+bitangent*sample.y+normal*sample.z
                hit,_,_,distance=bvh.ray_cast(position+normal*.0025,direction,.55)
                if hit is not None: occlusion += 1-distance/.55
            value=1-.58*occlusion/len(samples); cache[key]=value
        colour_values.extend((value,value,value,1))
    colours.data.foreach_set('color',colour_values)
    mesh.color_attributes.active_color=colours
print('FEEVALE_CONTACT_AO',len(cache),'surface samples',flush=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ART/'FeevaleComputerLab.glb'),export_format='GLB',use_selection=True,
    export_yup=True,export_apply=True,export_animations=False,export_cameras=False,export_lights=False,
    export_vertex_color='ACTIVE',export_all_vertex_colors=False)
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in scene.objects if o.type=='MESH')
record={'reference':'User-provided IMG_2844.MOV and tabletop photograph with a 30 cm ruler; originals remain local',
    'basis':['Video: blue/grey walls, tall windows with ochre surround, blinds, storage, ceiling beams and suspended tubes',
             'Photo: light oak laminate, thin edge band, black grommets and silver T-legs'],
    'reference_timecodes_s':[24.5,42.5,54.5,66.5,72.5],
    'known_measure_m':.30,'measured_room_dimensions':False,
    'right_window_bays':3,'desk_groups':4,'student_desks':16,'notebooks':notebook_count,
    'student_facing':'blinds or windows; projection board at the side (user confirmed)',
    'ceiling_luminaires':18,'tubes_per_luminaire':1,'luminaire_axis':'Y, parallel to blinds and window walls',
    'additional_shadow_lights':0,
    'surface_maps':'Shared albedo, tangent normal and roughness; oak desks 2048 px, paint/fabrics 1024 px',
    'contact_occlusion':'20 hemisphere rays per unique surface vertex; vertex colour, max distance 0.55m',
    'replica':False,'room_m':[6.8,9.4,3.3],'chairs':19,'table_top_m':.7557,
    'board_anchor_m':.78,'playing_area_m':.45,'environment_triangles':triangles,'render_meshes':len(groups),
    'characters_in_environment_asset':0,'colliders_in_environment_asset':0,'source_up':'Z',
    'unity_import_rotation_y':180,'headset_tested':False}
(OUT/'model-report.json').write_text(json.dumps(record,indent=2)+'\n')
print('FEEVALE_LAB_EXPORTED',json.dumps(record),flush=True)
