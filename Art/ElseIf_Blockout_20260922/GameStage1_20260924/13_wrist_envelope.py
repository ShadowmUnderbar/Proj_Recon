import bpy,math
from pathlib import Path
from mathutils import Vector
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);rig=bpy.data.objects['ElseIf_Game_Humanoid']
for o in bpy.data.collections['ElseIf_Game'].objects:
 if o.type!='MESH':continue
 if o.data.shape_keys:
  for k in list(o.data.shape_keys.key_blocks):
   if 'Contact_' in k.name and 'J_WristTwist' in k.name:o.shape_key_remove(k)
 n=o['master_source']
 if not any(t in n for t in ['Sleeve','Cuff']):continue
 side='L' if '_L' in n else 'R';b=rig.data.bones['J_Bip_'+side+'_UpperArm'];origin=b.head_local;axis=(b.tail_local-b.head_local).normalized()
 if not o.data.shape_keys:o.shape_key_add(name='Basis')
 key=o.shape_key_add(name='DLHN_Wrist_Clearance_'+side)
 for i,v in enumerate(o.data.vertices):
  p=v.co;s=(p-origin).dot(axis);t=max(0,min(1,(s-.35)/.10));t=t*t*(3-2*t);center=origin+axis*s;center.y-=.022*max(0,min(1,(s-.18)/.2));rad=p-center
  if rad.length>0:key.data[i].co+=rad.normalized()*(.013*t)
 d=key.driver_add('value').driver;d.type='SCRIPTED';var=d.variables.new();var.name='qw';var.type='SINGLE_PROP';var.targets[0].id=rig;var.targets[0].data_path='pose.bones["J_Bip_'+side+'_Hand"].rotation_quaternion[0]';d.expression='min(1.25,(1-qw*qw)/0.4131759)'
exec((OUT/'contact_utils.py').read_text(encoding='utf-8-sig'));solve('I_FrontBack');solve('J_WristTwist');scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'))
