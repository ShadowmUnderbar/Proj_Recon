import bpy,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');components={int(k):v for k,v in json.loads((OUT/'Component_Map.json').read_text()).items()};src=ast.parse((OUT/'20_final_stress.py').read_text(encoding='utf-8-sig'));fn=next(n for n in src.body if isinstance(n,ast.FunctionDef) and n.name=='split_clothes');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<split>','exec'));report={}
def body(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[x.co.copy() for x in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];at=m.attributes.get('DLHN_OriginalBodyFace');ids=[at.data[t.polygon_index].value if at else t.polygon_index for t in m.loop_triangles];tree=BVHTree.FromPolygons(v,fs,all_triangles=True);e.to_mesh_clear();return tree,ids
for label,frame in json.loads((OUT/'Pose_Frames.json').read_text()).items():
 hits={}
 for sn,cn,prefix in [('ElseIf_Game_Backup_Review','ElseIf_Game_Backup','Backup__'),('ElseIf_LOD0_Review','ElseIf_Game_Optimized','LOD0__')]:
  s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(frame);bpy.context.view_layer.update();bs={n:body(bpy.data.objects[prefix+n]) for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']};cl={}
  if prefix=='LOD0__':cl={n:item[4] for n,item in split_clothes().items()}
  else:
   for o in bpy.data.collections[cn].objects:
    if o.type!='MESH' or not o['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_')):continue
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();cl[o['master_source']]=BVHTree.FromPolygons([v.co.copy() for v in m.vertices],[tuple(f.vertices) for f in m.polygons]);e.to_mesh_clear()
  found=set()
  for n,t in cl.items():
   for bn,(bt,ids) in bs.items():
    for a,b in t.overlap(bt):found.add((n,bn,ids[b]))
  hits[prefix]=found
 new=hits['LOD0__']-hits['Backup__'];report[label]={'baseline_intersected_body_faces':len(hits['Backup__']),'optimized_intersected_body_faces':len(hits['LOD0__']),'new_intersected_body_faces':len(new),'new_details':sorted(new)}
for sn in ['ElseIf_Game_Backup_Review','ElseIf_LOD0_Review']:bpy.data.scenes[sn].frame_set(1)
(OUT/'Penetration_Comparison.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
