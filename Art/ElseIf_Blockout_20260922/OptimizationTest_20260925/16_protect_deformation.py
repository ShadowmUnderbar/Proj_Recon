import bpy,json,ast,bmesh,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;s.frame_set(1);rig=bpy.data.objects['ElseIf_Optimized_Humanoid'];rr=json.loads((P/'Regional_Reduction.json').read_text());surf=json.loads((P/'Surface_Comparison.json').read_text());names=sorted({o['object'] for p in surf.values() for o in p['objects'] if o['max_mm']>1.5});
for o in list(bpy.data.objects):
 if not o.users_collection and (any(o.name.startswith(n+'.') for n in names) or o.name.startswith('ElseIf_Optimized_Humanoid.')):bpy.data.objects.remove(o,do_unlink=True)
before=set(bpy.data.objects)
with bpy.data.libraries.load(str(P/'ElseIf_Corrected_BeforeReduction.blend'),link=False) as (fr,to):to.objects=list(names)
for name,loaded in zip(names,to.objects):
 target=bpy.data.objects[name];target.data=loaded.data.copy()
 if target.data.shape_keys and target.data.shape_keys.animation_data:
  for fc in target.data.shape_keys.animation_data.drivers:
   for var in fc.driver.variables:
    for t in var.targets:
     if t.id and t.id.type=='ARMATURE':t.id=rig
 # 削減前の補正を保持し、腕の影響と姿勢補正がある頂点を保護。
 m=target.data;attr=m.attributes.get('DLHN_DeformPriority') or m.attributes.new('DLHN_DeformPriority','INT','POINT')
 fn=next(n for n in ast.parse((P/'09_regional_reduce.py').read_text(encoding='utf-8-sig')).body if isinstance(n,ast.FunctionDef) and n.name=='priority');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<priority>','exec'))
 for v in m.vertices:
  k=priority(target['master_source'],v.co)
  if any(('Arm' in target.vertex_groups[g.group].name or 'Shoulder' in target.vertex_groups[g.group].name) and g.weight>.001 for g in v.groups):k=3
  if m.shape_keys and any((key.data[v.index].co-m.shape_keys.reference_key.data[v.index].co).length>.00002 for key in m.shape_keys.key_blocks if key.name!='Basis'):k=3
  attr.data[v.index].value=k
 bm=bmesh.new();bm.from_mesh(m);la=bm.verts.layers.int.get('DLHN_DeformPriority');low=[v for v in bm.verts if v[la]==1 and all(e.other_vert(v)[la]==1 for e in v.link_edges)];ls=set(low);edges=[e for e in bm.edges if all(v in ls for v in e.verts) and e.calc_length()<.018 and len(e.link_faces)==2]
 bmesh.ops.dissolve_limit(bm,angle_limit=math.radians(1.0),use_dissolve_boundaries=False,verts=low,edges=edges,delimit={'MATERIAL','SEAM','SHARP','UV'});bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4],quad_method='BEAUTY',ngon_method='BEAUTY');bm.to_mesh(m);bm.free();m.update();m.calc_loop_triangles()
 for r in rr:
  if r['object']==name:r['after_vertices']=len(m.vertices);r['after_triangles']=len(m.loop_triangles);r['extra_protection']='arm weights >0.001 or corrective displacement >0.02mm'
for o in list(bpy.data.objects):
 if o not in before:bpy.data.objects.remove(o,do_unlink=True)
(P/'Regional_Reduction.json').write_text(json.dumps(rr,indent=2));bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimization_Test.blend'));print('Refined:',names);print('Triangles',sum(r['after_triangles'] for r in rr))

