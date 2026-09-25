import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');scene=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=scene;rig=bpy.data.objects['ElseIf_Underarm_Humanoid'];frames=json.loads((OUT/'Pose_Frames.json').read_text());report={}
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];polys=[tuple(p.vertices) for p in m.polygons];gs=[[(g.group,g.weight) for g in v.groups] for v in m.vertices];e.to_mesh_clear();return vs,fs,polys,gs,BVHTree.FromPolygons(vs,fs,all_triangles=True)
def closed(vs,fs):
 m=bpy.data.meshes.new('_test');m.from_pydata(vs,[],fs);bm=bmesh.new();bm.from_mesh(m);bpy.data.meshes.remove(m);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0015);bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0);t=BVHTree.FromBMesh(bm);bm.free();return t
def inside(t,p):
 d=Vector((.963,.147,.223)).normalized();count=0;loc=p.copy()
 for i in range(40):
  h=t.ray_cast(loc,d,4)
  if h[0] is None:break
  count+=1;loc=h[0]+d*.00002
 return count%2==1
parts=[o for o in bpy.data.collections['ElseIf_Game_Optimized_Corrected'].objects if o.type=='MESH' and o['master_source'].startswith('ElseIf_Jacket_')];bodyob=bpy.data.objects['Fix__ElseIf_Mannequin']
for label,frame in frames.items():
 scene.frame_set(frame);bpy.context.view_layer.update();bodies={n:snap(bpy.data.objects['Fix__'+n]) for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']};cloth={o['master_source']:snap(o) for o in parts};collisions=[]
 for name,item in cloth.items():
  for bn,body in bodies.items():
   pairs=item[4].overlap(body[4])
   if pairs:
    samples=[sum((item[0][i] for i in item[1][a]),Vector())/3 for a,b in pairs];collisions.append({'cloth':name,'body':bn,'pairs':len(pairs),'bounds':[[min(p[i] for p in samples),max(p[i] for p in samples)] for i in range(3)]})
 hands={};body=bodies['ElseIf_Mannequin']
 for side in ['L','R']:
  vs=[];fs=[]
  for n in ['ElseIf_Jacket_Sleeve_'+side,'ElseIf_Jacket_Cuff_'+side]:
   item=cloth[n];count=len(bpy.data.objects[n].data.vertices);off=len(vs);vs+=item[0][:count];fs += [tuple(off+i for i in f) for f in item[2] if all(i<count for i in f)]
  t=closed(vs,fs);ids={g.index for g in bodyob.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(w in g.name for w in ['Hand','Thumb','Index','Middle','Ring','Little'])};pts=[p for p,gs in zip(body[0],body[3]) if any(i in ids and w>.1 for i,w in gs)];outside=[p for p in pts if not inside(t,p)];hands[side]={'points':len(pts),'outside':len(outside)}
 arms={}
 for side in ['L','R']:
  a=rig.pose.bones['J_Bip_'+side+'_UpperArm'];b=rig.pose.bones['J_Bip_'+side+'_LowerArm'];arms[side]={'upper_direction':list((a.tail-a.head).normalized()),'elbow_angle_deg':math.degrees((a.tail-a.head).angle(b.tail-b.head)),'wrist':list(b.tail)}
 report[label]={'frame':frame,'cloth_body_intersections':collisions,'hands':hands,'arms':arms}
 (OUT/'08_stress_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.frame_set(1);print(json.dumps({n:{'collision_pairs':sum(x['pairs'] for x in r['cloth_body_intersections']),'hands':r['hands']} for n,r in report.items()}))
