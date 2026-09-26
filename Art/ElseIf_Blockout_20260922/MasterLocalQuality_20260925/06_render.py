import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');r=json.loads((P/'Validation.json').read_text());assert not r['protected_changes'] and not r['protected_duplicate_changes'];assert r['topology_unchanged'] and r['material_slots_unchanged']
for p in r['poses'].values():
 assert not p['cloth_body_intersections'] and not p['design_body_intersections'];assert p['thigh_coverage']['outside']==0;assert all(v['outside']==0 for v in p['hands'].values());assert all(v['outside']==0 for v in p['foot_coverage'].values())
mapping=json.loads((P/'Duplicate_Map.json').read_text());manifest=[]
for sn,prefix,views in [('ElseIf_Master_Quality_Review','',['Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle']),('ElseIf_Master_Quality_NaturalPose','NaturalPose_',['Front','ThreeQuarter','HighAngle'])]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.render.image_settings.file_format='PNG';s.render.image_settings.compression=70
 for view in views:
  s.camera=bpy.data.objects[mapping['ElseIf_Cam_'+view]];s.render.filepath=str(P/(prefix+view+'.png'));bpy.ops.render.render(write_still=True);manifest.append({'file':prefix+view+'.png','scene':sn,'camera':s.camera.name,'resolution':[s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage],'samples':s.cycles.samples});(P/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2))
 s.camera=bpy.data.objects[mapping['ElseIf_Cam_ThreeQuarter']]
bpy.context.window.scene=bpy.data.scenes['ElseIf_Master_Quality_Review'];bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Master_LocalQuality.blend'));print('Nine requested renders and final blend saved.')
