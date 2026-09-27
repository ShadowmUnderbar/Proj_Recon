import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
# フラットケーブル改訂版：頭頂の円盤を廃止し、接続口を頭頂後方〜左右側頭部へ分散。
# 承認済みの筐体・FaceMonitorには触れず、ElseIf_HeadCablesだけを作り直す
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];col=bpy.data.collections['ElseIf_HeadPart_Monitor01']
shell=bpy.data.objects['ElseIf_HeadShell']
def sig(names):
    h=hashlib.sha256()
    for n in names:
        m=bpy.data.objects[n].data
        for v in m.vertices:h.update(struct.pack('3f',*v.co))
        for f in m.polygons:h.update(struct.pack('%di'%len(f.vertices),*f.vertices))
    return h.hexdigest()
before=sig(['ElseIf_HeadShell','ElseIf_FaceMonitor'])
M_CABLE=bpy.data.materials['ElseIf_HeadCable'];M_JOINT=bpy.data.materials['ElseIf_HeadJoint'];M_ACC=bpy.data.materials['ElseIf_HeadAccent']
bm=bmesh.new();bm.from_mesh(shell.data);bvh=BVHTree.FromBMesh(bm)
ZC=.125;CEN=Vector((0,-.004,ZC))
def surf(p):
    # 指定点に最も近い筐体表面の点と法線
    loc,nor,_,_=bvh.find_nearest(Vector(p));return loc,nor
def push_out(q,clear):
    loc,nor,_,_=bvh.find_nearest(q)
    if loc is not None and (q-loc).dot(nor)<clear:q=loc+nor*clear
    return q

# 断面：角を落とした長方形（8頂点）。幅約22mm×厚さ約4mm
HW,HT,CH=.0110,.0020,.0012
def section(q,tan,nrm,scale=1.0):
    nrm=(nrm-tan*nrm.dot(tan));nrm=nrm.normalized() if nrm.length>1e-6 else tan.orthogonal().normalized()
    wide=tan.cross(nrm).normalized();w=HW*scale;t=HT
    pts2=[(w,t-CH*.9),(w-CH,t),(-w+CH,t),(-w,t-CH*.9),(-w,-t+CH*.9),(-w+CH,-t),(w-CH,-t),(w,-t+CH*.9)]
    return [tuple(q+wide*a+nrm*b) for a,b in pts2]
# ケーブル定義：(ID, 根元の目安点, 出口方向の目安, 先端の目安点, 先端の浮かせ量, 赤い先端)
# 設定画に合わせて、頭頂後方〜側頭部上側から出し、側面〜後頭部へ垂らす。長さ・向き・曲率はそれぞれ変える
CABLES=[
 ('Cable_01',( .052,-.022,.212),( .7,-.25,0),( .106,-.050,.150),.004,False),  # 右前寄り：肩を越えて側面へ短め
 ('Cable_02',( .079, .004,.198),( .6, .45,0),( .110, .045,.092),.009,True),   # 右側頭部：後ろへ長く
 ('Cable_03',( .058, .044,.200),( .35,.9,0),( .090, .094,.070),.012,False),   # 右後方：後頭部の角を回って最長
 ('Cable_04',( .020, .036,.205),( .1, 1,0),( .030, .096,.108),.007,True),    # 頭頂後方：背面へ
 ('Cable_05',(-.018, .058,.199),(-.1, 1,0),(-.024, .098,.082),.010,False),   # 頭頂後方：背面へ長く
 ('Cable_06',(-.060, .034,.201),(-.45,.85,0),(-.094, .090,.078),.011,True),  # 左後方：後頭部の角を回る
 ('Cable_07',(-.081,-.002,.196),(-.65,.4,0),(-.111, .036,.100),.008,False),  # 左側頭部：後ろへ
 ('Cable_08',(-.048,-.030,.211),(-.75,-.2,0),(-.106,-.042,.160),.004,False), # 左前寄り：短め
 ('Cable_09',( .000, .000,.209),( .15,.95,0),( .006, .092,.142),.006,False), # 頭頂中央やや後ろ：背面上部で止まる
 ('Cable_10',( .082,-.030,.190),( .35,.3,-.2),( .108, .002,.122),.006,False),# 右側面の下寄り：中くらい
 ('Cable_11',(-.083, .024,.186),(-.4,.5,-.2),(-.106, .070,.112),.007,True),  # 左側面の後ろ寄り：中くらい
]
TS=[0,.10,.22,.36,.50,.64,.78,.90,1.0]   # 長手方向9断面（8分割）。Bone化の関節目安
V=[];F=[];MI=[];VG=[];TP=[]
def loft(rings,mats,g,tv,cap=None,capmat=0,close=True):
    off=len(V);n=len(rings[0])
    for r,t in zip(rings,tv):
        for p in r:V.append(p);VG.append(g);TP.append(t)
    for k in range(len(rings)-1):
        for j in range(n):
            j2=(j+1)%n;F.append((off+k*n+j,off+k*n+j2,off+(k+1)*n+j2,off+(k+1)*n+j));MI.append(mats[k])
    if cap is not None:
        c=len(V);V.append(tuple(cap));VG.append(g);TP.append(tv[-1]);b=off+(len(rings)-1)*n
        for j in range(n):F.append((b+j,b+(j+1)%n,c));MI.append(capmat)
report=[]
for ci,(cid,rguide,exitd,eguide,flare,tipped) in enumerate(CABLES):
    rp,rn=surf(rguide);ex=Vector(exitd);ex=(ex-rn*ex.dot(rn)).normalized()
    ep,en=surf(eguide)
    # 根元：接続口から表面に沿って出て、少し浮く → 肩で最も離れ → 重力で筐体に沿って垂れる
    p0=rp+rn*.0030
    p1=p0+ex*.026+rn*.009
    mid=surf((p1+ep)*.5)
    p2=mid[0]+mid[1]*(.012+flare*.5)
    p3=ep+en*(.004+flare)
    pts=[]
    for t in TS:
        q=(1-t)**3*p0+3*(1-t)**2*t*p1+3*(1-t)*t*t*p2+t**3*p3
        if t>0:q=push_out(q,HT+.0022)
        pts.append(q)
    # 断面の向き：前の断面から回転を最小にして引き継ぎ、表面法線へ少しずつ寄せる（ねじれ・折れ防止）
    rings=[];prev=rn
    for k,q in enumerate(pts):
        t=TS[k];d=3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)
        tan=d.normalized() if d.length>1e-7 else (pts[min(k+1,len(pts)-1)]-pts[max(k-1,0)]).normalized()
        pn=(prev-tan*prev.dot(tan)).normalized()
        sn=surf(q)[1];sn=(sn-tan*sn.dot(tan))
        nrm=(pn*.65+sn.normalized()*.35).normalized() if (k>0 and sn.length>1e-6 and sn.normalized().dot(pn)>-.2) else pn
        prev=nrm
        rings.append(section(q-tan*.006 if k==0 else q,tan,nrm,1-.08*t))
    # 幅広の断面は曲面で縁がめり込むため、断面8頂点のめり込み量を測って断面ごと外へ押し出す
    def pen(ring):
        worst=0.0
        for v in ring:
            v=Vector(v);loc,nor,_,_=bvh.find_nearest(v)
            if loc is None:continue
            dd=(v-loc).dot(nor);worst=max(worst,.0016-dd)
        return worst
    offs=[0.0]+[pen(r) for r in rings[1:]]
    for _ in range(2):   # 前後の断面と押し出し量をならし、急な折れを防ぐ
        offs=[0.0]+[max(offs[k],.5*(offs[k-1]+offs[min(k+1,len(offs)-1)])) for k in range(1,len(offs))]
    for k in range(1,len(rings)):
        if offs[k]>0:
            c=Vector(pts[k]);n_=(c-surf(c)[0]).normalized() if (c-surf(c)[0]).length>1e-6 else surf(c)[1]
            rings[k]=[tuple(Vector(v)+n_*offs[k]) for v in rings[k]];pts[k]=c+n_*offs[k]
    mats=[0]*(len(TS)-1)
    if tipped:mats[-1]=2
    tipdir=(pts[-1]-pts[-2]).normalized()
    loft(rings,mats,ci,TS,cap=pts[-1]+tipdir*.0012,capmat=2 if tipped else 0)
    # 接続口：ケーブル幅に合わせた小さな黒い差込口（筐体へ半分埋め込む）
    t0=(pts[1]-pts[0]).normalized()
    def slot(q,s,th):
        nrm=(rn-t0*rn.dot(t0)).normalized();wide=t0.cross(nrm).normalized();w=HW*s;c=.0022
        p2d=[(w,th-c),(w-c,th),(-w+c,th),(-w,th-c),(-w,-th+c),(-w+c,-th),(w-c,-th),(w,-th+c)]
        return [tuple(q+wide*a+nrm*b) for a,b in p2d]
    # 差込口は低く小さく：外側は筐体と同じ白、黒は口の縁だけ
    sq=rp+rn*.0004
    loft([slot(sq-t0*.008,1.14,.0032),slot(sq+t0*.0025,1.14,.0032),slot(sq+t0*.0034,1.04,.0026)],[3,1],len(CABLES),[0,0,0])
    report.append({'id':cid,'socket':[round(c,4) for c in rp],'tip':[round(c,4) for c in pts[-1]],'red_tip':tipped,
                   'length_mm':round(sum((pts[i+1]-pts[i]).length for i in range(len(pts)-1))*1000,1),
                   'max_lift_mm':round(max((q-surf(q)[0]).length for q in pts)*1000,1)})
bm.free()
me=bpy.data.meshes.new('ElseIf_HeadCables_Mesh_New');me.from_pydata(V,[],F)
for m in [M_CABLE,M_JOINT,M_ACC,bpy.data.materials['ElseIf_HeadShell']]:me.materials.append(m)   # 3=差込口の外側（筐体と同色）
for p,mi in zip(me.polygons,MI):p.material_index=mi;p.use_smooth=True
at=me.attributes.new('cable_t','FLOAT','POINT');at.data.foreach_set('value',TP)
me.validate();me.update()
b2=bmesh.new();b2.from_mesh(me);bmesh.ops.recalc_face_normals(b2,faces=b2.faces);b2.to_mesh(me);b2.free()
me.set_sharp_from_angle(angle=math.radians(50));me.update()
ob=bpy.data.objects['ElseIf_HeadCables'];old=ob.data;ob.data=me
if old.users==0:bpy.data.meshes.remove(old)
me.name='ElseIf_HeadCables_Mesh'
ob.vertex_groups.clear()
groups=[ob.vertex_groups.new(name=c[0]) for c in CABLES]+[ob.vertex_groups.new(name='Cable_Sockets')]
by={}
for vi,g in enumerate(VG):by.setdefault(g,[]).append(vi)
for g,ids in by.items():groups[g].add(ids,1.0,'REPLACE')
ob.parent=root;ob.matrix_parent_inverse.identity();ob.location=(0,0,0);ob.rotation_euler=(0,0,0);ob.scale=(1,1,1)
bpy.context.view_layer.update()
assert sig(['ElseIf_HeadShell','ElseIf_FaceMonitor'])==before,'承認済みの筐体/FaceMonitorが変化した'
def tri(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
heads=[o for o in col.objects if o.type=='MESH']
sock=sum(len(p.vertices)-2 for p in me.polygons if all(VG[v]==len(CABLES) for v in p.vertices))
res={'cables':len(CABLES),'cable_object_tris':tri(ob),'of_which_sockets':sock,'cable_vertices':len(me.vertices),
 'section':'面取り長方形8頂点 幅22mm×厚4mm','head_total_tris':sum(tri(o) for o in heads),'head_total_vertices':sum(len(o.data.vertices) for o in heads),
 'meshes':len(heads),'materials':sorted({m.name for o in heads for m in o.data.materials}),'shell_face_unchanged':True,'cables_detail':report}
json.dump(res,open(D+'/Cable_Build_Record.json','w'),indent=2,ensure_ascii=False);bpy.ops.wm.save_mainfile()
print(json.dumps({k:v for k,v in res.items() if k!='cables_detail'},ensure_ascii=False));print(json.dumps([(r['id'],r['length_mm'],r['max_lift_mm']) for r in report]))
