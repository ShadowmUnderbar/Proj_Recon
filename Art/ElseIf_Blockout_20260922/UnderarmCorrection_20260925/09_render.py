import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');manifest=[];cam=bpy.data.objects['Game_Cam_Stress_HighAngle'];jobs=[('ElseIf_Current_Review',71,'Current_Wide'),('ElseIf_Optimized_Review',71,'Optimized_Wide')]+[('ElseIf_Underarm_Review',f,'Corrected_'+n) for f,n in [(1,'Base'),(111,'Arms45'),(21,'Arms90'),(71,'Wide'),(41,'AsymmetricAim'),(61,'OneArmBack')]]
for sn,frame,name in jobs:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(frame);s.camera=cam;s.cycles.samples=48;s.render.filepath=str(P/(name+'.png'));bpy.ops.render.render(write_still=True);manifest.append({'file':name+'.png','scene':sn,'frame':frame,'camera':cam.name});(P/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2));s.frame_set(1)
s=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=s;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Underarm_Corrected.blend'));print('Eight final renders saved.')
