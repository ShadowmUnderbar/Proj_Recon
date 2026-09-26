import bpy,json,ast
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');code=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(code).body:
 if isinstance(fn,ast.FunctionDef) and fn.name=='snap':exec(compile(ast.Module(body=[fn],type_ignores=[]),'<snap>','exec'))
s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;rig=bpy.data.objects['ElseIf_MobileVR_Humanoid'];o=bpy.data.objects['Mobile__Jacket'];m=o.data;frames=json.loads((P/'Pose_Frames.json').read_text());logs=[]
for iteration in range(3):
 changes={};counts={}
 for label,frame in frames.items():
  s.frame_set(frame);bpy.context.view_layer.update();a=snap(o);count=0
  mats={b.name:(rig.matrix_world@rig.pose.bones[b.name].matrix@b.matrix_local.inverted()@rig.matrix_world.inverted()@o.matrix_world).to_3x3() for b in rig.data.bones}
  for bn in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
   b=snap(bpy.data.objects['Mobile__'+bn]);pairs=a['tree'].overlap(b['tree']);count+=len(pairs)
   for fi,bi in pairs:
    for vi in a['f'][fi]:
     p=a['v'][vi];hit=b['tree'].find_nearest(p)
     if not hit or hit[3]>.012:continue
     n=hit[1];signed=(p-hit[0]).dot(n);amount=min(.002,max(.0006,.0006-signed));worlddelta=n*amount;matrix=Matrix(((0,0,0),(0,0,0),(0,0,0)))
     for g in m.vertices[vi].groups:
      name=o.vertex_groups[g.group].name
      if name in mats:matrix+=mats[name]*g.weight
     delta=matrix.inverted_safe()@worlddelta
     if vi not in changes or delta.length>changes[vi].length:changes[vi]=delta
  counts[label]=count
 if not changes:logs.append({'iteration':iteration,'intersections':counts,'changed_vertices':0});break
 s.frame_set(1)
 for vi,d in changes.items():
  for k in m.shape_keys.key_blocks:k.data[vi].co+=d
 m.update();logs.append({'iteration':iteration,'intersections_before':counts,'changed_vertices':len(changes),'max_delta_mm':max(d.length for d in changes.values())*1000})
s.frame_set(1);(P/'Local_Clearance.json').write_text(json.dumps(logs,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(json.dumps(logs))
