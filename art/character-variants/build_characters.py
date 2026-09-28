"""Build direction 02 from preserved GLBs using Blender 5.2.

Clothing uses 4K color bakes and a shared 2K normal map per character.
Branding has its own conforming mesh and shared atlas. Never writes source GLBs.
Run: blender --background --python art/character-variants/build_characters.py
Optional: -- --kinds Pawn,King --quick (smaller review render, same game assets).
"""
from pathlib import Path
import bpy, bmesh, math, json, hashlib, sys, shutil
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
sys.path.insert(0,str(Path(__file__).resolve().parent))
from uv_layout import find_overlaps

ROOT = Path(__file__).resolve().parents[2]
ART = Path(__file__).resolve().parent / 'production'
ASSETS = ROOT / 'game/Assets/Art/Characters/Direction02'
BRAND = Path(__file__).resolve().parent / 'direction-v2/brand'
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
KINDS = ARGS[ARGS.index('--kinds') + 1].split(',') if '--kinds' in ARGS else None
QUICK = '--quick' in ARGS
NO_RENDER = '--no-render' in ARGS
ART.mkdir(parents=True, exist_ok=True)
ASSETS.mkdir(parents=True, exist_ok=True)

SPECS = [
    dict(kind='Pawn', stem='Pawn_Mathwidu_Redhead_v2', name='MATHEUS', height=1.15, cut=.20, logo=(0,1.27,.32)),
    dict(kind='Rook', stem='Rook_Alex', name='ALEX', height=1.31, cut=0, logo=(0,1.30,.30)),
    dict(kind='Knight', stem='Knight_Gustavo', name='GUSTAVO', height=1.31, cut=0, logo=(.035,1.16,.30)),
    dict(kind='Bishop', stem='Bishop_Rafael', name='RAFAEL', height=1.31, cut=.095, logo=(-.055,1.19,.27)),
    dict(kind='Queen', stem='Queen_Marta', name='MARTA', height=1.43, cut=0, logo=(-.06,.96,.205)),
    dict(kind='King', stem='King_Ricardo_Carioca', name='RICARDO', height=1.43, cut=0, logo=(0,1.24,.40)),
]
if KINDS: SPECS = [s for s in SPECS if s['kind'] in KINDS]

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = 'Direction02_Characters'
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.world = bpy.data.worlds.new('ReviewWorld')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.18,.20,.23,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .45
scene.view_settings.view_transform = 'AgX'

def mat(name, color, rough=.72, metal=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color,1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    return m

IVORY = mat('Side_Surface_White',(.76,.74,.68),.6)
CHARCOAL = mat('Side_Surface_Black',(.035,.042,.05),.65)
INK = mat('Side_Inlay_White',(.025,.029,.035))
LIGHT_INK = mat('Side_Inlay_Black',(.88,.85,.77))
BRASS = mat('BrushedBrass',(.48,.30,.10),.34,.72)
STEEL = mat('SwordSteel',(.48,.55,.62),.3,.85)
LEATHER = mat('GripLeather',(.035,.025,.019),.82)
FLOOR = mat('ReviewFloor',(.055,.064,.079),.78)
brand_images = {side:bpy.data.images.load(str(BRAND/file)) for side,file in
                [('White','feevale-color-dark-type.png'),('Black','feevale-color-white-type.png')]}
roots = []
report = {'source_commit':'81bea194e6b4a24adfaf3c24b235709660829c8e',
          'blender':bpy.app.version_string,'kind':'native 3D production candidates',
          'revision':'garment contours, tapered grips, satin plinths and printed back branding 2026-09-25',
          'branding':'back only','source_uv':'SourceUV','export_uv':'ProductionUV',
          'color_texture_size':4096,'normal_texture_size':2048,'brand_atlas_size':list(brand_images['White'].size),
          'base_radius':.556,'characters':[]}

def finish(obj, name, material, bevel=0, smooth=True):
    obj.name = name
    obj.data.materials.append(material)
    if smooth:
        for f in obj.data.polygons: f.use_smooth = True
    if bevel:
        mod = obj.modifiers.new('TailoredEdges','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=obj.modifiers.new('CornerNormals','WEIGHTED_NORMAL'); mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def sphere(name, center, size, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=1,location=center)
    ob=bpy.context.object; ob.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(ob,name,material)

def cylinder(name, center, radius, depth, material, vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=center)
    return finish(bpy.context.object,name,material,.004)

def bar(name, a, b, radius, material, vertices=16):
    a,b=Vector(a),Vector(b)
    ob=cylinder(name,(a+b)/2,radius,(b-a).length,material,vertices)
    ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return ob

def cube(name, center, scale, material, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    ob=bpy.context.object; ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(ob,name,material,bevel,False)

def path_tube(name, points, radius, material, radii=None):
    cv=bpy.data.curves.new(name,'CURVE'); cv.dimensions='3D'
    cv.resolution_u=5; cv.bevel_depth=radius; cv.bevel_resolution=2
    sp=cv.splines.new('BEZIER'); sp.bezier_points.add(len(points)-1)
    for i,(bp,p) in enumerate(zip(sp.bezier_points,points)):
        bp.co=p; bp.handle_left_type='AUTO'; bp.handle_right_type='AUTO'
        if radii is not None:bp.radius=radii[i]
    ob=bpy.data.objects.new(name,cv); scene.collection.objects.link(ob)
    cv.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
    bpy.context.view_layer.objects.active=ob; bpy.ops.object.convert(target='MESH')
    return ob

def plinth():
    # Profiled sidewalls share smooth normals; the flat caps remain separate.
    # This removes the alternating flat facets without bending the top normals.
    profile=[(.546,.010),(.554,.015),(.556,.023),(.556,.105),(.553,.114),(.545,.122)]
    segments=96;vertices=[];faces=[]
    for r,z in profile:
        vertices.extend((math.cos(i*math.tau/segments)*r,math.sin(i*math.tau/segments)*r,z) for i in range(segments))
    for row in range(len(profile)-1):
        for i in range(segments):
            a=row*segments+i;b=row*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))),tuple(range((len(profile)-1)*segments,len(profile)*segments))])
    mesh=bpy.data.meshes.new('SatinPlinth');mesh.from_pydata(vertices,[],faces);mesh.update()
    ob=bpy.data.objects.new('TeamPlinth',mesh);scene.collection.objects.link(ob);finish(ob,ob.name,IVORY)
    for face in mesh.polygons:face.use_smooth=len(face.vertices)==4
    return ob

def polygon(name, points, z, material):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata([(x,y,z) for x,y in points],[],[tuple(range(len(points)))])
    mesh.update()
    ob=bpy.data.objects.new(name,mesh); scene.collection.objects.link(ob)
    return finish(ob,name,material,0,False)

def glyph(kind, y, rotation):
    # Font outlines include the real holes/notches and are triangulated by
    # Blender, avoiding self-intersecting handmade n-gons on rook/knight.
    font=bpy.data.fonts.load(str(Path(__file__).parent/'fonts/NotoSansSymbols2-Regular.ttf'),check_existing=True)
    cv=bpy.data.curves.new('ClassicRole_'+kind,'FONT');cv.font=font
    cv.body={'King':'♚','Queen':'♛','Rook':'♜','Bishop':'♝','Knight':'♞','Pawn':'♟'}[kind]
    cv.resolution_u=6;cv.extrude=.002;cv.bevel_depth=.0006;cv.bevel_resolution=1
    ob=bpy.data.objects.new('RoleSymbol_'+kind,cv);scene.collection.objects.link(ob)
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.object.convert(target='MESH')
    lo=Vector(tuple(min(v.co[i] for v in ob.data.vertices) for i in range(3)))
    hi=Vector(tuple(max(v.co[i] for v in ob.data.vertices) for i in range(3)))
    size=.16/(hi.y-lo.y)
    for v in ob.data.vertices:v.co=(v.co-Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z)))*size
    ob.location=(.30 if y<0 else -.30,y,.126);ob.rotation_euler.z=rotation
    return finish(ob,ob.name,INK,0,False)

def import_body(spec):
    path=ROOT/'game/Assets/Resources/CustomPieces'/(spec['stem']+'_Assets')/'selected.glb'
    before=set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=str(path))
    added=list(set(bpy.data.objects)-before)
    body=next(o for o in added if o.type=='MESH')
    body.data.transform(body.matrix_world); body.parent=None; body.matrix_world=Matrix.Identity(4)
    for o in added:
        if o!=body: bpy.data.objects.remove(o,do_unlink=True)
    lo=min(v.co.z for v in body.data.vertices); hi=max(v.co.z for v in body.data.vertices)
    for v in body.data.vertices: v.co=(v.co-Vector((0,0,lo)))*(1.8/(hi-lo))
    body.data.update(); body.name=spec['kind']+'_Body'
    original=body.data.materials[0]
    tex=original.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].links[0].from_node.image
    px=np.asarray(tex.pixels[:],dtype=np.float32).reshape(tex.size[1],tex.size[0],4)
    uv=body.data.uv_layers.active
    primary=[]; trousers=[]; crowns=[]; skin=[]; coords=[]; values=[]
    for face in body.data.polygons:
        loc=sum((body.data.vertices[i].co for i in face.vertices),Vector())/len(face.vertices)
        uvs=[uv.data[i].uv for i in face.loop_indices]
        color=np.mean([px[min(max(int(v*tex.size[1]),0),tex.size[1]-1),min(max(int(u*tex.size[0]),0),tex.size[0]-1),:3] for u,v in uvs],axis=0)
        r,g,b=map(float,color); h=loc.z/1.8
        is_skin=r>g*1.15 and r>b*1.22 and r>.09
        if is_skin: skin.append((face.index,tuple(loc),[r,g,b]))
        kind=spec['kind']
        if kind=='Pawn':
            cloth=.49<h<.87
            pants=.12<h<.55
        elif kind=='Rook':
            # Mixed skin/shirt triangles cannot be classified by their average
            # color. Classify this whole region, then protect skin per texel.
            cloth=(.49 if loc.y>.02 else .52)<h<.85
            pants=False
        elif kind=='Knight':
            # Include the seated waist, cuffs and hood rim. The material's
            # per-texel garment contour separates them from trousers/mount.
            cloth=.40<h<.89
            pants=False
        elif kind=='Bishop':
            cloth=.41<h<.83
            pants=False
        elif kind=='Queen':
            cloth=.465<h<.835
            pants=False
        else:
            cloth=.34<h<.85
            pants=False
            # The original head and its small crown are preserved intact.
        if cloth:
            primary.append(face.index)
            linear=np.where(color<=.04045,color/12.92,((color+.055)/1.055)**2.4)
            values.append(float(np.dot(linear,[.2126,.7152,.0722])))
        elif pants: trousers.append(face.index)
        coords.append(tuple(loc))
    median=max(.015,float(np.median(values)))
    # Stable groups remain in the editable source to support manual finishing.
    for name,faces in [('TeamCloth',primary),('CargoTrousers',trousers)]:
        group=body.vertex_groups.new(name=name)
        verts=sorted({v for i in faces for v in body.data.polygons[i].vertices})
        if verts:group.add(verts,1,'REPLACE')
    for i in primary: body.data.polygons[i].material_index=1
    for i in trousers: body.data.polygons[i].material_index=2
    edit=bmesh.new(); edit.from_mesh(body.data); edit.faces.ensure_lookup_table()
    remove=[f for f in edit.faces if f.calc_center_median().z<spec['cut'] or f.index in crowns]
    bmesh.ops.delete(edit,geom=remove,context='FACES')
    edit.to_mesh(body.data);edit.free();body.data.update()
    return body,original,median,skin,path

def op(nodes,links,operation,a,b=None):
    n=nodes.new('ShaderNodeMath'); n.operation=operation
    for i,value in enumerate([a,b]):
        if value is None:continue
        if isinstance(value,(float,int)):n.inputs[i].default_value=value
        else:links.new(value,n.inputs[i])
    return n.outputs[0]

def ellipse_mask(nodes,links,x,z,cx,cz,rx,rz):
    dx=op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',x,cx),rx)
    dz=op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',z,cz),rz)
    distance=op(nodes,links,'ADD',op(nodes,links,'MULTIPLY',dx,dx),op(nodes,links,'MULTIPLY',dz,dz))
    return op(nodes,links,'LESS_THAN',distance,1)

def prepare_bake_uvs(body,original):
    """Preserve source lookup while giving production textures padded islands."""
    source_uv=body.data.uv_layers.active;source_uv.name='SourceUV'
    nodes=original.node_tree.nodes;links=original.node_tree.links
    uv=nodes.new('ShaderNodeUVMap');uv.uv_map='SourceUV'
    for node in nodes:
        if node.type=='TEX_IMAGE':links.new(uv.outputs['UV'],node.inputs['Vector'])
        if node.type=='NORMAL_MAP':node.uv_map='SourceUV'
    target=body.data.uv_layers.new(name='ProductionUV')
    body.data.uv_layers.active=target;target.active_render=True
    bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.02,area_weight=.25,correct_aspect=True,scale_to_bounds=True)
    # Projection alone can overlap an arm with the chest in the same chart.
    # Relax those charts on the real surface before packing their padding.
    bpy.ops.uv.seams_from_islands(mark_seams=True,mark_sharp=False)
    bpy.ops.uv.unwrap(method='ANGLE_BASED',margin=.005)
    bpy.ops.uv.pack_islands(rotate=True,margin=.012,margin_method='FRACTION',shape_method='CONCAVE')
    bpy.ops.object.mode_set(mode='OBJECT')
    # Folded fingers/arms can still intersect within an unwrapped chart.
    # Open only the affected faces and repack, retaining the original UV map.
    for attempt in range(4):
        overlaps=find_overlaps(body.data)
        if not overlaps:break
        affected={index for _,a,b in overlaps for index in (a,b)}
        edge_keys={tuple(sorted(edge)) for index in affected for edge in body.data.polygons[index].edge_keys}
        for edge in body.data.edges:
            if tuple(sorted(edge.vertices)) in edge_keys:edge.use_seam=True
        if attempt>=1:
            # A folded quad can overlap itself even after its boundary is cut.
            # Split only unresolved faces into planar charts.
            edit=bmesh.new();edit.from_mesh(body.data);edit.faces.ensure_lookup_table()
            split=bmesh.ops.triangulate(edit,faces=[edit.faces[i] for i in affected],quad_method='BEAUTY',ngon_method='BEAUTY')
            for face in split['faces']:
                for edge in face.edges:edge.seam=True
            edit.to_mesh(body.data);edit.free();body.data.update()
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.unwrap(method='ANGLE_BASED',margin=.005)
        bpy.ops.uv.pack_islands(rotate=True,margin=.012,margin_method='FRACTION',shape_method='CONCAVE')
        bpy.ops.object.mode_set(mode='OBJECT')
    remaining=find_overlaps(body.data)
    if remaining:raise RuntimeError(f'{body.name}: overlapping production UV triangles {remaining[:5]}')

def legacy_logo_mask(nodes,links,spec):
    """Erase old pseudo-lettering in both color and surface relief."""
    cx,cz,width,height=(.10,1.232,.135,.070) if spec['kind']=='Knight' else (0,1.235,.64,.17)
    coord=nodes.new('ShaderNodeTexCoord');xyz=nodes.new('ShaderNodeSeparateXYZ')
    links.new(coord.outputs['Object'],xyz.inputs[0])
    u=op(nodes,links,'ADD',op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',xyz.outputs['X'],cx),width),.5)
    v=op(nodes,links,'ADD',op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',xyz.outputs['Z'],cz),height),.5)
    geo=nodes.new('ShaderNodeNewGeometry');normal=nodes.new('ShaderNodeVectorTransform')
    normal.vector_type='NORMAL';normal.convert_from='WORLD';normal.convert_to='OBJECT';links.new(geo.outputs['Normal'],normal.inputs[0])
    nxyz=nodes.new('ShaderNodeSeparateXYZ');links.new(normal.outputs[0],nxyz.inputs[0])
    mask=op(nodes,links,'LESS_THAN',nxyz.outputs['Y'],-.25)
    for value in [u,op(nodes,links,'SUBTRACT',1,u),v,op(nodes,links,'SUBTRACT',1,v)]:
        feather=op(nodes,links,'MINIMUM',op(nodes,links,'MAXIMUM',op(nodes,links,'DIVIDE',value,.10),0),1)
        mask=op(nodes,links,'MULTIPLY',mask,feather)
    return mask

def clothing_material(original,spec,side,mode,median):
    m=original.copy(); m.name=f"Author_{spec['kind']}_{side}_{mode}"
    nodes=m.node_tree.nodes; links=m.node_tree.links
    shader=nodes.get('Principled BSDF'); source=shader.inputs['Base Color'].links[0].from_socket
    coord=nodes.new('ShaderNodeTexCoord')
    xyz=nodes.new('ShaderNodeSeparateXYZ'); links.new(coord.outputs['Object'],xyz.inputs[0])
    color=source
    if mode=='cloth' or (mode=='pants' and side=='Black'):
        bw=nodes.new('ShaderNodeRGBToBW');links.new(source,bw.inputs[0])
        norm=op(nodes,links,'DIVIDE',bw.outputs[0],median if mode=='cloth' else .34)
        shaped=op(nodes,links,'POWER',norm,.52 if side=='White' else .58)
        factor=op(nodes,links,'MULTIPLY',shaped,.67 if side=='White' else .029)
        factor=op(nodes,links,'MAXIMUM',factor,.20 if side=='White' else .009)
        factor=op(nodes,links,'MINIMUM',factor,.86 if side=='White' else .085)
        tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1
        links.new(factor,tint.inputs[1]);tint.inputs[2].default_value=(1,.99,.97,1) if side=='White' else (.90,.96,1,1)
        color=tint.outputs[0]
        if mode in ['cloth','pants']:
            # Preserve skin at mixed UV triangles along neck/sleeve seams.
            rgb=nodes.new('ShaderNodeSeparateColor');rgb.mode='RGB';links.new(source,rgb.inputs[0])
            warm=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Green'],1.18)),op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Blue'],1.30)))
            if spec['kind']=='Rook':
                # Light skin in the seated forearms has much less saturation
                # than the original red/orange skin heuristic assumed.
                warm=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Green'],1.03)),op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Blue'],1.05)))
            # Hue ratios alone classify near-black JPEG noise as skin. That
            # restored black flecks throughout the white hoodie in Unity.
            warm=op(nodes,links,'MULTIPLY',warm,op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],.07))
            if spec['kind']=='Pawn' and mode=='cloth':
                warm=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Green'],1.08)),op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Blue'],1.14)))
                warm=op(nodes,links,'MULTIPLY',warm,op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],.07))
                exposed=op(nodes,links,'MAXIMUM',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],1.43),op(nodes,links,'GREATER_THAN',op(nodes,links,'ABSOLUTE',xyz.outputs['X']),.20))
                warm=op(nodes,links,'MULTIPLY',warm,exposed)
                if side=='White':
                    cargo=op(nodes,links,'MULTIPLY',op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.0),op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Blue'],1.14)))
                    warm=op(nodes,links,'MAXIMUM',warm,cargo)
            if spec['kind']=='Queen' and mode=='cloth':
                wrists=op(nodes,links,'MULTIPLY',op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.0),op(nodes,links,'GREATER_THAN',op(nodes,links,'ABSOLUTE',xyz.outputs['X']),.24))
                exposed=op(nodes,links,'MAXIMUM',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],1.46),wrists)
                warm=op(nodes,links,'MULTIPLY',warm,exposed)
            if mode=='pants':
                hand=op(nodes,links,'MAXIMUM',op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.21),op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.16),op(nodes,links,'LESS_THAN',xyz.outputs['Y'],-.145)))
                hand=op(nodes,links,'MULTIPLY',hand,op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],.76))
                warm=op(nodes,links,'MULTIPLY',warm,hand)
            if spec['kind']=='Pawn':
                # The free fingers overlap the cargo in height, but sit in front
                # of it. A height/x-only mask recolored the hand as trousers.
                free_hand=op(nodes,links,'MAXIMUM',op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.21),op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.16),op(nodes,links,'LESS_THAN',xyz.outputs['Y'],-.145)))
                free_hand=op(nodes,links,'MULTIPLY',free_hand,op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],.76))
                free_hand=op(nodes,links,'MULTIPLY',free_hand,op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.20))
                wrist=op(nodes,links,'MULTIPLY',op(nodes,links,'LESS_THAN',xyz.outputs['X'],-.19),op(nodes,links,'LESS_THAN',xyz.outputs['Y'],-.205))
                wrist=op(nodes,links,'MULTIPLY',wrist,op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.36))
                warm=op(nodes,links,'MAXIMUM',warm,op(nodes,links,'MAXIMUM',free_hand,wrist))
            preserve=nodes.new('ShaderNodeMixRGB');links.new(warm,preserve.inputs[0]);links.new(color,preserve.inputs[1]);links.new(source,preserve.inputs[2]);color=preserve.outputs[0]
    if spec['kind']=='King' and mode=='cloth':
        # Original blue hoodie pixels, including mixed sleeve-edge triangles.
        # Skin and trousers retain their original color at per-texel precision.
        rgb=nodes.new('ShaderNodeSeparateColor');links.new(source,rgb.inputs[0])
        chroma=op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',rgb.outputs['Blue'],rgb.outputs['Red']),op(nodes,links,'ADD',op(nodes,links,'ADD',rgb.outputs['Blue'],rgb.outputs['Red']),.02))
        blue=op(nodes,links,'GREATER_THAN',chroma,.28)
        mix=nodes.new('ShaderNodeMixRGB');links.new(blue,mix.inputs[0]);links.new(source,mix.inputs[1]);links.new(color,mix.inputs[2]);color=mix.outputs[0]
    if spec['kind']=='Bishop' and mode=='cloth':
        # A per-texel hem avoids triangular white fragments on the trousers.
        garment=op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],.855)
        left=ellipse_mask(nodes,links,xyz.outputs['X'],xyz.outputs['Z'],-.054,1.425,.063,.050)
        right=ellipse_mask(nodes,links,xyz.outputs['X'],xyz.outputs['Z'],.053,1.425,.065,.052)
        band=ellipse_mask(nodes,links,xyz.outputs['X'],xyz.outputs['Z'],0,1.465,.118,.026)
        headphones=op(nodes,links,'MAXIMUM',op(nodes,links,'MAXIMUM',left,right),band)
        headphones=op(nodes,links,'MULTIPLY',headphones,op(nodes,links,'LESS_THAN',xyz.outputs['Y'],.14))
        garment=op(nodes,links,'MULTIPLY',garment,op(nodes,links,'SUBTRACT',1,headphones))
        mix=nodes.new('ShaderNodeMixRGB');links.new(garment,mix.inputs[0]);links.new(source,mix.inputs[1]);links.new(color,mix.inputs[2]);color=mix.outputs[0]
    if spec['kind']=='Rook' and mode=='cloth':
        # The forward thigh sits below the hands; the shirt hem is behind it.
        trousers=op(nodes,links,'MULTIPLY',op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.015),op(nodes,links,'LESS_THAN',xyz.outputs['Y'],.11))
        # The raised right cuff overlaps the lap in height, but is behind
        # the forward thigh. Keep its actual fabric in the shirt region.
        cuff=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],.973),op(nodes,links,'GREATER_THAN',xyz.outputs['Y'],.055))
        cuff=op(nodes,links,'MULTIPLY',cuff,op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.165))
        cuff=op(nodes,links,'MULTIPLY',cuff,op(nodes,links,'LESS_THAN',xyz.outputs['X'],.30))
        trousers=op(nodes,links,'MULTIPLY',trousers,op(nodes,links,'SUBTRACT',1,cuff))
        mix=nodes.new('ShaderNodeMixRGB');links.new(trousers,mix.inputs[0]);links.new(color,mix.inputs[1]);links.new(source,mix.inputs[2]);color=mix.outputs[0]
    if spec['kind']=='Knight' and mode=='cloth':
        # The hoodie drops behind the seated waist. A constant Z cutoff used
        # to leave a black band across the sleeves and half of the back.
        back=op(nodes,links,'MINIMUM',op(nodes,links,'MAXIMUM',op(nodes,links,'DIVIDE',op(nodes,links,'SUBTRACT',xyz.outputs['Y'],.04),.23),0),1)
        hem=op(nodes,links,'SUBTRACT',.865,op(nodes,links,'MULTIPLY',back,.105))
        garment=op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],hem)
        garment=op(nodes,links,'MULTIPLY',garment,op(nodes,links,'GREATER_THAN',xyz.outputs['Y'],-.17))
        # At neck height only the hood behind the head belongs to the cloth.
        hood=op(nodes,links,'MAXIMUM',op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.48),op(nodes,links,'GREATER_THAN',xyz.outputs['Y'],.08))
        hood=op(nodes,links,'MULTIPLY',hood,op(nodes,links,'LESS_THAN',xyz.outputs['Z'],1.56))
        garment=op(nodes,links,'MULTIPLY',garment,hood)
        mix=nodes.new('ShaderNodeMixRGB');links.new(garment,mix.inputs[0]);links.new(source,mix.inputs[1]);links.new(color,mix.inputs[2]);color=mix.outputs[0]
    if spec['kind']=='Queen' and mode=='cloth':
        # Scarf is in front of the cardigan. Its cream highlights belong to
        # the scarf too, so selecting only blue pixels creates apparent tears.
        lower_edge=op(nodes,links,'SUBTRACT',1.235,op(nodes,links,'MULTIPLY',op(nodes,links,'ABSOLUTE',xyz.outputs['X']),.35))
        upper=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],lower_edge),op(nodes,links,'LESS_THAN',xyz.outputs['Y'],-.08))
        upper=op(nodes,links,'MULTIPLY',upper,op(nodes,links,'LESS_THAN',op(nodes,links,'ABSOLUTE',xyz.outputs['X']),.19))
        left=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],1.015),op(nodes,links,'LESS_THAN',xyz.outputs['X'],-.018))
        left=op(nodes,links,'MULTIPLY',left,op(nodes,links,'LESS_THAN',xyz.outputs['Y'],-.135))
        right=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',xyz.outputs['Z'],.935),op(nodes,links,'GREATER_THAN',xyz.outputs['X'],.052))
        depth=op(nodes,links,'ADD',-.185,op(nodes,links,'MINIMUM',op(nodes,links,'MAXIMUM',op(nodes,links,'MULTIPLY',op(nodes,links,'SUBTRACT',xyz.outputs['Z'],.94),.32),0),.045))
        right=op(nodes,links,'MULTIPLY',right,op(nodes,links,'LESS_THAN',xyz.outputs['Y'],depth))
        scarf=op(nodes,links,'MAXIMUM',upper,op(nodes,links,'MAXIMUM',left,right))
        mix=nodes.new('ShaderNodeMixRGB');links.new(scarf,mix.inputs[0]);links.new(color,mix.inputs[1]);links.new(source,mix.inputs[2]);color=mix.outputs[0]
    if spec['kind'] in ['King','Knight'] and mode=='cloth':
        plain=nodes.new('ShaderNodeMixRGB');links.new(legacy_logo_mask(nodes,links,spec),plain.inputs[0]);links.new(color,plain.inputs[1])
        plain.inputs[2].default_value=(.67,.6633,.6499,1) if side=='White' else (.0261,.02784,.029,1)
        color=plain.outputs[0]
    output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
    emission=nodes.new('ShaderNodeEmission');links.new(color,emission.inputs[0]);links.new(emission.outputs[0],output.inputs['Surface'])
    return m

def bake_body(body,original,spec,median,directory):
    textures={}; authored=[]
    for side in ['White','Black']:
        mats=[clothing_material(original,spec,side,mode,median) for mode in ['original','cloth','pants']]
        indices=[p.material_index for p in body.data.polygons]
        body.data.materials.clear()
        for m in mats:body.data.materials.append(m)
        for p,index in zip(body.data.polygons,indices):p.material_index=index
        img=bpy.data.images.new(f"{spec['kind']}_Body_{side}",4096,4096,alpha=False)
        for m in mats:
            node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=img;m.node_tree.nodes.active=node
        bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
        # Wide UV padding keeps atlas borders from turning dark in Unity mip levels.
        scene.cycles.samples=1; scene.render.bake.margin=48;scene.render.bake.margin_type='EXTEND'
        bpy.ops.object.bake(type='EMIT')
        img.filepath_raw=str(directory/f'Body_{side}.png');img.file_format='PNG';img.save()
        textures[side]=img;authored+=mats
    # Re-bake source normals plus a restrained woven relief on garment faces.
    # The added detail is authored here; enlarging the color map alone would
    # not recover detail missing from the original 2K facial texture.
    normal=bpy.data.images.new(spec['kind']+'_Body_Normal',2048,2048,alpha=False)
    normal.colorspace_settings.name='Non-Color'
    normal_mats=[]
    for mode in ['original','cloth','pants']:
        material=original.copy();material.name=spec['kind']+'_Weave_'+mode
        nodes=material.node_tree.nodes;links=material.node_tree.links;p=nodes.get('Principled BSDF')
        if mode=='cloth' and spec['kind'] in ['King','Knight']:
            remaining=op(nodes,links,'SUBTRACT',1,legacy_logo_mask(nodes,links,spec))
            for node in list(nodes):
                if node.type=='NORMAL_MAP':links.new(remaining,node.inputs['Strength'])
        if mode!='original':
            coord=nodes.new('ShaderNodeTexCoord');weave=nodes.new('ShaderNodeTexWave')
            weave.bands_direction='DIAGONAL';weave.inputs['Scale'].default_value=180
            weave.inputs['Distortion'].default_value=1.3;links.new(coord.outputs['Object'],weave.inputs['Vector'])
            bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.07;bump.inputs['Distance'].default_value=.00025
            if mode=='cloth':
                rgb=nodes.new('ShaderNodeSeparateColor');links.new(p.inputs['Base Color'].links[0].from_socket,rgb.inputs[0])
                skin_mask=op(nodes,links,'MULTIPLY',op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],op(nodes,links,'MULTIPLY',rgb.outputs['Green'],1.10)),op(nodes,links,'GREATER_THAN',rgb.outputs['Red'],.07))
                links.new(op(nodes,links,'MULTIPLY',op(nodes,links,'SUBTRACT',1,skin_mask),.07),bump.inputs['Strength'])
            if p.inputs['Normal'].is_linked:links.new(p.inputs['Normal'].links[0].from_socket,bump.inputs['Normal'])
            links.new(weave.outputs['Color'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])
        target=nodes.new('ShaderNodeTexImage');target.image=normal;nodes.active=target;normal_mats.append(material)
    body.data.materials.clear()
    for material in normal_mats:body.data.materials.append(material)
    for p,index in zip(body.data.polygons,indices):p.material_index=index
    bpy.ops.object.bake(type='NORMAL')
    normal.filepath_raw=str(directory/'Body_Normal.png');normal.file_format='PNG';normal.save()
    white=original.copy(); white.name=spec['kind']+'_Body_White'
    p=white.node_tree.nodes.get('Principled BSDF')
    node=white.node_tree.nodes.new('ShaderNodeTexImage');node.image=textures['White'];white.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
    for input_name in ['Metallic','Roughness']:
        for link in list(p.inputs[input_name].links):white.node_tree.links.remove(link)
    p.inputs['Metallic'].default_value=0;p.inputs['Roughness'].default_value=.72
    normal_tex=white.node_tree.nodes.new('ShaderNodeTexImage');normal_tex.image=normal
    normal_map=white.node_tree.nodes.new('ShaderNodeNormalMap');white.node_tree.links.new(normal_tex.outputs['Color'],normal_map.inputs['Color']);white.node_tree.links.new(normal_map.outputs['Normal'],p.inputs['Normal'])
    black=white.copy();black.name=spec['kind']+'_Body_Black'
    black.node_tree.nodes.get(node.name).image=textures['Black']
    body.data.materials.clear();body.data.materials.append(white)
    for face in body.data.polygons:face.material_index=0
    # The editable source keeps SourceUV; the exported material uses ProductionUV.
    for material in authored:material.use_fake_user=True
    return white,black

def make_brand_materials():
    """Original transparent artwork, shared by all backs without a badge plate."""
    result={};directory=ASSETS/'Brand';directory.mkdir(exist_ok=True)
    for side in ['White','Black']:
        material=mat('FeevalePatch_'+side,(1,1,1),.90)
        nodes=material.node_tree.nodes;links=material.node_tree.links
        shader=nodes.get('Principled BSDF');logo=nodes.new('ShaderNodeTexImage');logo.image=brand_images[side]
        links.new(logo.outputs['Color'],shader.inputs['Base Color'])
        # Smooth alpha keeps thin official lettering intact in distant mipmaps.
        # This decal receives depth from the opaque garment and casts no shadow.
        links.new(logo.outputs['Alpha'],shader.inputs['Alpha'])
        material.surface_render_method='DITHERED'
        shutil.copy2(bpy.path.abspath(logo.image.filepath),directory/(side+'.png'))
        result[side]=material
    return result

def add_brand_patches(body,spec,materials):
    """Back-only Feevale application, below the hood/scarf and clear of arms."""
    surface=BVHTree.FromPolygons([v.co for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons])
    for back in [True]:
        cx,cz,width=spec['logo'] if not back else (0,1.21,.35)
        nx,nz=32,12
        clearance=.0022
        faces=[]
        for attempt in range(9):
            height=width*369/950;verts=[];coords=[];misses=0
            for row in range(nz+1):
                for col in range(nx+1):
                    u,v=col/nx,row/nz;x=cx+(u-.5)*width;z=cz+(v-.5)*height
                    origin=Vector((x,2 if back else -2,z));direction=Vector((0,-1 if back else 1,0))
                    hit,normal,_,_=surface.ray_cast(origin,direction)
                    if hit is None or abs(normal.y)<.35:
                        misses+=1;continue
                    # Keep the UV grid's X/Z coordinates exact. Offsetting by
                    # source normals can push a label into inward-facing folds
                    # or move its lettering sideways across a hoodie cord.
                    verts.append(hit-direction*clearance);coords.append((1-u if back else u,v))
            if not misses:break
            width*=.94
        if misses:raise RuntimeError(f'Brand patch misses clothing: {spec["kind"]} back={back} misses={misses}')
        for row in range(nz):
            for col in range(nx):
                a=row*(nx+1)+col;face=(a,a+1,a+nx+2,a+nx+1)
                faces.append(tuple(reversed(face)) if back else face)
        mesh=bpy.data.meshes.new(spec['kind']+'_Feevale');mesh.from_pydata(verts,[],faces);mesh.update()
        uv=mesh.uv_layers.new(name='BrandUV')
        for loop in mesh.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
        ob=bpy.data.objects.new(spec['kind']+('_FeevaleBack' if back else '_FeevaleChest'),mesh);scene.collection.objects.link(ob)
        finish(ob,ob.name,materials['White'])

brand_materials=make_brand_materials()

def hand_and_grip(body,spec,skin,kind):
    # Keep the forearm; replace the open/gesture hand with an authored curled grip.
    if kind=='Pawn':
        candidates=[(i,Vector(c),rgb) for i,c,rgb in skin if c[0]<-.175 and c[1]<-.26 and 1.21<c[2]<1.36]
    else:
        candidates=[(i,Vector(c),rgb) for i,c,rgb in skin if c[0]<-.20 and .48<c[2]<.88]
    if not candidates:
        raise RuntimeError('Could not locate hand: '+kind)
    center=sum((c for _,c,_ in candidates),Vector())/len(candidates)
    if kind=='Pawn':center.z=1.25
    color=np.median(np.asarray([rgb for _,_,rgb in candidates]),axis=0)
    color=np.where(color<=.04045,color/12.92,((color+.055)/1.055)**2.4)
    skin_mat=mat(kind+'_HandSkin',tuple(float(v) for v in color),.62)
    # Identify by spatial proximity after crown/base face removal changed indices.
    edit=bmesh.new();edit.from_mesh(body.data)
    source_material=body.data.materials[0]
    source_texture=source_material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].links[0].from_node.image
    pixels=np.asarray(source_texture.pixels[:],dtype=np.float32).reshape(source_texture.size[1],source_texture.size[0],4)
    source_uv=edit.loops.layers.uv.active
    deleted=[]
    hand_min=Vector(tuple(min(c[axis] for _,c,_ in candidates)-.035 for axis in range(3)))
    hand_max=Vector(tuple(max(c[axis] for _,c,_ in candidates)+.035 for axis in range(3)))
    for f in edit.faces:
        c=f.calc_center_median()
        samples=[]
        for loop in f.loops:
            uv=loop[source_uv].uv
            samples.append(pixels[min(max(int(uv.y*source_texture.size[1]),0),source_texture.size[1]-1),min(max(int(uv.x*source_texture.size[0]),0),source_texture.size[0]-1),:3])
        r,g,b=np.mean(samples,axis=0)
        hand_color=r>g*1.02 and r>b*1.05 and r>.045
        replace_pawn=kind=='Pawn' and c.x<-.175 and c.y<-.26 and 1.21<c.z<1.40
        replace_other=kind!='Pawn' and hand_color and c.x<-.18 and hand_min.y<c.y<hand_max.y and hand_min.z<c.z<center.z+.15 and c.x<hand_max.x
        if replace_pawn or replace_other:deleted.append(f)
    bmesh.ops.delete(edit,geom=deleted,context='FACES');edit.to_mesh(body.data);edit.free();body.data.update()
    if kind!='Pawn':
        # Bend the forearm forward from the elbow; the staff must not follow
        # the arm's vertical silhouette. The old hand was removed above.
        delta=Vector((-.035,-.13,.015))
        for vertex in body.data.vertices:
            p=vertex.co
            # The hand rests close to the thigh in the source. A half-space
            # would move the trouser vertices too; restrict this to the arm.
            radial=math.hypot(p.x-center.x,p.y-center.y)
            radius=.095 if kind=='King' else .080
            if p.x<-.19 and radial<radius and center.z+.045<p.z<center.z+.31:
                weight=max(0,min(1,(center.z+.31-p.z)/.20))
                weight=weight*weight*(3-2*weight)
                weight*=min(1,(radius-radial)/.02)
                vertex.co+=delta*weight
        body.data.update()
        center+=delta+Vector((0,-.07,0))
    else:
        center.y-=.008
    axis=Vector((.22,-.28,-.72)).normalized() if kind=='Pawn' else Vector((-.07,.30,-1)).normalized()
    grip_length=.13
    start=center-axis*.09;end=center+axis*.105
    bar(kind+'_Grip',start,end,.020,LEATHER)
    side=Vector((1,0,0));front=axis.cross(side).normalized();side=front.cross(axis).normalized()
    palm=sphere(kind+'_GrippingPalm',center-front*.016,(.043,.024,.055),skin_mat)
    palm.rotation_euler=axis.to_track_quat('Z','Y').to_euler()
    if kind!='Pawn':bar(kind+'_Wrist',center+Vector((0,.014,.026)),center+Vector((.002,.085,.105)),.030,skin_mat)
    for idx,offset in enumerate([-.041,-.014,.014,.040]):
        origin=center+axis*offset
        angles=np.linspace(-.42,[3.78,3.94,3.86,3.60][idx],7)
        pts=[origin+side*(math.cos(a)*.032)+front*(math.sin(a)*.028)+axis*(.003*math.sin(a)) for a in angles]
        path_tube(kind+'_Finger_'+str(idx),pts,.012 if idx<3 else .0098,skin_mat,[1.05,1.12,1.0,1.04,.94,.80,.54])
    path_tube(kind+'_Thumb',[center-axis*.030+side*.042+front*.018,center-axis*.048+side*.025-front*.007,center-axis*.040+side*.001-front*.027],.016,skin_mat,[1.1,1,.60])
    if kind=='Pawn':
        guard=center+axis*.095
        bar('Sword_Crossguard',guard-side*.07,guard+side*.07,.012,BRASS)
        sphere('Sword_Pommel',center-axis*.090,(.025,.025,.026),BRASS)
        a=guard-axis*.005;tip=a+axis*.41
        # Raised central ridge and tapered edges, with a small blunt tip.
        width=.046;thickness=.010
        verts=[a-side*width,a+side*width,a+front*thickness,a-front*thickness,
               tip-side*.005,tip+side*.005,tip+front*.003,tip-front*.003]
        faces=[(0,4,6,2),(2,6,5,1),(1,5,7,3),(3,7,4,0),(0,2,1,3),(4,7,5,6)]
        mesh=bpy.data.meshes.new('SwordBlade');mesh.from_pydata(verts,[],faces);mesh.update()
        ob=bpy.data.objects.new('Sword_Blade',mesh);scene.collection.objects.link(ob);finish(ob,ob.name,STEEL,0,False)
    else:
        up=-axis
        at_z=lambda z:center+up*((z-center.z)/up.z)
        top=at_z(1.10 if kind=='King' else 1.56)
        foot=at_z(spec['cut'])
        bar(kind+'_StaffShaft',foot,top,.014,LEATHER)
        for z in [foot.z+.03,center.z-.09,center.z+.09,top.z-.055]:
            ferrule=cylinder(kind+'_StaffFerrule',at_z(z),.023,.035,BRASS,20)
            ferrule.rotation_euler=up.to_track_quat('Z','Y').to_euler()
        if kind=='King':
            cube('King_ScepterCrossStem',top+Vector((0,0,.049)),(.025,.022,.14),BRASS,.004)
            cube('King_ScepterCrossArms',top+Vector((0,0,.07)),(.11,.022,.026),BRASS,.004)
        else:
            pts=[top+Vector((math.sin(a)*.060,0,math.cos(a)*.060)) for a in np.linspace(-math.pi/2,math.pi,9)]
            path_tube('Bishop_Crozier',pts,.015,BRASS)
    return tuple(round(v,5) for v in center)

for column,spec in enumerate(SPECS):
    kind=spec['kind']; directory=ASSETS/kind;directory.mkdir(parents=True,exist_ok=True)
    print('BUILD_CHARACTER',kind,flush=True)
    before=set(bpy.data.objects)
    body,original,median,skin,source=import_body(spec)
    grip=None
    if kind in ['Pawn','King','Bishop']:grip=hand_and_grip(body,spec,skin,kind)
    # Preserve the complete original head, including its small crown. A height
    # plane cannot distinguish crown from hair/forehead on the combined mesh.
    # A single subdivision pass rounds garment edges and the original coarse silhouettes.
    # UVs and the source facial textures stay attached to the original surface.
    # glTF splits vertices at UV/normal seams. Weld positions before subdivision;
    # per-loop UV coordinates remain intact, so facial and clothing maps are retained.
    welded=bmesh.new();welded.from_mesh(body.data)
    bmesh.ops.remove_doubles(welded,verts=list(welded.verts),dist=.00001)
    if kind in ['King','Bishop']:
        # Removing only skin preserves the adjacent trousers. Discard isolated
        # remnants of the old fingers/wrist; the new hand has its own geometry.
        seen=set();remnants=[]
        for face in welded.faces:
            if face in seen:continue
            pending=[face];seen.add(face);component=[]
            while pending:
                current=pending.pop();component.append(current)
                for vertex in current.verts:
                    for neighbor in vertex.link_faces:
                        if neighbor not in seen:seen.add(neighbor);pending.append(neighbor)
            vertices={v for f in component for v in f.verts}
            if len(component)<200 and all(v.co.x<-.12 and .48<v.co.z<1.05 for v in vertices):
                remnants.extend(component)
        bmesh.ops.delete(welded,geom=remnants,context='FACES')
    welded.to_mesh(body.data);welded.free();body.data.update()
    bpy.context.view_layer.objects.active=body
    subdiv=body.modifiers.new('GarmentAndSilhouetteFinish','SUBSURF');subdiv.levels=1
    bpy.ops.object.modifier_apply(modifier=subdiv.name)
    prepare_bake_uvs(body,original)
    white,black=bake_body(body,original,spec,median,directory)
    add_brand_patches(body,spec,brand_materials)
    authored=sorted(set(bpy.data.objects)-before,key=lambda o:o.name)
    maxz=max((o.matrix_world@Vector(c)).z for o in authored if o.type=='MESH' for c in o.bound_box)
    scale=(spec['height']-.12)/(maxz-spec['cut'])
    matrix=Matrix.Translation((0,0,.12))@Matrix.Scale(scale,4)@Matrix.Translation((0,0,-spec['cut']))
    root=bpy.data.objects.new(kind+'_White',None);scene.collection.objects.link(root)
    for ob in authored:
        ob.matrix_world=matrix@ob.matrix_world;ob.parent=root
    base=plinth();base.parent=root
    foot=cylinder('PlinthFoot',(0,0,.007),.55,.014,LEATHER,96);foot.parent=root
    for z,radius in [(.032,.555),(.123,.547)]:
        bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=.0025,major_segments=96,minor_segments=8,location=(0,0,z))
        finish(bpy.context.object,'BrassRim',BRASS).parent=root
    for y,rot in [(-.30,0),(.30,math.pi)]:glyph(kind,y,rot).parent=root
    # Merge export copies by material to avoid one draw call for every finger.
    export_root=bpy.data.objects.new(kind+'_Model',None);scene.collection.objects.link(export_root)
    groups={}
    for ob in root.children:
        duplicate=ob.copy();duplicate.data=ob.data.copy();scene.collection.objects.link(duplicate)
        if duplicate.type=='MESH' and 'SourceUV' in duplicate.data.uv_layers:
            duplicate.data.uv_layers.remove(duplicate.data.uv_layers['SourceUV'])
        duplicate.parent=export_root
        groups.setdefault(ob.data.materials[0].name,[]).append(duplicate)
    for material_name,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for ob in objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join();objects[0].name='Mesh_'+material_name
        # Joined accessories inherit the first part's rotation. Bake it into
        # vertices so Unity measures the actual footprint, not a rotated AABB
        # around a long staff or a collection of brass parts.
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in [export_root,*export_root.children_recursive]:ob.select_set(True)
    bpy.context.view_layer.objects.active=export_root
    bpy.ops.export_scene.gltf(filepath=str(directory/'Model.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_animations=False,export_cameras=False,export_lights=False)
    for ob in [*export_root.children_recursive,export_root]:bpy.data.objects.remove(ob,do_unlink=True)
    # Black pieces use the very same Mesh datablocks; only object material slots differ.
    dark=bpy.data.objects.new(kind+'_Black',None);scene.collection.objects.link(dark)
    swaps={white:black,IVORY:CHARCOAL,INK:LIGHT_INK,brand_materials['White']:brand_materials['Black']}
    for ob in root.children:
        clone=ob.copy();clone.data=ob.data;scene.collection.objects.link(clone);clone.parent=dark
        clone.name=ob.name+'_Black'
        for slot in clone.material_slots:
            existing=slot.material;slot.link='OBJECT';slot.material=swaps.get(existing,existing)
    root.location=((column-(len(SPECS)-1)/2)*1.18,0,0)
    dark.location=(root.location.x,2.5,0)
    roots += [root,dark]
    record={'kind':kind,'prefab':spec['stem'],'name':spec['name'],'height':spec['height'],
            'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'cloth_luminance_median':median,
            'grip_center_authoring_space':grip,'authored_objects':len(root.children),'export_renderers':len(groups),
            'mesh_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in root.children if o.type=='MESH'),
            'same_meshes_for_sides':sorted(o.data.name for o in root.children)==sorted(o.data.name for o in dark.children),
            'body_black_texture':f'Assets/Art/Characters/Direction02/{kind}/Body_Black.png'}
    report['characters'].append(record)
    print('CHARACTER_OK',json.dumps(record),flush=True)

# One native review scene, showing actual exported candidate geometry and textures.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001));finish(bpy.context.object,'StudioFloor',FLOOR,0,False)
for name,loc,power,size in [('Key',(-3,-4,6),800,5),('Fill',(4,-1,5),650,5),('Rim',(0,4,5),1000,5)]:
    ld=bpy.data.lights.new(name,'AREA');ob=bpy.data.objects.new(name,ld);scene.collection.objects.link(ob)
    ob.location=loc;ob.rotation_euler=(Vector((0,1,.8))-ob.location).to_track_quat('-Z','Y').to_euler();ld.energy=power;ld.shape='DISK';ld.size=size
cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO'
width=max(3.0,len(SPECS)*1.20)
scene.render.resolution_x=1600 if QUICK else 2400;scene.render.resolution_y=1100 if QUICK else 1650;scene.render.resolution_percentage=100
scene.cycles.samples=20 if QUICK else 48
for name,loc,target,scale in [('front',(0,-9,8.0),(0,1.2,.63),width+1),('angle',(0,-7,9),(0,1.2,.45),width+1.0),('top',(0,1.25,10),(0,1.25,0),max(width+.8,4.0*scene.render.resolution_x/scene.render.resolution_y))]:
    if NO_RENDER:break
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=max(scale,5.1) if KINDS else scale
    scene.render.filepath=str(ART/(('pilot-' if KINDS else '')+name+'.png'));bpy.ops.render.render(write_still=True)
# Temporary bake graphs and export copies must not inflate the editable source.
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.file.pack_all()
# Generated PNGs already live alongside the Unity assets. Keep relative
# references instead of duplicating them in a >100 MB Blender file.
for image in bpy.data.images:
    path=Path(bpy.path.abspath(image.filepath)).resolve()
    if path.is_file() and path.is_relative_to(ASSETS):
        if image.packed_file:image.unpack(method='REMOVE')
        image.filepath=bpy.path.relpath(str(path),start=str(ART))
bpy.ops.wm.save_as_mainfile(filepath=str(ART/('pilot.blend' if KINDS else 'characters.blend')),compress=True)
(ART/('pilot-report.json' if KINDS else 'report.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('DIRECTION02_BUILD_COMPLETE',flush=True)
