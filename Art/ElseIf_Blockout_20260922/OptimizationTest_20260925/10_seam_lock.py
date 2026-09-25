import bpy,bmesh,math
from pathlib import Path
from mathutils.kdtree import KDTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized_Test'];trees={}
for side in ['L','R']:
 source=bpy.data.objects['ElseIf_Jacket_Sleeve_'+side];target=bpy.data.objects['Opt__ElseIf_Jacket_Sleeve_'+side];bm=bmesh.new();bm.from_mesh(source.data);pts=[target.data.vertices[v.index].co.copy() for v in bm.verts if v.is_boundary and v.co.z>1.19];bm.free();kd=KDTree(len(pts))
 for i,p in enumerate(pts):kd.insert(p,i)
 kd.balance();trees[side]=kd
def sm(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
for o in col.objects:
 if o.type!='MESH' or not o.data.shape_keys:continue
 keys=o.data.shape_keys;basis=keys.reference_key
 for k in list(keys.key_blocks):
  if k.name.startswith('DLHN_Contact') and k.name.endswith('.001'):
   k.driver_remove('value');o.shape_key_remove(k)
 for k in keys.key_blocks:
  if not k.name.startswith('DLHN_Wide_ClothEase'):continue
  side=k.name[-1]
  for i,v in enumerate(k.data):
   p=basis.data[i].co;dist=trees[side].find(p)[2];v.co=p+(v.co-p)*sm(.024,.055,dist)
s.frame_set(71);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];s.cycles.samples=32;s.render.filepath=str(P/'Corrected_Wide_HighAngle.png');bpy.ops.render.render(write_still=True);s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Corrected_BeforeReduction.blend'),copy=True)
