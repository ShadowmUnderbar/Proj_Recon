import bpy,math
from mathutils import Vector,Matrix,Quaternion
POSES={
 'Base':((.422618,0,-.906308),(-.422618,0,-.906308),0,0),
 'A_Natural':((.36,-.035,-.932),(-.36,-.035,-.932),8,8),
 'B_Side90':((1,0,0),(-1,0,0),0,0),
 'C_Forward':((.16,-1,-.06),(-.16,-1,-.06),10,10),
 'D_AsymmetricAim':((.80,-.57,.16),(-.43,.86,-.27),30,45),
 'E_Elbow90':((.422618,0,-.906308),(-.422618,0,-.906308),90,90),
 'F_OneArmBack':((.30,.86,-.42),(-.40,-.035,-.916),20,8),
 'G_WideOpen':((.96,.16,.23),(-.96,.16,.23),5,5),
 'H_FrontNear':((.30,-1,-.14),(-.30,-1,-.14),18,18),
 'I_FrontBack':((.10,-1,.04),(-.10,1,-.04),15,15),
 'J_WristTwist':((.38,-.15,-.91),(-.38,-.15,-.91),35,35)
}
def set_pose(label,keyframe=None):
 rig=bpy.data.objects['ElseIf_Game_Humanoid']
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION';pb.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
 left,right,el,er=POSES[label]
 for side,desired,angle in [('L',left,el),('R',right,er)]:
  name='J_Bip_'+side+'_UpperArm';b=rig.data.bones[name];pb=rig.pose.bones[name];rest=(b.tail_local-b.head_local).normalized();target=Vector(desired).normalized();q=rest.rotation_difference(target);world=Matrix.Translation(b.head_local)@q.to_matrix().to_4x4()@b.matrix_local.to_3x3().to_4x4();pb.matrix=world
  bpy.context.view_layer.update()
  lower=rig.pose.bones['J_Bip_'+side+'_LowerArm'];axis=target.cross(Vector((0,-1,0)))
  if axis.length<.15:axis=Vector((1 if side=='L' else -1,0,0))
  axis.normalize();rot=Quaternion(axis,math.radians(angle));mat=lower.matrix.copy();pivot=mat.translation.copy();lower.matrix=Matrix.Translation(pivot)@rot.to_matrix().to_4x4()@Matrix.Translation(-pivot)@mat
  if label=='H_FrontNear':
   # 両袖を正中線の前へ寄せる。袖同士を意図的に交差させるポーズにはしない。
   current=(lower.tail-lower.head).normalized();desired_lower=Vector((-.08 if side=='L' else .08,-1,-.05)).normalized();turn=current.rotation_difference(desired_lower);mat=lower.matrix.copy();pivot=mat.translation.copy();lower.matrix=Matrix.Translation(pivot)@turn.to_matrix().to_4x4()@Matrix.Translation(-pivot)@mat
  if label=='J_WristTwist':rig.pose.bones['J_Bip_'+side+'_Hand'].rotation_quaternion=Quaternion((0,1,0),math.radians(80 if side=='L' else -80))
  bpy.context.view_layer.update()
 if keyframe is not None:
  for pb in rig.pose.bones:
   if any(t in pb.name for t in ['UpperArm','LowerArm','Hand']):pb.keyframe_insert('rotation_quaternion',frame=keyframe,group=pb.name)
 return rig
