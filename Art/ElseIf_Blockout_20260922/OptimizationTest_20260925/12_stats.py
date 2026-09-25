import bpy,json,ast,hashlib,struct
from pathlib import Path
from mathutils.kdtree import KDTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');old=P.parent/'GameStage1_20260924';cur=bpy.data.collections['ElseIf_Game_Current'];opt=bpy.data.collections['ElseIf_Game_Optimized_Test'];stats={}
def count(obs,evaluated):
 result={'vertices':0,'triangles':0,'material_count':0,'shape_key_instances_including_basis':0,'shape_key_instances_excluding_basis':0,'shape_key_unique_names':[]};mats=set();names=set();basis=0
 for o in obs:
  if o.type!='MESH':continue
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get()) if evaluated else None;m=e.to_mesh() if e else o.data;m.calc_loop_triangles();result['vertices']+=len(m.vertices);result['triangles']+=len(m.loop_triangles);mats.update(x.name for x in m.materials if x)
  if e:e.to_mesh_clear()
  if o.data.shape_keys:
   result['shape_key_instances_including_basis']+=len(o.data.shape_keys.key_blocks);basis+=1
   for k in o.data.shape_keys.key_blocks:
    if k.name!='Basis':names.add(k.name)
 result['shape_key_instances_excluding_basis']=result['shape_key_instances_including_basis']-basis;result['shape_key_unique_names']=sorted(names);result['material_count']=len(mats);return result
for sn,label,objects in [('ElseIf_Basis_Construction','Master',[bpy.data.objects[o['master_source']] for o in cur.objects if o.type=='MESH']),('ElseIf_Current_Review','Current',list(cur.objects)),('ElseIf_Optimized_Review','Optimized',list(opt.objects))]:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(1);bpy.context.view_layer.update();stats[label]=count(objects,label=='Master')
for label,rn in [('Current','ElseIf_Game_Humanoid'),('Optimized','ElseIf_Optimized_Humanoid')]:
 rig=bpy.data.objects[rn];stats[label]['bone_count']=len(rig.data.bones);stats[label]['auxiliary_bones']=[b.name for b in rig.data.bones if b.name.startswith('DLHN_')];stats[label]['corrective_instances']=stats[label]['shape_key_instances_excluding_basis']
stats['current_reduction_percent']=(1-stats['Optimized']['triangles']/stats['Current']['triangles'])*100;stats['master_reduction_percent']=(1-stats['Optimized']['triangles']/stats['Master']['triangles'])*100
# 保護頂点が残っていることを座標で確認。
protected=[]
for o in opt.objects:
 if o.type!='MESH':continue
 source=bpy.data.objects.get('Game__'+o['master_source']);attr=o.data.attributes.get('DLHN_DeformPriority');kd=KDTree(len(o.data.vertices))
 for i,v in enumerate(o.data.vertices):kd.insert(v.co,i)
 kd.balance();err=0;cnt=0
 # 元頂点の分類は今回の削減処理と同じ関数を使う。
 text=(P/'09_regional_reduce.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(text).body if isinstance(n,ast.FunctionDef) and n.name=='priority');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<priority>','exec'))
 for v in source.data.vertices:
  if priority(o['master_source'],v.co)>=2:cnt+=1;err=max(err,kd.find(v.co)[2])
 protected.append({'object':o.name,'protected_vertices':cnt,'max_missing_distance_mm':err*1000})
stats['protected_vertices_max_error_mm']=max(x['max_missing_distance_mm'] for x in protected)
for s in bpy.data.scenes:bpy.context.window.scene=s;bpy.context.view_layer.update()
src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
before=json.loads((old/'Master_Audit_Before.json').read_text());stats['master_changes']=[n for n,r in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=r]
(P/'Statistics.json').write_text(json.dumps(stats,indent=2));(P/'Protected_Vertices.json').write_text(json.dumps(protected,indent=2));bpy.context.window.scene=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimization_Test.blend'));print(json.dumps(stats))
