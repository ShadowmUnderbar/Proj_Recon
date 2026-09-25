import bpy,bmesh,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925');rep={}
print('Active',bpy.data.filepath,bpy.context.scene.name)
for name in ['ElseIf_Jacket_Sleeve_L','Opt__ElseIf_Jacket_Sleeve_L','Opt__ElseIf_Jacket_Front_L','Opt__ElseIf_Jacket_Back','Opt__ElseIf_Mannequin','Opt__ElseIf_Anatomy_Restoration']:
 o=bpy.data.objects[name];bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table();bm.edges.ensure_lookup_table();boundary=[list(v.co) for v in bm.verts if v.is_boundary];rep[name]={'verts':len(bm.verts),'faces':len(bm.faces),'boundary_count':len(boundary),'boundary_upper':sorted([v for v in boundary if v[2]>1.12],key=lambda v:v[2])[:15],'polygons_first':[list(p.vertices) for p in o.data.polygons[:5]],'bounds':[[min(v.co[i] for v in bm.verts),max(v.co[i] for v in bm.verts)] for i in range(3)],'mods':[(m.name,m.type) for m in o.modifiers]};bm.free()
(P/'Initial_Inspection.json').write_text(json.dumps(rep,indent=2));print(json.dumps(rep))
