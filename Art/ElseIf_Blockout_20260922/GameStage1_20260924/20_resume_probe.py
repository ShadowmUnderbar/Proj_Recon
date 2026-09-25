import bpy,json
print(json.dumps({'file':bpy.data.filepath,'scene':bpy.context.scene.name,'game_exists':'ElseIf_Game' in bpy.data.collections,'game_meshes':len(bpy.data.collections['ElseIf_Game'].objects) if 'ElseIf_Game' in bpy.data.collections else 0}))
