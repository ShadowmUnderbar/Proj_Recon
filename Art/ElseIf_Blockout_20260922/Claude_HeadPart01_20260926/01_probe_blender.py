import bpy,json
print(json.dumps({'filepath':bpy.data.filepath,'is_dirty':bpy.data.is_dirty,'version':bpy.app.version_string,'scene':bpy.context.scene.name}))
