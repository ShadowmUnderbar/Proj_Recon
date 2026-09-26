import bpy,json,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');frames=json.loads((P/'Pose_Frames.json').read_text());report={};activation={};dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z]
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[o.matrix_world@x.co for x in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];components=[m.attributes['DLHN_Component'].data[t.vertices[0]].value for t in m.loop_triangles] if m.attributes.get('DLHN_Component') else None;groups=[[(o.vertex_groups[g.group].name,g.weight) for g in x.groups] for x in m.vertices];polys=[tuple(f.vertices) for f in m.polygons];e.to_mesh_clear();return {'v':v,'f':fs,'c':components,'g':groups,'p':polys,'tree':BVHTree.FromPolygons(v,fs,all_triangles=True)}
def union(items):
 v=[];f=[]
 for a in items:off=len(v);v+=a['v'];f.extend(tuple(i+off for i in face) for face in a['f'])
 return BVHTree.FromPolygons(v,f,all_triangles=True)
for label,frame in frames.items():
 models={};row={}
 for prefix,sn,cn in [('LOD0__','ElseIf_LOD0_Review','ElseIf_Game_Optimized'),('Mobile__','ElseIf_MobileVR_Review','ElseIf_Game_MobileVR_Test')]:
  s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(frame);bpy.context.view_layer.update();items={o.name.removeprefix(prefix):snap(o) for o in bpy.data.collections[cn].objects if o.type=='MESH'};models[prefix]=items
  cloth={n:a for n,a in items.items() if 'Mannequin' not in n and 'Anatomy' not in n};tree=union(cloth.values());hands={};body=items['ElseIf_Mannequin']
  for side in ['L','R']:
   ids=[i for i,gs in enumerate(body['g']) if any('J_Bip_'+side+'_' in n and any(t in n for t in ['Hand','Thumb','Index','Middle','Ring','Little']) and w>.1 for n,w in gs)];bad=[]
   for i in ids:
    if any(tree.ray_cast(body['v'][i]+d*.00005,d,2)[0] is None for d in dirs):bad.append(i)
   hands[side]={'points':len(ids),'uncovered_26ray':len(bad),'ids':bad}
  coll={}
  for bn in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
   coll[bn]={n:len(a['tree'].overlap(items[bn]['tree'])) for n,a in cloth.items()}
  row[prefix]={'hands':hands,'intersections':coll}
  if prefix=='Mobile__':
   keys=bpy.data.objects['Mobile__Jacket'].data.shape_keys
   for k in keys.key_blocks[1:]:activation.setdefault(k.name,{})[label]=k.value
 row['surface']={}
 for n,a in models['Mobile__'].items():
  if 'Mannequin' in n or 'Anatomy' in n:continue
  b=models['LOD0__'][n];trees={}
  if b['c']:
   for cid in set(b['c']):trees[cid]=BVHTree.FromPolygons(b['v'],[f for f,c in zip(b['f'],b['c']) if c==cid],all_triangles=True)
  # 新メッシュの頂点および三角形中心を旧部品表面へ照合。
  errors=[]
  for f,cid in zip(a['f'],a['c']):
   p=sum((a['v'][i] for i in f),Vector())/3;hit=trees[cid].find_nearest(p);errors.append(hit[3]*1000)
  row['surface'][n]={'sampled_triangle_centers':len(errors),'mean_mm':float(np.mean(errors)),'p95_mm':float(np.percentile(errors,95)),'max_mm':max(errors)}
 report[label]=row;(P/'Stress_Audit.json').write_text(json.dumps(report,indent=2));(P/'Shape_Activation.json').write_text(json.dumps(activation,indent=2));print(label,{n:round(r['p95_mm'],3) for n,r in row['surface'].items()},flush=True)
for sn in ['ElseIf_LOD0_Review','ElseIf_MobileVR_Review']:bpy.data.scenes[sn].frame_set(1)
bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review'];print('Twelve poses checked')
