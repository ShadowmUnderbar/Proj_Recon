from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');exec((p/'contact_utils.py').read_text(encoding='utf-8-sig'));bpy.context.window.scene=scene;scene.frame_set(1)
for o in col.objects:
 if o.type=='MESH' and o.data.shape_keys:
  for k in list(o.data.shape_keys.key_blocks):
   if k.name.startswith('DLHN_Contact_'):o.shape_key_remove(k)
for label in ['A_Natural','B_Side90','C_Forward','D_AsymmetricAim','F_OneArmBack','G_WideOpen','H_FrontNear','I_FrontBack','J_WristTwist']:solve(label)
scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'))
