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
 arm=smooth(.046,.140,x)*smooth(1.105,1.205,p.z)
 if sleeve:arm=arm+(1-arm)*smooth(.110,.180,s)
 else:arm*=smooth(1.105,1.205,p.z)
 arm=max(0,min(1,arm));shoulder=.25*smooth(.028,.092,x)*smooth(1.29,1.36,p.z)*(1-arm)
 for n in list(w):w[n]*=1-arm-shoulder
 add(w,suf+'Shoulder',shoulder)
 lower=smooth(.170,.265,s);tip=smooth(.355,.510,s)*.90
 add(w,suf+'UpperArm',arm*(1-lower));add(w,suf+'LowerArm',arm*lower*(1-tip));add(w,'DLHN_SleeveTip_'+side,arm*lower*tip)
 # 最大4影響を正規化。胸郭の広い面へ腕のウェイトを広げない。
 items=sorted(((n,v) for n,v in w.items() if v>.0001),key=lambda a:-a[1])[:4];total=sum(v for n,v in items);return {n:v/total for n,v in items}

