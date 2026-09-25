import bpy
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
from mathutils import Vector
s=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=s;s.frame_set(1);src=bpy.data.objects['Game__ElseIf_Mannequin'];dst=bpy.data.objects['Game__ElseIf_Anatomy_Restoration'];m=src.data;m.calc_loop_triangles();vs=[v.co.copy() for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];tree=BVHTree.FromPolygons(vs,fs,all_triangles=True);dst.vertex_groups.clear();groups={}
for v in dst.data.vertices:
 hit=tree.find_nearest(v.co);a,b,c=fs[hit[2]];bc=barycentric_transform(hit[0],vs[a],vs[b],vs[c],Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)));weights={}
 for idx,factor in zip([a,b,c],bc):
  for g in m.vertices[idx].groups:
   name=src.vertex_groups[g.group].name
   if name in bpy.data.objects['ElseIf_Game_Humanoid'].data.bones:weights[name]=weights.get(name,0)+g.weight*max(0,factor)
 items=sorted(weights.items(),key=lambda a:-a[1])[:4];total=sum(w for n,w in items)
 for n,w in items:
  if n not in groups:groups[n]=dst.vertex_groups.new(name=n)
  groups[n].add([v.index],w/total,'REPLACE')
print('Internal restored anatomy receives interpolated body weights, independent of garment weights.')
