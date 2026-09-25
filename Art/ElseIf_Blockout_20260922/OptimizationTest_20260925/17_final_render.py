import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');r=json.loads((P/'08_stress_audit.json').read_text());assert all(not x['cloth_body_intersections'] and all(h['outside']==0 for h in x['hands'].values()) for x in r.values())
frames=json.loads((P/'Pose_Frames.json').read_text());jobs=[('Base','Base_HighAngle'),('A_Natural','Natural_HighAngle'),('D_AsymmetricAim','AsymmetricAim'),('G_WideOpen','ArmsWide'),('E_Elbow90','Elbow90'),('F_OneArmBack','OneArmBack'),('J_WristTwist','WristTwist80'),('B_Side90','Arms90'),('Base','Back')];manifest=[r for r in json.loads((P/'Render_Manifest.json').read_text()) if r['model']=='Current']
for sn,label in [('ElseIf_Optimized_Review','Optimized')]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.cycles.samples=48
 for pose,name in jobs:
  s.frame_set(frames[pose]);s.camera=bpy.data.objects['ElseIf_Cam_Back' if name=='Back' else 'ElseIf_Cam_HighAngle' if name in ['Base_HighAngle','Natural_HighAngle'] else 'Game_Cam_Stress_HighAngle'];s.render.filepath=str(P/(label+'_'+name+'.png'));bpy.ops.render.render(write_still=True);manifest.append({'model':label,'pose':pose,'file':label+'_'+name+'.png','camera':s.camera.name});(P/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2))
 s.frame_set(1);s.camera=bpy.data.objects['ElseIf_Cam_HighAngle']
bpy.context.window.scene=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimization_Test.blend'));print('Comparison renders completed.')
