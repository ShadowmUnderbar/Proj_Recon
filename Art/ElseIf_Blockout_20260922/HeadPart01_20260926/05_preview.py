import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926')
for sn,cam,file in [('ElseIf_Head01_Standalone_Review','Head01_Cam_ThreeQuarter','Preview_Head_ThreeQuarter'),('ElseIf_Head01_Assembly_Review','Game_Cam_Stress_HighAngle','Preview_Assembly_HighAngle')]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.camera=bpy.data.objects[cam];s.render.image_settings.file_format='PNG';s.render.filepath=str(P/(file+'.png'));bpy.ops.render.render(write_still=True)
print('Two preview renders complete')
