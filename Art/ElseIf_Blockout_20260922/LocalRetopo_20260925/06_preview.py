import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925');s=bpy.data.scenes['ElseIf_Topology_Review'];bpy.context.window.scene=s;s.frame_set(71);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];s.cycles.samples=24;s.render.filepath=str(P/'Trial_Wide.png');bpy.ops.render.render(write_still=True);print([(o.name) for o in bpy.data.objects if o.type=='CAMERA']);s.frame_set(1)
