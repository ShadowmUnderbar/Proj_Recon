import bpy,bmesh,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];src=bpy.data.objects['LOD0__ElseIf_Anatomy_Restoration'];m=o.data
keyv=lambda p:tuple(round(c,7) for c in p)
key=lambda ps:tuple(sorted(keyv(p) for p in ps))
report=json.loads((P/'Boundary_Visibility_Final.json').read_text());bad={i for r in report.values() for s in r['ElseIf_Anatomy_Restoration']['samples'] for i in s['edge']};coords={keyv(m.vertices[i].co) for i in bad};sm=src.data;protect={v.index for v in sm.vertices if keyv(v.co) in coords}
for _ in range(4):
 protect|={i for f in sm.polygons if any(i in protect for i in f.vertices) for i in f.vertices}
deleted=set()
for file in ['Body_Deletion_Record.json','Body_Overlap_Deletion_Record.json']:
 r=json.loads((P/file).read_text())['Mobile__ElseIf_Anatomy_Restoration']
 for i in r['deleted_faces']:deleted.add(key(r['vertices'][j] for j in r['faces'][i]))
o.data=sm.copy();m=o.data;selected=[f.index for f in m.polygons if key(m.vertices[i].co for i in f.vertices) in deleted and not any(i in protect for i in f.vertices)];bm=bmesh.new();bm.from_mesh(m);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in selected],context='FACES');bm.to_mesh(m);bm.free();m.update();r={'source_triangles':sum(len(f.vertices)-2 for f in sm.polygons),'retained_triangles':sum(len(f.vertices)-2 for f in m.polygons),'deleted_faces':len(selected),'boundary_margin_rings':4};(P/'Body_Final_Conservative.json').write_text(json.dumps(r,indent=2));print(r)
