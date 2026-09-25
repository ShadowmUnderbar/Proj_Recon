import bpy,json,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;c=bpy.data.collections['ElseIf_Game_Optimized_Test'];rig=bpy.data.objects['ElseIf_Optimized_Humanoid']
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();r=[v.co.copy() for v in m.vertices];e.to_mesh_clear();return r
obs=[o for o in c.objects if o.type=='MESH' and o.get('master_source','').startswith(('ElseIf_Jacket_','EL_'))];orig={o.name:[[(g.group,g.weight) for g in v.groups] for v in o.data.vertices] for o in obs};before={}
for f in [1,11,21,71]:
 s.frame_set(f);bpy.context.view_layer.update();before[f]={o.name:snap(o) for o in obs}
for o in obs:
 for v in o.data.vertices:
  x=abs(v.co.x);z=v.co.z
  mask=math.exp(-((x-.12)/.07)**2-((z-1.285)/.08)**2)*.12
  if mask<.001:continue
  side='L' if v.co.x>0 else 'R';group=o.vertex_groups.get('J_Bip_'+side+'_UpperArm');torso=o.vertex_groups.get('J_Bip_C_UpperChest')
  if not group:continue
  w=dict(orig[o.name][v.index]);a=w.get(group.index,0);take=a*mask
  if not torso:torso=o.vertex_groups.new(name='J_Bip_C_UpperChest')
  group.add([v.index],a-take,'REPLACE');torso.add([v.index],w.get(torso.index,0)+take,'REPLACE')
report={}
for f in [1,11,21,71]:
 s.frame_set(f);bpy.context.view_layer.update();ds=[]
 for o in obs:ds.extend((a-b).length for a,b in zip(snap(o),before[f][o.name]))
 report[str(f)]={'max_motion_mm':max(ds)*1000,'mean_motion_mm':sum(ds)/len(ds)*1000}
for o in obs:
 for g in o.vertex_groups:g.remove(list(range(len(o.data.vertices))))
 for i,gs in enumerate(orig[o.name]):
  for gi,w in gs:o.vertex_groups[gi].add([i],w,'REPLACE')
s.frame_set(1);(P/'Weight_Trial.json').write_text(json.dumps(report,indent=2));print(report)
