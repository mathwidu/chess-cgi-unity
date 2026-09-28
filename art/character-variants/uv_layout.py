"""Check real UV triangle intersections, including coplanar/self intersections.

The grid is a broad phase only; polygon clipping rejects touching edges.
Blender BVH self-overlap is not a substitute for this 2D check.
"""
import math
def area(p):
 return abs(sum(p[i][0]*p[(i+1)%len(p)][1]-p[(i+1)%len(p)][0]*p[i][1] for i in range(len(p))))/2 if len(p)>2 else 0

def overlap(subject,clip):
 cross=lambda a,b,c:(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
 if cross(*clip)<0:clip=clip[::-1]
 for a,b in zip(clip,clip[1:]+clip[:1]):
  incoming=subject;subject=[]
  if not incoming:return 0
  prev=incoming[-1];dp=cross(a,b,prev)
  for current in incoming:
   dc=cross(a,b,current)
   if (dc>=0)!=(dp>=0):
    t=dp/(dp-dc);subject.append((prev[0]+t*(current[0]-prev[0]),prev[1]+t*(current[1]-prev[1])))
   if dc>=0:subject.append(current)
   prev=current;dp=dc
 return area(subject)

def find_overlaps(mesh,minimum_area=1e-8):
    mesh.calc_loop_triangles();triangles=list(mesh.loop_triangles)
    uv=mesh.uv_layers.active
    polygons=[[tuple(uv.data[i].uv) for i in t.loops] for t in triangles]
    bins={};pairs=set()
    for i,polygon in enumerate(polygons):
        lo=[math.floor(min(p[axis] for p in polygon)*64) for axis in range(2)]
        hi=[math.floor(max(p[axis] for p in polygon)*64) for axis in range(2)]
        for x in range(lo[0],hi[0]+1):
            for y in range(lo[1],hi[1]+1):
                cell=bins.setdefault((x,y),[])
                for j in cell:pairs.add((j,i))
                cell.append(i)
    result=[]
    for i,j in pairs:
        common_area=overlap(polygons[i],polygons[j])
        if common_area>minimum_area:
            result.append((common_area,triangles[i].polygon_index,triangles[j].polygon_index))
    return result
