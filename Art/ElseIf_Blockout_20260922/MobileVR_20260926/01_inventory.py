import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926')
r={'file':bpy.data.filepath,'scene':bpy.context.scene.name,'meshes':[],'keys':[]}
col=bpy.data.collections.get('ElseIf_Game_Optimized')
assert col,'Approved optimized collection missing'
for o in col.objects:
 if o.type!='MESH':continue
 m=o.data;m.calc_loop_triangles()
 r['meshes'].append({'name':o.name,'vertices':len(m.vertices),'triangles':len(m.loop_triangles),'modifiers':[(x.name,x.type) for x in o.modifiers],'materials':[s.material.name if s.material else None for s in o.material_slots]})
 if m.shape_keys:
  sk=m.shape_keys
  for k in sk.key_blocks:
   r['keys'].append({'name':k.name,'value':k.value,'changed_vertices':sum((v.co-b.co).length>1e-7 for v,b in zip(k.data,sk.key_blocks[0].data)),'drivers':[{'path':fc.data_path,'expression':fc.driver.expression,'variables':[{'name':v.name,'targets':[{'id':t.id.name if t.id else None,'bone':t.bone_target,'path':t.data_path} for t in v.targets]} for v in fc.driver.variables]} for fc in sk.animation_data.drivers if k.path_from_id() in fc.data_path] if sk.animation_data else []})
r['meshes'].sort(key=lambda x:-x['triangles']);total=sum(x['triangles'] for x in r['meshes'])
for x in r['meshes']:x['percent']=100*x['triangles']/total
(P/'Initial_Inventory.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
