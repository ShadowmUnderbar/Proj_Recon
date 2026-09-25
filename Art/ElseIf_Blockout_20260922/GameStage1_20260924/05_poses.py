import bpy,json,sys
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');sys.path.insert(0,str(OUT));from pose_utils import set_pose,POSES
scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;rig=bpy.data.objects['ElseIf_Game_Humanoid'];frames={}
for i,name in enumerate(POSES):
 frame=1+i*10;set_pose(name,keyframe=frame);scene.timeline_markers.new(name,frame=frame);frames[name]=frame
rig.animation_data.action.name='DLHN_StressTest_Base_A_to_I_and_Wrist';scene.frame_start=1;scene.frame_end=101
(OUT/'Pose_Frames.json').write_text(json.dumps(frames,indent=2))
# 応力検証用だけ少し広い画角。Master/Game比較用カメラは既存と同条件。
cam=bpy.data.objects['ElseIf_Cam_ThreeQuarter'].copy();cam.data=cam.data.copy();cam.name='Game_Cam_Stress_ThreeQuarter';scene.collection.objects.link(cam);cam.data.ortho_scale*=1.12
scene.camera=cam;scene.cycles.samples=24
for name in ['B_Side90','D_AsymmetricAim','E_Elbow90','G_WideOpen','H_FrontNear','I_FrontBack']:
 scene.frame_set(frames[name]);bpy.context.view_layer.update();scene.render.filepath=str(OUT/('Initial_'+name+'.png'));bpy.ops.render.render(write_still=True)
scene.frame_set(1);scene.cycles.samples=48;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Stress action and six initial deformation reviews saved.')
