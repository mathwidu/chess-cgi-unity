"""Verify that production UV charts do not paint one surface over another."""
from pathlib import Path
import bpy,json,sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from uv_layout import find_overlaps
checks=[];failures=[]
for body in sorted((o for o in bpy.data.objects if o.type=='MESH' and o.name.endswith('_Body')),key=lambda o:o.name):
    pairs=find_overlaps(body.data)
    kind=body.name.removesuffix('_Body')
    checks.append({'kind':kind,'overlapping_triangle_pairs':len(pairs),'overlap_area':sum(area for area,_,_ in pairs),'source_uv_preserved':'SourceUV' in body.data.uv_layers})
    if pairs:failures.append(kind+': overlapping production UV triangles')
    if 'SourceUV' not in body.data.uv_layers:failures.append(kind+': source UV missing')
report={'checks':checks,'failures':failures,'passed':not failures}
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if '--report' in args:Path(args[args.index('--report')+1]).write_text(json.dumps(report,indent=2)+'\n')
print('UV_LAYOUT_AUDIT '+json.dumps(report),flush=True)
if failures:raise RuntimeError('; '.join(failures))
