import bpy,json
from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925')
r={'file':bpy.data.filepath,'scene':bpy.context.scene.name,'scenes':[s.name for s in bpy.data.scenes],'collections':[c.name for c in bpy.data.collections],'game':[]}
for o in bpy.data.collections.get('ElseIf_Game').objects:
 r['game'].append({'name':o.name,'type':o.type,'verts':len(o.data.vertices) if o.type=='MESH' else 0,'keys':len(o.data.shape_keys.key_blocks) if o.type=='MESH' and o.data.shape_keys else 0})
(p/'inventory.json').write_text(json.dumps(r,indent=2));print(json.dumps({k:v for k,v in r.items() if k!='game'}))
