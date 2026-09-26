import bpy,json,ast
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');src=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(src).body:
 if isinstance(fn,ast.FunctionDef) and fn.name=='snap':exec(compile(ast.Module(body=[fn],type_ignores=[]),'<snap>','exec'))
from mathutils.bvhtree import BVHTree
s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;r={};cm=json.loads((P.parent/'LOD0Candidate_20260925/Component_Map.json').read_text())
for label,frame in json.loads((P/'Pose_Frames.json').read_text()).items():
 s.frame_set(frame);bpy.context.view_layer.update();j=snap(bpy.data.objects['Mobile__Jacket']);row=[]
 for bn in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
  b=snap(bpy.data.objects['Mobile__'+bn]);pairs=j['tree'].overlap(b['tree']);cs={}
  for a,bi in pairs:
   cid=j['c'][a];name=cm[str(cid)]['source'];p=sum((j['v'][i] for i in j['f'][a]),Vector())/3;cs.setdefault(name,[]).append(list(p))
  row.append({'body':bn,'parts':{n:{'count':len(ps),'bounds':[[min(p[i] for p in ps),max(p[i] for p in ps)] for i in range(3)]} for n,ps in cs.items()}})
 r[label]=row
s.frame_set(1);(P/'Collision_Diagnostic.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
