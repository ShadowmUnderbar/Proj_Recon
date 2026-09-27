import bpy,json,math,numpy as np,bmesh
from pathlib import Path
from mathutils import Vector,Matrix
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926')
source=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=source;source.frame_set(1)
assert not bpy.data.collections.get('ElseIf_HeadPart_Monitor01'),'Head part already exists; do not overwrite'
assembly=source.copy();assembly.name='ElseIf_Head01_Assembly_Review';bpy.context.window.scene=assembly
col=bpy.data.collections.new('ElseIf_HeadPart_Monitor01');assembly.collection.children.link(col)
socket=bpy.data.objects['Mobile__LOD0__Backup__Fix__HeadSocket'];root=bpy.data.objects.new('HeadPart_ElseIf_Monitor01',None);col.objects.link(root);root.empty_display_type='ARROWS';root.empty_display_size=.045;root.parent=socket;root.matrix_parent_inverse=Matrix.Identity(4);root.location=(0,0,0);root.rotation_euler=(0,0,0);root.scale=(1,1,1);root['HeadPart_ID']='ElseIf_Monitor01';root['Attach']='Parent to HeadSocket; local Position/Rotation=0, Scale=1';root['Forward']='Blender -Y';root['Up']='Blender +Z'
def mat(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.node_tree.nodes.clear();n=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled');n.name='HeadPrincipled';out=m.node_tree.nodes.new('ShaderNodeOutputMaterial');m.node_tree.links.new(n.outputs['BSDF'],out.inputs['Surface']);n.inputs['Base Color'].default_value=(*color,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough;m.diffuse_color=(*color,1);return m
shell=mat('ElseIf_HeadShell',(.57,.55,.53),.24,.36);trim=mat('ElseIf_HeadJoint',(.018,.022,.03),.48,.32)
# 角丸断面を一定の対応関係でつなぐ。前面は薄いフレーム、後ろへ穏やかに絞る。
N=48;zc=.108
V=[];F=[];M=[]
def rr(a,b,r,y,center=zc):
 pts=[]
 for cx,cz,start in [(a-r,b-r,0),(-a+r,b-r,90),(-a+r,-b+r,180),(a-r,-b+r,270)]:
  for j in range(12):
   ang=math.radians(start+j*90/12);pts.append((cx+r*math.cos(ang),y,center+cz+r*math.sin(ang)))
 return pts
# 輪郭は反時計回り。法線は最後に整える。
def rings(rings,material=0,cap_end=False,cap_start=False):
 offset=len(V)
 for ring in rings:V.extend(ring)
 count=len(rings[0]);nr=len(rings)
 for k in range(nr-1):
  for j in range(count):F.append((offset+k*count+j,offset+k*count+(j+1)%count,offset+(k+1)*count+(j+1)%count,offset+(k+1)*count+j));M.append(material)
 if cap_start:F.append(tuple(offset+j for j in reversed(range(count))));M.append(material)
 if cap_end:F.append(tuple(offset+(nr-1)*count+j for j in range(count)));M.append(material)
 return offset
rings([rr(.0845,.0745,.016,-.088),rr(.0905,.0835,.018,-.091),rr(.094,.088,.020,-.086),rr(.094,.088,.020,-.074),rr(.093,.087,.021,-.010),rr(.086,.082,.025,.054),rr(.079,.075,.025,.071),rr(.068,.066,.023,.077)],0,True)
rings([rr(.0845,.0745,.016,-.0882),rr(.083,.073,.015,-.0885),rr(.083,.073,.015,-.084)],1)
# 後部の控えめな継ぎ目。大きな装飾や追加モニターは作らない。
rings([rr(.0795,.0755,.025,.0706),rr(.079,.075,.025,.0714)],1)
# 円形の回転接続。筐体に含めるが、身体側のGeometryとは独立。
def circle(rad,z):return [(rad*math.cos(2*math.pi*i/32),rad*math.sin(2*math.pi*i/32),z) for i in range(32)]
rings([circle(.025,-.053),circle(.027,-.049),circle(.027,-.012),circle(.030,-.008),circle(.030,.009),circle(.033,.014),circle(.030,.023)],1,True,True)
rings([circle(.0285,-.028),circle(.0285,-.024)],0)
# 小さな前面下端インジケータ座。顔の要素ではない。
rings([rr(.012,.0016,.0012,-.0909,center=.028),rr(.012,.0016,.0012,-.0915,center=.028)],1,True)
m=bpy.data.meshes.new('ElseIf_HeadShell_Mesh');m.from_pydata(V,[],F);m.materials.append(shell);m.materials.append(trim);m.update()
for f,mi in zip(m.polygons,M):f.material_index=mi;f.use_smooth=True
bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();ob=bpy.data.objects.new('ElseIf_HeadShell',m);col.objects.link(ob);ob.parent=root
# 表示面は浅い曲面。UVは正面XY相当の平面投影で1アイランドにする。
per=rr(.083,.073,.015,0);vs=[(0,-.092,.108)];uv=[(.5,.5)];faces=[]
for t in [.25,.50,.75,1.0]:
 for x,y,z in per:
  xx=x*t;zz=(z-zc)*t;bulge=.0035*max(0,1-(xx/.083)**2)*max(0,1-(zz/.073)**2);vs.append((xx,-.0885-bulge,zc+zz));uv.append((xx/.166+.5,zz/.146+.5))
for j in range(N):faces.append((0,1+j,1+(j+1)%N))
for k in range(3):
 a=1+k*N;b=a+N
 for j in range(N):faces.append((a+j,b+j,b+(j+1)%N,a+(j+1)%N))
mm=bpy.data.meshes.new('ElseIf_FaceMonitor_Mesh');mm.from_pydata(vs,[],faces);mm.update();layer=mm.uv_layers.new(name='FaceUV')
for f in mm.polygons:
 f.use_smooth=True
 for li,vi in zip(f.loop_indices,f.vertices):layer.data[li].uv=uv[vi]
bm=bmesh.new();bm.from_mesh(mm);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mm);bm.free()
# 画面の法線を正面(-Y)へ統一。
if sum(f.normal.y for f in mm.polygons)>0:
 bm=bmesh.new();bm.from_mesh(mm);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(mm);bm.free()
face=bpy.data.objects.new('ElseIf_FaceMonitor',mm);col.objects.link(face);face.parent=root
# 仮表情：512px、ドット表示。顔は一切Geometry化しない。
def expression(name,mode):
 W=128;arr=np.zeros((W,W,4),dtype=np.float32);arr[:,:,:3]=(.0025,.003,.005);arr[:,:,3]=1;color=(.95,.48,.80)
 def dot(x,y,r=1.05):
  for yy in range(max(0,int(y-r-1)),min(W,int(y+r+2))):
   for xx in range(max(0,int(x-r-1)),min(W,int(x+r+2))):
    if (xx-x)**2+(yy-y)**2<=r*r:arr[yy,xx,:3]=color
 def path(points,spacing=3.0):
  for a,b in zip(points,points[1:]):
   d=math.dist(a,b);n=max(1,round(d/spacing))
   for j in range(n+1):t=j/n;dot(a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t)
 path([(20+3*math.cos(t),64+11*math.sin(t)) for t in np.linspace(math.pi/2,3*math.pi/2,12)])
 path([(108+3*math.cos(t),64+11*math.sin(t)) for t in np.linspace(-math.pi/2,math.pi/2,12)])
 if mode=='Normal':
  path([(36,73),(46,65),(36,57)]);path([(92,73),(82,65),(92,57)]);path([(55,70),(64,55),(73,70)]);path([(58,65),(70,65)])
 elif mode=='Joy':
  path([(34,64),(41,73),(48,64)]);path([(80,64),(87,73),(94,64)])
  path([(55,62),(55,56),(59,53),(64,56),(69,53),(73,56),(73,62)])
  dot(32,57);dot(36,55);dot(92,55);dot(96,57)
 else:
  dot(41,71,1.4);dot(87,71,1.4);path([(41,63),(40,58),(38,54)]);path([(87,63),(86,58),(84,54)])
  path([(55,62),(55,56),(59,53),(64,56),(69,53),(73,56),(73,62)])
 data=np.repeat(np.repeat(arr,4,axis=0),4,axis=1);im=bpy.data.images.new(name,width=512,height=512,alpha=False);im.colorspace_settings.name='sRGB';im.pixels.foreach_set(data.ravel());im.filepath_raw=str(P/(name+'.png'));im.file_format='PNG';im.save();im.pack();return im
images=[expression('ElseIf_Expression_Test_'+n,n) for n in ['Normal','Joy','Cry']]
monitor=mat('ElseIf_FaceMonitor',(.006,.008,.014),.05,.29);nodes=monitor.node_tree.nodes;links=monitor.node_tree.links;bs=nodes.get('HeadPrincipled');bs.inputs['Coat Weight'].default_value=.12;tex=nodes.new('ShaderNodeTexImage');tex.name='ExpressionTexture';tex.label='Expression Texture / UV0';tex.image=images[0];tex.interpolation='Closest';tex.location=(-650,100);uvnode=nodes.new('ShaderNodeUVMap');uvnode.uv_map='FaceUV';uvnode.location=(-850,100);links.new(uvnode.outputs['UV'],tex.inputs['Vector']);tint=nodes.new('ShaderNodeRGB');tint.name='MonitorTint';tint.outputs[0].default_value=(1,1,1,1);tint.location=(-650,-160);mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.location=(-400,100);links.new(tex.outputs['Color'],mix.inputs[1]);links.new(tint.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],bs.inputs['Emission Color']);strength=nodes.new('ShaderNodeValue');strength.name='MonitorBrightness';strength.outputs[0].default_value=1.5;strength.location=(-400,-130);links.new(strength.outputs[0],bs.inputs['Emission Strength']);mm.materials.append(monitor);monitor['Display_Control']='ExpressionTexture image, MonitorTint RGB, MonitorBrightness value; opaque material'
# 単体確認Sceneは同じHeadオブジェクトを参照する。
solo=assembly.copy();solo.name='ElseIf_Head01_Standalone_Review';body=bpy.data.collections['ElseIf_Game_MobileVR_Test'];solo.collection.children.unlink(body)
# レビュー用カメラは新規作成し、既存カメラは変更しない。
def camera(name,location,target,scale,scene):
 d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=location;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
center=socket.matrix_world.translation+Vector((0,0,.089));cameras={}
for n,delta in [('Front',(0,-2,0)),('Side',(2,0,0)),('Back',(0,2,0)),('ThreeQuarter',(1.6,-2,.8)),('HighAngle',(1.2,-1.7,2.5))]:cameras[n]=camera('Head01_Cam_'+n,center+Vector(delta),center,.28,solo).name
for n in ['Front','ThreeQuarter','BackQuarter']:
 old=bpy.data.objects['ElseIf_Cam_'+n];copy=old.copy();copy.data=old.data.copy();copy.name='Head01_Assembly_Cam_'+n;assembly.collection.objects.link(copy)
for sc in [assembly,solo]:sc.frame_set(1);sc.cycles.samples=48;sc.cycles.seed=0
solo.render.resolution_x=720;solo.render.resolution_y=720;solo.render.resolution_percentage=100;assembly.render.resolution_x=880;assembly.render.resolution_y=1120;assembly.render.resolution_percentage=100
bpy.context.window.scene=assembly;assembly.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];bpy.context.view_layer.update();result={'root':root.name,'socket':socket.name,'root_local_location':list(root.location),'root_local_rotation':list(root.rotation_euler),'root_local_scale':list(root.scale),'cameras':cameras,'shell_dimensions':list(ob.dimensions),'monitor_dimensions':list(face.dimensions),'head_world_top':max((ob.matrix_world@v.co).z for v in m.vertices),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in [ob,face]),'vertices':len(m.vertices)+len(mm.vertices)};(P/'Build_Record.json').write_text(json.dumps(result,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_MobileVR_HeadPart01.blend'));print(result)

