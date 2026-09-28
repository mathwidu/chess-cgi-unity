"""Geometric regression checks against the actual authored Blender scene.

blender -b production/characters.blend --python-exit-code 1 --python audit_characters.py
This checks contact/occlusion, not artistic quality; review the renders as well.
"""
import bpy, bmesh, json, sys, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from pathlib import Path
import numpy as np

bpy.context.view_layer.update()
results = []; failures = []

def tree(ob):
    return BVHTree.FromPolygons([ob.matrix_world @ v.co for v in ob.data.vertices],
                               [tuple(p.vertices) for p in ob.data.polygons])

for root in sorted((o for o in bpy.data.objects if o.type == 'EMPTY' and o.name.endswith('_White')), key=lambda o:o.name):
    kind=root.name.split('_')[0]
    body=next(o for o in root.children if o.name.startswith(kind+'_Body'))
    body_tree=tree(body)
    record={'kind':kind}
    if kind=='Knight':
        texture=bpy.data.images['Knight_Body_White']
        pixels=np.asarray(texture.pixels[:],dtype=np.float32).reshape(texture.size[1],texture.size[0],4)
        body.data.calc_loop_triangles();uv=body.data.uv_layers.active;group=body.vertex_groups['TeamCloth'].index;samples=[]
        for triangle in body.data.loop_triangles:
            point=sum((body.data.vertices[i].co for i in triangle.vertices),Vector())/3
            if not 1.10<point.z<1.30:continue
            if not all(any(g.group==group and g.weight>.99 for g in body.data.vertices[i].groups) for i in triangle.vertices):continue
            coords=[uv.data[i].uv for i in triangle.loops]
            for weights in [(1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)]:
                q=sum((co*w for co,w in zip(coords,weights)),Vector((0,0)))
                rgb=pixels[min(max(int(q.y*texture.size[1]),0),texture.size[1]-1),min(max(int(q.x*texture.size[0]),0),texture.size[0]-1),:3]
                samples.append(float(np.mean(rgb)))
        record['white_cloth_samples']=len(samples)
        record['white_cloth_min_value']=round(min(samples),4)
        if min(samples)<.25:failures.append('Knight: dark fabric pixels wrongly preserved as skin on the white hoodie')
    if kind in ['King','Bishop']:
        mesh=bmesh.new();mesh.from_mesh(body.data);seen=set();remnants=[]
        for face in mesh.faces:
            if face in seen:continue
            pending=[face];seen.add(face);component=[]
            while pending:
                current=pending.pop();component.append(current)
                for vertex in current.verts:
                    for neighbor in vertex.link_faces:
                        if neighbor not in seen:seen.add(neighbor);pending.append(neighbor)
            vertices={v for f in component for v in f.verts}
            if len(component)<600 and all(v.co.x<-.12 and .48<v.co.z<1.05 for v in vertices):remnants.append(len(component))
        mesh.free();record['detached_old_hand_fragments']=remnants
        if remnants:failures.append(kind+': disconnected remnants of the old hand')
    if kind=='King':
        source=Path(__file__).resolve().parents[2]/'game/Assets/Resources/CustomPieces/King_Ricardo_Carioca_Assets/selected.glb'
        before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(source))
        added=set(bpy.data.objects)-before;original=next(o for o in added if o.type=='MESH')
        points=[original.matrix_world@v.co for v in original.data.vertices]
        lo=min(v.z for v in points);height=max(v.z for v in points)-lo
        local_tree=BVHTree.FromPolygons([v.co for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons])
        points=[(p-Vector((0,0,lo)))*(1.8/height) for p in points]
        distances=[local_tree.find_nearest(p)[3] for p in points if p.z>1.62]
        record['original_head_max_deviation']=round(max(distances),5)
        record['original_head_mean_deviation']=round(sum(distances)/len(distances),5)
        if max(distances)>.025 or sum(distances)/len(distances)>.009:
            failures.append('King: original head silhouette has been cut or replaced')
        for ob in added:bpy.data.objects.remove(ob,do_unlink=True)
    shafts=[o for o in root.children if '_StaffShaft' in o.name]
    for shaft in shafts:
        lo=min(v.co.z for v in shaft.data.vertices);hi=max(v.co.z for v in shaft.data.vertices)
        # The source body has open boundaries where a hand was replaced.
        # Nearest-normal signs cannot classify inside/outside on that mesh.
        # Test actual shaft triangle edges against body triangles instead.
        shaft.data.calc_loop_triangles()
        edges={tuple(sorted((t.vertices[i],t.vertices[(i+1)%3]))) for t in shaft.data.loop_triangles for i in range(3)}
        collisions=set()
        for a,b in sorted(edges):
            start=shaft.matrix_world@shaft.data.vertices[a].co;end=shaft.matrix_world@shaft.data.vertices[b].co
            direction=end-start;length=direction.length
            if length<.000001:continue
            hit,_,_,distance=body_tree.ray_cast(start,direction.normalized(),length)
            if hit is not None and .000001<distance<length-.000001:
                collisions.add(tuple(round(v,5) for v in hit))
        record['shaft_body_intersections']={'count':len(collisions),'first_points':sorted(collisions)[:8]}
        if collisions:failures.append(kind+': staff intersects body outside the grip ('+str(len(collisions))+' samples)')
    symbols=[o for o in root.children if o.name.startswith('RoleSymbol_')]
    visibility=[]
    for symbol in symbols:
        local_points=[root.matrix_world.inverted()@symbol.matrix_world@v.co for v in symbol.data.vertices]
        if max(math.hypot(v.x,v.y) for v in local_points)>.545:
            failures.append(kind+': role symbol extends beyond the plinth rim')
        symbol.data.calc_loop_triangles()
        visible=total=0.
        for tri in symbol.data.loop_triangles:
            points=[symbol.matrix_world @ symbol.data.vertices[i].co for i in tri.vertices]
            area=(points[1]-points[0]).cross(points[2]-points[0]).length/2
            center=sum(points,Vector())/3
            hit=body_tree.ray_cast(center+Vector((0,0,4)),Vector((0,0,-1)),3.999)[0]
            total+=area
            if hit is None:visible+=area
        fraction=visible/total if total else 0
        visibility.append(round(fraction,4))
    record['role_symbol_visible_from_above']=visibility
    if any(v<.95 for v in visibility):failures.append(kind+': role symbol hidden from above')
    patches=[o for o in root.children if o.name.startswith(kind+'_Feevale')]
    patch_checks=[]
    for patch in patches:
        uv=patch.data.uv_layers.active
        patch.data.calc_loop_triangles()
        uv_area=0.;min_gap=1.;max_gap=0.
        for tri in patch.data.loop_triangles:
            a,b,c=[uv.data[i].uv for i in tri.loops]
            uv_area+=abs((b.x-a.x)*(c.y-a.y)-(c.x-a.x)*(b.y-a.y))/2
            point=sum((patch.matrix_world@patch.data.vertices[i].co for i in tri.vertices),Vector())/3
            nearest,normal,_,distance=body_tree.find_nearest(point)
            outward=Vector((0,1 if 'Back' in patch.name else -1,0))
            first_surface=body_tree.ray_cast(point+outward*4,-outward,8)[0]
            signed=(point-first_surface).dot(outward) if first_surface is not None else -1
            min_gap=min(min_gap,signed);max_gap=max(max_gap,distance)
        patch_checks.append({'patch':patch.name,'complete_uv_area':round(uv_area,4),'min_surface_gap':round(min_gap,5),'max_surface_gap':round(max_gap,5)})
        if abs(uv_area-1)>.001 or min_gap<.0001 or max_gap>.007:
            failures.append(kind+': logo clipped or detached from clothing')
    record['brand_patches']=patch_checks
    if len(patches)!=1 or not patches[0].name.endswith('_FeevaleBack'):
        failures.append(kind+': Feevale must appear only on the back')
    results.append(record)

report={'checks':results,'failures':failures,'passed':not failures}
print('CHARACTER_GEOMETRY_AUDIT '+json.dumps(report,ensure_ascii=False),flush=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if '--report' in args:Path(args[args.index('--report')+1]).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
if failures:raise RuntimeError('; '.join(failures))
