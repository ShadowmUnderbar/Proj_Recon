import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
# 承認済みの筐体・FaceMonitorには触れず、ケーブル意匠だけを別Objectとして追加する
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];col=bpy.data.collections['ElseIf_HeadPart_Monitor01']
shell=bpy.data.objects['ElseIf_HeadShell'];face=bpy.data.objects['ElseIf_FaceMonitor']
def sig(names):
    h=hashlib.sha256()
    for n in names:
        m=bpy.data.objects[n].data
        for v in m.vertices:h.update(struct.pack('3f',*v.co))
        for f in m.polygons:h.update(struct.pack('%di'%len(f.vertices),*f.vertices))
    return h.hexdigest()
before=sig(['ElseIf_HeadShell','ElseIf_FaceMonitor'])
M_CABLE=bpy.data.materials.get('ElseIf_HeadCable')
if M_CABLE is None:M_CABLE=bpy.data.materials.new('ElseIf_HeadCable')
# フラットケーブル：筐体の白と区別できる明るいグレー（設定画パレットのサブカラー）
M_CABLE.use_nodes=True;nt=M_CABLE.node_tree;nt.nodes.clear();pb=nt.nodes.new('ShaderNodeBsdfPrincipled');po=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(pb.outputs['BSDF'],po.inputs['Surface'])
pb.inputs['Base Color'].default_value=(.17,.17,.18,1);pb.inputs['Roughness'].default_value=.55;M_CABLE.diffuse_color=(.17,.17,.18,1)
M_SHELL=bpy.data.materials['ElseIf_HeadShell'];M_JOINT=bpy.data.materials['ElseIf_HeadJoint'];M_ACC=bpy.data.materials['ElseIf_HeadAccent']
bm=bmesh.new();bm.from_mesh(shell.data);bvh=BVHTree.FromBMesh(bm)
ZC=.125;AX=Vector((0,-.004,0))

def hsh(a,b):
    x=math.sin(a*12.9898+b*78.233)*43758.5453;return x-math.floor(x)
def top_point(x,y):
    hit=bvh.ray_cast(Vector((x,y,.40)),Vector((0,0,-1)));return hit[0],hit[1]
def side_point(h,z):
    hit=bvh.ray_cast(Vector((AX.x,AX.y,z))+h*.35,-h);return hit[0],hit[1]
def push_out(q,clear):
    loc,nor,_,_=bvh.find_nearest(q)
    if loc is not None and (q-loc).dot(nor)<clear:q=loc+nor*clear
    return q

# 設定画の頭部ケーブルを読み取った配置。(方位deg[+X=0,前=-90], 根元半径の倍率, 先端高さ, 張り出し)
# 頭頂のやや後ろから放射状に出て、側面と後頭部へ流れる。前方は短い前髪状で、Monitorの前へは垂らさない
CABLES=[
 (-90,1.0,.203,.000),(-66,1.0,.196,.002),(-114,1.0,.196,.002),        # 前：フレーム上端で止まる短いもの
 (-38,1.0,.142,.006),(-142,1.0,.140,.006),                          # 前寄りの側頭部
 (-12,1.0,.128,.008),(-168,1.0,.131,.008),
 (14,1.0,.122,.009),(166,1.0,.119,.009),                            # 側頭部の後ろ寄り
 (40,1.0,.114,.009),(140,1.0,.117,.009),
 (62,.95,.108,.008),(118,.95,.106,.008),                            # 後頭部
 (82,1.0,.100,.007),(98,1.0,.103,.007)]
RX,RY,CY=.050,.044,.014        # 頭頂の接続モジュール（やや後ろ寄りの楕円）。ケーブルはこの縁から出る
PAD_H=.0065                    # モジュールの高さ
W,FLAT,SIDES=.0046,.16,4       # 断面：フラットケーブル。4角の長方形で幅約13mm×厚さ約2mm（ユーザー指示）
TS=[0,.16,.34,.52,.70,.86,1.0] # 長手方向7断面（6分割）。Bone化するときはこの断面位置を関節の目安にする
V=[];F=[];MI=[];VG=[];TP=[]      # VG=所属グループ番号、TP=根元0〜先端1
def add_ring_loft(rings,mats,group,tvals,cap=None,capmat=0):
    off=len(V);n=len(rings[0])
    for r,t in zip(rings,tvals):
        for p in r:V.append(p);VG.append(group);TP.append(t)
    for k in range(len(rings)-1):
        for j in range(n):
            j2=(j+1)%n;F.append((off+k*n+j,off+k*n+j2,off+(k+1)*n+j2,off+(k+1)*n+j));MI.append(mats[k])
    if cap is not None:
        c=len(V);V.append(tuple(cap));VG.append(group);TP.append(tvals[-1]);b=off+(len(rings)-1)*n
        for j in range(n):F.append((b+j,b+(j+1)%n,c));MI.append(capmat)
def frame_ring(q,tan,nrm,w,th,n):
    nrm=(nrm-tan*nrm.dot(tan));nrm=nrm.normalized() if nrm.length>1e-6 else tan.orthogonal().normalized()
    wide=tan.cross(nrm).normalized()
    return [tuple(q+wide*math.cos(2*math.pi*(s+.5)/n)*w+nrm*math.sin(2*math.pi*(s+.5)/n)*th) for s in range(n)]
report=[]
for ci,(az,rs,zend,flare) in enumerate(CABLES):
    a=math.radians(az+5*(hsh(ci,1)-.5));h=Vector((math.cos(a),math.sin(a),0))
    rp,rn=top_point(h.x*RX*rs,CY+h.y*RY*rs)
    if rp is None:continue
    front=az<-50 and az>-130
    # 案内点：根元 → 少し持ち上げて外へ → 筐体の肩 → 先端
    rp=rp+rn*.0022              # 接続溝の高さから出す
    p1=rp+rn*.004+h*.016
    if front:
        ep,epn=side_point(h,.200)
        end=Vector((ep.x,ep.y,zend))+h*.004 if ep else rp+h*.05
        shoulder=(p1+end)*.5+Vector((0,0,.006))
    else:
        sp,sn=side_point(h,.192-.004*(hsh(ci,2)))
        shoulder=sp+sn*.014 if sp else p1+h*.03
        ep,en=side_point(h,zend)
        end=(ep+en*(.0080+flare*1.3)) if ep else shoulder-Vector((0,0,.05))
    # 3次Bezier：肩を通るよう制御点を補正
    c1=p1+(shoulder-p1)*.9+Vector((0,0,.010));c2=shoulder+(end-shoulder)*.45+(end-shoulder).cross(Vector((0,0,1))).normalized()*.004*(hsh(ci,3)-.5)
    pts=[]
    for t in TS:
        q=(1-t)**3*rp+3*(1-t)**2*t*c1+3*(1-t)*t*t*c2+t**3*end
        if t>0:q=push_out(q,FLAT*W+.0018)
        pts.append(q)
    rings=[]
    for k,q in enumerate(pts):
        tan=(pts[min(k+1,len(pts)-1)]-pts[max(k-1,0)]).normalized()
        loc,nor,_,_=bvh.find_nearest(q);nrm=nor if nor is not None else Vector((0,0,1))
        taper=1-.18*TS[k]
        rings.append(frame_ring(q-h*.005 if k==0 else q,tan,nrm,W*taper,W*FLAT*taper,SIDES))
    tipped=hsh(ci,7)<.45 and not front
    mats=[0]*(len(TS)-1)
    if tipped:mats[-1]=2
    tipdir=(pts[-1]-pts[-2]).normalized()
    add_ring_loft(rings,mats,ci,TS,cap=pts[-1]+tipdir*.0015,capmat=2 if tipped else 0)
    report.append({'id':'Cable_%02d'%(ci+1),'root':[round(c,4) for c in rp],'tip':[round(c,4) for c in pts[-1]],'tipped':tipped,
                   'length_mm':round(sum((pts[i+1]-pts[i]).length for i in range(len(pts)-1))*1000,1)})
# 頭頂の接続モジュール：黒い接続溝（側面）＋白い上面。筐体の傾斜に沿わせる
def pad_ring(scale,off,n=24):
    out=[]
    for i in range(n):
        t=2*math.pi*i/n;c=math.cos(t);sn=math.sin(t)
        x=RX*scale*math.copysign(abs(c)**(2/3.2),c);y=CY+RY*scale*math.copysign(abs(sn)**(2/3.2),sn)
        q,nn=top_point(x,y);out.append(tuple(q+nn*off))
    return out
add_ring_loft([pad_ring(1.0,-.002),pad_ring(1.0,PAD_H*.62),pad_ring(.975,PAD_H*.85),pad_ring(.90,PAD_H)],[1,3,3],len(CABLES),[0,0,0,0],
              cap=tuple(Vector(top_point(0,CY)[0])+Vector((0,0,PAD_H+.0006))),capmat=3)
bm.free()
me=bpy.data.meshes.new('ElseIf_HeadCables_Mesh_New');me.from_pydata(V,[],F)
for m in [M_CABLE,M_JOINT,M_ACC,M_SHELL]:me.materials.append(m)   # 3=接続モジュール上面は筐体と同じ白
for p,mi in zip(me.polygons,MI):p.material_index=mi;p.use_smooth=True
me.set_sharp_from_angle(angle=math.radians(50))
# Rigging準備：ケーブルごとの頂点グループと、根元0〜先端1の属性
at=me.attributes.new('cable_t','FLOAT','POINT');at.data.foreach_set('value',TP)
me.validate();me.update()
ob=bpy.data.objects.get('ElseIf_HeadCables')
if ob is None:ob=bpy.data.objects.new('ElseIf_HeadCables',me);col.objects.link(ob)
else:
    old=ob.data;ob.data=me
    if old.users==0:bpy.data.meshes.remove(old)
ob.vertex_groups.clear()
groups=[ob.vertex_groups.new(name='Cable_%02d'%(i+1)) for i in range(len(CABLES))]+[ob.vertex_groups.new(name='Cable_Socket_Module')]
by={}
for vi,g in enumerate(VG):by.setdefault(g,[]).append(vi)
for g,ids in by.items():groups[g].add(ids,1.0,'REPLACE')
bm2=bmesh.new();bm2.from_mesh(me);bmesh.ops.recalc_face_normals(bm2,faces=bm2.faces);bm2.to_mesh(me);bm2.free();me.update()
me.name='ElseIf_HeadCables_Mesh'
ob.parent=root;ob.matrix_parent_inverse.identity();ob.location=(0,0,0);ob.rotation_euler=(0,0,0);ob.scale=(1,1,1)
bpy.context.view_layer.update()
assert sig(['ElseIf_HeadShell','ElseIf_FaceMonitor'])==before,'承認済みの筐体/FaceMonitorが変化した'
def tri(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
cable_tris=tri(ob);sock_tris=sum(len(p.vertices)-2 for p in me.polygons if all(VG[v]==len(CABLES) for v in p.vertices))
heads=[o for o in col.objects if o.type=='MESH']
res={'cables':len(report),'cable_object_tris':cable_tris,'of_which_socket_module':sock_tris,'cable_vertices':len(me.vertices),
 'head_total_tris':sum(tri(o) for o in heads),'head_total_vertices':sum(len(o.data.vertices) for o in heads),'meshes':len(heads),
 'materials':sorted({m.name for o in heads for m in o.data.materials}),'shell_face_unchanged':True,'cables_detail':report}
json.dump(res,open(D+'/Cable_Build_Record.json','w'),indent=2,ensure_ascii=False);bpy.ops.wm.save_mainfile()
print(json.dumps({k:v for k,v in res.items() if k!='cables_detail'}))
