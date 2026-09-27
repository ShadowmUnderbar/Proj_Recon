import bpy,json,hashlib,struct,ast,numpy as np
from pathlib import Path
from mathutils import Matrix
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;s.frame_set(1);root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];root.rotation_euler=(0,0,0);bpy.context.view_layer.update();col=bpy.data.collections['ElseIf_HeadPart_Monitor01'];objects=[o for o in col.objects if o.type=='MESH'];rows=[]
for o in objects:
 o.data.calc_loop_triangles();rows.append({'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials],'location':list(o.location),'rotation':list(o.rotation_euler),'scale':list(o.scale)})
# 同じHeadSocketへローカル初期値で装着されることを記録。
report={'meshes':rows,'vertices':sum(r['vertices'] for r in rows),'triangles':sum(r['triangles'] for r in rows),'mesh_count':len(rows),'material_count':len({m for o in objects for m in o.data.materials}),'material_slots':sum(len(o.data.materials) for o in objects),'socket_name':root.parent.name,'socket_world_matrix':[list(r) for r in root.parent.matrix_world],'root_local_location':list(root.location),'root_local_rotation':list(root.rotation_euler),'root_local_scale':list(root.scale),'monitor_material':'ElseIf_FaceMonitor','monitor_uv':'FaceUV','textures':[{'name':i.name,'size':list(i.size),'packed':bool(i.packed_file)} for i in bpy.data.images if i.name.startswith('ElseIf_Expression_Test_')],'casing_world_top':max((bpy.data.objects['ElseIf_HeadShell'].matrix_world@v.co).z for v in bpy.data.objects['ElseIf_HeadShell'].data.vertices)}
# 既存BodyとMaster、既存カメラを保全記録と照合する。
code=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(code).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for sc in list(bpy.data.scenes):bpy.context.window.scene=sc;bpy.context.view_layer.update()
before=json.loads((P/'Protected_Before.json').read_text());changed=[n for n,r in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=r];report['protected_existing_objects_checked']=len(before);report['protected_existing_changes']=changed;assert not changed,changed
bpy.context.window.scene=s;bpy.context.view_layer.update();camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];deps=bpy.context.evaluated_depsgraph_get();projection=np.array(camera.calc_matrix_camera(deps,x=880,y=1120))@np.array(camera.matrix_world.inverted());points=[]
for o in list(bpy.data.collections['ElseIf_Game_MobileVR_Test'].objects)+objects:
 if o.type!='MESH':continue
 e=o.evaluated_get(deps);m=e.to_mesh();coords=np.ones((len(m.vertices),4));coords[:,:3]=np.array([list(v.co) for v in m.vertices]);clip=coords@np.array(e.matrix_world).T@projection.T;ndc=clip[:,:2]/clip[:,3:4];points.extend(np.column_stack(((ndc[:,0]+1)*440,(1-ndc[:,1])*560)).tolist());e.to_mesh_clear()
a=np.array(points);lo=np.floor(a.min(axis=0)-6).astype(int);hi=np.ceil(a.max(axis=0)+6).astype(int);report['highangle_crop']=[int(lo[0]),int(lo[1]),int(hi[0]-lo[0]),int(hi[1]-lo[1])]
(P/'Final_Statistics.json').write_text(json.dumps(report,indent=2))
# 再利用用の頭部Collectionだけを原点基準で保存。身体は書き出さない。
parent=root.parent;inverse=root.matrix_parent_inverse.copy();basis=root.matrix_basis.copy()
try:
 root.parent=None;root.matrix_parent_inverse=Matrix.Identity(4);root.matrix_basis=Matrix.Identity(4)
 assets={col}|{i for i in bpy.data.images if i.name.startswith('ElseIf_Expression_Test_')};bpy.data.libraries.write(str(P/'ElseIf_HeadPart_Monitor01.blend'),assets,path_remap='RELATIVE',fake_user=True,compress=True)
finally:root.parent=parent;root.matrix_parent_inverse=inverse;root.matrix_basis=basis
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_MobileVR_HeadPart01.blend'));print(json.dumps(report))
