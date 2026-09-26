import bpy,bmesh,json,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');mapping=json.loads((P/'Duplicate_Map.json').read_text());report={'protected_changes':[],'poses':{}}
src=(P.parent/'LocalRefinement_20260924/07_validation.py').read_text(encoding='utf-8-sig');funcs=[n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name in ['snap','closed','inside']];exec(compile(ast.Module(body=funcs,type_ignores=[]),'<validation helpers>','exec'))
src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fingerprint>','exec'))
for s in list(bpy.data.scenes):bpy.context.window.scene=s;bpy.context.view_layer.update()
for n,old in json.loads((P/'Protected_Before.json').read_text()).items():
 if n not in bpy.data.objects or fp(bpy.data.objects[n])!=old:report['protected_changes'].append(n)
for sn,pre in [('ElseIf_Master_Quality_Review',''),('ElseIf_Master_Quality_NaturalPose','NaturalPose__')]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;bpy.context.view_layer.update();bodies={n:snap(bpy.data.objects[mapping[pre+n]]) for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']};bodyob=bpy.data.objects[mapping[pre+'ElseIf_Mannequin']];body=bodies['ElseIf_Mannequin'];parts=[o for o in s.objects if o.type=='MESH' and o.get('quality_source','').startswith(pre+'ElseIf_Jacket_')];cloth={o['quality_source'].removeprefix(pre):snap(o) for o in parts};collisions=[]
 for name,item in cloth.items():
  for bn,b in bodies.items():
   pairs=item['tree'].overlap(b['tree'])
   if pairs:collisions.append({'part':name,'body':bn,'pairs':len(pairs)})
 addon=[]
 for o in s.objects:
  if o.type!='MESH' or 'EL_' not in o.get('quality_source',''):continue
  item=snap(o)
  for bn,b in bodies.items():
   pairs=item['tree'].overlap(b['tree'])
   if pairs:addon.append({'part':o.name,'body':bn,'pairs':len(pairs)})
 hands={}
 for side in ['L','R']:
  vs=[];fs=[]
  for n in ['ElseIf_Jacket_Sleeve_'+side,'ElseIf_Jacket_Cuff_'+side]:
   item=cloth[n];count=len(bpy.data.objects[mapping[n]].data.vertices);off=len(vs);vs+=item['vs'][:count];fs += [tuple(off+i for i in f) for f in item['polys'] if all(i<count for i in f)]
  tree=closed(vs,fs);ids={g.index for g in bodyob.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(t in g.name for t in ['Hand','Thumb','Index','Middle','Ring','Little'])};points=[p for p,gs in zip(body['vs'],body['groups']) if any(i in ids and w>.1 for i,w in gs)];hands[side]={'points':len(points),'outside':sum(not inside(tree,p) for p in points)}
 def volume(name):
  o=bpy.data.objects[mapping[name]];mods=[m for m in o.modifiers if m.type=='SOLIDIFY'];flags=[m.show_viewport for m in mods]
  try:
   for m in mods:m.show_viewport=False
   bpy.context.view_layer.update();item=snap(o);return closed(item['vs'],item['polys'])
  finally:
   for m,f in zip(mods,flags):m.show_viewport=f
   bpy.context.view_layer.update()
 volumes=[volume(n) for n in ['ElseIf_Shorts','ElseIf_Sock_L','ElseIf_Sock_R']];pts=[p for p in body['vs'] if .735<p.z<.92 and .015<abs(p.x)<.18];thigh={'points':len(pts),'outside':sum(not any(inside(t,p) for t in volumes) for p in pts)};shoes={}
 for side in ['L','R']:
  volumes=[volume('ElseIf_Shoe_'+side+'_'+part) for part in ['Upper','Sole','AnkleCollar']];ids={g.index for g in bodyob.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(t in g.name for t in ['Foot','Toe'])};pts=[p for p,gs in zip(body['vs'],body['groups']) if any(i in ids and w>.1 for i,w in gs)];bad=[p for p in pts if not any(inside(t,p) for t in volumes)];shoes[side]={'points':len(pts),'outside':len(bad),'sample':[list(p) for p in bad[:10]]}
 report['poses'][sn]={'cloth_body_intersections':collisions,'design_body_intersections':addon,'hands':hands,'thigh_coverage':thigh,'foot_coverage':shoes}
report['topology_unchanged']=all(len(bpy.data.objects[a].data.vertices)==len(bpy.data.objects[b].data.vertices) and len(bpy.data.objects[a].data.polygons)==len(bpy.data.objects[b].data.polygons) for a,b in mapping.items() if bpy.data.objects[a].type=='MESH');report['material_slots_unchanged']=all(list(bpy.data.objects[a].data.materials)==list(bpy.data.objects[b].data.materials) for a,b in mapping.items() if bpy.data.objects[a].type=='MESH');report['protected_duplicate_changes']=[]
for a,b in mapping.items():
 if any(t in a for t in ['Mannequin','Anatomy_Restoration','HeadSocket','Humanoid','ElseIf_Shorts','ElseIf_Sock_']):
  if fp(bpy.data.objects[a])!=fp(bpy.data.objects[b]):report['protected_duplicate_changes'].append(b)
(P/'Validation.json').write_text(json.dumps(report,indent=2));bpy.context.window.scene=bpy.data.scenes['ElseIf_Master_Quality_Review'];print(json.dumps(report))

