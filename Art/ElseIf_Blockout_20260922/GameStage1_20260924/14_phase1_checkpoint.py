import bpy,json
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');s=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=s;s.frame_set(1);s.camera=bpy.data.objects['ElseIf_Cam_HighAngle'];s.render.filepath=str(OUT/'Game_BeforeReduction_HighAngle.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Rigged_BeforeReduction.blend'),copy=True)
frames=json.loads((OUT/'Pose_Frames.json').read_text());s.camera=bpy.data.objects['Game_Cam_Stress_ThreeQuarter'];s.cycles.samples=24
for label in ['G_WideOpen','I_FrontBack']:
 s.frame_set(frames[label]);s.render.filepath=str(OUT/('Rigged_'+label+'.png'));bpy.ops.render.render(write_still=True)
s.frame_set(1);s.cycles.samples=48
print(json.dumps({o.name:[(m.name,m.type,getattr(m,'levels',None)) for m in o.modifiers] for o in bpy.data.objects if o.name in ['ElseIf_Anatomy_Restoration','ElseIf_Shorts','ElseIf_Sock_L','ElseIf_Shoe_L_Upper']}))
