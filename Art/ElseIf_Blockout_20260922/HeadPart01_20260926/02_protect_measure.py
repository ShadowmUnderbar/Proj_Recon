import bpy,json,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);o=bpy.data.objects['Mobile__Jacket'];m=o.data;cm={int(k):v for k,v in json.loads((P.parent/'LOD0Candidate_20260925/Component_Map.json').read_text()).items()};a=m.attributes['DLHN_Component'];rows=[]
for cid,v in cm.items():
 if 'Collar' not in v['source']:continue
 vs=[o.matrix_world@x.co for x in m.vertices if a.data[x.index].value==cid]
 if vs:rows.append({'component':v['source'],'vertices':len(vs),'min':[min(p[i] for p in vs) for i in range(3)],'max':[max(p[i] for p in vs) for i in range(3)]})
(P/'Collar_Measurement.json').write_text(json.dumps(rows,indent=2));print(rows)
src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for ss in list(bpy.data.scenes):bpy.context.window.scene=ss;bpy.context.view_layer.update()
protected={o.name:fp(o) for o in bpy.data.objects};(P/'Protected_Before.json').write_text(json.dumps(protected));bpy.context.window.scene=s
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Body_Before_Head.blend'),copy=True);print('Existing model protected and backup saved')
