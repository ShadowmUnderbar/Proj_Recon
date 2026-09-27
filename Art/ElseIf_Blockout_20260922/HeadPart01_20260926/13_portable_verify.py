import bpy,json,ast,hashlib,struct
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926')
with bpy.data.libraries.load(str(P/'ElseIf_HeadPart_Monitor01.blend'),link=False) as (data,target):
 r={k:list(getattr(data,k)) for k in ['objects','meshes','materials','images','armatures','cameras','collections']}
assert len(r['meshes'])==2 and len(r['objects'])==3 and not r['armatures'] and not r['cameras'],r
code=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(code).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for sc in list(bpy.data.scenes):bpy.context.window.scene=sc;bpy.context.view_layer.update()
before=json.loads((P/'Protected_Before.json').read_text());changed=[n for n,v in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=v];assert not changed,changed;r['protected_existing_changes']=changed
bpy.context.window.scene=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.scene.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];bpy.context.scene.frame_set(1);bpy.data.materials['ElseIf_FaceMonitor'].node_tree.nodes['ExpressionTexture'].image=bpy.data.images['ElseIf_Expression_Test_Normal'];(P/'Portable_Asset_Verification.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
