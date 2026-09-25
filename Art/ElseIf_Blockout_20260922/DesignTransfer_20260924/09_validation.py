import bpy,bmesh,json,hashlib,struct,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/DesignTransfer_20260924')
# 保全時と同じ指紋関数だけを再利用する（バックアップ保存処理は実行しない）。
src=(OUT/'07_preserve_recover.py').read_text(encoding='utf-8-sig');parsed=ast.parse(src);fn=next(n for n in parsed.body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fingerprint>','exec'))
before=json.loads((OUT/'Recovered_Protected_Audit.json').read_text(encoding='utf-8'));report={'protected_changes':[n for n,v in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=v],'poses':{}}
base=bpy.data.scenes['ElseIf_Basis_Construction'];natural=bpy.data.scenes['ElseIf_NaturalPose_Validation']
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];ps=[tuple(p.vertices) for p in m.polygons];gs=[[(g.group,g.weight) for g in v.groups] for v in m.vertices];e.to_mesh_clear();return {'tree':BVHTree.FromPolygons(vs,fs,all_triangles=True),'vs':vs,'fs':fs,'polys':ps,'groups':gs}
def closed(vs,faces):
 me=bpy.data.meshes.new('_VerifyVolume');me.from_pydata(vs,[],faces);me.update();bm=bmesh.new();bm.from_mesh(me);bpy.data.meshes.remove(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0015);bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));tree=BVHTree.FromBMesh(bm);bm.free();return tree
def inside(tree,p):
 d=Vector((.963,.147,.223)).normalized();loc=p.copy();count=0
 for _ in range(40):
  hit=tree.ray_cast(loc,d,4)
  if hit[0] is None:break
  count+=1;loc=hit[0]+d*.00002
 return count%2==1
for scene,prefix,colname in [(base,'','ElseIf_Design_Basis'),(natural,'NaturalPose__','ElseIf_Design_NaturalPose')]:
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 bodies={n:snap(bpy.data.objects[prefix+n]) for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']}
 parts=[o for o in bpy.data.collections['ElseIf_Primary_Construction'].objects if not o.hide_render]
 cloth={o.name:snap(bpy.data.objects[prefix+o.name]) for o in parts};collisions=[]
 for name,item in cloth.items():
  for bn,body in bodies.items():
   pairs=item['tree'].overlap(body['tree'])
   if pairs:collisions.append({'part':name,'body':bn,'pairs':len(pairs)})
 addon_collisions=[]
 for o in bpy.data.collections[colname].objects:
  item=snap(o)
  for bn,body in bodies.items():
   pairs=item['tree'].overlap(body['tree'])
   if pairs:addon_collisions.append({'part':o.name,'body':bn,'pairs':len(pairs)})
 hand_report={};body=bodies['ElseIf_Mannequin'];bodyob=bpy.data.objects[prefix+'ElseIf_Mannequin']
 for side in ['L','R']:
  vs=[];fs=[]
  for n in ['ElseIf_Jacket_Sleeve_'+side,'ElseIf_Jacket_Cuff_'+side]:
   item=cloth[n];count=len(bpy.data.objects[n].data.vertices);off=len(vs);vs+=item['vs'][:count];fs += [tuple(off+i for i in f) for f in item['polys'] if all(i<count for i in f)]
  tree=closed(vs,fs);ids={g.index for g in bodyob.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(t in g.name for t in ['Hand','Thumb','Index','Middle','Ring','Little'])};points=[p for p,gs in zip(body['vs'],body['groups']) if any(i in ids and w>.1 for i,w in gs)];hand_report[side]={'points':len(points),'outside':sum(not inside(tree,p) for p in points)}
 # 肌を出さない仕様を、パンツとソックスで覆う上腿領域で確認する。
 volumes=[]
 for n in ['ElseIf_Shorts','ElseIf_Sock_L','ElseIf_Sock_R']:
  o=bpy.data.objects[n];mods=[m for m in o.modifiers if m.type=='SOLIDIFY'];flags=[m.show_viewport for m in mods]
  try:
   for m in mods:m.show_viewport=False
   bpy.context.view_layer.update();s=snap(o);volumes.append(closed(s['vs'],s['polys']))
  finally:
   for m,f in zip(mods,flags):m.show_viewport=f
   bpy.context.view_layer.update()
 thighs=[p for p in body['vs'] if .735<p.z<.92 and .015<abs(p.x)<.18];uncovered=[p for p in thighs if not any(inside(t,p) for t in volumes)]
 report['poses'][scene.name]={'base_cloth_body_intersections':collisions,'design_body_intersections':addon_collisions,'hands':hand_report,'thigh_coverage':{'sample_points':len(thighs),'uncovered':len(uncovered),'sample':[list(p) for p in uncovered[:8]]},'render':{'engine':scene.render.engine,'resolution':[scene.render.resolution_x,scene.render.resolution_y,scene.render.resolution_percentage],'samples':scene.cycles.samples,'view':scene.view_settings.view_transform}}
bpy.context.window.scene=base
report['headsocket']={'exists':'HeadSocket' in bpy.data.objects,'type':bpy.data.objects['HeadSocket'].type,'constraints':[(m.name,m.type) for m in bpy.data.objects['HeadSocket'].constraints]}
report['shape_keys']={o.name:{k.name:k.value for k in o.data.shape_keys.key_blocks} for o in bpy.data.objects if o.type=='MESH' and o.data.shape_keys and 'ElseIf_Jacket' in o.name}
report['backup_collections']=[c.name for c in bpy.data.collections if any(w in c.name for w in ['Backup','Before','Approved','UNTOUCHED'])]
(OUT/'09_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(report,ensure_ascii=False))
