import bpy,json
print(json.dumps({'scenes':[(s.name,len(s.objects)) for s in bpy.data.scenes],'collections':[(c.name,len(c.objects)) for c in bpy.data.collections if 'Game' in c.name],'rigs':[o.name for o in bpy.data.objects if o.type=='ARMATURE'],'objects':[o.name for o in bpy.data.collections['ElseIf_Game'].objects][:5]}))
