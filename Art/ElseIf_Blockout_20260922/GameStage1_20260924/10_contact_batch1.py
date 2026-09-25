from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');exec((p/'contact_utils.py').read_text(encoding='utf-8-sig'))
bpy.context.window.scene=scene
for label in ['A_Natural','B_Side90','C_Forward']:solve(label)
scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'))
