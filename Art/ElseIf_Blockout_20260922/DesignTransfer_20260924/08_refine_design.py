import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/DesignTransfer_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];nat=bpy.data.scenes['ElseIf_NaturalPose_Validation'];cache={}
def snap(name):
 if name in cache:return cache[name]
 bpy.context.window.scene=base;bpy.context.view_layer.update();o=bpy.data.objects[name];ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();m.calc_loop_triangles();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];ev.to_mesh_clear()
 bpy.context.window.scene=nat;bpy.context.view_layer.update();o=bpy.data.objects['NaturalPose__'+name];ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();nv=[o.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear();cache[name]=(BVHTree.FromPolygons(vs,fs,all_triangles=True),vs,fs,nv);return cache[name]
def project(q,names,offset=.002):
 hits=[]
 for name in names:
  t,vs,fs,nv=snap(name);hit=t.find_nearest(q)
  if hit[0] is not None:hits.append((hit[3],name,hit))
 _,name,(p,n,i,d)=min(hits,key=lambda x:x[0]);t,vs,fs,nv=cache[name];a,b,c=fs[i];np=barycentric_transform(p,vs[a],vs[b],vs[c],nv[a],nv[b],nv[c]);nn=(nv[b]-nv[a]).cross(nv[c]-nv[a]).normalized();return p+n*offset,np+nn*offset
for side in ['L','R']:
 shoulder=bpy.data.objects['EL_Shoulder_Webbing_'+side];web=bpy.data.objects['EL_Back_Webbing_'+side]
 # 既存2パーツの端点を測り、その間だけを曲面に沿って接続する。
 shoulder_edge=sorted([shoulder.data.vertices[i].co.copy() for i in [24,49,74]],key=lambda p:p.x)
 web_edge=sorted([v.co.copy() for v in web.data.vertices if abs(v.co.z-max(w.co.z for w in web.data.vertices))<.002],key=lambda p:p.x)
 a,b=web_edge[0],web_edge[-1];c,d=shoulder_edge[0],shoulder_edge[-1];points=[];faces=[]
 for j in range(15):
  t=j/14
  for i in range(5):
   u=i/4;q=a.lerp(b,u).lerp(c.lerp(d,u),t);points.append(project(q,['ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_'+side],.0020))
 for j in range(14):
  for i in range(4):k=j*5+i;faces.append((k,k+1,k+6,k+5))
 for natural in [False,True]:
  name=('NaturalPose__' if natural else '')+'EL_Shoulder_Back_Connector_'+side;me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata([tuple(p[1 if natural else 0]) for p in points],[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.data.collections['ElseIf_Design_NaturalPose' if natural else 'ElseIf_Design_Basis'].objects.link(o);me.materials.append(bpy.data.materials['EL_Design_Ink'])
  for f in me.polygons:f.use_smooth=True
# 後ろ上腕の文字も、背面から正しく読める向きにする。
source=bpy.data.objects['EL_UpperArm_DUM'];target=bpy.data.objects['NaturalPose__EL_UpperArm_DUM'];sh=Vector((.08914964,.02525085,1.35648966));axis=Vector((.422618,0,-.906308));u=Vector((.906308,0,.422618));v=Vector((0,-1,0))
for i,vert in enumerate(source.data.vertices):
 p=vert.co.copy();s=(p-sh).dot(axis);center=sh+axis*s;center.y-=.022*min(1,max(0,(s-.18)/.2));radial=p-center;angle=math.atan2(radial.dot(v),radial.dot(u));flipped=-1.44-angle;q=center+(u*math.cos(flipped)+v*math.sin(flipped))*radial.length;bp,np=project(q,['ElseIf_Jacket_Sleeve_L'],.002);source.data.vertices[i].co=bp;target.data.vertices[i].co=np
source.data.update();target.data.update()
for scene,col in [('ElseIf_Secondary_Raking_Basis','ElseIf_Design_Basis'),('ElseIf_Secondary_Raking_NaturalPose','ElseIf_Design_NaturalPose')]:
 s=bpy.data.scenes.get(scene);c=bpy.data.collections[col]
 if s and col not in s.collection.children:s.collection.children.link(c)
bpy.context.window.scene=base;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Design_Transfer.blend'))
for scene,view,name in [(base,'Back','Review_Back_Corrected'),(nat,'HighAngle','Review_NaturalPose_HighAngle')]:
 bpy.context.window.scene=scene;scene.camera=bpy.data.objects['ElseIf_Cam_'+view];scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.context.window.scene=base;print('Back orientations and shoulder connections corrected; natural high-angle review rendered')
