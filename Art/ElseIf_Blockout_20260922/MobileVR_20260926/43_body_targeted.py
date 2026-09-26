import bpy,json
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;o=bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration'];backup=o.data.copy();backup.name='Mobile_Body_Conservative_Backup';backup.use_fake_user=True
s.frame_set(21);bpy.context.view_layer.update();e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());em=e.to_mesh();target=Vector((-.09771069,-.04520052,1.16512918));idx=min(range(len(em.vertices)),key=lambda i:(em.vertices[i].co-target).length);point=o.data.vertices[idx].co.copy();e.to_mesh_clear();s.frame_set(1)
# 腕90度で境界候補になった位置を元の姿勢へ対応させ、その近傍も完全保護。
code=(P/'28_body_local_protect.py').read_text(encoding='utf-8-sig');code=code.replace("keep=v.index in boundary or", "keep=(v.co-Vector("+repr(list(point))+")).length<.028 or (v.co-Vector("+repr([-point.x,point.y,point.z])+")).length<.028 or v.index in boundary or");code=code.replace('Body_Density_Final.json','Body_Density_Targeted_Final.json');exec(compile(code,'<targeted body>','exec'));print('Extra protected center',list(point))
