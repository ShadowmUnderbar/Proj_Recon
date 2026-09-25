import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;rig=bpy.data.objects['ElseIf_Game_Humanoid'];col=bpy.data.collections['ElseIf_Game']
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def add(w,n,v):
 if v>1e-7:w[n]=w.get(n,0)+v

def torso(p):
 z=p.z
 if z<1.04:
  t=smooth(.91,1.07,z);return {'J_Bip_C_Hips':1-t,'J_Bip_C_Spine':t}
 if z<1.20:
  t=smooth(1.07,1.20,z);return {'J_Bip_C_Spine':1-t,'J_Bip_C_Chest':t}
 t=smooth(1.20,1.32,z);return {'J_Bip_C_Chest':1-t,'J_Bip_C_UpperChest':t}

def weights(p,name):
 side='L' if p.x>=0 else 'R';suf='J_Bip_'+side+'_';x=abs(p.x)
 if 'Shoe' in name:return {suf+'Foot':1}
 if 'Sock' in name or 'Thigh' in name:
  t=smooth(.49,.61,p.z);return {suf+'LowerLeg':1-t,suf+'UpperLeg':t}
 if 'Shorts' in name:
  t=1-smooth(.80,.94,p.z);return {'J_Bip_C_Hips':1-t,suf+'UpperLeg':t}
 w=torso(p)
 if 'Collar' in name and 'Shoe' not in name:return {'J_Bip_C_UpperChest':1}
 sh=rig.data.bones[suf+'UpperArm'].head_local;ax=(rig.data.bones[suf+'UpperArm'].tail_local-sh).normalized();s=(p-sh).dot(ax)
 sleeve=any(t in name for t in ['Sleeve','Cuff','UpperArm'])
 arm=smooth(.070,.152,x)*smooth(1.150,1.280,p.z)
 if sleeve:arm=arm+(1-arm)*smooth(.155,.235,s)
 else:arm*=smooth(1.17,1.22,p.z)
 arm=max(0,min(1,arm));shoulder=.45*smooth(.028,.092,x)*smooth(1.29,1.36,p.z)*(1-arm)
 for n in list(w):w[n]*=1-arm-shoulder
 add(w,suf+'Shoulder',shoulder)
 lower=smooth(.170,.265,s);tip=smooth(.355,.510,s)*.90
 add(w,suf+'UpperArm',arm*(1-lower));add(w,suf+'LowerArm',arm*lower*(1-tip));add(w,'DLHN_SleeveTip_'+side,arm*lower*tip)
 # 最大4影響を正規化。胸郭の広い面へ腕のウェイトを広げない。
 items=sorted(((n,v) for n,v in w.items() if v>.0001),key=lambda a:-a[1])[:4];total=sum(v for n,v in items);return {n:v/total for n,v in items}

count=0
for o in col.objects:
 if o.type!='MESH':continue
 name=o['master_source']
 if name!='ElseIf_Mannequin':
  o.vertex_groups.clear();groups={}
  for v in o.data.vertices:
   p=o.matrix_world@v.co;w=weights(p,name)
   for n,value in w.items():
    if n not in groups:groups[n]=o.vertex_groups.new(name=n)
    groups[n].add([v.index],value,'REPLACE')
 # BlenderのDQに依存せず、通常の線形スキニングで検証する。
 mod=o.modifiers.new('DLHN_Linear_Skinning','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=False
 if any(t in name for t in ['Sleeve','Cuff','UpperArm']):
  o.shape_key_add(name='Basis')
  side='L' if ('_L' in name or 'UpperArm' in name) else 'R';sh=rig.data.bones['J_Bip_'+side+'_UpperArm'].head_local;ax=(rig.data.bones['J_Bip_'+side+'_UpperArm'].tail_local-sh).normalized();length=rig.data.bones['J_Bip_'+side+'_UpperArm'].length
  key=o.shape_key_add(name='DLHN_ElbowVolume_'+side)
  for i,v in enumerate(o.data.vertices):
   p=o.matrix_world@v.co;s=(p-sh).dot(ax);radial=p-(sh+ax*s);key.data[i].co=v.co+radial*(.32*math.exp(-((s-length)/.052)**2))
  f=key.driver_add('value');d=f.driver;d.type='SCRIPTED';var=d.variables.new();var.name='qw';var.type='SINGLE_PROP';var.targets[0].id=rig;var.targets[0].data_path='pose.bones["J_Bip_'+side+'_LowerArm"].rotation_quaternion[0]';d.expression='min(1.35,2*(1-qw*qw))'
 count+=1
rig['weight_design']='Manual normalized 4-weight field; localized shoulder/axilla transitions; forearm-led sleeve tips with 12% wrist follow; elbow-volume correctives; LBS validation'
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Manual weights installed on '+str(count)+' meshes; sleeve elbow correctives enabled.')
