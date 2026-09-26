import bpy,json,ast,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');cm=json.loads((P.parent/'LOD0Candidate_20260925/Component_Map.json').read_text());src=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(src).body:
 if isinstance(fn,ast.FunctionDef) and fn.name=='snap':exec(compile(ast.Module(body=[fn],type_ignores=[]),'<snap>','exec'))
r={}
for frame in [1,71]:
 bpy.context.window.scene=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();old={n:snap(bpy.data.objects['LOD0__'+n]) for n in ['Jacket','Socks_L','Socks_R']}
 bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();row=[]
 for n,b in old.items():
  ob=bpy.data.objects['Mobile__'+n];a=snap(ob)
  for cid in set(b['c']):
   tree=BVHTree.FromPolygons(b['v'],[f for f,c in zip(b['f'],b['c']) if c==cid],all_triangles=True);bad=[]
   for f,c in zip(a['f'],a['c']):
    if c!=cid:continue
    p=sum((a['v'][i] for i in f),Vector())/3;dist=tree.find_nearest(p)[3]*1000
    if dist>3:bad.append({'mm':dist,'vertices':list(f),'center':list(p)})
   if bad:row.append({'component':cm[str(cid)]['source'],'bad_count':len(bad),'worst':max(bad,key=lambda x:x['mm'])})
 r[frame]=row
r['groups']=[g.name for g in bpy.data.objects['Mobile__Jacket'].vertex_groups];(P/'Reduction_Diagnostic.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
for sn in ['ElseIf_LOD0_Review','ElseIf_MobileVR_Review']:bpy.data.scenes[sn].frame_set(1)
