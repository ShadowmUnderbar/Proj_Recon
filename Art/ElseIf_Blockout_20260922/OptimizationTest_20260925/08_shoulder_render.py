import bpy
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925')
for sn,label in [('ElseIf_Current_Review','Current'),('ElseIf_Optimized_Review','Corrected')]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(71);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];s.cycles.samples=32;s.render.filepath=str(P/(label+'_Wide_HighAngle.png'));bpy.ops.render.render(write_still=True)
bpy.context.window.scene=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Corrected_BeforeReduction.blend'),copy=True)
