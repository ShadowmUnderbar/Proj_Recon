import bpy
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
# v2ファイルへの一時変更（Rootの親外し・検証シーン）は保存せずに破棄し、作業ファイルへ戻る
bpy.ops.wm.open_mainfile(filepath=D+'/Claude_HeadPart01_Work.blend');print(bpy.data.filepath)
