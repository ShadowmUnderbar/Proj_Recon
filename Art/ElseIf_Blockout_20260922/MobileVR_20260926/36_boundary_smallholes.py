import bpy,bmesh,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];a=json.loads((P/'Boundary_Visibility_Final.json').read_text());ids={i for r in a.values() for e in r['ElseIf_Anatomy_Restoration']['samples'] for i in e['edge']};points=[o.data.vertices[i].co.copy() for i in ids];bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);edges={e for e in bm.edges if e.is_boundary};done=set();records=[]
for start in list(edges):
 if start in done:continue
 stack=[start];component=set()
 while stack:
  e=stack.pop()
  if e in component:continue
  component.add(e)
  for v in e.verts:stack.extend(x for x in v.link_edges if x in edges and x not in component)
 done.update(component);verts={v for e in component for v in e.verts}
 if len(verts)>40 or not all(sum(e in component for e in v.link_edges)==2 for v in verts):continue
 if not any((v.co-p).length<.00001 for v in verts for p in points):continue
 if max((v.co-w.co).length for v in verts for w in verts)>.045:continue
 result=bmesh.ops.holes_fill(bm,edges=list(component),sides=40);records.append({'boundary_vertices':len(verts),'faces_restored':len(result['faces'])})
bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4]);bm.to_mesh(o.data);bm.free();o.data.update();(P/'Boundary_SmallHole_Restore.json').write_text(json.dumps(records,indent=2));print(records)
