"""Native model inspection, including surfaces hidden in the cast overview."""
import bpy,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
directory=Path(args[args.index('--out')+1]) if '--out' in args else Path(__file__).parent/'production/details'
directory.mkdir(parents=True,exist_ok=True)
kinds=args[args.index('--kinds')+1].split(',') if '--kinds' in args else ['Pawn','Rook','Knight','Bishop','Queen','King']
views=args[args.index('--views')+1].split(',') if '--views' in args else ['front','side','back']
scene=bpy.context.scene;camera=scene.camera
scene.render.resolution_x=1100;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
scene.cycles.samples=24
roots=[o for o in bpy.data.objects if o.type=='EMPTY' and o.name.endswith(('_White','_Black'))]
for root in roots:
    for ob in root.children:ob.hide_render=True
for kind in kinds:
    root=bpy.data.objects[kind+'_White']
    for ob in root.children:ob.hide_render=False
    for view,offset in [('front',(0,-5,1.4)),('side',(-4,-3,1.7)),('back',(0,5,1.5))]:
        if view not in views:continue
        target=root.location+Vector((0,0,.83));camera.location=target+Vector(offset)
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=1.5
        scene.render.filepath=str(directory/(kind+'-'+view+'.png'));bpy.ops.render.render(write_still=True)
    for ob in root.children:ob.hide_render=True
