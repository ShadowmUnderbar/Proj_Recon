import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');scene=bpy.data.scenes['ElseIf_Underarm_Review'];rig=bpy.data.objects['ElseIf_Underarm_Humanoid'];frames=json.loads((OUT/'Pose_Frames.json').read_text());col=bpy.data.collections['ElseIf_Game_Optimized_Corrected']
class Surface:
 def __init__(self,tree,ids):self.tree=tree;self.ids=ids
 def overlap(self,other):return [(self.ids[a],other.ids[b]) for a,b in self.tree.overlap(other.tree)]
 def find_nearest(self,p):return self.tree.find_nearest(p)
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];ids=[t.polygon_index for t in m.loop_triangles];e.to_mesh_clear();return vs,Surface(BVHTree.FromPolygons(vs,fs,all_triangles=True),ids)

def add_driver(key,side,targets):
 d=key.driver_add('value').driver;d.type='SCRIPTED';terms=[];base=0
 for short,bn in [('u','UpperArm'),('l','LowerArm'),('h','Hand')]:
  quat=targets[bn];base+=1-quat[0]*quat[0];pieces=[]
  for i in range(4):
   v=d.variables.new();v.name=short+str(i);v.type='SINGLE_PROP';v.targets[0].id=rig;v.targets[0].data_path='pose.bones["J_Bip_'+side+'_'+bn+'"].rotation_quaternion['+str(i)+']';pieces.append('('+format(quat[i],'.6f')+'*'+v.name+')')
  terms.append('(1-('+('+'.join(pieces))+')**2)')
 radius=min(.055,max(.0003,base*.65));d.expression='max(0,1-('+('+'.join(terms))+')/'+str(radius)+')**2'
def solve(label):
 scene.frame_set(frames[label]);bpy.context.view_layer.update();targets={side:{bn:list(rig.pose.bones['J_Bip_'+side+'_'+bn].rotation_quaternion) for bn in ['UpperArm','LowerArm','Hand']} for side in ['L','R']};bodies=[snap(bpy.data.objects['Fix__'+n])[1] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']]
 cloth=[o for o in col.objects if o.type=='MESH' and o['master_source'].startswith('ElseIf_Jacket_')];keys={};transforms={b.name:rig.pose.bones[b.name].matrix@b.matrix_local.inverted() for b in rig.data.bones};changes=[]
 for o in cloth:
  if not o.data.shape_keys:o.shape_key_add(name='Basis')
  pair={side:o.shape_key_add(name='DLHN_Contact_'+side+'_'+label,from_mix=False) for side in ['L','R']}
  for k in pair.values():k.value=1
  keys[o.name]=pair
 for iteration in range(32):
  bpy.context.view_layer.update()
  for o in cloth:
   vs,tree=snap(o);deltas={};basis=o.data.shape_keys.reference_key
   contacts={}
   for body_index,body in enumerate(bodies):
    for face_index,body_face in tree.overlap(body):
     for vi in o.data.polygons[face_index].vertices:contacts.setdefault(vi,set()).add(body_index)
   if not contacts:continue
   for i,body_ids in contacts.items():
    p=vs[i]
    rest=basis.data[i].co
    if rest.z<1.05 and label!='J_WristTwist':continue
    if abs(rest.x)<.03:continue
    best=Vector()
    for body_id in body_ids:
     body=bodies[body_id];hit=body.find_nearest(p)
     if hit[3]>.035:continue
     signed=(p-hit[0]).dot(hit[1])
     if signed<.006:
      push=hit[1]*min(.010,.006-signed)
      if push.length>best.length:best=push
    if best.length<.00005:continue
    m=Matrix(((0,0,0,0),)*4)
    for g in o.data.vertices[i].groups:
     n=o.vertex_groups[g.group].name
     if n in transforms:m+=transforms[n]*g.weight
    try:deltas[i]=m.to_3x3().inverted()@best
    except:pass
   if not deltas:continue
   # 接触部の補正を近傍へ広げ、細かい突起ではなく布の面として扱う。
   adjacent=[[] for _ in o.data.vertices]
   for e in o.data.edges:a,b=e.vertices;adjacent[a].append(b);adjacent[b].append(a)
   for _ in range(5):
    expanded=set(deltas)
    for i in list(deltas):expanded.update(adjacent[i])
    nxt={}
    for i in expanded:
     neighbours=adjacent[i];avg=sum((deltas.get(j,Vector()) for j in neighbours),Vector())/max(1,len(neighbours));cur=deltas.get(i,Vector());val=cur*.55+avg*.45
     if val.length>.000015:nxt[i]=val
    deltas=nxt
   for i,d in deltas.items():
    side='L' if basis.data[i].co.x>=0 else 'R';keys[o.name][side].data[i].co+=d
   changes.append((o.name,iteration,len(deltas),max((d.length for d in deltas.values()),default=0)*1000))
 # デザイン面に同じ補正を補間する。内容・配色は保持。
 for side in ['L','R']:
  points=[];vectors=[]
  for o in cloth:
   k=keys[o.name][side];basis=o.data.shape_keys.reference_key
   for i,v in enumerate(basis.data):
    d=k.data[i].co-v.co
    if d.length>.00002:points.append(v.co.copy());vectors.append(d.copy())
  if not points:continue
  kd=KDTree(len(points))
  for i,p in enumerate(points):kd.insert(p,i)
  kd.balance()
  for o in col.objects:
   if o.type!='MESH' or not o['master_source'].startswith('EL_'):continue
   updates={}
   for v in o.data.vertices:
    if (v.co.x>=0)!=(side=='L'):continue
    nearest=kd.find_n(v.co,4)
    if nearest[0][2]>.018:continue
    total=sum(1/(d+.0001)**2 for p,i,d in nearest);updates[v.index]=sum((vectors[i]/(d+.0001)**2 for p,i,d in nearest),Vector())/total
   if updates:
    if not o.data.shape_keys:o.shape_key_add(name='Basis')
    k=o.shape_key_add(name='DLHN_Contact_'+side+'_'+label,from_mix=False)
    for i,d in updates.items():k.data[i].co+=d
    add_driver(k,side,targets[side])
 for o in cloth:
  for side,k in keys[o.name].items():
   maximum=max((k.data[i].co-o.data.shape_keys.reference_key.data[i].co).length for i in range(len(k.data)))
   if maximum<.00002:o.shape_key_remove(k)
   else:add_driver(k,side,targets[side])
 (OUT/('Contact_'+label+'.json')).write_text(json.dumps(changes,indent=2));print(label,changes)


for label in ["B_Side90","G_WideOpen","K_Arms45"]:solve(label)
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"ElseIf_Underarm_Corrected.blend"))
