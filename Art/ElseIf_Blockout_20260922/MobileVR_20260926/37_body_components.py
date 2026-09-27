import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');m=bpy.data.objects['LOD0__ElseIf_Anatomy_Restoration'].data;adj=[[] for v in m.vertices]
for e in m.edges:a,b=e.vertices;adj[a].append(b);adj[b].append(a)
seen=set();parts=[]
for v in m.vertices:
 if v.index in seen:continue
 stack=[v.index];ids=[]
 while stack:
  i=stack.pop()
  if i in seen:continue
  seen.add(i);ids.append(i);stack.extend(j for j in adj[i] if j not in seen)
 parts.append(ids)
r=[]
for i,ids in enumerate(parts):
 pts=[m.vertices[j].co for j in ids];r.append({'id':i,'vertices':len(ids),'bounds':[[min(p[k] for p in pts),max(p[k] for p in pts)] for k in range(3)]})
(P/'Body_Components.json').write_text(json.dumps({'components':r,'vertex_ids':parts},indent=2));print(r)
