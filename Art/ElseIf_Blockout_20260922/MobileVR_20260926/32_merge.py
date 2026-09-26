import bpy,json,numpy as np
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];frames=json.loads((P/'Pose_Frames.json').read_text());groups={'Body':['ElseIf_Mannequin','ElseIf_Anatomy_Restoration'],'Socks':['Socks_L','Socks_R'],'Shoes':['Shoes_L','Shoes_R']};objects=[bpy.data.objects['Mobile__'+n] for ns in groups.values() for n in ns];ref={};offset=0
for o in objects:
 a=o.data.attributes.get('Mobile_PreMerge_ID') or o.data.attributes.new('Mobile_PreMerge_ID','INT','POINT');a.data.foreach_set('value',list(range(offset,offset+len(o.data.vertices))));offset+=len(o.data.vertices)
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();positions=np.empty((offset,3),np.float32)
 for o in objects:
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();ids=np.array([a.value for a in m.attributes['Mobile_PreMerge_ID'].data]);positions[ids]=np.array([list(e.matrix_world@v.co) for v in m.vertices]);e.to_mesh_clear()
 ref[label]=positions
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_Final_Merge.blend'),copy=True);merged=[]
for group,names in groups.items():
 obs=[bpy.data.objects['Mobile__'+n] for n in names];assert all(not o.data.shape_keys for o in obs)
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 active=obs[0];bpy.context.view_layer.objects.active=active;bpy.ops.object.join();active.name='Mobile__'+group;merged.append(active)
 mats=list(active.data.materials);unique=[];mp={}
 for i,m in enumerate(mats):
  if m not in unique:unique.append(m)
  mp[i]=unique.index(m)
 idx=[mp[f.material_index] for f in active.data.polygons];active.data.materials.clear()
 for m in unique:active.data.materials.append(m)
 for f,i in zip(active.data.polygons,idx):f.material_index=i
report={}
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();maxerr=0
 for o in merged:
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();ids=np.array([a.value for a in m.attributes['Mobile_PreMerge_ID'].data]);ps=np.array([list(e.matrix_world@v.co) for v in m.vertices]);maxerr=max(maxerr,float(np.linalg.norm(ps-ref[label][ids],axis=1).max(initial=0)));e.to_mesh_clear()
 report[label]={'max_position_change_mm':maxerr*1000}
assert max(v['max_position_change_mm'] for v in report.values())<.001
s.frame_set(1);(P/'Final_Merge_Equivalence.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(json.dumps(report))
