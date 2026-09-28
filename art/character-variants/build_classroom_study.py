"""Offline scale/composition study, NOT the Unity environment or a campus replica.

Loads the approved local character source and renders a proposed classroom.
Run after build_characters.py: blender -b production/characters.blend
  --python build_classroom_study.py. Only writes classroom-discovery-20260925/.
"""
from pathlib import Path
import bpy, math, json
from mathutils import Vector

HERE=Path(__file__).resolve().parent
OUT=HERE/'classroom-discovery-20260925';OUT.mkdir(exist_ok=True)
scene=bpy.context.scene
scene.name='Feevale_Classroom_Scale_Study_Not_Integrated'
prototypes={kind+'_'+side:bpy.data.objects[kind+'_'+side]
            for kind in ['Pawn','Rook','Knight','Bishop','Queen','King'] for side in ['White','Black']}
source_objects={o for root in prototypes.values() for o in [root,*root.children]}
for o in list(bpy.data.objects):
    if o not in source_objects:bpy.data.objects.remove(o,do_unlink=True)

def material(name,color,rough=.7,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m
def box(name,loc,size,mat,bevel=.005):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
    if bevel:
        b=o.modifiers.new('Rounded edges','BEVEL');b.width=bevel;b.segments=3
        n=o.modifiers.new('Weighted corners','WEIGHTED_NORMAL');n.keep_sharp=True
    return o
def wood(name,a,b):
    m=material(name,a,.57);n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
    uv=n.new('ShaderNodeTexCoord');scale=n.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(3,95,2);l.new(uv.outputs['Generated'],scale.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=3;noise.inputs['Detail'].default_value=2;l.new(scale.outputs[0],noise.inputs['Vector'])
    mix=n.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(*a,1);mix.inputs[2].default_value=(*b,1);l.new(noise.outputs['Fac'],mix.inputs[0]);l.new(mix.outputs[0],p.inputs['Base Color'])
    return m

wall=material('Warm classroom paint',(.70,.71,.67),.93)
floor=material('Quiet matte floor',(.24,.26,.25),.88)
metal=material('Powder coated frames',(.055,.064,.066),.50,.15)
desk=wood('Light laminate',(.46,.34,.20),(.60,.48,.31))
walnut=wood('Board walnut',(.10,.060,.035),(.145,.092,.055))
maple=wood('Board maple',(.60,.50,.35),(.71,.62,.48))
green=material('Muted green chairs',(.025,.11,.07),.72)
white=material('Whiteboard enamel',(.80,.82,.79),.36)
paper=material('Notebook paper',(.82,.80,.72),.83)

# Study dimensions: 6.8 x 7.0 m; furniture and openings are proposals.
box('Floor',(0,1.5,-.06),(6.8,7,.12),floor)
box('Front wall',(0,5,1.55),(6.8,.14,3.1),wall)
box('Right wall',(3.4,1.5,1.55),(.14,7,3.1),wall)
box('Left wall under windows',(-3.4,1.5,.48),(.14,7,.96),wall)
box('Left wall above windows',(-3.4,1.5,2.87),(.14,7,.46),wall)
box('Ceiling',(0,1.5,3.15),(6.8,7,.10),wall)
for y in [-2,.3,2.6,5]:box('Window jamb',(-3.4,y,1.80),(.18,.06,1.70),metal,.002)
for z in [.97,2.62]:box('Window rail',(-3.4,1.5,z),(.18,7,.06),metal,.002)
sky=material('Diffuse window daylight',(.65,.78,.92),.7)
p=sky.node_tree.nodes.get('Principled BSDF');p.inputs['Emission Color'].default_value=(.65,.78,.92,1);p.inputs['Emission Strength'].default_value=.6
box('Window daylight plane',(-3.48,1.5,1.80),(.02,7,1.60),sky,0)

def table(name,x,y,width,depth,top=.774):
    box(name+' top',(x,y,top-.0175),(width,depth,.035),desk,.009)
    for dx in [-width/2+.065,width/2-.065]:
        for dy in [-depth/2+.065,depth/2-.065]:box(name+' leg',(x+dx,y+dy,(top-.035)/2),(.036,.036,top-.035),metal,.003)
def chair(name,x,y):
    box(name+' seat',(x,y,.45),(.40,.39,.045),green,.024)
    box(name+' back',(x,y-.18,.68),(.40,.045,.39),green,.025)
    for dx in [-.16,.16]:
        for dy in [-.15,.15]:box(name+' leg',(x+dx,y+dy,.22),(.023,.023,.44),metal,.003)

table('Chess table',0,0,1.30,.90)
for y in [1.75,3.05]:
    for x in [-1.85,1.85]:
        table('Student desk',x,y,1.10,.60,.75)
        chair('Student chair',x,y-.50)
box('Whiteboard frame',(0,4.86,1.66),(3.25,.065,1.20),metal,.014)
box('Whiteboard',(0,4.815,1.66),(3.18,.03,1.13),white,.004)

# Use the existing official artwork; no invented logo or photographed room.
logo=bpy.data.materials.new('Feevale wall print');logo.use_nodes=True
n=logo.node_tree.nodes;l=logo.node_tree.links;p=n.get('Principled BSDF')
tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(HERE/'direction-v2/brand/feevale-color-dark-type.png'),check_existing=True)
l.new(tex.outputs['Color'],p.inputs['Base Color']);l.new(tex.outputs['Alpha'],p.inputs['Alpha']);p.inputs['Roughness'].default_value=.85
logo.surface_render_method='DITHERED'
bpy.ops.mesh.primitive_plane_add(size=1,location=(-.98,4.795,1.99));o=bpy.context.object;o.name='Official Feevale whiteboard mark';o.rotation_euler=(math.pi/2,0,0);o.scale=(.67,.67*369/950,1);o.data.materials.append(logo)

def label(text,loc,size,mat,rotation=(math.pi/2,0,0)):
    c=bpy.data.curves.new(text,'FONT');c.body=text;c.size=size;c.extrude=.0001
    o=bpy.data.objects.new(text,c);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=rotation;c.materials.append(mat)
    return o
label('COMPUTACAO GRAFICA',(-1.30,4.79,1.70),.12,metal)
label('Xadrez | formas, luz e materiais',(-1.30,4.79,1.48),.066,metal)

# Board area stays 45 cm (1.25 * 8 * 0.045), matching the current VR game.
box('Board frame',(0,0,.7875),(.52,.52,.027),walnut,.004)
size=.05625;playing_z=.802
for rank in range(8):
    for file in range(8):box('Square '+str(file)+str(rank),((file-3.5)*size,(rank-3.5)*size,playing_z-.001), (size,size,.002),maple if (file+rank)%2 else walnut,.00035)
for file in range(8):label('abcdefgh'[file],((file-3.5)*size-.004,-.246,.802),.009,maple,(0,0,0))
for rank in range(8):label(str(rank+1),(-.247,(rank-3.5)*size-.004,.802),.009,maple,(0,0,0))
pieces=bpy.data.objects.new('Chess pieces at VR scale',None);scene.collection.objects.link(pieces);pieces.location=(0,0,playing_z+.0005);pieces.scale=(.045,)*3
back=['Rook','Knight','Bishop','Queen','King','Bishop','Knight','Rook']
for side,ranks in [('White',(0,1)),('Black',(7,6))]:
    for rank in ranks:
        for file in range(8):
            kind='Pawn' if rank in [1,6] else back[file];proto=prototypes[kind+'_'+side]
            root=bpy.data.objects.new(f'{side} {kind} {file} {rank}',None);scene.collection.objects.link(root);root.parent=pieces;root.location=((file-3.5)*1.25,(rank-3.5)*1.25,0);root.rotation_euler.z=math.pi if side=='White' else 0
            for child in proto.children:
                o=child.copy();o.data=child.data;scene.collection.objects.link(o);o.parent=root;o.matrix_local=child.matrix_local.copy()
for o in source_objects:bpy.data.objects.remove(o,do_unlink=True)
# Quiet tabletop props stay outside the board and hand interaction area.
box('Closed notebook',(.48,.17,.785),(.18,.23,.019),green,.003)
box('Notebook paper',(.48,.17,.786),(.169,.224,.013),paper,.001)

scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.8,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.28
def area(name,loc,target,power,size,color):
    l=bpy.data.lights.new(name,'AREA');l.energy=power;l.shape='DISK';l.size=size;l.color=color
    o=bpy.data.objects.new(name,l);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
area('Window light',(-3,1,2.25),(0,0,.7),430,3,(.87,.93,1))
area('Ceiling fill',(0,1.3,2.95),(0,0,.6),220,3,(1,.96,.88))
area('Classroom rear fill',(0,4,2.9),(0,2,.9),120,2,(1,.97,.93))
scene.render.engine='CYCLES';scene.cycles.samples=28;scene.cycles.use_denoising=True
scene.render.resolution_x=1920;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
camera=bpy.data.objects.new('StudyCamera',bpy.data.cameras.new('StudyCamera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.lens=20
for name,loc,target in [('classroom-eye',(0,-.6,1.2),(0,.18,.80)),('classroom-overview',(1.8,-2,1.65),(0,1.7,1.25))]:
    camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
# Packed study can be reviewed separately without changing game assets.
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'classroom-study.blend'),compress=True)
(OUT/'study.json').write_text(json.dumps({'status':'offline composition study; not integrated or headset-tested','room_replica':False,'playing_area_m':.45,'playing_surface_m':.802,'coordinate_system':'Blender Z-up','table_top_m':.774,'table_dimensions_m':[1.3,.9,.035],'seated_eye_m':[0,-.6,1.2],'character_scale':.045,'characters':32},indent=2)+'\n')
print('CLASSROOM_STUDY_COMPLETE',flush=True)
