import bpy,json,math,ast,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);col=bpy.data.collections['ElseIf_Game'];frames=json.loads((OUT/'Pose_Frames.json').read_text());bodies=[bpy.data.objects['Game__'+n] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']]
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices) for p in m.polygons];e.to_mesh_clear();return vs,fs
candidates={}
for o in bodies:
 selected=set()
 for f in o.data.polygons:
  points=[o.data.vertices[i].co for i in f.vertices]
  if all((.94<p.z<1.17 and abs(p.x)<.065) or (.26<p.z<.68 and .030<abs(p.x)<.12) or (.025<p.z<.075 and .035<abs(p.x)<.11) for p in points):selected.add(f.index)
 candidates[o.name]=selected
clothes=[o for o in col.objects if o.type=='MESH' and o['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_'))];dirs=[Vector(v).normalized() for v in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1),(1,1,1),(-1,-1,1)]]
for label,frame in frames.items():
 scene.frame_set(frame);bpy.context.view_layer.update();vs=[];fs=[]
 for o in clothes:
  v,f=snap(o);off=len(vs);vs+=v;fs.extend(tuple(off+i for i in p) for p in f)
 tree=BVHTree.FromPolygons(vs,fs)
 for o in bodies:
  v,f=snap(o);remove=[]
  for idx in candidates[o.name]:
   pts=[v[i] for i in f[idx]];center=sum(pts,Vector())/len(pts)
   if not all(tree.ray_cast(p,d,1.0)[0] is not None for p in [center]+pts for d in dirs):remove.append(idx)
  candidates[o.name].difference_update(remove)
scene.frame_set(1);classification={}
for o in bodies:
 attr=o.data.attributes.get('DLHN_HiddenCandidate_TestedPoses') or o.data.attributes.new('DLHN_HiddenCandidate_TestedPoses','INT','FACE')
 for d in attr.data:d.value=0
 for idx in candidates[o.name]:attr.data[idx].value=1
 classification[o.name]={'candidate_faces':len(candidates[o.name]),'candidate_triangles':sum(len(o.data.polygons[i].vertices)-2 for i in candidates[o.name]),'deleted_faces':0}
(OUT/'Hidden_Surface_Candidates.json').write_text(json.dumps(classification,indent=2))
# Master全オブジェクトとコレクション所属を編集前の指紋と比較。
src=(OUT.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
before=json.loads((OUT/'Master_Audit_Before.json').read_text(encoding='utf-8'));changes=[n for n,r in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=r];cb=json.loads((OUT/'Master_Collections_Before.json').read_text());cchanges=[n for n,r in cb.items() if n not in bpy.data.collections or sorted(o.name for o in bpy.data.collections[n].objects)!=r]
objects=[o for o in col.objects if o.type=='MESH'];tris=0;faces=0;verts=0;mats=set();used=set();keynames=set();keyinstances=0;maxinf=0
for o in objects:
 o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles);faces+=len(o.data.polygons);verts+=len(o.data.vertices);mats.update(m.name for m in o.data.materials if m)
 for v in o.data.vertices:
  gs=[g for g in v.groups if g.weight>.00001 and o.vertex_groups[g.group].name in bpy.data.objects['ElseIf_Game_Humanoid'].data.bones];maxinf=max(maxinf,len(gs));used.update(o.vertex_groups[g.group].name for g in gs if o.vertex_groups[g.group].name in bpy.data.objects['ElseIf_Game_Humanoid'].data.bones)
 if o.data.shape_keys:
  for k in o.data.shape_keys.key_blocks:
   if k.name!='Basis':keynames.add(k.name);keyinstances+=1
stats={'master_triangles':665037,'game_triangles':tris,'game_polygons':faces,'game_vertices':verts,'reduction_percent':(1-tris/665037)*100,'materials':sorted(mats),'material_count':len(mats),'rig_bones':len(bpy.data.objects['ElseIf_Game_Humanoid'].data.bones),'weighted_bones':len(used),'weighted_bone_names':sorted(used),'auxiliary_bones':['DLHN_SleeveTip_L','DLHN_SleeveTip_R'],'corrective_key_names':sorted(keynames),'corrective_instances':keyinstances,'max_vertex_influences':maxinf,'master_object_changes':changes,'master_collection_changes':cchanges,'hidden_candidates':classification}
(OUT/'Stage1_Statistics.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print(json.dumps(stats))

