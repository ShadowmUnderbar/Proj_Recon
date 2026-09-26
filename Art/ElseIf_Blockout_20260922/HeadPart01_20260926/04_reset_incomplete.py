import bpy
bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review']
s=bpy.data.scenes.get('ElseIf_Head01_Assembly_Review')
if s:bpy.data.scenes.remove(s)
c=bpy.data.collections.get('ElseIf_HeadPart_Monitor01')
if c:
 for o in list(c.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(c)
m=bpy.data.materials.get('ElseIf_HeadShell')
if m and m.users==0:bpy.data.materials.remove(m)
print('Incomplete new head setup removed; body unchanged')
