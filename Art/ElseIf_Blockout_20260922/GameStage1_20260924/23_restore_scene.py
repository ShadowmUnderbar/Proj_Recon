import bpy,json
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];s=base.copy();s.name='ElseIf_Game_Validation';s.use_fake_user=True
for c in list(s.collection.children):s.collection.children.unlink(c)
for o in list(s.collection.objects):s.collection.objects.unlink(o)
s.collection.children.link(bpy.data.collections['ElseIf_Review_Stage']);s.collection.children.link(bpy.data.collections['ElseIf_Game']);bpy.data.collections['ElseIf_Game'].use_fake_user=True;bpy.context.window.scene=s;s.frame_start=1;s.frame_end=101
for label,frame in json.loads((OUT/'Pose_Frames.json').read_text()).items():s.timeline_markers.new(label,frame=frame)
for view in ['ThreeQuarter','HighAngle']:
 name='Game_Cam_Stress_'+view;cam=bpy.data.objects.get(name)
 if not cam:cam=bpy.data.objects['ElseIf_Cam_'+view].copy();cam.data=cam.data.copy();cam.name=name;cam.data.ortho_scale*=1.12
 if cam.name not in s.objects:s.collection.objects.link(cam)
s.camera=bpy.data.objects['ElseIf_Cam_HighAngle'];s.frame_set(1);print('Game validation scene restored with persistent scene/collection users.')
