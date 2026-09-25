import bpy,json,ast,hashlib,struct,shutil
from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924')
for s in bpy.data.scenes:
 bpy.context.window.scene=s;bpy.context.view_layer.update()
bpy.context.window.scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.view_layer.update()
src=(p/'28_audit_diff.py').read_text(encoding='utf-8-sig');exec(src)
changes=[n for n in before if n not in bpy.data.objects or fp(bpy.data.objects[n])!=before[n]]
stats=json.loads((p/'Stage1_Statistics.json').read_text(encoding='utf-8'));stats['master_object_changes']=changes;(p/'Stage1_Statistics.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(p/'ElseIf_Game_Stage1.blend'))
shutil.copy2(p/'ElseIf_Game_Stage1.blend',p/'ElseIf_Game_Verified_Checkpoint.blend')
with bpy.data.libraries.load(str(p/'ElseIf_Game_Verified_Checkpoint.blend'),link=False) as (fr,to):print('Saved scenes',fr.scenes)
