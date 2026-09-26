import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');scene=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=scene;scene.frame_set(1);col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];rig=bpy.data.objects['ElseIf_MobileVR_Humanoid'];frames=json.loads((P/'Pose_Frames.json').read_text())
bodies=[bpy.data.objects['Mobile__'+n] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']];clothes=[o for o in col.objects if o.type=='MESH' and o.name not in [b.name for b in bodies]]
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=[x.co.copy() for x in m.vertices];f=[tuple(p.vertices) for p in m.polygons];e.to_mesh_clear();return v,f
keep={};candidate={};maps={}
for o in bodies:
 protected=set()
 for v in o.data.vertices:
  p=v.co;side='L' if p.x>=0 else 'R';sh=rig.data.bones['J_Bip_'+side+'_UpperArm'].head_local;wrist=rig.data.bones['J_Bip_'+side+'_Hand'].head_local
  hand=any(g.weight>.005 and any(t in o.vertex_groups[g.group].name for t in ['Hand','Thumb','Index','Middle','Ring','Little']) for g in v.groups)
  border=any(abs(p.z-z)<width for z,width in [(.727,.025),(.920,.022),(.216,.022),(1.075,.018)])
  if hand or (p.z>1.325 and abs(p.x)<.105) or (p-wrist).length<.035 or border:protected.add(v.index)
 keep[o.name]=protected;candidate[o.name]={f.index for f in o.data.polygons if not any(i in protected for i in f.vertices)}
 attr=o.data.attributes.get('DLHN_OriginalBodyVertex') or o.data.attributes.new('DLHN_OriginalBodyVertex','INT','POINT')
 for v in o.data.vertices:attr.data[v.index].value=v.index
 fa=o.data.attributes.get('DLHN_OriginalBodyFace') or o.data.attributes.new('DLHN_OriginalBodyFace','INT','FACE')
 for f in o.data.polygons:fa.data[f.index].value=f.index
 maps[o.name]={'vertices':[list(v.co) for v in o.data.vertices],'faces':[list(f.vertices) for f in o.data.polygons],'protected_vertices':sorted(protected)}
dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z]
for label,frame in frames.items():
 scene.frame_set(frame);bpy.context.view_layer.update();cv=[];cf=[]
 for o in clothes:
  v,f=snap(o);offset=len(cv);cv+=v;cf.extend(tuple(i+offset for i in p) for p in f)
 for body in bodies:
  v,f=snap(body);offset=len(cv);cv+=v
  cf.extend(tuple(i+offset for i in face) for face in f if all(i in keep[body.name] for i in face))
 tree=BVHTree.FromPolygons(cv,cf)
 for o in bodies:
  vs,fs=snap(o);ids={i for fi in candidate[o.name] for i in fs[fi]};good={}
  for i in ids:good[i]=all(tree.ray_cast(vs[i]+d*.00005,d,2)[0] is not None for d in dirs)
  surviving=set()
  for fi in candidate[o.name]:
   face=fs[fi]
   if not all(good[i] for i in face):continue
   center=sum((vs[i] for i in face),Vector())/len(face)
   if all(tree.ray_cast(center+d*.00005,d,2)[0] is not None for d in dirs):surviving.add(fi)
  candidate[o.name]=surviving
 print(label,{n:len(v) for n,v in candidate.items()},flush=True)
scene.frame_set(1);result={}
for o in bodies:
 m=o.data;geokey={v.index:tuple(round(c,5) for c in v.co) for v in m.vertices};edgefaces={}
 for f in m.polygons:
  for a,b in f.edge_keys:edgefaces.setdefault(tuple(sorted((geokey[a],geokey[b]))),set()).add(f.index)
 adj=[set() for f in m.polygons]
 for fs in edgefaces.values():
  for i in fs:adj[i].update(fs-{i})
 selected=candidate[o.name]
 # 衣服境界と不確実領域から、人体側へ2面リング残す。
 for _ in range(2):selected={i for i in selected if all(j in selected for j in adj[i])}
 before=sum(len(f.vertices)-2 for f in m.polygons);deleted=sum(len(m.polygons[i].vertices)-2 for i in selected)
 maps[o.name]['deleted_faces']=sorted(selected);maps[o.name]['remaining_faces']=[f.index for f in m.polygons if f.index not in selected]
 bm=bmesh.new();bm.from_mesh(m);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in selected],context='FACES');bm.to_mesh(m);bm.free();m.update();m.calc_loop_triangles();result[o.name]={'before_triangles':before,'deleted_triangles':deleted,'after_triangles':len(m.loop_triangles),'deleted_faces':len(selected)}
(P/'Body_Deletion_Record.json').write_text(json.dumps(maps));(P/'Body_Deletion_Counts.json').write_text(json.dumps(result,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(result)
