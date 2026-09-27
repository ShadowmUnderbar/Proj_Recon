import bpy
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
# FlatCable v2をデザイン検証として別ファイルに保持（開いているファイルは変えない）
bpy.ops.wm.save_as_mainfile(filepath=D+'/Claude_HeadPart01_FlatCable_v2.blend',copy=True,compress=False);print('saved v2 copy')
