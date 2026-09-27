import bpy
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
# 作業ファイルは直前に保存済み。書き出し元としてv2（承認済み筐体＋ケーブル）を開く。こちらは保存しない
assert not bpy.data.is_dirty or True
bpy.ops.wm.open_mainfile(filepath=D+'/Claude_HeadPart01_FlatCable_v2.blend');print(bpy.data.filepath)
