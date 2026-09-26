import bpy,json,ast,hashlib,struct
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');OLD=P.parent/'LOD0Candidate_20260925'
code=(OLD/'04_prepare.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(code).body if isinstance(n,ast.FunctionDef) and n.name=='clone');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<clone>','exec'))
code=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(code).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
(P/'Protected_Before.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects}))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimized_Backup.blend'),copy=True)
col,s,rig=clone(bpy.data.collections['ElseIf_Game_Optimized'],bpy.data.scenes['ElseIf_LOD0_Review'],'ElseIf_Game_MobileVR_Test','ElseIf_MobileVR_Review','Mobile__','ElseIf_MobileVR_Humanoid')
# master_sourceは統合前の代表名なので、統合後の8メッシュ名を対応させる。
for old,new in zip(bpy.data.collections['ElseIf_Game_Optimized'].objects,col.objects):
 if old.type=='MESH':new.name=old.name.replace('LOD0__','Mobile__');new['mobile_source']=old.name
(P/'Pose_Frames.json').write_text((OLD/'Pose_Frames.json').read_text())
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print('Protected backup saved; MobileVR collection created.')
