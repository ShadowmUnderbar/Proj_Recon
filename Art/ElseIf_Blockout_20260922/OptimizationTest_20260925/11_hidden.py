import bpy,json,math,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');scene=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=scene;scene.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized_Test'];frames=json.loads((OUT/'Pose_Frames.json').read_text());bodies=[bpy.data.objects['Opt__'+n] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']]
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices) for p in m.polygons];e.to_mesh_clear();return vs,fs
candidates={};mandatory={}
for o in bodies:
 selected=set();keep=set()
 for f in o.data.polygons:
  points=[o.data.vertices[i].co for i in f.vertices]
  sensitive=any(p.z>1.17 or (p.z>1.08 and abs(p.x)>.07) for p in points)
  hand=any(any(any(t in o.vertex_groups[g.group].name for t in ['Hand','Thumb','Index','Middle','Ring','Little']) and g.weight>.05 for g in o.data.vertices[i].groups) for i in f.vertices)
  if sensitive or hand:keep.add(f.index)
  else:selected.add(f.index)
 candidates[o.name]=selected;mandatory[o.name]=keep

clothes=[o for o in col.objects if o.type=='MESH' and o['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_'))];dirs=[Vector(v).normalized() for v in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1),(1,1,1),(-1,-1,1)]]
for label,frame in frames.items():
 scene.frame_set(frame);bpy.context.view_layer.update();vs=[];fs=[]
 for o in clothes:
  v,f=snap(o);off=len(vs);vs+=v;fs.extend(tuple(off+i for i in p) for p in f)
 tree=BVHTree.FromPolygons(vs,fs)
 for o in bodies:
  v,f=snap(o);remove=[]
  for idx in candidates[o.name]:
   pts=[v[i] for i in f[idx]];center=sum(pts,Vector())/len(pts)
   if not all(tree.ray_cast(p,d,1.0)[0] is not None for p in [center]+pts for d in dirs):remove.append(idx)
  candidates[o.name].difference_update(remove)
scene.frame_set(1);classification={}
for o in bodies:
 attr=o.data.attributes.get('DLHN_HiddenFaceClass') or o.data.attributes.new('DLHN_HiddenFaceClass','INT','FACE')
 for d in attr.data:d.value=2
 for idx in candidates[o.name]:attr.data[idx].value=1
 for idx in mandatory[o.name]:attr.data[idx].value=3
 classification[o.name]={str(k):{'faces':sum(d.value==k for d in attr.data),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons if attr.data[f.index].value==k)} for k in [1,2,3]}
(OUT/'Hidden_Surface_Candidates.json').write_text(json.dumps(classification,indent=2))

print(json.dumps(classification))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"ElseIf_Optimization_Test.blend"))
