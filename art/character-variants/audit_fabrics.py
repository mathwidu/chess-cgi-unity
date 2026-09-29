"""Sample rendered body UVs in garment regions reported by the user.

Run with Blender on production/characters.blend. Coordinates refer to the
normalized source mesh (1.8 high), not the display layout or team material.
The checks concern unwanted old color/holes; artistic review is separate.
"""
from pathlib import Path
import bpy, json, sys
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

REGIONS = [
    ('Pawn', 'Black', 'shirt', (-.14,.14,1.03,1.40), .065, .48),
    ('Pawn', 'Black', 'cargo', (-.14,.14,.48,.85), .065, .48),
    ('King', 'Black', 'hoodie_hem', (-.17,.17,.75,.82), .065, .48),
    ('Queen', 'Black', 'right_shoulder', (.16,.205,1.24,1.39), .065, .48),
    ('Queen', 'Black', 'left_shoulder', (-.205,-.16,1.24,1.39), .065, .48),
    ('Queen', 'Black', 'cardigan_between_scarf_tails', (-.006,.04,1.16,1.21), .065, .48),
    ('Bishop', 'White', 'fabric_below_headphones', (-.13,.13,1.315,1.35), 1., 1.),
    ('Pawn', 'Black', 'free_hand', (.215,.245,.825,.915), 1., 1.),
    ('Pawn', 'White', 'waist_cargo_preserved', (-.14,.14,.885,.917), 1., 1.),
    ('Rook', 'White', 'collar_cloth', (-.12,.12,1.37,1.455), 1., 1.),
    ('Rook', 'White', 'sleeve_edges', (-.34,.34,1.04,1.13), 1., 1.),
    ('Rook', 'White', 'right_cuff', (.205,.245,.986,1.013), 1., 1.),
    ('Rook', 'White', 'seated_forearm', (-.28,.28,.84,1.02), 1., 1.),
    ('Rook', 'Black', 'seated_forearm', (-.28,.28,.84,1.02), 1., 1.),
    # These narrow regions avoid the horse's head and exposed wrists.
    ('Knight', 'White', 'lower_hoodie', (-.10,-.065,.96,.99), 1., 1.),
    ('Knight', 'White', 'left_cuff', (-.32,-.29,.95,.98), 1., 1.),
    ('Knight', 'White', 'hood_rim_back', (-.06,.035,1.515,1.54), 1., 1.),
    ('Knight', 'White', 'hoodie_waist_back', (-.08,.08,.83,.90), 1., 1.),
    ('Knight', 'White', 'hands_preserved', (-.24,-.12,.88,.95), 1., 1.),
    ('Knight', 'White', 'mount_preserved', (-.065,.065,.66,.84), 1., 1.),
    ('Knight', 'Black', 'hands_preserved', (-.24,-.12,.88,.95), 1., 1.),
    ('Knight', 'Black', 'mount_preserved', (-.065,.065,.66,.84), 1., 1.),
]
def image_pixels(image):
    values=np.empty(len(image.pixels),dtype=np.float32)
    image.pixels.foreach_get(values)
    return values.reshape(image.size[1],image.size[0],4)

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if '--kinds' in args:
    kinds=args[args.index('--kinds')+1].split(',')
    REGIONS=[r for r in REGIONS if r[0] in kinds]
records=[];failures=[];cache={}
for kind,side,name,(x0,x1,z0,z1),max_chroma,max_value in REGIONS:
    body=bpy.data.objects[kind+'_Body'];mesh=body.data;mesh.calc_loop_triangles()
    triangles=list(mesh.loop_triangles)
    bvh=BVHTree.FromPolygons([v.co for v in mesh.vertices],[tuple(t.vertices) for t in triangles],all_triangles=True)
    image=bpy.data.images[kind+'_Body_'+side]
    key=(kind,side)
    if key not in cache:cache[key]=image_pixels(image)
    pixels=cache[key];samples=[];bad_points=[];mip_bleed=[];source_errors=[]
    if name in ('seated_forearm','collar_cloth','sleeve_edges','right_cuff') or name.endswith('_preserved'):
        original=bpy.data.materials['Author_'+kind+'_White_original'].node_tree.nodes.get('Principled BSDF').inputs['Base Color'].links[0].from_node.image
        original_pixels=image_pixels(original)
    for z in np.linspace(z0,z1,45):
        for x in np.linspace(x0,x1,45):
            back=name.endswith('_back')
            hit,_,index,_=bvh.ray_cast(Vector((float(x),3 if back else -3,float(z))),Vector((0,-1 if back else 1,0)))
            if hit is None:continue
            tri=triangles[index]
            coords=[Vector((*mesh.uv_layers.active.data[i].uv,0)) for i in tri.loops]
            uv=barycentric_transform(hit,*(mesh.vertices[i].co for i in tri.vertices),*coords)
            rgb=pixels[min(max(int(uv.y*image.size[1]),0),image.size[1]-1),min(max(int(uv.x*image.size[0]),0),image.size[0]-1),:3]
            if name in ('seated_forearm','collar_cloth','sleeve_edges','right_cuff') or name.endswith('_preserved'):
                coords=[Vector((*mesh.uv_layers['SourceUV'].data[i].uv,0)) for i in tri.loops]
                original_uv=barycentric_transform(hit,*(mesh.vertices[i].co for i in tri.vertices),*coords)
                source_rgb=original_pixels[int(original_uv.y*original.size[1]),int(original_uv.x*original.size[0]),:3]
                if name in ('collar_cloth','sleeve_edges','right_cuff'):
                    if not (source_rgb[2]>source_rgb[0]*1.10 and source_rgb[0]<.45):continue
                elif source_rgb[0]-source_rgb[2]<.15 or float(np.mean(source_rgb))<.35:continue
                if name.endswith('_preserved'):source_errors.append(float(np.max(np.abs(source_rgb-rgb))))
            samples.append(rgb)
            if kind=='Pawn' and name=='shirt':
                ix=int(uv.x*image.size[0]);iy=int(uv.y*image.size[1])
                footprint=pixels[max(0,iy-16):min(image.size[1],iy+17):4,max(0,ix-16):min(image.size[0],ix+17):4,:3]
                filtered=footprint.mean(axis=(0,1))
                mip_bleed.append(float(np.ptp(filtered))>.035)
            if float(np.ptp(rgb))>max_chroma or float(np.mean(rgb))>max_value:bad_points.append([round(float(x),3),round(float(z),3),*[round(float(v),3) for v in rgb]])
    if len(samples)<100:raise RuntimeError(kind+' '+name+': insufficient surface samples')
    values=np.asarray(samples);fraction=len(bad_points)/len(samples)
    record={'kind':kind,'side':side,'region':name,'samples':len(samples),'old_color_fraction':round(fraction,5),'chroma_99_percentile':round(float(np.quantile(np.ptp(values,axis=1),.99)),4),'first_bad_points':bad_points[:8]}
    if mip_bleed:
        record['distant_texture_color_bleed_fraction']=round(sum(mip_bleed)/len(mip_bleed),5)
        if sum(mip_bleed)/len(mip_bleed)>.015:failures.append(kind+' '+name+': adjacent skin/hair colors bleed into clothing at distance')
    records.append(record)
    if fraction>.015:failures.append(kind+' '+name+': old garment colors retained')
    if name in ('fabric_below_headphones','collar_cloth','sleeve_edges','right_cuff'):
        dark=float(np.mean(np.mean(values,axis=1)<.50))
        record['dark_patch_fraction']=round(dark,5)
        if dark>.015:failures.append(kind+' '+name+': old dark material remains along the white garment')
    if kind=='Knight' and name in ('lower_hoodie','left_cuff','hood_rim_back','hoodie_waist_back'):
        dark=float(np.mean(np.mean(values,axis=1)<.50))
        record['dark_patch_fraction']=round(dark,5)
        if dark>.015:failures.append('Knight '+name+': black material cuts across the white hoodie')
    if source_errors:
        changed=float(np.mean(np.asarray(source_errors)>.10))
        record['source_color_changed_fraction']=round(changed,5)
        if changed>.02:failures.append(kind+' '+side+' '+name+': recoloring changes a protected surface')
    if name=='free_hand':
        lost=float(np.mean(np.mean(values,axis=1)<.35))
        record['skin_recolored_as_clothing_fraction']=round(lost,5)
        if lost>.015:failures.append('Pawn: free hand was recolored as cargo fabric')
    if name=='seated_forearm':
        lost=float(np.mean(values[:,0]-values[:,2]<.05))
        record['skin_recolored_as_clothing_fraction']=round(lost,5)
        if lost>.015:failures.append('Rook '+side+': recoloring reaches the exposed forearm')
report={'checks':records,'failures':failures,'passed':not failures}
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if '--report' in args:Path(args[args.index('--report')+1]).write_text(json.dumps(report,indent=2)+'\n')
print('FABRIC_AUDIT '+json.dumps(report),flush=True)
if failures:raise RuntimeError('; '.join(failures))
