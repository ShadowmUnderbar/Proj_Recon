import bpy,json,math,numpy as np
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1)
o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];m=o.data;before=sum(len(f.vertices)-2 for f in m.polygons);vg=o.vertex_groups.new(name='_BodyReduction');protected=[]
edgecounts={}
for f in m.polygons:
 for a,b in f.edge_keys:edgecounts[tuple(sorted((a,b)))]=edgecounts.get(tuple(sorted((a,b))),0)+1
boundary={i for e,c in edgecounts.items() if c==1 for i in e}
for v in m.vertices:
 keep=v.index in boundary or (v.co.z>1.13 and abs(v.co.x)<.14) or v.co.z>1.32 or any(g.weight>.001 and any(x in o.vertex_groups[g.group].name for x in ['Hand','Thumb','Index','Middle','Ring','Little']) for g in v.groups)
 if keep:protected.append(list(v.co))
 vg.add([v.index],0 if keep else 1,'REPLACE')
mod=o.modifiers.new('HiddenBody_Regional','DECIMATE');mod.ratio=.28;mod.vertex_group=vg.name;mod.vertex_group_factor=100.0;mod.use_collapse_triangulate=True
# 骨変形の前で削減を確定する。
bpy.context.view_layer.objects.active=o
while o.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
bpy.ops.object.modifier_apply(modifier=mod.name)
if o.vertex_groups.get('_BodyReduction'):o.vertex_groups.remove(o.vertex_groups['_BodyReduction'])
for v in o.data.vertices:
 gs=sorted([(g.group,g.weight) for g in v.groups if g.weight>1e-8],key=lambda x:-x[1]);top=gs[:4];total=sum(w for _,w in top)
 for g,w in gs:o.vertex_groups[g].remove([v.index])
 for g,w in top:o.vertex_groups[g].add([v.index],w/total,'REPLACE')
after=sum(len(f.vertices)-2 for f in o.data.polygons);from mathutils.kdtree import KDTree
kd=KDTree(len(o.data.vertices))
for v in o.data.vertices:kd.insert(v.co,v.index)
kd.balance();mx=max((kd.find(p)[2] for p in protected),default=0)
r={'before_triangles':before,'after_triangles':after,'protected_points':len(protected),'max_protected_vertex_distance_mm':mx*1000};(P/'Body_Density_Reduction_Final.json').write_text(json.dumps(r,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(r)

