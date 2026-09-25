import bpy
from pathlib import Path
for o in bpy.data.collections['ElseIf_Game'].objects:
 if o.type!='MESH' or not o.data.shape_keys:continue
 basis=o.data.shape_keys.reference_key
 for k in o.data.shape_keys.key_blocks:
  if k.name.startswith('DLHN_Wrist_Clearance'):
   for i,v in enumerate(k.data):v.co=basis.data[i].co+(v.co-basis.data[i].co)*1.5
bpy.context.scene.frame_set(1)
print('Wrist rotation clearance increased; zero change in base pose.')
