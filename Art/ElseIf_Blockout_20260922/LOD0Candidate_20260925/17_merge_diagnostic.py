import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');r={}
for cn in ['ElseIf_Game_Backup','ElseIf_Game_Optimized']:
 rows=[]
 for o in bpy.data.collections[cn].objects:
  if o.type!='MESH':continue
  rows.append({'name':o.name,'mods':[{k:getattr(m,k) for k in ['use_deform_preserve_volume','use_vertex_groups','use_bone_envelopes','vertex_group','invert_vertex_group','use_multi_modifier']} for m in o.modifiers if m.type=='ARMATURE'],'keys':[{k:getattr(x,k) for k in ['name','vertex_group','mute','interpolation']} for x in o.data.shape_keys.key_blocks] if o.data.shape_keys else []})
 r[cn]=rows
(P/'Merge_Diagnostic.json').write_text(json.dumps(r,indent=2));print(json.dumps({cn:{str(row['mods']):[a['name'] for a in rows if a['mods']==row['mods']] for row in rows} for cn,rows in r.items()}))
