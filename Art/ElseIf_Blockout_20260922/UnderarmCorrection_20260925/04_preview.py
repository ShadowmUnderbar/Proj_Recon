import bpy
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');s=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=s;s.frame_set(71);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];s.cycles.samples=32;s.render.filepath=str(P/'WeightTrial_Wide.png');bpy.ops.render.render(write_still=True);s.frame_set(1)
