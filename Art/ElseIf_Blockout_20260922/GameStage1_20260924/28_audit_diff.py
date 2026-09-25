import bpy,json,ast,hashlib,struct
from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924')
src=(p.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
before=json.loads((p/'Master_Audit_Before.json').read_text(encoding='utf-8'))
print([(n,before[n],fp(bpy.data.objects[n])) for n in before if n in bpy.data.objects and fp(bpy.data.objects[n])!=before[n]])

