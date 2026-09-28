"""Estudo 3D aditivo. Executar com Blender; não altera os assets do jogo."""
import bpy
import bmesh
import math
import json
import hashlib
import subprocess
from pathlib import Path
from mathutils import Vector, Matrix
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / 'review'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x = 2200
scene.render.resolution_y = 1500
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Luz neutra de revisão')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.19,.21,.25,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .45
scene.view_settings.view_transform = 'AgX'

def material(name, color, metal=0, roughness=.7):
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=(*color,1)
    mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*color,1)
    bsdf.inputs['Metallic'].default_value=metal
    bsdf.inputs['Roughness'].default_value=roughness
    return mat

white=material('Brancas · marfim',(.78,.77,.73))
black=material('Pretas · carvão',(.018,.021,.027))
gold=material('Detalhes · latão fosco',(.48,.31,.09),.65,.36)
floor=material('Fundo neutro',(.043,.053,.07))

def finish(ob, name, parent, mat):
    ob.name=name
    ob.parent=parent
    ob.data.materials.append(mat)
    if ob.type=='MESH':
        bevel=ob.modifiers.new('Arestas suaves','BEVEL')
        bevel.width=.008
        bevel.segments=2
        ob.modifiers.new('Normais','WEIGHTED_NORMAL')
    return ob

def cube(name, parent, pos, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    ob=bpy.context.object
    ob.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(ob,name,parent,mat)

def cylinder(name,parent,pos,radius,depth,mat,vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=pos)
    return finish(bpy.context.object,name,parent,mat)

def sphere(name,parent,pos,radius,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=radius,location=pos)
    ob=finish(bpy.context.object,name,parent,mat)
    for face in ob.data.polygons:face.use_smooth=True
    return ob

def outline(name,parent,points,height,mat,z):
    n=len(points)
    vertices=[(x,y,z+level) for level in [0,height] for x,y in points]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);scene.collection.objects.link(ob)
    return finish(ob,name,parent,mat)

def accessory(kind, root, head, mat):
    mount=bpy.data.objects.new('Acessório · '+kind,None)
    scene.collection.objects.link(mount)
    if kind=='Pawn':
        # O peão conserva o cabelo e a cabeça redonda, sem adereço alto.
        bpy.data.objects.remove(mount)
        return
    z=0
    if kind=='Rook':
        for axis in [0,1]:
            for sign in [-1,1]:
                pos=[0,0,.025];pos[axis]=sign*.14
                size=[.32,.045,.075] if axis==1 else [.045,.32,.075]
                cube('Ameia · parede',mount,pos,size,mat)
        for x in [-.14,0,.14]:
            for y in [-.14,0,.14]:
                if x==0 and y==0:continue
                cube('Ameia · merlão',mount,(x,y,.098),(.068,.068,.095),mat)
    elif kind=='Knight':
        cylinder('Elmo aberto · aro',mount,(0,0,-.015),.115,.048,mat)
        # Perfil clássico do cavalo em relevo horizontal, legível pelo topo.
        points=[(-.16,.19),(.09,.19),(.13,.14),(.07,.1),(.025,.10),(.075,.02),(.17,-.035),(.17,-.11),(.13,-.14),(.045,-.095),(-.015,-.13),(-.10,-.12),(-.15,-.04),(-.09,-.055),(-.15,.02),(-.09,.0),(-.15,.09),(-.11,.06)]
        outline('Cavalo · perfil no elmo',mount,points,.10,mat,.025)
        sphere('Cavalo · olho',mount,(.06,-.057,.133),.016,gold)
    elif kind=='Bishop':
        # Duas metades de uma mitra alongada deixam uma fenda real no topo.
        points=[(-.105,0),(0,-.205),(.105,0),(0,.205)]
        outline('Mitra · contorno',mount,points,.08,mat,-.005)
        for sign in [-1,1]:
            verts=[(sign*.014,-.15,.075),(sign*.10,0,.075),(sign*.014,.15,.075),(sign*.014,0,.34)]
            mesh=bpy.data.meshes.new('Mitra · metade');mesh.from_pydata(verts,[],[(0,1,2),(0,3,1),(1,3,2),(2,3,0)]);mesh.update()
            ob=bpy.data.objects.new('Mitra · metade',mesh);scene.collection.objects.link(ob);finish(ob,ob.name,mount,mat)
    elif kind=='Queen':
        count=10
        vertices=[]
        for top in [False,True]:
            for i in range(count):
                a=i*2*math.pi/count
                radius=.145 if not top else (.205 if i%2==0 else .112)
                zz=0 if not top else (.26 if i%2==0 else .115)
                vertices.append((math.sin(a)*radius,math.cos(a)*radius,zz))
        faces=[(i,(i+1)%count,(i+1)%count+count,i+count) for i in range(count)]
        mesh=bpy.data.meshes.new('Coroa · cinco pontas');mesh.from_pydata(vertices,[],faces);mesh.update()
        ob=bpy.data.objects.new('Coroa · cinco pontas',mesh);scene.collection.objects.link(ob);finish(ob,ob.name,mount,mat)
        solid=ob.modifiers.new('Espessura da coroa','SOLIDIFY');solid.thickness=.018
        for i in range(0,count,2):
            a=i*2*math.pi/count;sphere('Coroa · ponta arredondada',mount,(math.sin(a)*.205,math.cos(a)*.205,.26),.023,gold)
    elif kind=='King':
        cylinder('Coroa real · base',mount,(0,0,.02),.145,.065,mat)
        cylinder('Coroa real · apoio da cruz',mount,(0,0,.105),.07,.12,mat)
        cube('Cruz real · haste',mount,(0,0,.20),(.09,.37,.067),mat)
        cube('Cruz real · travessa',mount,(0,0,.20),(.37,.09,.067),mat)
        sphere('Cruz real · centro',mount,(0,0,.245),.033,gold)
    mount.parent=root
    mount.location=head

pieces=[('Pawn','Pawn_Mathwidu_Redhead_v2','PEÃO · MATHEUS'),('Rook','Rook_Alex','TORRE · ALEX'),('Knight','Knight_Gustavo','CAVALO · GUSTAVO'),('Bishop','Bishop_Rafael','BISPO · RAFAEL'),('Queen','Queen_Marta','RAINHA · MARTA'),('King','King_Ricardo_Carioca','REI · RICARDO')]
variants={}
report={'status':'Estudo visual, sem integração Unity','source_commit':subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip(),'blender':bpy.app.version_string,'pieces':[]}
for column,(kind,stem,label) in enumerate(pieces):
    source=ROOT/'game/Assets/Resources/CustomPieces'/f'{stem}_Assets/selected.glb'
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(source))
    imported=list(set(bpy.data.objects)-before)
    meshes=[ob for ob in imported if ob.type=='MESH']
    assert len(meshes)==1
    body=meshes[0]
    original_face_count=len(body.data.polygons)
    # Congela somente a cópia em memória; GLB e prefab de origem ficam intactos.
    body.data.transform(body.matrix_world)
    body.parent=None
    body.matrix_world=Matrix.Identity(4)
    for ob in imported:
        if ob!=body:bpy.data.objects.remove(ob,do_unlink=True)
    points=[v.co for v in body.data.vertices]
    bottom=min(v.z for v in points);height=max(v.z for v in points)-bottom
    for v in body.data.vertices:v.co=(v.co-Vector((0,0,bottom)))*(1.8/height)+Vector((0,0,.12))
    body.data.update()
    original=body.data.materials[0]
    bsdf=original.node_tree.nodes.get('Principled BSDF')
    image_node=bsdf.inputs['Base Color'].links[0].from_node
    tex=image_node.image
    pixels=np.array(tex.pixels[:],dtype=np.float32).reshape((tex.size[1],tex.size[0],4))
    uv=body.data.uv_layers.active
    clothing=[]
    old_crown=[]
    for face in body.data.polygons:
        loc=sum((body.data.vertices[i].co for i in face.vertices),Vector())/len(face.vertices)
        h=(loc.z-.12)/1.8
        samples=[]
        for loop in face.loop_indices:
            u,v=uv.data[loop].uv
            samples.append(pixels[min(int(v*tex.size[1]),tex.size[1]-1),min(int(u*tex.size[0]),tex.size[0]-1),:3])
        r,g,b=np.mean(samples,axis=0)
        skin=(r>g*1.16 and r>b*1.22)
        if kind in ['Queen','King'] and h>.89 and r>g*1.08 and g>b*1.4:
            old_crown.append(face.index)
        if kind=='Pawn':
            pants=.145<h<.52 and not (h>.38 and abs(loc.x)>.22 and skin)
            clothes=(.135<h<.84 and not skin) or pants
        elif kind=='Rook':
            clothes=.12<h<.85 and not skin and (b>r*1.045 or max(r,g,b)<.29)
        elif kind=='Knight':
            clothes=.10<h<.86 and not skin and max(r,g,b)<.48
        elif kind=='Bishop':
            clothes=.13<h<.85 and not skin
        elif kind=='Queen':
            skirt=.07<h<.50 and not (h>.35 and abs(loc.x)>.225 and skin)
            clothes=(.07<h<.805 and not skin) or (.7<h<.89 and b>r*1.05) or skirt
        else:
            clothes=(.06<h<.79 and not skin) or (.7<h<.88 and b>r*1.10)
        if clothes:clothing.append(face.index)
    assert len(clothing)>len(body.data.polygons)*.1,(kind,len(clothing))
    # A atribuição do protótipo é por face; o acabamento das costuras ainda é revisão artística.
    for face_index in clothing:body.data.polygons[face_index].material_index=1
    if old_crown:
        edit=bmesh.new();edit.from_mesh(body.data);edit.faces.ensure_lookup_table()
        bmesh.ops.delete(edit,geom=[edit.faces[i] for i in old_crown],context='FACES')
        edit.to_mesh(body.data);edit.free();body.data.update()
    for side,mat,y in [('Brancas',white,0),('Pretas',black,2.65)]:
        root=bpy.data.objects.new(f'{label} · {side}',None);scene.collection.objects.link(root)
        variants[(kind,side)]=root
        clone=body.copy();clone.data=body.data.copy();scene.collection.objects.link(clone)
        clone.name=f'{stem} · roupa {side.lower()}';clone.parent=root
        cloth=original.copy();cloth.name=f'Roupa · {kind} · {side}'
        shader=cloth.node_tree.nodes.get('Principled BSDF')
        for socket in ['Base Color','Metallic','Roughness']:
            for link in list(shader.inputs[socket].links):cloth.node_tree.links.remove(link)
        shader.inputs['Base Color'].default_value=mat.diffuse_color
        shader.inputs['Metallic'].default_value=0
        shader.inputs['Roughness'].default_value=.78
        clone.data.materials.append(cloth)
        cylinder('Base do lado',root,(0,0,.06),.43,.12,mat)
        cylinder('Filete da base',root,(0,0,.017),.437,.026,gold)
        head_vertices=[v.co for v in clone.data.vertices if v.co.z>1.72]
        head=sum(head_vertices,Vector())/len(head_vertices)
        head.z=1.825 if kind in ['Queen','King'] else 1.90
        accessory(kind,root,head,mat)
        root.location=((column-2.5)*1.38,y,0)
    bpy.data.objects.remove(body,do_unlink=True)
    report['pieces'].append({'kind':kind,'character':stem,'clothing_faces':sum(f.material_index==1 for f in clone.data.polygons),'source_faces':original_face_count,'study_faces':len(clone.data.polygons),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()})

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.018));bpy.context.object.data.materials.append(floor)
for name,loc,power,size in [('Luz principal',(-3,-4,8),1600,6),('Preenchimento',(5,-1,6),1000,5),('Recorte',(0,6,6),1600,5)]:
    data=bpy.data.lights.new(name,'AREA');ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob)
    ob.location=loc;ob.rotation_euler=(Vector((0,1.3,.8))-ob.location).to_track_quat('-Z','Y').to_euler()
    data.energy=power;data.shape='DISK';data.size=size
cam=bpy.data.objects.new('Câmera de revisão',bpy.data.cameras.new('Câmera de revisão'));scene.collection.objects.link(cam)
scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=9.4
for name,pos,target in [('angle-45',(0,-10,12),(0,1.325,.8)),('top',(0,1.325,15),(0,1.325,0)),('front',(0,-14,7),(0,1.325,.9))]:
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/f'{name}.png');bpy.ops.render.render(write_still=True)
cam.location=(0,-10,12);cam.rotation_euler=(Vector((0,1.325,.8))-cam.location).to_track_quat('-Z','Y').to_euler()
study_scene=scene
study_scene.name='01 · Comparativo dos personagens'
scene=bpy.data.scenes.new('02 · Leitura no tabuleiro')
bpy.context.window.scene=scene
scene.world=study_scene.world
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1800;scene.render.resolution_y=1600;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
light_tile=material('Casa clara',(.42,.41,.36))
dark_tile=material('Casa escura',(.052,.077,.071))
for rank in range(8):
    for file in range(8):
        cube('Casa '+chr(97+file)+str(rank+1),None,((file-3.5)*1.25,(rank-3.5)*1.25,-.11),(1.25,1.25,.16),light_tile if (file+rank)%2 else dark_tile)
cube('Moldura do tabuleiro',None,(0,0,-.25),(10.4,10.4,.15),black)

def clone_tree(source,parent=None):
    copy=source.copy();scene.collection.objects.link(copy);copy.parent=parent
    for child in source.children:clone_tree(child,copy)
    return copy

back=['Rook','Knight','Bishop','Queen','King','Bishop','Knight','Rook']
for side,rank,pawns in [('Brancas',0,1),('Pretas',7,6)]:
    for file,kind in enumerate(back):
        for piece_kind,piece_rank in [(kind,rank),('Pawn',pawns)]:
            root=clone_tree(variants[(piece_kind,side)])
            root.name=f'{side} {piece_kind} {chr(97+file)}{piece_rank+1}'
            root.location=((file-3.5)*1.25,(piece_rank-3.5)*1.25,0)
            scale={'Pawn':1.15,'Rook':1.31,'Knight':1.31,'Bishop':1.31,'Queen':1.43,'King':1.43}[piece_kind]/1.8
            root.scale=(scale,)*3
            if side=='Pretas':root.rotation_euler.z=math.pi
for source in study_scene.objects:
    if source.type=='LIGHT':
        copy=source.copy();scene.collection.objects.link(copy)
        copy.location.y-=1.325
cam=bpy.data.objects.new('Câmera de tabuleiro',bpy.data.cameras.new('Câmera de tabuleiro'));scene.collection.objects.link(cam)
scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=14.4
cam.location=(0,-12,15);cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(OUT/'board.png');bpy.ops.render.render(write_still=True)
bpy.context.window.scene=study_scene
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'character-variants-study.blend'))
(OUT/'report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False))
print('ESTUDO_CONCLUÍDO',json.dumps(report,ensure_ascii=False))
