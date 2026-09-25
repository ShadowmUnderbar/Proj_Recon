import bpy,json
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);rig=bpy.data.objects['ElseIf_Game_Humanoid'];count=0
for o in bpy.data.collections['ElseIf_Game'].objects:
 if o.type!='MESH' or not o.data.shape_keys:continue
 keys=o.data.shape_keys;keep={k.name:[v.co.copy() for v in k.data] for k in keys.key_blocks if k.name.startswith(('DLHN_ElbowVolume_','DLHN_Wrist_Clearance_'))};basis=[v.co.copy() for v in keys.reference_key.data]
 # Driverの参照を先に外してからGame側だけのキーを再構成する。
 keys.animation_data_clear();o.shape_key_clear()
 for v,p in zip(o.data.vertices,basis):v.co=p
 if keep:
  o.shape_key_add(name='Basis',from_mix=False)
  for name,coords in keep.items():
   k=o.shape_key_add(name=name,from_mix=False)
   for v,p in zip(k.data,coords):v.co=p
   side=name[-1];bone='LowerArm' if 'ElbowVolume' in name else 'Hand';d=k.driver_add('value').driver;d.type='SCRIPTED';v=d.variables.new();v.name='qw';v.type='SINGLE_PROP';v.targets[0].id=rig;v.targets[0].data_path='pose.bones["J_Bip_'+side+'_'+bone+'"].rotation_quaternion[0]';d.expression='min(1.35,2*(1-qw*qw))' if bone=='LowerArm' else 'min(1.25,(1-qw*qw)/0.4131759)'
 count+=1
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_CleanCorrectiveBase.blend'),copy=True);print('Safely reconstructed shape keys on '+str(count)+' Game meshes; master untouched.')
