import bpy,bmesh,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s;s.frame_set(1);records=json.loads((P/'Body_Deletion_Record.json').read_text());audit=json.loads((P/'Boundary_Audit.json').read_text());result={};restored={}
for name,r in records.items():
 o=bpy.data.objects[name];kv={i:tuple(round(c,5) for c in p) for i,p in enumerate(r['vertices'])};ef={};deleted=set(r['deleted_faces']);origmap={a.value:i for i,a in enumerate(o.data.attributes['DLHN_OriginalBodyVertex'].data)};coordmap={kv[old]:i for old,i in origmap.items()}
 for fi,f in enumerate(r['faces']):
  for a,b in zip(f,f[1:]+f[:1]):ef.setdefault(tuple(sorted((kv[a],kv[b]))),set()).add(fi)
 edges=[(key,fs) for key,fs in ef.items() if fs&deleted and fs-deleted and all(a in coordmap for a in key)];bad={i for pose in audit['poses'].values() for i in pose[name]['unoccluded_cut_edges']};restore={fi for i in bad for fi in edges[i][1] if fi in deleted};adj=[set() for _ in r['faces']]
 for fs in ef.values():
  for fi in fs:adj[fi].update(fs-{fi})
 for _ in range(2):restore|={j for fi in list(restore) for j in adj[fi] if j in deleted}
 deleted-=restore;restored[name]=len(restore);src=bpy.data.objects['Backup__'+o['master_source']];o.data=src.data.copy();m=o.data;va=m.attributes.get('DLHN_OriginalBodyVertex') or m.attributes.new('DLHN_OriginalBodyVertex','INT','POINT');fa=m.attributes.get('DLHN_OriginalBodyFace') or m.attributes.new('DLHN_OriginalBodyFace','INT','FACE')
 for v in m.vertices:va.data[v.index].value=v.index
 for f in m.polygons:fa.data[f.index].value=f.index
 before=sum(len(f.vertices)-2 for f in m.polygons);removed=sum(len(m.polygons[i].vertices)-2 for i in deleted);bm=bmesh.new();bm.from_mesh(m);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in deleted],context='FACES');bm.to_mesh(m);bm.free();m.update();r['deleted_faces']=sorted(deleted);r['remaining_faces']=[i for i in range(len(r['faces'])) if i not in deleted];result[name]={'before_triangles':before,'deleted_triangles':removed,'after_triangles':before-removed,'deleted_faces':len(deleted)}
(P/'Body_Deletion_Record.json').write_text(json.dumps(records));(P/'Body_Deletion_Counts.json').write_text(json.dumps(result,indent=2));print(restored)
