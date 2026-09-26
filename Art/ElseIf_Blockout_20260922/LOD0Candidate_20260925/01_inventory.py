import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');r={'file':bpy.data.filepath,'collections':{},'scenes':list(bpy.data.scenes.keys())}
for cn in ['ElseIf_Game_Current','ElseIf_Game_Optimized_Test','ElseIf_Game_Optimized_Corrected','ElseIf_Game_LocalRetopo','ElseIf_Master_Quality']:
 c=bpy.data.collections.get(cn)
 if not c:continue
 meshes=[o for o in c.all_objects if o.type=='MESH'];r['collections'][cn]={'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in meshes),'skinned':sum(any(m.type=='ARMATURE' for m in o.modifiers) for o in meshes),'armatures':[o.name for o in c.all_objects if o.type=='ARMATURE'],'shapes':{o.name:[k.name for k in o.data.shape_keys.key_blocks] for o in meshes if o.data.shape_keys and 'Sleeve_L' in o.name},'body':{o.name:sum(len(f.vertices)-2 for f in o.data.polygons) for o in meshes if any(x in o.name for x in ['Mannequin','Anatomy_Restoration'])}}
(P/'Initial_Inventory.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
