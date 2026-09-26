import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];src=bpy.data.objects['LOD0__ElseIf_Anatomy_Restoration'];audit=json.loads((P/'Boundary_Visibility_Final.json').read_text());ids={i for pose in audit.values() for entry in pose['ElseIf_Anatomy_Restoration']['samples'] for i in entry['edge']};points=[o.data.vertices[i].co.copy() for i in ids];faces=[f for f in src.data.polygons if any((src.data.vertices[i].co-p).length<.012 for i in f.vertices for p in points)];bm=bmesh.new();bm.from_mesh(o.data);layer=bm.verts.layers.deform.verify();coordmap={tuple(round(c,6) for c in v.co):v for v in bm.verts};existing={tuple(sorted(tuple(round(c,6) for c in v.co) for v in f.verts)) for f in bm.faces};added=0
for f in faces:
 key=tuple(sorted(tuple(round(c,6) for c in src.data.vertices[i].co) for i in f.vertices))
 if key in existing:continue
 vs=[]
 for i in f.vertices:
  v=src.data.vertices[i];k=tuple(round(c,6) for c in v.co)
  if k not in coordmap:
   n=bm.verts.new(v.co);coordmap[k]=n
   for g in v.groups:
    name=src.vertex_groups[g.group].name;vg=o.vertex_groups.get(name) or o.vertex_groups.new(name=name);n[layer][vg.index]=g.weight
  vs.append(coordmap[k])
 try:nf=bm.faces.new(vs);nf.smooth=f.use_smooth;nf.material_index=f.material_index;added+=len(vs)-2
 except ValueError:pass
bm.to_mesh(o.data);bm.free();o.data.update();(P/'Boundary_Restore.json').write_text(json.dumps({'boundary_vertices':sorted(ids),'restored_triangles':added,'reason':'Two exposed boundary candidates at B_Side90; preserve original body patch'},indent=2));print({'restored_triangles':added})
