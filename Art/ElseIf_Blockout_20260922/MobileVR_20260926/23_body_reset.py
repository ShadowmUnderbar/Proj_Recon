import bpy
col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];rig=bpy.data.objects['ElseIf_MobileVR_Humanoid']
for name in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
 old=bpy.data.objects['Mobile__'+name];bpy.data.objects.remove(old,do_unlink=True);src=bpy.data.objects['LOD0__'+name];n=src.copy();n.data=src.data.copy();n.name='Mobile__'+name;col.objects.link(n)
 if src.parent and src.parent.type=='ARMATURE':n.parent=rig
 for mod in n.modifiers:
  if mod.type=='ARMATURE':mod.object=rig
print('Body copied from protected Optimized; skeleton unchanged')
