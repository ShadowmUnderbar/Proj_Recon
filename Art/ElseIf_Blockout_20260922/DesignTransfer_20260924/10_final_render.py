import bpy,json
from pathlib import Path
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/DesignTransfer_20260924')
r=json.loads((out/'09_validation.json').read_text(encoding='utf-8'))
assert not r['protected_changes']
for p in r['poses'].values():
 assert not p['base_cloth_body_intersections'] and not p['design_body_intersections']
 assert p['thigh_coverage']['uncovered']==0
 assert all(h['outside']==0 for h in p['hands'].values())
base=bpy.data.scenes['ElseIf_Basis_Construction'];natural=bpy.data.scenes['ElseIf_NaturalPose_Validation']
bpy.context.window.scene=base
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ElseIf_Design_Transfer.blend'))
manifest=[]
for scene,prefix,views in [(base,'',['Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle']),(natural,'NaturalPose_',['Front','ThreeQuarter','HighAngle'])]:
 bpy.context.window.scene=scene
 for view in views:
  scene.camera=bpy.data.objects['ElseIf_Cam_'+view]
  scene.render.filepath=str(out/(prefix+view+'.png'))
  bpy.ops.render.render(write_still=True)
  manifest.append({'file':prefix+view+'.png','scene':scene.name,'camera':scene.camera.name})
  (out/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
 scene.camera=bpy.data.objects['ElseIf_Cam_ThreeQuarter']
bpy.context.window.scene=base
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ElseIf_Design_Transfer.blend'))
print('Completed nine renders and saved main model.')
