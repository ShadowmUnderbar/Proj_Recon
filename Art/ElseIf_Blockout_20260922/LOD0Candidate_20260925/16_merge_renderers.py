import bpy,json,hashlib,numpy as np
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized'];meshes=sorted([o for o in col.objects if o.type=='MESH'],key=lambda o:o.name);frames=json.loads((P/'Pose_Frames.json').read_text());reference={};components={}
for cid,o in enumerate(meshes):
 assert all(m.type=='ARMATURE' for m in o.modifiers),o.name
 assert np.max(np.abs(np.array(o.matrix_world)-np.eye(4)))<1e-6,o.name
 for attr in ['DLHN_Component','DLHN_ComponentVertex']:
  a=o.data.attributes.get(attr) or o.data.attributes.new(attr,'INT','POINT');a.data.foreach_set('value',[cid]*len(o.data.vertices) if attr=='DLHN_Component' else list(range(len(o.data.vertices))))
 components[cid]={'source':o['master_source'],'object_before':o.name,'vertices':len(o.data.vertices)}
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();reference[label]={}
 for cid,o in enumerate(meshes):
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',a);reference[label][cid]=a.reshape(-1,3).copy();e.to_mesh_clear()
s.frame_set(1);bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_Renderer_Merge.blend'),copy=True)
def driver_info(o,k):
 a=o.data.shape_keys.animation_data
 fc=a.drivers.find(k.path_from_id('value')) if a else None
 if not fc:return None
 d=fc.driver;vs=[]
 for v in d.variables:
  ts=[]
  for t in v.targets:
   ts.append({n:(getattr(t,n).name if n=='id' and getattr(t,n) else getattr(t,n)) for n in ['id_type','id','data_path','bone_target','transform_type','transform_space','rotation_mode'] if hasattr(t,n)})
  vs.append({'name':v.name,'type':v.type,'targets':ts})
 return {'type':d.type,'expression':d.expression,'use_self':d.use_self,'variables':vs}
def restore_driver(k,spec):
 if not spec:return
 k.driver_remove('value');d=k.driver_add('value').driver;d.type=spec['type'];d.expression=spec['expression'];d.use_self=spec['use_self']
 for v in spec['variables']:
  var=d.variables.new();var.name=v['name'];var.type=v['type']
  for t,ts in zip(var.targets,v['targets']):
   for prop,val in ts.items():
    try:setattr(t,prop,bpy.data.objects.get(val) if prop=='id' and val else val)
    except (AttributeError,TypeError):pass
def category(o):
 n=o['master_source']
 if n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:return n
 if 'Shoe_L_' in n:return 'Shoes_L'
 if 'Shoe_R_' in n:return 'Shoes_R'
 if n.startswith('ElseIf_Shorts') or n.startswith('EL_Thigh_'):return 'Shorts'
 if 'Sock_L' in n:return 'Socks_L'
 if 'Sock_R' in n:return 'Socks_R'
 return 'Jacket'
groups={}
for o in meshes:groups.setdefault(category(o),[]).append(o)
joinreport=[]
for group,objects in groups.items():
 if len(objects)<2:continue
 keys={};signatures={}
 for o in objects:
  if not o.data.shape_keys:continue
  for k in o.data.shape_keys.key_blocks:
   if k==o.data.shape_keys.reference_key:continue
   d=driver_info(o,k);sig=hashlib.sha256(json.dumps(d,sort_keys=True).encode()).hexdigest()[:8];signatures.setdefault(k.name,set()).add(sig);keys[(o.name,k.name)]={'driver':d,'sig':sig,'value':k.value,'slider_min':k.slider_min,'slider_max':k.slider_max}
 specs={}
 for o in objects:
  if not o.data.shape_keys:continue
  for k in list(o.data.shape_keys.key_blocks):
   if k==o.data.shape_keys.reference_key:continue
   spec=keys[(o.name,k.name)];name=k.name if len(signatures[k.name])==1 else k.name+'_'+spec['sig'];k.name=name;specs[name]=spec
 active=max(objects,key=lambda o:len(o.data.vertices))
 if specs and not active.data.shape_keys:active.shape_key_add(name='Basis')
 for name,spec in specs.items():
  if name not in active.data.shape_keys.key_blocks:active.shape_key_add(name=name,from_mix=False)
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=active;count=len(objects);bpy.ops.object.join();active.name='LOD0__'+group;active['master_sources']=json.dumps([components[i]['source'] for i in components if components[i]['object_before'] in [x for x in keys.keys()]]) if False else group
 if active.data.shape_keys:
  for name,spec in specs.items():
   k=active.data.shape_keys.key_blocks[name];k.slider_min=spec['slider_min'];k.slider_max=spec['slider_max'];k.value=spec['value'];restore_driver(k,spec['driver'])
 old=list(active.data.materials);unique=[];remap={}
 for i,m in enumerate(old):
  if m not in unique:unique.append(m)
  remap[i]=unique.index(m)
 indices=[remap[p.material_index] for p in active.data.polygons];active.data.materials.clear()
 for m in unique:active.data.materials.append(m)
 for p,idx in zip(active.data.polygons,indices):p.material_index=idx
 joinreport.append({'group':group,'objects_before':count,'objects_after':1,'material_slots_after':len(unique),'shape_keys_without_basis':len(specs)})
verification={}
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();maximum=0;total=0
 for o in col.objects:
  if o.type!='MESH':continue
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',a);a=a.reshape(-1,3);cid=np.empty(len(m.vertices),np.int32);idx=np.empty(len(m.vertices),np.int32);m.attributes['DLHN_Component'].data.foreach_get('value',cid);m.attributes['DLHN_ComponentVertex'].data.foreach_get('value',idx)
  for c in np.unique(cid):
   mask=cid==c;ds=np.linalg.norm(a[mask]-reference[label][int(c)][idx[mask]],axis=1);maximum=max(maximum,float(ds.max(initial=0)));total+=len(ds)
  e.to_mesh_clear()
 verification[label]={'tested_vertices':total,'max_position_change_mm':maximum*1000}
s.frame_set(1);(P/'Component_Map.json').write_text(json.dumps(components,indent=2));(P/'Renderer_Merge.json').write_text(json.dumps({'groups':joinreport,'pose_equivalence':verification},indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'));print(json.dumps({'groups':joinreport,'pose_equivalence':verification}))
