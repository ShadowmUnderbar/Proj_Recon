import bpy,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');code=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(code).body:
 if isinstance(fn,ast.FunctionDef) and fn.name in ['snap','union']:exec(compile(ast.Module(body=[fn],type_ignores=[]),'<helpers>','exec'))
dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z];boundary={};s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1)
for name in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
 o=bpy.data.objects['Mobile__'+name];m=o.data;geo={v.index:tuple(round(c,5) for c in v.co) for v in m.vertices};edges={}
 for f in m.polygons:
  for a,b in f.edge_keys:
   k=tuple(sorted((geo[a],geo[b])));edges.setdefault(k,[]).append((a,b))
 source=bpy.data.objects['LOD0__'+name].data;sg={v.index:tuple(round(c,5) for c in v.co) for v in source.vertices};se={}
 for f in source.polygons:
  for a,b in f.edge_keys:
   key=tuple(sorted((sg[a],sg[b])));se[key]=se.get(key,0)+1
 inherited={e for e,c in se.items() if c==1}
 result=[]
 for e,instances in edges.items():
  if len(instances)!=1 or e in inherited:continue
  a,b=instances[0]
  if max(m.vertices[a].co.z,m.vertices[b].co.z)>1.32:continue
  if any(any(g.weight>.001 and any(t in o.vertex_groups[g.group].name for t in ['Hand','Thumb','Index','Middle','Ring','Little']) for g in m.vertices[i].groups) for i in (a,b)):continue
  result.append((a,b))
 boundary[name]=result
report={}
for label,frame in json.loads((P/'Pose_Frames.json').read_text()).items():
 s.frame_set(frame);bpy.context.view_layer.update();items={o.name.removeprefix('Mobile__'):snap(o) for o in bpy.data.collections['ElseIf_Game_MobileVR_Test'].objects if o.type=='MESH'};row={}
 for name,edges in boundary.items():
  tree=union(items.values());vs=items[name]['v'];bad=[]
  for a,b in edges:
   for p in [vs[a],vs[b],(vs[a]+vs[b])*.5]:
    if any(tree.ray_cast(p+d*.00005,d,2)[0] is None for d in dirs):bad.append({'edge':[a,b],'point':list(p)});break
  row[name]={'tested_boundary_edges':len(edges),'uncovered_edges':len(bad),'samples':bad[:20]}
 report[label]=row;(P/'Boundary_Visibility_Final.json').write_text(json.dumps(report,indent=2))
s.frame_set(1);print(json.dumps({p:{n:r['uncovered_edges'] for n,r in v.items()} for p,v in report.items()}))


