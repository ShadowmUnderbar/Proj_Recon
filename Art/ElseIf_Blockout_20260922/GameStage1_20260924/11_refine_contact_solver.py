import bpy,ast
from pathlib import Path
from mathutils import Vector
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);rig=bpy.data.objects['ElseIf_Game_Humanoid']
for o in bpy.data.collections['ElseIf_Game'].objects:
 if o.type=='MESH' and o.data.shape_keys:
  for k in list(o.data.shape_keys.key_blocks):
   if k.name.startswith('DLHN_Contact_'):o.shape_key_remove(k)
s=(OUT/'weight_utils.py').read_text();exec(compile(ast.Module(body=[n for n in ast.parse(s).body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'<weights>','exec'))
o=bpy.data.objects['Game__ElseIf_Anatomy_Restoration'];o.vertex_groups.clear();groups={}
for v in o.data.vertices:
 for n,w in weights(v.co,o['master_source']).items():
  if n not in groups:groups[n]=o.vertex_groups.new(name=n)
  groups[n].add([v.index],w,'REPLACE')
print('Removed provisional contact keys; restored smooth anatomical support weights.')
