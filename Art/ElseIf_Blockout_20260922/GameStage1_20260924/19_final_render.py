import bpy,json
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene
r=json.loads((OUT/'08_stress_audit.json').read_text());assert all(not p['cloth_body_intersections'] and all(h['outside']==0 for h in p['hands'].values()) for p in r.values()),'Stress audit must pass before final rendering'
frames=json.loads((OUT/'Pose_Frames.json').read_text());hi=bpy.data.objects['ElseIf_Cam_HighAngle'];cam=bpy.data.objects.get('Game_Cam_Stress_HighAngle')
if not cam:
 cam=hi.copy();cam.data=hi.data.copy();cam.name='Game_Cam_Stress_HighAngle';scene.collection.objects.link(cam);cam.data.ortho_scale*=1.12
scene.cycles.samples=48;manifest=[]
jobs=[('Base','Game_BasePose_HighAngle',hi),('A_Natural','Game_NaturalPose_HighAngle',hi)]+[(p,'Game_'+p+'_HighAngle',cam) for p in frames if p not in ['Base','A_Natural']]+[(p,'Game_'+p+'_ThreeQuarter',bpy.data.objects['Game_Cam_Stress_ThreeQuarter']) for p in ['D_AsymmetricAim','E_Elbow90','F_OneArmBack']]
for pose,name,camera in jobs:
 scene.frame_set(frames[pose]);scene.camera=camera;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True);manifest.append({'pose':pose,'file':name+'.png','camera':camera.name,'frame':frames[pose]});(OUT/'Final_Render_Manifest.json').write_text(json.dumps(manifest,indent=2))
scene.frame_set(1);scene.camera=hi;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Final stress and comparison renders completed.')
