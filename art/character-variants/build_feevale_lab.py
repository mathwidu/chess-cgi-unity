"""Editable, metre-scale computer lab inspired by official Feevale photographs.

blender -b --python art/character-variants/build_feevale_lab.py
Exports environment only. The chessboard and characters remain owned by Unity.
"""
from pathlib import Path
from collections import defaultdict
import bpy, math, json, random
from mathutils import Vector

HERE = Path(__file__).resolve().parent
OUT = HERE / 'tabletop-polish-20260926' / 'lab-source'
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

wall = material('WarmWhitePaint', (.57, .55, .49), .9)
ceiling = material('CeilingPaint', (.67, .66, .61), .9, emission=.10)
underlay = material('FloorUnderlay', (.17, .105, .054), .9)
laminate = material('IvoryLaminate', (.46, .47, .44), .62)
edge = material('LaminateEdge', (.22, .18, .12), .61)
white = material('BoardEnamel', (.82, .84, .81), .32)
alum = material('BrushedAluminium', (.35, .39, .41), .38, .65)
metal = material('GraphiteSteel', (.045, .053, .055), .45, .35)
plastic = material('PCGraphite', (.024, .029, .032), .48)
keys = material('KeyboardKeys', (.043, .050, .055), .57)
fabric = material('ChairFabric', (.034, .040, .043), .95)
screen = material('SleepingDisplay', (.011, .019, .024), .22, .18)
green = material('FeevaleGreenAccent', (.012, .13, .068), .70)
blind = material('WovenVerticalBlinds', (.57, .59, .57), .90)
glass = material('FrostedDaylight', (.65, .76, .80), .6, emission=.35)
light = material('CeilingDiffuser', (.91, .92, .88), .8, emission=.7)
blue = material('PCPowerLED', (.08, .28, .40), .3, emission=.4)
ink = material('GraphiteLettering', (.045, .055, .057), .8)
wood = material('OakLaminate', (.39, .27, .15), .63)
deskwood = material('NaturalAshDesk', (.44, .32, .20), .58)
storage_teal = material('StorageTeal', (.028, .25, .29), .72)
storage_ochre = material('StorageOchre', (.53, .36, .055), .72)
storage_rose = material('StorageRose', (.39, .12, .16), .72)

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

bake_surface(deskwood, 'wood', (.285,.225,.155), (.405,.33,.235), 2048)
bake_surface(wall, 'paint', (.52,.50,.445), (.59,.57,.515))
bake_surface(fabric, 'fabric', (.020,.027,.030), (.043,.052,.054))
bake_surface(blind, 'fabric', (.47,.49,.455), (.60,.61,.575))

# A softly frosted view gives the windows depth and sky/foliage colour, without
# transparency sorting or a second rendered outdoor scene on a VR headset.
daylight = material('RightWindowDaylight', (.55,.65,.71), .36, emission=.16)
nodes, links = daylight.node_tree.nodes, daylight.node_tree.links
p = nodes.get('Principled BSDF'); coord = nodes.new('ShaderNodeTexCoord')
sep = nodes.new('ShaderNodeSeparateXYZ'); links.new(coord.outputs['UV'], sep.inputs[0])
noise = nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 4
noise.inputs['Detail'].default_value = 1; links.new(coord.outputs['UV'], noise.inputs[0])
scaled = nodes.new('ShaderNodeMath'); scaled.operation = 'MULTIPLY'; scaled.inputs[1].default_value = .22
links.new(noise.outputs['Fac'],scaled.inputs[0])
height = nodes.new('ShaderNodeMath'); height.operation = 'ADD'
links.new(sep.outputs['Y'],height.inputs[0]); links.new(scaled.outputs[0],height.inputs[1])
ramp = nodes.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].position = .05; ramp.color_ramp.elements[0].color = (.20,.30,.24,1)
ramp.color_ramp.elements[1].position = .77; ramp.color_ramp.elements[1].color = (.50,.66,.77,1)
mid = ramp.color_ramp.elements.new(.46); mid.color = (.39,.49,.45,1)
links.new(height.outputs[0],ramp.inputs[0]); links.new(ramp.outputs['Color'],p.inputs['Base Color'])
image = bpy.data.images.new('FrostedWindow_Albedo',512,512)
dest = nodes.new('ShaderNodeTexImage'); dest.image=image; nodes.active=dest
bpy.ops.mesh.primitive_plane_add(size=1); plane=bpy.context.object; plane.data.materials.append(daylight)
bpy.ops.object.bake(type='DIFFUSE')
image.filepath_raw=str(OUT/'FrostedWindow_Albedo.png');image.file_format='PNG';image.save()
bpy.data.objects.remove(plane,do_unlink=True)
links.new(dest.outputs['Color'],p.inputs['Base Color']); links.new(dest.outputs['Color'],p.inputs['Emission Color'])

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
mix = nodes.new('ShaderNodeMixRGB'); mix.inputs[1].default_value = (.28, .18, .092, 1); mix.inputs[2].default_value = (.46, .33, .19, 1)
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

# Clear central area lets either side of the board use the existing seated rig.
box('Subfloor', (0, .1, -.067), (6.8, 9.4, .11), underlay, 0, 'Shell')
for j in range(52):
    y = -4.6 + (j+.5)*9.4/52
    offset = [0, .38, .75][j%3]
    edges = [-3.4] + [x for x in [-3.4+i*1.13+offset for i in range(8)] if -3.399 < x < 3.399] + [3.4]
    for i,(start,end) in enumerate(zip(edges, edges[1:])):
        x = (start+end)/2
        tile = box('Oak floor plank', (x, y, -.006), (end-start-.0012, 9.4/52-.0012, .012), wood, 0, 'Floor')
        # World-projected grain avoids the stretched 1-pixel side UV of a cube.
        for poly in tile.data.polygons:
            for li in poly.loop_indices:
                v = tile.data.vertices[tile.data.loops[li].vertex_index].co
                tile.data.uv_layers.active.data[li].uv = (v.x/1.13+.5+i*.071, v.y/.181+.5+j*.027)

box('Front wall', (0, 4.87, 1.65), (6.94, .14, 3.3), wall, .004, 'Shell')
box('Back wall', (0, -4.67, 1.65), (6.94, .14, 3.3), wall, .004, 'Shell')
# Real openings on the right: three recessed bays, aluminium sliding frames,
# sill and upper transom. Layout is adapted; the reference is not a floor plan.
box('Right sill wall', (3.47, .1, .57), (.14, 9.4, 1.14), wall, .004, 'Shell')
box('Right lintel', (3.47, .1, 3.05), (.14, 9.4, .50), wall, .004, 'Shell')
window_centres = [-2.95, .1, 3.15]
for y in [-4.425, -1.425, 1.625, 4.65]:
    box('Right wall pier',(3.47,y,1.97),(.14,.35,1.66),wall,.004,'Shell')
for y in window_centres:
    width, bottom, top = 2.70, 1.14, 2.80
    box('Right frosted daylight',(3.515,y,1.97),(.016,width,top-bottom),daylight,0,'Shell')
    box('Right stone sill',(3.375,y,1.135),(.31,width+.09,.055),laminate,.008,'Shell')
    for offset in [-width/2,0,width/2]:
        box('Right aluminium mullion',(3.388,y+offset,1.97),(.09,.047,top-bottom),alum,.003,'Shell')
    for z in [bottom,2.40,top]:
        box('Right aluminium rail',(3.39,y,z),(.09,width,.047),alum,.003,'Shell')
    for offset in [-.10,.10]:
        box('Window latch',(3.33,y+offset,1.77),(.027,.021,.13),metal,.008,'Shell')
box('Left sill wall', (-3.47, .1, .48), (.14, 9.4, .96), wall, .004, 'Shell')
box('Left lintel', (-3.47, .1, 3.02), (.14, 9.4, .56), wall, .004, 'Shell')
box('Ceiling', (0, .1, 3.35), (6.8, 9.4, .10), ceiling, 0, 'Shell')
for x in [-3.395, 3.395]: box('Skirting', (x, .1, .05), (.02, 9.4, .1), edge, .002, 'Shell')
for y in [-4.595, 4.795]: box('Skirting', (0, y, .05), (6.8, .02, .1), edge, .002, 'Shell')
box('Window daylight', (-3.49, .1, 1.85), (.018, 9.4, 1.76), glass, 0, 'Shell')
for y in [-4.59, -2.25, .1, 2.45, 4.79]:
    box('Window mullion', (-3.385, y, 1.85), (.12, .045, 1.8), alum, .002, 'Shell')
for z in [.96, 2.74]: box('Window rail', (-3.39, .1, z), (.12, 9.4, .04), alum, .002, 'Shell')
for j in range(76):
    slat = box('Vertical blind slat', (-3.29, -4.50+j*.122, 1.86), (.008, .108, 1.69), blind, .001, 'Shell')
    slat.rotation_euler.z = -.27
box('Blind headrail', (-3.29, .1, 2.76), (.065, 9.3, .047), laminate, .002, 'Shell')

# Low cubbies echo the coloured storage beneath the blinds in the official 102
# photograph. They add human scale without filling the central playing area.
for i in range(10):
    y = -3.9+i*.84
    box('Window storage back',(-3.30,y,.43),(.028,.82,.66),laminate,.003)
    for z in [.105,.76]: box('Window storage shelf',(-3.13,y,z),(.36,.82,.025),deskwood,.004)
    for dy in [-.41,.41]: box('Window storage divider',(-3.13,y+dy,.43),(.36,.026,.66),laminate,.003)
    if i%3 != 1:
        colour = [storage_teal, storage_ochre, storage_rose][i%3]
        box('Coloured storage inset',(-3.279,y,.43),(.018,.77,.60),colour,.004)

for x in [-1.75, 1.75]:
    for y in [-2.6, .1, 2.8]:
        box('Ceiling luminaire body', (x, y, 3.28), (.31, 1.25, .06), alum, .009, 'Shell')
        box('Ceiling luminaire diffuser', (x, y, 3.244), (.25, 1.18, .014), light, .004, 'Shell')

def desk(name, x, y, width, depth, top=.75):
    box(name+' edge', (x,y,top-.029), (width,depth,.030), edge, .006)
    box(name+' laminate', (x,y,top-.007), (width-.006,depth-.006,.014), deskwood, .006)
    for dx in [-width/2+.10, width/2-.10]:
        box(name+' upright', (x+dx,y,(top-.055)/2+.025), (.054,.11,top-.055), alum, .006)
        box(name+' foot', (x+dx,y,.031), (.09,depth-.10,.046), alum, .015)
        for dy in [-depth/2+.10, depth/2-.10]: box(name+' foot cap', (x+dx,y+dy,.014), (.095,.085,.019), plastic, .006)
    box(name+' modesty panel', (x,y+depth*.30,.56), (width-.21,.028,.23), laminate, .004)

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

def workstation(x,y):
    # Screen faces toward -Y, matching the classroom photographs.
    box('Monitor foot',(x-.07,y+.075,.758),(.25,.175,.016),metal,.009)
    box('Monitor stem',(x-.07,y+.115,.86),(.045,.039,.21),plastic,.006)
    box('Monitor housing',(x-.07,y+.12,1.05),(.49,.042,.30),plastic,.012)
    box('LCD glass',(x-.07,y+.097,1.055),(.454,.004,.253),screen,.003)
    box('Display lower bezel',(x-.07,y+.093,.912),(.454,.005,.018),plastic,.001)
    box('Standby LED',(x+.127,y+.089,.913),(.008,.002,.003),blue,0)
    box('Compact tower',(x+.303,y+.09,.917),(.14,.31,.334),plastic,.009)
    box('Tower front inset',(x+.303,y-.068,.922),(.117,.008,.28),metal,.003)
    for i in range(11): box('Tower ventilation',(x+.303,y-.073,.823+i*.009),(.094,.002,.003),plastic,0)
    for dx in [-.032,.01]: box('USB port',(x+.303+dx,y-.074,1.008),(.017,.003,.006),plastic,0)
    box('Tower indicator',(x+.34,y-.074,1.052),(.005,.002,.006),blue,0)
    box('Keyboard',(x-.075,y-.164,.764),(.369,.131,.021),plastic,.005)
    for row in range(5):
        for col in range(14):
            box('Keycap',(x-.24+col*.025,y-.207+row*.022,.778),(.020,.016,.006),keys,0)
    box('Space bar',(x-.08,y-.224,.778),(.14,.01,.006),keys,0)
    box('Mouse pad',(x+.22,y-.185,.752),(.14,.17,.004),metal,.006)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=(x+.22,y-.18,.767))
    o=bpy.context.object; o.name='Mouse'; o.scale=(.028,.048,.017); o.data.materials.append(plastic); o['batch']='Furniture'
    for p in o.data.polygons: p.use_smooth=True
    # Short under-desk cable route; no loose cables in the play area.
    rod('Cable descent',(x+.30,y+.12,.74),(x+.30,y+.18,.56),.0035,plastic,6)

# A 26 mm board keeps the playing surface at its established interaction height.
# Lower only this tabletop by 18 mm; seats and the room anchor stay unchanged.
desk('Chess table',0,0,1.30,.90,.7557)
marker('ChessTableSurface',(0,0,.7557))
marker('BoardAnchor',(0,0,.78))
marker('SeatedEyeWhite',(0,-.6,1.2))
marker('SeatedEyeBlack',(0,.6,1.2))
marker('WindowSide',(-3.4,.1,1.8))
marker('RightWindows',(3.4,.1,1.97))
chair('Player chair',0,-.91)
chair('Opponent chair',0,.91,math.pi)
for row,y in enumerate([-2.60,1.75,3.20]):
    for side,x in enumerate([-2.05,2.05]):
        desk('Computer bench',x,y,2.04,.66)
        for delta in [-.48,.48]:
            workstation(x+delta,y)
            chair('Lab chair',x+delta,y-.58)

# Institutional details are intentionally restrained around the game.
box('Whiteboard frame',(-.35,4.755,1.78),(3.45,.055,1.15),alum,.012,'Shell')
box('Whiteboard',(-.35,4.719,1.78),(3.39,.018,1.09),white,.004,'Shell')
box('Marker tray',(-.35,4.66,1.215),(3.42,.14,.016),alum,.004,'Shell')
for x in [-1.7,-1.53]: rod('Board marker',(x,4.65,1.239),(x+.11,4.65,1.239),.007,green,10,'Signage')
box('Board eraser',(-1.10,4.65,1.239),(.11,.045,.026),plastic,.003,'Signage')
label('COMPUTAÇÃO GRÁFICA',(-1.87,4.706,2.16),.105,ink)
label('Formas  ·  luz  ·  materiais',(-1.87,4.706,1.98),.065,ink)
label('Xadrez em realidade virtual',(-1.87,4.706,1.53),.073,green)

logo=material('OfficialFeevaleMark',(1,1,1),.8)
tex=logo.node_tree.nodes.new('ShaderNodeTexImage')
tex.image=bpy.data.images.load(str(HERE/'direction-v2/brand/feevale-color-dark-type.png'),check_existing=True)
lp=logo.node_tree.nodes.get('Principled BSDF')
logo.node_tree.links.new(tex.outputs['Color'],lp.inputs['Base Color'])
logo.node_tree.links.new(tex.outputs['Alpha'],lp.inputs['Alpha']); logo.surface_render_method='DITHERED'
bpy.ops.mesh.primitive_plane_add(size=1,location=(2.19,4.712,2.09))
o=bpy.context.object; o.name='Feevale official wall mark'; o.rotation_euler=(math.pi/2,0,0)
o.scale=(1.04,1.04*369/950,1); o.data.materials.append(logo); o['batch']='Signage'
label('LABORATÓRIO', (1.69,4.713,1.72),.073,ink)
label('DE INFORMÁTICA', (1.69,4.713,1.61),.060,ink)

box('Projector mount',(0,2.10,3.06),(.05,.05,.39),alum,.004,'Shell')
box('Ceiling projector',(0,2.10,2.865),(.30,.26,.11),laminate,.023,'Shell')
rod('Projector lens',(-.075,2.219,2.86),(-.075,2.243,2.86),.033,screen,20,'Shell')
box('Air conditioner',(.1,4.65,2.92),(1.0,.26,.29),laminate,.04,'Shell')
for z in [2.825,2.845,2.865]: box('AC grille',(.1,4.504,z),(.82,.006,.008),edge,.001,'Shell')
box('Storage cabinet',(2.82,-4.35,.93),(.88,.42,1.86),laminate,.009)
for x in [2.61,3.03]:
    box('Cabinet door',(x,-4.126,.94),(.415,.019,1.77),laminate,.004)
    rod('Cabinet pull',(x+(-.12 if x>2.8 else .12),-4.106,.87),(x+(-.12 if x>2.8 else .12),-4.106,1.01),.006,alum)
box('Entrance frame',(-2.29,-4.583,1.065),(1.10,.065,2.13),alum,.004,'Shell')
door=box('Entrance door',(-2.29,-4.535,1.04),(1.01,.044,2.05),wood,.008,'Shell')
rod('Door handle',(-1.94,-4.497,1.0),(-2.06,-4.497,1.0),.012,alum,12,'Shell')

# Surface UVs use metres, not the stretched default cube atlas. A shared map
# retains the same grain size on the chess table, benches and storage shelves.
surface_scales = {deskwood.name:(1.6,.55), wall.name:(1,1), fabric.name:(.45,.45), blind.name:(.55,.55), daylight.name:(2.70,1.66)}
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
    if o.name.startswith('Signage_') or 'Daylight' in o.name or 'Diffuser' in o.name: continue
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
record={'reference':'https://www.feevale.br/pos-graduacao/stricto-sensu/mestrado--profissional-em-industria-criativa/infraestrutura',
    'basis':['Laboratório de Redes, sala 102 do prédio Verde','Laboratório de Projetos de TI'],
    'right_window_bays':3,'surface_maps':'Shared albedo, tangent normal and roughness; ash desks 2048 px, paint/fabrics 1024 px',
    'contact_occlusion':'20 hemisphere rays per unique surface vertex; vertex colour, max distance 0.55m',
    'replica':False,'room_m':[6.8,9.4,3.3],'workstations':12,'chairs':14,'table_top_m':.7557,
    'board_anchor_m':.78,'playing_area_m':.45,'environment_triangles':triangles,'render_meshes':len(groups),
    'characters_in_environment_asset':0,'colliders_in_environment_asset':0,'source_up':'Z',
    'unity_import_rotation_y':180,'headset_tested':False}
(OUT/'model-report.json').write_text(json.dumps(record,indent=2)+'\n')
print('FEEVALE_LAB_EXPORTED',json.dumps(record),flush=True)
