import bpy
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_Seam_Protection.blend'),copy=True)
col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];rig=bpy.data.objects['ElseIf_MobileVR_Humanoid']
for src in bpy.data.collections['ElseIf_Game_Optimized'].objects:
 if src.name!='LOD0__Jacket':continue
 name=src.name.replace('LOD0__','Mobile__');old=bpy.data.objects[name];bpy.data.objects.remove(old,do_unlink=True);n=src.copy();n.data=src.data.copy();n.name=name;col.objects.link(n);n.parent=rig if src.parent and src.parent.type=='ARMATURE' else src.parent
 for mod in n.modifiers:
  if mod.type=='ARMATURE':mod.object=rig
 if n.data.shape_keys and n.data.shape_keys.animation_data:
  for fc in n.data.shape_keys.animation_data.drivers:
   for v in fc.driver.variables:
    for t in v.targets:
     if t.id and t.id.type=='ARMATURE':t.id=rig
print('Mobile clothing reset from protected Optimized source; body work retained')

