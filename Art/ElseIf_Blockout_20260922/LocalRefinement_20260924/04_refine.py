import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];nat=bpy.data.scenes['ElseIf_NaturalPose_Validation']
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[o.matrix_world@x.co for x in m.vertices];f=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear();return (v,f,BVHTree.FromPolygons(v,f,all_triangles=True))
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def gauss(x,s):return math.exp(-(x/s)**2)
jackets=['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R']
shoes=['ElseIf_Shoe_'+s+'_'+p for s in ['L','R'] for p in ['Sole','Upper','AnkleCollar','Tongue']]
# 旧曲面から新曲面へプリントとストラップを同一三角形座標で追従させる。
old={}
for scene,prefix in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 for n in jackets:old[prefix+n]=snap(bpy.data.objects[prefix+n])
for n in shoes:old[n]=snap(bpy.data.objects[n])
# 実際の袖付け境界から距離場を作る。袖口側は含めない。
bpy.context.window.scene=base;o=bpy.data.objects['ElseIf_Jacket_Sleeve_L'];bm=bmesh.new();bm.from_mesh(o.data);points=[o.matrix_world@v.co for v in bm.verts if v.is_boundary and v.co.z>1.19];bm.free();kd=KDTree(len(points))
for i,p in enumerate(points):kd.insert(p,i)
kd.balance()
def cloth_delta(p):
 x=abs(p.x);side=1 if p.x>=0 else -1;q=Vector((x,p.y,p.z));d=kd.find(q)[2]
 gate=smooth(.072,.091,x)*(1-smooth(.205,.24,x))*smooth(1.16,1.21,p.z)*(1-smooth(1.39,1.405,p.z))
 # 袖山のごく弱い縫製による谷と、その外側の布の立ち上がり。
 cap=(-.0012*gauss(d,.0045)+.0023*gauss(d-.014,.009))*smooth(1.235,1.31,p.z)
 radial=Vector((side*.65,(p.y-.020)*8,.30)).normalized()
 delta=radial*(cap*gate)
 # 肩先実測x=.1263を越えた布の落ち方。最大約3mm。
 delta.z-=.0032*gauss(x-.145,.030)*gauss(p.z-1.348,.040)*gate
 # 脇下面実測z=1.2436を基準に、広い二面の余りを作る。
 frontback=(p.y-.018)/.085
 waves=.0027*gauss(p.z-(1.246+.30*(x-.125)),.014)-.0018*gauss(p.z-(1.215+.20*(x-.125)),.013)
 delta.y+=frontback*waves*gauss(x-.143,.042)*gate
 delta.z-=.0014*gauss(p.z-1.233,.027)*gauss(x-.150,.035)*gate
 return delta
report={}
for prefix in ['', 'NaturalPose__']:
 for n in jackets:
  o=bpy.data.objects[prefix+n];key=o.shape_key_add(name='Local_Shoulder_Armhole_20260924',from_mix=False);basis=o.data.shape_keys.reference_key;mx=0
  for i,v in enumerate(basis.data):
   p=o.matrix_world@v.co;d=cloth_delta(p);key.data[i].co=v.co+o.matrix_world.to_3x3().inverted()@d;mx=max(mx,d.length)
  key.value=1;report[o.name]={'max_delta_mm':mx*1000};o['local_refinement']='実測肩先・脇と既存袖付け境界に基づく局所補正。既存Shape Keyは保持。'
def shoe_delta(p,part,side):
 cx=side*.07260618;dx=p.x-cx;y=p.y;z=p.z
 width=1-.135*gauss(y+.124,.044)*(1-smooth(.105,.145,z))-.075*smooth(.105,.185,z)-.045*gauss(y-.084,.035)
 out=p.copy();out.x=cx+dx*width
 # 接地を保ったままつま先だけをごく弱く反らせる。
 out.z+=.0035*(1-smooth(-.140,-.085,y))*(1-smooth(.09,.15,z))
 if part!='Sole':
  out.z-=.0045*gauss(y+.112,.037)*gauss(z-.086,.035)
  out.y+=.002*gauss(y+.068,.042)*gauss(z-.137,.035)
 if part in ['Upper','AnkleCollar']:
  out.z-=.007*smooth(.180,.205,z)*(1-smooth(-.028,.036,y))
 if part=='Tongue':
  out.y-=.0025
  out.z-=.0035*(abs(dx)/.028)**2*smooth(.193,.22,z)
 return out-p
for n in shoes:
 o=bpy.data.objects[n];o.shape_key_add(name='Basis');k=o.shape_key_add(name='Local_StreetShoe_Form_20260924');side=1 if '_L_' in n else -1;part=n.split('_')[-1];mx=0
 for i,v in enumerate(o.data.vertices):
  d=shoe_delta(o.matrix_world@v.co,part,side);k.data[i].co=v.co+d;mx=max(mx,d.length)
 k.value=1;report[n]={'max_delta_mm':mx*1000}
 # 既存ソールの底部と甲のつま先を材質で区別し、厚みは増やさない。
 if part in ['Sole','Upper']:
  o.data.materials.append(bpy.data.materials['EL_Design_Graphite']);idx=len(o.data.materials)-1
  for f in o.data.polygons:
   center=sum((o.data.vertices[i].co for i in f.vertices),Vector())/len(f.vertices)
   if (part=='Sole' and center.z<.012) or (part=='Upper' and center.y<-.105 and center.z<.09):f.material_index=idx
 if part=='Tongue':o.data.materials[0]=bpy.data.materials['EL_Design_Graphite']
# 既存装飾の配置のみを更新。新しいデザイン要素は作らない。
new={}
for scene,prefix in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 for n in jackets:new[prefix+n]=snap(bpy.data.objects[prefix+n])
for n in shoes:new[n]=snap(bpy.data.objects[n])
for prefix,col in [('', 'ElseIf_Design_Basis'),('NaturalPose__','ElseIf_Design_NaturalPose')]:
 for o in bpy.data.collections[col].objects:
  if o.type!='MESH':continue
  isshoe='EL_Shoe_' in o.name
  if isshoe:
   side='L' if 'Shoe_L_' in o.name else 'R';candidates=['ElseIf_Shoe_'+side+'_'+p for p in ['Upper','Tongue','AnkleCollar','Sole']]
  else:candidates=[prefix+n for n in jackets]
  mx=0
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if not isshoe and (p.z<1.16 or abs(p.x)<.071 or abs(p.x)>.25):continue
   hits=[(old[n][2].find_nearest(p),n) for n in candidates];hit,n=min(hits,key=lambda a:a[0][3]);loc,normal,idx,dist=hit
   if dist>.035:continue
   ov,faces,_=old[n];nv,_,_=new[n];a,b,c=faces[idx];mapped=barycentric_transform(loc,ov[a],ov[b],ov[c],nv[a],nv[b],nv[c]);delta=mapped-loc
   v.co+=o.matrix_world.to_3x3().inverted()@delta;mx=max(mx,delta.length)
  o.data.update()
  if mx:report[o.name]={'surface_follow_mm':mx*1000}
bpy.context.window.scene=base;bpy.context.view_layer.update()
(OUT/'04_change_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Local_Refinement.blend'))
for view in ['ThreeQuarter','HighAngle','Front']:
 base.camera=bpy.data.objects['ElseIf_Cam_'+view];base.render.filepath=str(OUT/('Review_'+view+'.png'));bpy.ops.render.render(write_still=True)
base.camera=bpy.data.objects['ElseIf_Cam_ThreeQuarter'];print('Local shape keys and surface-following design placement completed. Three reviews rendered.')
