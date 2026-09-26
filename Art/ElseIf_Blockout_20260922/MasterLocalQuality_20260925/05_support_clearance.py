import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');s=bpy.data.scenes['ElseIf_Master_Quality_Review'];bpy.context.window.scene=s;bpy.context.view_layer.update();vs=[];fs=[]
for n in ['Quality__ElseIf_Mannequin','Quality__ElseIf_Anatomy_Restoration']:
 o=bpy.data.objects[n];e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();off=len(vs);vs += [o.matrix_world@v.co for v in m.vertices];fs += [tuple(off+i for i in p.vertices) for p in m.polygons];e.to_mesh_clear()
tree=BVHTree.FromPolygons(vs,fs);report={}
for o in bpy.data.objects:
 if not o.name.startswith('Quality__') or o.type!='MESH' or not o.data.shape_keys:continue
 k=o.data.shape_keys.key_blocks.get('Quality_Shoulder_Armhole_20260925')
 if not k:continue
 basis=o.data.shape_keys.reference_key;count=0
 for i,v in enumerate(basis.data):
  d=k.data[i].co-v.co
  if d.length<1e-7:continue
  p=v.co+sum(((other.data[i].co-other.relative_key.data[i].co)*other.value for other in o.data.shape_keys.key_blocks if other not in [basis,k] and not other.mute),Vector());hit=tree.find_nearest(o.matrix_world@p)
  if hit[3]>.014:continue
  n=o.matrix_world.to_3x3().inverted()@hit[1];inward=d.dot(n)
  if inward<0:
   d-=n*inward;k.data[i].co=v.co+d;count+=1
 report[o.name]=count
(P/'Support_Clearance_Adjustment.json').write_text(json.dumps(report,indent=2));bpy.context.view_layer.update();print(report)
