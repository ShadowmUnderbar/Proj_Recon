import bpy,bmesh,json,ast,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s;s.frame_set(1);rig=bpy.data.objects['ElseIf_LOD0_Humanoid'];surf=json.loads((P/'Surface_Comparison.json').read_text());names={o['object'] for r in surf.values() for o in r['objects'] if o['max_mm']>1.5};rr=json.loads((P/'Regional_Reduction.json').read_text());fn=next(n for n in ast.parse((P/'07_regional_reduce.py').read_text(encoding='utf-8-sig')).body if isinstance(n,ast.FunctionDef) and n.name=='priority');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<priority>','exec'))
for name in names:
 o=bpy.data.objects[name];o.data=bpy.data.objects['Backup__'+o['master_source']].data.copy();m=o.data
 if m.shape_keys and m.shape_keys.animation_data:
  for fc in m.shape_keys.animation_data.drivers:
   for var in fc.driver.variables:
    for t in var.targets:
     if t.id and isinstance(t.id,bpy.types.Object) and t.id.type=='ARMATURE':t.id=rig
 a=m.attributes.get('DLHN_DeformPriority') or m.attributes.new('DLHN_DeformPriority','INT','POINT')
 for v in m.vertices:
  k=max(a.data[v.index].value,priority(o['master_source'],v.co))
  if any(('Arm' in o.vertex_groups[g.group].name or 'Shoulder' in o.vertex_groups[g.group].name) and g.weight>.001 for g in v.groups):k=3
  if m.shape_keys and any((key.data[v.index].co-m.shape_keys.reference_key.data[v.index].co).length>.00002 for key in m.shape_keys.key_blocks if key!=m.shape_keys.reference_key):k=3
  a.data[v.index].value=k
 bm=bmesh.new();bm.from_mesh(m);la=bm.verts.layers.int.get('DLHN_DeformPriority');low=[v for v in bm.verts if v[la]==1 and all(e.other_vert(v)[la]==1 for e in v.link_edges)];ls=set(low);edges=[e for e in bm.edges if all(v in ls for v in e.verts) and e.calc_length()<.018 and len(e.link_faces)==2];bmesh.ops.dissolve_limit(bm,angle_limit=math.radians(2),use_dissolve_boundaries=False,verts=low,edges=edges,delimit={'MATERIAL','SEAM','SHARP','UV'});bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4]);bm.to_mesh(m);bm.free();m.update();m.calc_loop_triangles()
 for r in rr:
  if r['object']==name:r['after_vertices']=len(m.vertices);r['after_triangles']=len(m.loop_triangles);r['protection']='Original priority, arm influences, all corrective support'
(P/'Regional_Reduction.json').write_text(json.dumps(rr,indent=2));print(sorted(names));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'))
