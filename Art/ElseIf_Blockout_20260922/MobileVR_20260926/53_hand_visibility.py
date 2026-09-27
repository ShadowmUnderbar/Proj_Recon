import bpy,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926')
for fn in ast.parse((P/'08_stress.py').read_text(encoding='utf-8-sig')).body:
 if isinstance(fn,ast.FunctionDef) and fn.name in ['snap','union']:exec(compile(ast.Module(body=[fn],type_ignores=[]),'<helpers>','exec'))
report={}
for prefix,sn,cn in [('LOD0__','ElseIf_LOD0_Review','ElseIf_Game_Optimized'),('Mobile__','ElseIf_MobileVR_Review','ElseIf_Game_MobileVR_Test')]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;rr={}
 for label,frame in json.loads((P/'Pose_Frames.json').read_text()).items():
  s.frame_set(frame);bpy.context.view_layer.update();items={o.name:snap(o) for o in bpy.data.collections[cn].objects if o.type=='MESH'};tree=union(items.values());body=items[prefix+'ElseIf_Mannequin'];ids=[i for i,gs in enumerate(body['g']) if any(any(t in n for t in ['Hand','Thumb','Index','Middle','Ring','Little']) and w>.1 for n,w in gs)];row={}
  for camname in ['Game_Cam_Stress_HighAngle','ElseIf_Cam_BackQuarter']:
   cam=bpy.data.objects[camname];forward=cam.matrix_world.to_quaternion()@Vector((0,0,-1));seen=[]
   for i in ids:
    p=body['v'][i];origin=p-forward*5 if cam.data.type=='ORTHO' else cam.matrix_world.translation;d=(p-origin).normalized();hit=tree.ray_cast(origin,d,20)
    if hit[0] is not None and (hit[0]-p).length<.0002:seen.append(i)
   row[camname]={'tested_hand_vertices':len(ids),'visible_samples_0_2mm':len(seen)}
  rr[label]=row
 s.frame_set(1);report[prefix]=rr
(P/'Hand_Camera_Visibility.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review']
