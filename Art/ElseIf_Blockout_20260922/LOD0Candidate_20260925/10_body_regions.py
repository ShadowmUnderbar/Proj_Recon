import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');o=bpy.data.objects['Backup__ElseIf_Mannequin'];r={}
for v in o.data.vertices:
 w=[(o.vertex_groups[g.group].name,g.weight) for g in v.groups if o.vertex_groups[g.group].name.startswith('J_Bip')];n=max(w,key=lambda a:a[1])[0] if w else 'none';entry=r.setdefault(n,{'vertices':0,'z':[10,-10],'x':[10,-10]});entry['vertices']+=1;entry['z']=[min(entry['z'][0],v.co.z),max(entry['z'][1],v.co.z)];entry['x']=[min(entry['x'][0],abs(v.co.x)),max(entry['x'][1],abs(v.co.x))]
print(json.dumps({n:v for n,v in r.items() if any(x in n for x in ['Arm','Chest','Foot','Leg','Spine'])}));(P/'Body_Bone_Regions.json').write_text(json.dumps(r,indent=2))
