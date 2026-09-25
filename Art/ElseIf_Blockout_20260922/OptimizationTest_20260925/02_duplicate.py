import bpy,json,shutil
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');base=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=base;base.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_Current_Backup.blend'),copy=True)
source=bpy.data.collections['ElseIf_Game'];cur=bpy.data.collections.new('ElseIf_Game_Current')
for o in source.objects:cur.objects.link(o)
cur.use_fake_user=True
opt=bpy.data.collections.new('ElseIf_Game_Optimized_Test');opt.use_fake_user=True;mapping={}
for o in source.objects:
 n=o.copy();n.name='Opt__'+o.name.removeprefix('Game__');opt.objects.link(n)
 if o.data:n.data=o.data.copy()
 if n.animation_data and n.animation_data.action:n.animation_data.action=n.animation_data.action.copy()
 mapping[o]=n
for o,n in mapping.items():
 if o.parent in mapping:n.parent=mapping[o.parent]
 for m in n.modifiers:
  if m.type=='ARMATURE' and m.object in mapping:m.object=mapping[m.object]
 for con in list(n.constraints)+([c for pb in n.pose.bones for c in pb.constraints] if n.type=='ARMATURE' else []):
  if hasattr(con,'target') and con.target in mapping:con.target=mapping[con.target]
 for data in [n,n.data.shape_keys if n.type=='MESH' else None]:
  if data and data.animation_data:
   for fc in data.animation_data.drivers:
    for v in fc.driver.variables:
     for t in v.targets:
      if t.id in mapping:t.id=mapping[t.id]
for name,collection in [('ElseIf_Current_Review',cur),('ElseIf_Optimized_Review',opt)]:
 s=base.copy();s.name=name;s.use_fake_user=True
 for c in list(s.collection.children):s.collection.children.unlink(c)
 for o in list(s.collection.objects):s.collection.objects.unlink(o)
 s.collection.children.link(bpy.data.collections['ElseIf_Review_Stage']);s.collection.children.link(collection)
 for cn in ['Game_Cam_Stress_HighAngle','Game_Cam_Stress_ThreeQuarter']:
  if cn in bpy.data.objects:s.collection.objects.link(bpy.data.objects[cn])
 s.frame_set(1)
optRig=mapping[bpy.data.objects['ElseIf_Game_Humanoid']];optRig.name='ElseIf_Optimized_Humanoid'
bpy.context.window.scene=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.view_layer.update()
(P/'Mapping.json').write_text(json.dumps({o.name:n.name for o,n in mapping.items()},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimization_Test.blend'))
print('Independent optimized collection created; current and master unchanged.')
