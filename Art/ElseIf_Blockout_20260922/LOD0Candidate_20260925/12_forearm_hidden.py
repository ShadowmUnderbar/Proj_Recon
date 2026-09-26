import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');scene=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=scene;scene.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized'];o=bpy.data.objects['LOD0__ElseIf_Mannequin'];rig=bpy.data.objects['ElseIf_LOD0_Humanoid'];records=json.loads((P/'Body_Deletion_Record.json').read_text());r=records[o.name];oldface=o.data.attributes['DLHN_OriginalBodyFace'];vmap=o.data.attributes['DLHN_OriginalBodyVertex'];candidate=set()
for f in o.data.polygons:
 okay=True
 for i in f.vertices:
  v=o.data.vertices[i];side='L' if v.co.x>0 else 'R';wrist=rig.data.bones['J_Bip_'+side+'_Hand'].head_local
  if not (.99<v.co.z<1.15 and abs(v.co.x)>.19 and (v.co-wrist).length>.060):okay=False;break
  if any(g.weight>.005 and any(t in o.vertex_groups[g.group].name for t in ['Hand','Thumb','Index','Middle','Ring','Little']) for g in v.groups):okay=False;break
 if okay:candidate.add(f.index)
def snap(ob):
 e=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[v.co.copy() for v in m.vertices];fs=[tuple(f.vertices) for f in m.polygons];e.to_mesh_clear();return vs,fs
dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z]
for frame in json.loads((P/'Pose_Frames.json').read_text()).values():
 scene.frame_set(frame);bpy.context.view_layer.update();cv=[];cf=[]
 for ob in col.objects:
  if ob.type!='MESH' or not ob['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_')):continue
  vs,fs=snap(ob);off=len(cv);cv+=vs;cf += [tuple(i+off for i in f) for f in fs]
 tree=BVHTree.FromPolygons(cv,cf);vs,fs=snap(o);passed=set()
 for fi in candidate:
  pts=[vs[i] for i in fs[fi]];pts.append(sum(pts,Vector())/len(pts))
  if all(tree.ray_cast(p+d*.00005,d,2)[0] is not None for p in pts for d in dirs):passed.add(fi)
 candidate=passed
scene.frame_set(1);extra={oldface.data[i].value for i in candidate};r['deleted_faces']=sorted(set(r['deleted_faces'])|extra);r['remaining_faces']=[i for i in range(len(r['faces'])) if i not in set(r['deleted_faces'])];oldpoints={vmap.data[i].value for fi in candidate for i in o.data.polygons[fi].vertices};r['protected_vertices']=[i for i in r['protected_vertices'] if i not in oldpoints];bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in candidate],context='FACES');bm.to_mesh(o.data);bm.free();o.data.update();counts=json.loads((P/'Body_Deletion_Counts.json').read_text());counts[o.name]['deleted_triangles']+=len(extra);counts[o.name]['deleted_faces']+=len(extra);counts[o.name]['after_triangles']-=len(extra);(P/'Body_Deletion_Record.json').write_text(json.dumps(records));(P/'Body_Deletion_Counts.json').write_text(json.dumps(counts,indent=2));print({'extra_forearm_triangles':len(extra)});bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'))
