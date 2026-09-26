import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s
jobs=[('Base_HighAngle',1,'Game_Cam_Stress_HighAngle'),('Natural_HighAngle',11,'Game_Cam_Stress_HighAngle'),('ArmsWide',71,'Game_Cam_Stress_HighAngle'),('AsymmetricAim',41,'Game_Cam_Stress_HighAngle'),('Elbow90',51,'Game_Cam_Stress_HighAngle'),('OneArmBack',61,'Game_Cam_Stress_HighAngle'),('WristTwist',101,'Game_Cam_Stress_HighAngle'),('Base_BackQuarter',1,'ElseIf_Cam_BackQuarter')]
manifest=[]
for label,frame,camera in jobs:
 s.frame_set(frame);s.camera=bpy.data.objects[camera];s.cycles.samples=48;s.cycles.seed=0;s.render.image_settings.file_format='PNG';s.render.image_settings.compression=75;s.render.filepath=str(P/('MobileVR_'+label+'.png'));bpy.ops.render.render(write_still=True);manifest.append({'file':'MobileVR_'+label+'.png','frame':frame,'camera':camera,'resolution':[s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage],'samples':48});(P/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2))
s.frame_set(1);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];print('Eight renders complete')
