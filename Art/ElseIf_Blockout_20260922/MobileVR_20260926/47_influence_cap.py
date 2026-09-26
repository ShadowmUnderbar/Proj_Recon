import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];bones={b.name for o in col.objects if o.type=='ARMATURE' for b in o.data.bones};report={}
for o in col.objects:
 if o.type!='MESH':continue
 changed=0;maxdiscard=0
 for v in o.data.vertices:
  gs=sorted([(g.group,g.weight) for g in v.groups if o.vertex_groups[g.group].name in bones and g.weight>1e-8],key=lambda x:-x[1])
  if len(gs)<=4:continue
  changed+=1;top=gs[:4];total=sum(w for _,w in top);maxdiscard=max(maxdiscard,sum(w for _,w in gs[4:]))
  for i,w in gs:o.vertex_groups[i].remove([v.index])
  for i,w in top:o.vertex_groups[i].add([v.index],w/total,'REPLACE')
 report[o.name]={'vertices_capped':changed,'max_discarded_weight':maxdiscard}
(P/'Influence_Cap_Final.json').write_text(json.dumps(report,indent=2));print(report)
