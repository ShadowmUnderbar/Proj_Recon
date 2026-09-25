import bpy,json
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/DesignTransfer_20260924')
bpy.context.window.scene=bpy.data.scenes['ElseIf_Basis_Construction']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Design_Transfer.blend'))
report={'design_count':len(bpy.data.collections['ElseIf_Design_Basis'].objects),'materials':[m.name for m in bpy.data.materials],'collections':[(c.name,len(c.objects)) for c in bpy.data.collections]}
(OUT/'Recovery_State.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for view in ['Front','Back','HighAngle']:
 s=bpy.context.scene;s.camera=bpy.data.objects['ElseIf_Cam_'+view];s.render.filepath=str(OUT/('Review_'+view+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(report,ensure_ascii=False))
