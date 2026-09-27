import bpy,bmesh,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];src=bpy.data.objects['LOD0__ElseIf_Anatomy_Restoration'];assert [g.name for g in o.vertex_groups]==[g.name for g in src.vertex_groups]
def key(points):return tuple(sorted(tuple(round(c,7) for c in p) for p in points))
deleted=set()
for file in ['Body_Deletion_Record.json','Body_Overlap_Deletion_Record.json']:
 r=json.loads((P/file).read_text())['Mobile__ElseIf_Anatomy_Restoration']
 for i in r['deleted_faces']:deleted.add(key(r['vertices'][j] for j in r['faces'][i]))
o.data=src.data.copy();m=o.data;selected=[f.index for f in m.polygons if key(m.vertices[i].co for i in f.vertices) in deleted];bm=bmesh.new();bm.from_mesh(m);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in selected],context='FACES');bm.to_mesh(m);bm.free();m.update();r={'source_triangles':sum(len(f.vertices)-2 for f in src.data.polygons),'retained_triangles':sum(len(f.vertices)-2 for f in m.polygons),'deleted_faces':len(selected),'decision':'Retain original topology in uncertain neck/upper-chest restoration; apply verified hidden-face deletion only'};(P/'Body_Final_Conservative.json').write_text(json.dumps(r,indent=2));print(r)
