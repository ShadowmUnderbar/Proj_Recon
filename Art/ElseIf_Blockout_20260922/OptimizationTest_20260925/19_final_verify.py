import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925')
s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;s.frame_set(1)
print({'file':bpy.data.filepath,'scene':s.name,'frame':s.frame_current,'current_collection':len(bpy.data.collections['ElseIf_Game_Current'].objects),'optimized_collection':len(bpy.data.collections['ElseIf_Game_Optimized_Test'].objects),'render_files':len(json.loads((P/'Render_Manifest.json').read_text()))})
assert all((P/r['file']).is_file() for r in json.loads((P/'Render_Manifest.json').read_text()))
assert (P/'ElseIf_Game_Current_Backup.blend').is_file()
print('Deliverables verified; no additional edits.')
