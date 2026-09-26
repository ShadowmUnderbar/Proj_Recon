import bpy,json,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for s in list(bpy.data.scenes):bpy.context.window.scene=s;bpy.context.view_layer.update()
(P/'Protected_Before.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects}))
def clone(src,base,cn,sn,prefix,rig_name):
 col=bpy.data.collections.new(cn);col.use_fake_user=True;mp={}
 for o in src.objects:
  n=o.copy();n.name=prefix+o.get('master_source',o.name);col.objects.link(n)
  if o.data:n.data=o.data.copy()
  if n.animation_data and n.animation_data.action:n.animation_data.action=n.animation_data.action.copy()
  mp[o]=n
 for o,n in mp.items():
  if o.parent in mp:n.parent=mp[o.parent]
  for m in n.modifiers:
   if getattr(m,'object',None) in mp:m.object=mp[m.object]
  cs=list(n.constraints)+([c for pb in n.pose.bones for c in pb.constraints] if n.type=='ARMATURE' else [])
  for c in cs:
   if getattr(c,'target',None) in mp:c.target=mp[c.target]
  for data in [n,n.data.shape_keys if n.type=='MESH' else None]:
   if data and data.animation_data:
    for fc in data.animation_data.drivers:
     for v in fc.driver.variables:
      for t in v.targets:
       if t.id in mp:t.id=mp[t.id]
 rig=next(n for n in mp.values() if n.type=='ARMATURE');rig.name=rig_name
 s=base.copy();s.name=sn
 for c in list(s.collection.children):s.collection.children.unlink(c)
 s.collection.children.link(bpy.data.collections['ElseIf_Review_Stage']);s.collection.children.link(col);bpy.context.window.scene=s;s.frame_set(1);bpy.context.view_layer.update();return col,s,rig
source=bpy.data.collections['ElseIf_Game_Optimized_Corrected'];base=bpy.data.scenes['ElseIf_Underarm_Review'];base.frame_set(1)
backup,bs,br=clone(source,base,'ElseIf_Game_Backup','ElseIf_Game_Backup_Review','Backup__','ElseIf_Backup_Humanoid')
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear();return vs,fs,BVHTree.FromPolygons(vs,fs,all_triangles=True)
changes=json.loads((P.parent/'MasterLocalQuality_20260925/Changes.json').read_text());names={n.removeprefix('Quality__') for n in changes if not n.startswith('Quality__NaturalPose__')};old={};new={}
bpy.context.window.scene=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.view_layer.update()
for n in names:old[n]=snap(bpy.data.objects[n])
bpy.context.window.scene=bpy.data.scenes['ElseIf_Master_Quality_Review'];bpy.context.view_layer.update()
for n in names:new[n]=snap(bpy.data.objects['Quality__'+n])
bpy.context.window.scene=bs;bpy.context.view_layer.update();report={}
for o in backup.objects:
 n=o.get('master_source','')
 if o.type!='MESH' or n not in names:continue
 ov,fs,tree=old[n];nv,_,_=new[n];mx=0;count=0
 for v in o.data.vertices:
  p=o.matrix_world@v.co;hit=tree.find_nearest(p)
  if hit[3]>.025:continue
  a,b,c=fs[hit[2]];mapped=barycentric_transform(hit[0],ov[a],ov[b],ov[c],nv[a],nv[b],nv[c]);d=o.matrix_world.to_3x3().inverted()@(mapped-hit[0])
  if d.length<1e-8:continue
  if o.data.shape_keys:
   for k in o.data.shape_keys.key_blocks:k.data[v.index].co+=d
  else:v.co+=d
  mx=max(mx,d.length);count+=1
 o.data.update();report[o.name]={'vertices_changed':count,'max_delta_mm':mx*1000}
(P/'Appearance_Transfer.json').write_text(json.dumps(report,indent=2));(P/'Pose_Frames.json').write_text((P.parent/'UnderarmCorrection_20260925/Pose_Frames.json').read_text());bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_Backup.blend'),copy=True)
col,s,rig=clone(backup,bs,'ElseIf_Game_Optimized','ElseIf_LOD0_Review','LOD0__','ElseIf_LOD0_Humanoid');bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'));print(json.dumps({'baseline':'Verified UnderarmCorrected Game + approved Master local quality deltas','transferred_objects':len(report),'optimized_collection':col.name,'backup_collection':backup.name}))
