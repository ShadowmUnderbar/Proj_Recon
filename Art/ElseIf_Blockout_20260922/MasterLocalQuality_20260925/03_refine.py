import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');base=bpy.data.scenes['ElseIf_Master_Quality_Review'];nat=bpy.data.scenes['ElseIf_Master_Quality_NaturalPose'];mapping=json.loads((P/'Duplicate_Map.json').read_text());report={}
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[o.matrix_world@x.co for x in m.vertices];f=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear();return v,f,BVHTree.FromPolygons(v,f,all_triangles=True)
def sm(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def g(x,w):return math.exp(-(x/w)**2)
def mixed(o):
 if not o.data.shape_keys:return [v.co.copy() for v in o.data.vertices]
 basis=o.data.shape_keys.reference_key
 return [v.co+sum(((k.data[i].co-k.relative_key.data[i].co)*k.value for k in o.data.shape_keys.key_blocks if k!=basis and not k.mute),Vector()) for i,v in enumerate(basis.data)]
jackets=['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R'];shoes=['ElseIf_Shoe_'+side+'_'+part for side in ['L','R'] for part in ['Sole','Upper','AnkleCollar','Tongue']];old={}
for s,pre in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=s;bpy.context.view_layer.update()
 for name in jackets:old[pre+name]=snap(bpy.data.objects[mapping[pre+name]])
bpy.context.window.scene=base;bpy.context.view_layer.update()
for name in shoes:old[name]=snap(bpy.data.objects[mapping[name]])
o=bpy.data.objects[mapping['ElseIf_Jacket_Sleeve_L']];bm=bmesh.new();bm.from_mesh(o.data);mv=mixed(o);points=[o.matrix_world@mv[v.index] for v in bm.verts if v.is_boundary and v.co.z>1.19];bm.free();kd=KDTree(len(points))
for i,p in enumerate(points):kd.insert(p,i)
kd.balance()
def cloth(p):
 x=abs(p.x);sgn=1 if p.x>=0 else -1;q=Vector((x,p.y,p.z));d=kd.find(q)[2];gate=sm(.084,.112,x)*(1-sm(.211,.238,x))*sm(1.17,1.20,p.z)*(1-sm(1.389,1.402,p.z));normal=Vector((sgn*.48,(p.y-.02525)*9,.22)).normalized()
 delta=normal*(-.0009*g(d,.0045)*sm(1.245,1.30,p.z))
 delta.z-=.0028*g(x-.148,.027)*g(p.z-1.346,.026)*gate
 wave=.0017*g(p.z-(1.245+.24*(x-.126)),.017)-.0010*g(p.z-(1.216+.18*(x-.126)),.018)
 delta.y+=(p.y-.02525)/.080*wave*g(x-.147,.040)*gate
 delta.z-=.0012*g(p.z-1.236,.025)*g(x-.151,.033)*gate
 return delta
for s,pre in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=s;bpy.context.view_layer.update()
 for name in jackets:
  o=bpy.data.objects[mapping[pre+name]];coords=mixed(o);k=o.shape_key_add(name='Quality_Shoulder_Armhole_20260925',from_mix=False);basis=o.data.shape_keys.reference_key;deltas=[]
  for i,p in enumerate(coords):
   d=o.matrix_world.to_3x3().inverted()@cloth(o.matrix_world@p);k.data[i].co=basis.data[i].co+d;deltas.append(d.length)
  k.value=1;report[o.name]={'max_delta_mm':max(deltas)*1000,'changed_vertices':sum(d>.00001 for d in deltas)}
def shoe(p,part,side):
 out=p.copy();cx=side*.07260618;dx=p.x-cx;front=1-sm(-.128,-.074,p.y);low=1-sm(.13,.17,p.z)
 # 前端の両隅を後退させ、つま先の平面形を丸くする。
 out.y+=.0065*front*min(1,(abs(dx)/.051)**2)*low
 out.x-=dx*.045*g(p.y+.119,.035)*low
 out.z+=.0018*front*(1-sm(.095,.135,p.z))
 if part=='Upper':
  out.z-=.0025*g(p.y+.112,.030)*g(p.z-.080,.033)
  out.z+=.0025*g(p.y+.046,.037)*g(dx,.036)*g(p.z-.124,.035)
 if part in ['AnkleCollar','Upper']:
  out.z-=.0020*sm(.174,.207,p.z)*(1-sm(-.025,.030,p.y))
 if part=='Tongue':
  out.y-=.0018*g(dx,.035);out.z+=.0015*g(dx,.025)*sm(.180,.213,p.z)
 return out-p
bpy.context.window.scene=base;bpy.context.view_layer.update()
for name in shoes:
 o=bpy.data.objects[mapping[name]];coords=mixed(o)
 if not o.data.shape_keys:o.shape_key_add(name='Basis')
 k=o.shape_key_add(name='Quality_StreetShoe_Form_20260925',from_mix=False);basis=o.data.shape_keys.reference_key;deltas=[];side=1 if '_L_' in name else -1;part=name.split('_')[-1]
 for i,p in enumerate(coords):
  d=o.matrix_world.to_3x3().inverted()@shoe(o.matrix_world@p,part,side);k.data[i].co=basis.data[i].co+d;deltas.append(d.length)
 k.value=1;report[o.name]={'max_delta_mm':max(deltas)*1000,'changed_vertices':sum(d>.00001 for d in deltas)}
new={}
for s,pre in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=s;bpy.context.view_layer.update()
 for name in jackets:new[pre+name]=snap(bpy.data.objects[mapping[pre+name]])
bpy.context.window.scene=base;bpy.context.view_layer.update()
for name in shoes:new[name]=snap(bpy.data.objects[mapping[name]])
for oldname,newname in mapping.items():
 o=bpy.data.objects[newname]
 if o.type!='MESH' or 'EL_' not in oldname:continue
 pre='NaturalPose__' if oldname.startswith('NaturalPose__') else '';is_shoe='EL_Shoe_' in oldname
 if is_shoe:
  side='L' if 'Shoe_L_' in oldname else 'R';candidates=['ElseIf_Shoe_'+side+'_'+p for p in ['Upper','Tongue','AnkleCollar','Sole']]
 else:candidates=[pre+n for n in jackets]
 mx=0
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if not is_shoe and (p.z<1.17 or abs(p.x)<.080 or abs(p.x)>.245):continue
  hit,n=min([(old[n][2].find_nearest(p),n) for n in candidates],key=lambda a:a[0][3]);loc,normal,idx,dist=hit
  if dist>.035:continue
  ov,faces,_=old[n];nv,_,_=new[n];a,b,c=faces[idx];mapped=barycentric_transform(loc,ov[a],ov[b],ov[c],nv[a],nv[b],nv[c]);d=mapped-loc;v.co+=o.matrix_world.to_3x3().inverted()@d;mx=max(mx,d.length)
 o.data.update()
 if mx:report[o.name]={'existing_detail_follow_mm':mx*1000}
bpy.context.window.scene=base;bpy.context.view_layer.update();rig=bpy.data.objects['Quality__ElseIf_Humanoid'];r=json.loads((P/'Anatomy_Measurements.json').read_text());r['posed_bones']={pb.name:{'head':list(rig.matrix_world@pb.head),'tail':list(rig.matrix_world@pb.tail)} for pb in rig.pose.bones if any(t in pb.name for t in ['Shoulder','UpperArm','LowerArm'])};(P/'Anatomy_Measurements.json').write_text(json.dumps(r,indent=2));(P/'Changes.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Master_LocalQuality.blend'))
for view in ['ThreeQuarter','Front']:
 base.camera=bpy.data.objects[mapping['ElseIf_Cam_'+view]];base.render.filepath=str(P/('Preview_'+view+'.jpg'));base.render.image_settings.file_format='JPEG';bpy.ops.render.render(write_still=True)
base.render.image_settings.file_format='PNG';print(json.dumps(report))
