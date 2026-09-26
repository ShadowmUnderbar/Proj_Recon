import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925');s=bpy.data.scenes['ElseIf_Topology_Review'];bpy.context.window.scene=s;col=bpy.data.collections['ElseIf_Game_LocalRetopo'];records=json.loads((P/'Body_Deletion_Record.json').read_text());frames=json.loads((P/'Pose_Frames.json').read_text());dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z]
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=[o.matrix_world@x.co for x in m.vertices];f=[tuple(p.vertices) for p in m.polygons];e.to_mesh_clear();return v,f
edges={};report={};classify={}
for name,r in records.items():
 o=bpy.data.objects[name];kv={i:tuple(round(c,5) for c in p) for i,p in enumerate(r['vertices'])};ef={};deleted=set(r['deleted_faces']);origmap={a.value:i for i,a in enumerate(o.data.attributes['DLHN_OriginalBodyVertex'].data)};coordmap={kv[old]:i for old,i in origmap.items()}
 for fi,f in enumerate(r['faces']):
  for a,b in zip(f,f[1:]+f[:1]):ef.setdefault(tuple(sorted((kv[a],kv[b]))),set()).add(fi)
 edges[name]=[(coordmap[a],coordmap[b]) for (a,b),fs in ef.items() if fs&deleted and fs-deleted and a in coordmap and b in coordmap]
 bins={}
 src=bpy.data.objects['Opt__'+o['master_source']]
 for fi in deleted:
  f=r['faces'][fi];p=sum((Vector(r['vertices'][i]) for i in f),Vector())/len(f);w={}
  for i in f:
   for g in src.data.vertices[i].groups:
    n=src.vertex_groups[g.group].name;w[n]=w.get(n,0)+g.weight
  n=max(w,key=w.get) if w else 'none'
  reg='Torso'
  if any(t in n for t in ['UpperArm','LowerArm']):reg='UpperArm' if 'UpperArm' in n else 'Forearm'
  elif any(t in n for t in ['Foot','Toe']):reg='Foot'
  elif 'UpperLeg' in n:reg='UpperLeg'
  elif 'LowerLeg' in n:reg='LowerLeg'
  elif p.z<1.10:reg='Pelvis'
  bins[reg]=bins.get(reg,0)+len(f)-2
 classify[name]={'deleted_triangles_by_region':bins,'new_cut_edges':len(edges[name])}
for label,frame in frames.items():
 s.frame_set(frame);bpy.context.view_layer.update();cv=[];cf=[]
 for o in col.objects:
  if o.type!='MESH' or not o['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_')):continue
  vs,fs=snap(o);off=len(cv);cv+=vs;cf.extend(tuple(i+off for i in f) for f in fs)
 for name,r in records.items():
  o=bpy.data.objects[name];vs,fs=snap(o);off=len(cv);cv+=vs;prot=set(r['protected_vertices']);at=o.data.attributes['DLHN_OriginalBodyVertex'];ids={i for i,a in enumerate(at.data) if a.value in prot};cf.extend(tuple(i+off for i in f) for f in fs if all(i in ids for i in f))
 tree=BVHTree.FromPolygons(cv,cf);row={}
 for name,ee in edges.items():
  vs,fs=snap(bpy.data.objects[name]);bad=[];mind=10
  for j,(a,b) in enumerate(ee):
   good=True
   for p in [vs[a],vs[b],(vs[a]+vs[b])*.5]:
    for d in dirs:
     hit=tree.ray_cast(p+d*.00005,d,2)
     if hit[0] is None:good=False;break
     mind=min(mind,hit[3])
    if not good:break
   if not good:bad.append(j)
  row[name]={'cut_edges':len(ee),'unoccluded_cut_edges':bad,'minimum_ray_clearance_m':mind}
 report[label]=row;(P/'Boundary_Audit.json').write_text(json.dumps({'regions':classify,'poses':report},indent=2))
s.frame_set(1);print(json.dumps({'regions':classify,'failures':{k:{n:len(v['unoccluded_cut_edges']) for n,v in r.items()} for k,r in report.items()}}))
