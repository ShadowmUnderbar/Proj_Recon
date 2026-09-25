import bpy,json,math,ast
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');frames=json.loads((P/'Pose_Frames.json').read_text());cur=bpy.data.scenes['ElseIf_Current_Review'];opt=bpy.data.scenes['ElseIf_Optimized_Review'];col=bpy.data.collections['ElseIf_Game_Optimized_Test'];rig=bpy.data.objects['ElseIf_Optimized_Humanoid'];report={}
fn=next(n for n in ast.parse((P/'09_regional_reduce.py').read_text(encoding='utf-8-sig')).body if isinstance(n,ast.FunctionDef) and n.name=='priority');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<priority>','exec'))
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[x.co.copy() for x in m.vertices];f=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear();return v,f
pairs=[(bpy.data.objects['Game__'+o['master_source']],o) for o in col.objects if o.type=='MESH' and len(o.data.vertices)!=len(bpy.data.objects['Game__'+o['master_source']].data.vertices)]
for label,frame in frames.items():
 bpy.context.window.scene=cur;cur.frame_set(frame);bpy.context.view_layer.update();cv={a.name:snap(a)[0] for a,b in pairs}
 bpy.context.window.scene=opt;opt.frame_set(frame);bpy.context.view_layer.update();dist=[];detail=[]
 for a,b in pairs:
  v,f=snap(b);tree=BVHTree.FromPolygons(v,f,all_triangles=True);ds=[tree.find_nearest(cv[a.name][i])[3] for i,p in enumerate(a.data.vertices) if priority(a['master_source'],p.co)==1]
  if ds:dist.extend(ds);detail.append({'object':b.name,'max_mm':max(ds)*1000})
 key=bpy.data.objects['Opt__ElseIf_Jacket_Back'].data.shape_keys;activation={side:key.key_blocks['DLHN_Wide_ClothEase_'+side].value for side in ['L','R']}
 report[label]={'low_priority_max_surface_distance_mm':max(dist)*1000,'low_priority_mean_surface_distance_mm':sum(dist)/len(dist)*1000,'wide_ease_activation':activation,'objects':detail}
cur.frame_set(1);opt.frame_set(1);(P/'Surface_Comparison.json').write_text(json.dumps(report,indent=2));print({n:{k:v for k,v in r.items() if k!='objects'} for n,r in report.items()})
