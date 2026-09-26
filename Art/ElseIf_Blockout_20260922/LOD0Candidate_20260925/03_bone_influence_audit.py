import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');r=json.loads((P/'Source_Costs.json').read_text())
for cn in r:
 c=bpy.data.collections[cn];bone_names={b.name for o in c.objects if o.type=='ARMATURE' for b in o.data.bones};mx=0;nonbone=set()
 for o in c.objects:
  if o.type!='MESH':continue
  for v in o.data.vertices:
   active=[o.vertex_groups[g.group].name for g in v.groups if g.weight>1e-6];mx=max(mx,sum(n in bone_names for n in active));nonbone.update(n for n in active if n not in bone_names)
 r[cn]['max_bone_influence']=mx;r[cn]['non_bone_vertex_groups']=sorted(nonbone)
(P/'Source_Costs.json').write_text(json.dumps(r,indent=2));print(json.dumps({n:{k:v for k,v in x.items() if k not in ['mesh_breakdown']} for n,x in r.items()}))
