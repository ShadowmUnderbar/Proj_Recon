import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925');s=bpy.data.scenes['ElseIf_Topology_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_LocalRetopo'];rig=bpy.data.objects['ElseIf_Topology_Humanoid'];report=[]
for o in col.objects:
 if o.type!='MESH':continue
 if o.data.shape_keys:
  for k in list(o.data.shape_keys.key_blocks):
   if k.name.startswith('DLHN_Wide_ClothEase') or (k.name.startswith('DLHN_Contact') and 'J_WristTwist' not in k.name):k.driver_remove('value');o.shape_key_remove(k)
 if o['master_source'].startswith(('ElseIf_Jacket_Sleeve_','ElseIf_Jacket_Cuff_')):
  at=o.data.attributes.get('DLHN_OuterSurface') or o.data.attributes.new('DLHN_OuterSurface','FLOAT','POINT');count=len(bpy.data.objects[o['master_source']].data.vertices)
  for v in o.data.vertices:at.data[v.index].value=1.0 if v.index<count else 0.0
for name in ['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R']:
 o=bpy.data.objects['Topo__'+name];m=o.data;old=len(m.vertices);m.calc_loop_triangles();oldt=len(m.loop_triangles);bm=bmesh.new();bm.from_mesh(m)
 def local(v):
  p=v.co
  return 1.14<p.z<1.325 and .07<abs(p.x)<.235
 verts=[v for v in bm.verts if local(v)];selected=set(verts);edges=[e for e in bm.edges if all(v in selected for v in e.verts) and len(e.link_faces)==2]
 bmesh.ops.dissolve_limit(bm,angle_limit=.045,use_dissolve_boundaries=False,verts=verts,edges=edges,delimit={'MATERIAL','SEAM','SHARP','UV'})
 planes=[]
 if 'Sleeve' in name:
  sign=1 if name.endswith('_L') else -1;head=rig.data.bones['J_Bip_'+('L' if sign>0 else 'R')+'_UpperArm'].head_local;axis=Vector((.422618*sign,0,-.906308))
  planes=[(head+axis*t,axis) for t in [.055,.085,.115,.145]]
 else:planes=[(Vector((0,0,z)),Vector((0,0,1))) for z in [1.185,1.215,1.245,1.275]]
 for point,normal in planes:
  faces=[f for f in bm.faces if all(local(v) for v in f.verts)];es={e for f in faces for e in f.edges};vs={v for e in es for v in e.verts}
  bmesh.ops.bisect_plane(bm,geom=list(vs)+list(es)+faces,dist=.000001,plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
 bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4],quad_method='BEAUTY',ngon_method='BEAUTY');bm.to_mesh(m);bm.free();m.update();m.calc_loop_triangles();report.append({'object':name,'before_vertices':old,'after_vertices':len(m.vertices),'before_triangles':oldt,'after_triangles':len(m.loop_triangles)})
def sm(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
# 支持帯に合わせ、身頃では胸郭へ、袖外側では腕へ重みを分配。
changed={}
for o in col.objects:
 if o.type!='MESH' or not o['master_source'].startswith(('ElseIf_Jacket_','EL_')):continue
 count=0
 for v in o.data.vertices:
  p=v.co;inner=-.906308*(abs(p.x)-.08914964)-.422618*(p.z-1.35648966)
  amount=.75*sm(-.006,.032,inner)*(1-sm(.165,.235,abs(p.x)))*(1-sm(1.195,1.315,p.z))*sm(1.075,1.135,p.z)
  if amount<.001:continue
  w={o.vertex_groups[g.group].name:g.weight for g in v.groups};take=0
  for n in list(w):
   if 'UpperArm' in n or 'Shoulder' in n:cut=w[n]*amount;w[n]-=cut;take+=cut
  if take<.00001:continue
  t=sm(1.17,1.28,p.z)
  for n,a in [('J_Bip_C_Chest',1-t),('J_Bip_C_UpperChest',t)]:w[n]=w.get(n,0)+take*a
  items=sorted(w.items(),key=lambda x:-x[1])[:4];total=sum(a for n,a in items)
  for g in o.vertex_groups:g.remove([v.index])
  for n,a in items:(o.vertex_groups.get(n) or o.vertex_groups.new(name=n)).add([v.index],a/total,'REPLACE')
  count+=1
 if count:changed[o.name]=count
(P/'Topology_Changes.json').write_text(json.dumps({'objects':report,'reweighted':changed},indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_LocalRetopo.blend'));print(report)
