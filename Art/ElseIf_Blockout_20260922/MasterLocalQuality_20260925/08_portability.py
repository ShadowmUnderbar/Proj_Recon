import bpy,json,os
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');r=[]
for im in bpy.data.images:
 if im.source=='FILE':r.append({'name':im.name,'packed':bool(im.packed_file),'path':im.filepath,'exists':os.path.exists(bpy.path.abspath(im.filepath))})
(P/'Texture_Portability.json').write_text(json.dumps(r,indent=2));print(json.dumps(r));print('Active scene:',bpy.context.scene.name)
