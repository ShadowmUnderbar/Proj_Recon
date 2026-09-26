import bpy,json,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925')
src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for s in list(bpy.data.scenes):bpy.context.window.scene=s;bpy.context.view_layer.update()
(P/'Protected_Before.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects},ensure_ascii=False))
configs=[('ElseIf_Basis_Construction','ElseIf_Master_Quality_Review','ElseIf_Master_Quality'),('ElseIf_NaturalPose_Validation','ElseIf_Master_Quality_NaturalPose','ElseIf_Master_Quality_NaturalPose')];mapping={};sceneinfo={}
for source,sn,cn in configs:
 src=bpy.data.scenes[source];bpy.context.window.scene=src;bpy.context.view_layer.update();wanted={o for o in src.objects if o.visible_get() and not o.hide_render}
 for o in src.objects:
  if o.type=='ARMATURE' or 'HeadSocket' in o.name:wanted.add(o)
 while True:
  dependencies=set()
  for o in wanted:
   if o.parent:dependencies.add(o.parent)
   for m in o.modifiers:
    if getattr(m,'object',None):dependencies.add(m.object)
   for c in o.constraints:
    if getattr(c,'target',None):dependencies.add(c.target)
  if dependencies<=wanted:break
  wanted|=dependencies
 s=src.copy();s.name=sn
 for c in list(s.collection.children):s.collection.children.unlink(c)
 for o in list(s.collection.objects):s.collection.objects.unlink(o)
 col=bpy.data.collections.new(cn);s.collection.children.link(col)
 for o in wanted:
  if o not in mapping:
   n=o.copy();n.name='Quality__'+o.name
   if o.data:n.data=o.data.copy()
   if n.animation_data and n.animation_data.action:n.animation_data.action=n.animation_data.action.copy()
   n['quality_source']=o.name;mapping[o]=n
  col.objects.link(mapping[o])
 s.camera=mapping[src.camera];sceneinfo[sn]={'source':source,'collection':cn}
for old,n in mapping.items():
 if old.parent in mapping:n.parent=mapping[old.parent]
 for m in n.modifiers:
  if getattr(m,'object',None) in mapping:m.object=mapping[m.object]
 for c in n.constraints:
  if getattr(c,'target',None) in mapping:c.target=mapping[c.target]
 if n.type=='ARMATURE':
  for pb in n.pose.bones:
   for c in pb.constraints:
    if getattr(c,'target',None) in mapping:c.target=mapping[c.target]
 if n.type=='MESH' and n.data.shape_keys and n.data.shape_keys.animation_data:
  for fc in n.data.shape_keys.animation_data.drivers:
   for var in fc.driver.variables:
    for t in var.targets:
     if t.id in mapping:t.id=mapping[t.id]
base=bpy.data.scenes['ElseIf_Master_Quality_Review'];bpy.context.window.scene=base;bpy.context.view_layer.update();rig=bpy.data.objects['Quality__ElseIf_Humanoid'];report={'bones':{},'surface':{}}
for side in ['L','R']:
 for part in ['Shoulder','UpperArm','LowerArm']:
  b=rig.data.bones['J_Bip_'+side+'_'+part];report['bones'][b.name]={'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)}
vs=[];fs=[]
for name in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
 o=bpy.data.objects['Quality__'+name];e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();off=len(vs);vs += [o.matrix_world@v.co for v in m.vertices];fs += [tuple(off+i for i in p.vertices) for p in m.polygons];e.to_mesh_clear()
tree=BVHTree.FromPolygons(vs,fs)
for name,origin,direction in [('clavicle',(.055,-.3,1.365),(0,1,0)),('shoulder_tip',(.4,.02525,1.355),(-1,0,0)),('shoulder_top',(.08915,.02525,1.55),(0,0,-1)),('upper_arm',(.125,-.3,1.285),(0,1,0)),('axilla',(.105,.02525,1.20),(0,0,1))]:
 hit=tree.ray_cast(Vector(origin),Vector(direction));report['surface'][name]=list(hit[0]) if hit[0] is not None else None
(P/'Anatomy_Measurements.json').write_text(json.dumps(report,indent=2));(P/'Duplicate_Map.json').write_text(json.dumps({o.name:n.name for o,n in mapping.items()},indent=2));(P/'Scene_Map.json').write_text(json.dumps(sceneinfo,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Master_LocalQuality.blend'));print(json.dumps({'objects_duplicated':len(mapping),'anatomy':report}))
