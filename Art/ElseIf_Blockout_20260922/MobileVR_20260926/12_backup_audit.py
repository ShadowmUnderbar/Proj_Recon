import bpy,ast,json,hashlib,struct
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig')
fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for s in bpy.data.scenes:
 bpy.context.window.scene=s;bpy.context.view_layer.update()
(P/'Protected_Backup_Evaluated.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects}))
print('Read-only backup audit complete')
