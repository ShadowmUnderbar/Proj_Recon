import bpy,json
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
orig=bpy.data.filepath
# 未保存状態を失わないよう、元ファイルには触れずClaudeフォルダへ複製保存する
bpy.ops.wm.save_as_mainfile(filepath=D+'/Blender_UnsavedState_Snapshot.blend',copy=True,compress=False)
assert bpy.data.filepath==orig
bpy.ops.wm.open_mainfile(filepath=D+'/Claude_MobileVR_HeadPart01_Work.blend')
print(json.dumps({'snapshot_from':orig,'now_open':bpy.data.filepath,'scenes':[s.name for s in bpy.data.scenes]}))
