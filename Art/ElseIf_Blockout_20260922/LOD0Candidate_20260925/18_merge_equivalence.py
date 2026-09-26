import bpy,json,numpy as np
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized'];rig=bpy.data.objects['ElseIf_LOD0_Humanoid'];components={int(k):v for k,v in json.loads((P/'Component_Map.json').read_text()).items()};before=set(bpy.data.objects);temp=bpy.data.collections.new('_LOD0_MergeVerification');s.collection.children.link(temp)
ids=sorted(components);names=[components[i]['object_before'] for i in ids]
with bpy.data.libraries.load(str(P/'Before_Renderer_Merge.blend'),link=False) as (fr,to):to.objects=names
loaded=dict(zip(ids,to.objects))
for cid,o in loaded.items():
 temp.objects.link(o);o.hide_render=True
 for m in o.modifiers:
  if m.type=='ARMATURE':m.object=rig
 if o.data.shape_keys and o.data.shape_keys.animation_data:
  for fc in o.data.shape_keys.animation_data.drivers:
   for var in fc.driver.variables:
    for t in var.targets:
     if t.id and isinstance(t.id,bpy.types.Object) and t.id.type=='ARMATURE':t.id=rig
fixes={}
for o in col.objects:
 if o.type!='MESH' or not o.data.shape_keys:continue
 ca=o.data.attributes['DLHN_Component'];va=o.data.attributes['DLHN_ComponentVertex'];mx=0;count=0
 for k in o.data.shape_keys.key_blocks:
  for i in range(len(o.data.vertices)):
   src=loaded[ca.data[i].value];j=va.data[i].value;sk=src.data.shape_keys
   target=(sk.key_blocks.get(k.name) or sk.reference_key).data[j].co if sk else src.data.vertices[j].co
   d=(k.data[i].co-target).length
   if d>1e-8:k.data[i].co=target;count+=1;mx=max(mx,d)
 fixes[o.name]={'repaired_key_vertices':count,'max_key_data_difference_mm':mx*1000}
frames=json.loads((P/'Pose_Frames.json').read_text());report={}
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();ref={}
 for cid,o in loaded.items():
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',a);ref[cid]=a.reshape(-1,3).copy();e.to_mesh_clear()
 maximum=0;details={}
 for o in col.objects:
  if o.type!='MESH':continue
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',a);a=a.reshape(-1,3);cid=np.empty(len(m.vertices),np.int32);idx=np.empty(len(m.vertices),np.int32);m.attributes['DLHN_Component'].data.foreach_get('value',cid);m.attributes['DLHN_ComponentVertex'].data.foreach_get('value',idx)
  for c in np.unique(cid):
   mask=cid==c;d=float(np.linalg.norm(a[mask]-ref[int(c)][idx[mask]],axis=1).max(initial=0));maximum=max(maximum,d)
   if d>1e-6:details[components[int(c)]['source']]=d*1000
  e.to_mesh_clear()
 report[label]={'max_position_change_mm':maximum*1000,'nonzero_components':details}
(P/'Merge_Equivalence_Final.json').write_text(json.dumps({'shape_data_repair':fixes,'poses':report},indent=2));s.frame_set(1)
for o in list(bpy.data.objects):
 if o not in before:bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(temp);bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'));print(json.dumps({'shape_data_repair':fixes,'poses':report}))
