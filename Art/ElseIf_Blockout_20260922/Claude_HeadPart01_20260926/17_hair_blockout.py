import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
# 髪型の一次ブロックアウト：髪の房（Hair Strand）をフラットケーブル状の房へ置き換える
# 役割：トップ（短い房でつむじとボリューム）／前髪／横髪／後ろ髪。内側・中間・外側の3層で重ねる
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
M_HAIR=bpy.data.materials['ElseIf_HeadCable']
bm=bmesh.new();bm.from_mesh(shell.data);bvh=BVHTree.FromBMesh(bm)

C=Vector((0,-.005,.140))                 # 髪型の中心
ELL=Vector((.108,.098,.100))             # 髪型の外形（丸いシルエットの基準楕円体）
POLE=Vector((0,.22,1)).normalized()      # つむじ方向（頭頂のやや後ろ）
GAP=.008                                 # 筐体との最小の隙間
def house_r(d):
    hit=bvh.ray_cast(C+d*.5,-d)
    return (C-hit[0]).length if hit[0] else 0.0
def ell_r(d):
    return 1/math.sqrt((d.x/ELL.x)**2+(d.y/ELL.y)**2+(d.z/ELL.z)**2)
def vol_point(theta,phi,layer):
    # つむじを極とする球面座標 → 髪型の外形上の点（筐体より必ずGAP以上外側）
    ref=Vector((1,0,0));ex=(ref-POLE*ref.dot(POLE)).normalized();ey=POLE.cross(ex)
    d=(POLE*math.cos(theta)+(ex*math.cos(phi)+ey*math.sin(phi))*math.sin(theta)).normalized()
    return C+d*max(ell_r(d),house_r(d)+GAP)*layer,d
def push_out(q,clear):
    loc,nor,_,_=bvh.find_nearest(q)
    if loc is not None and (q-loc).dot(nor)<clear:q=loc+nor*clear
    return q
def hsh(a,b):
    x=math.sin(a*12.9898+b*78.233)*43758.5453;return x-math.floor(x)

# 房の定義：(名前, 根元の極角deg, 方位deg, 層, 先端高さ, 最大半幅, ねじれdeg)
# 方位はつむじ座標系：0=+X(右)、-90付近=前、+90付近=後ろ（POLEの傾きで少しずれる）
S=[]
for i,(ph,te) in enumerate([(-100,40),(-40,46),(8,42),(52,50),(98,44),(146,48),(196,42),(242,46)]):
    S.append(('Hair_Top_%02d'%(i+1),2+3*hsh(i,1),ph+8*(hsh(i,2)-.5),1.13,None,.0125,10*(hsh(i,3)-.5),te))  # トップ：短い
for i,ph in enumerate([-122,-101,-80,-58]):
    S.append(('Hair_Bang_%02d'%(i+1),14,ph,1.07,.203+.008*hsh(i,4),.0140,8*(hsh(i,5)-.5),None))            # 前髪：額に少し掛かる
for side,sg in [('R',1),('L',-1)]:
    for i,(ph,layer,zend) in enumerate([(-34,1.00,.100),(-12,1.10,.128),(10,1.00,.090),(32,1.06,.106),(54,1.12,.122)]):
        a=ph if sg>0 else 180-ph
        S.append(('Hair_Side_%s_%02d'%(side,i+1),18+4*hsh(i,6+sg),a+5*(hsh(i,7)-.5),layer,zend+.006*(hsh(i,8+sg)-.5),.0150,12*(hsh(i,9)-.5)*sg,None))
for i,(ph,layer,zend) in enumerate([(66,1.00,.078),(80,1.06,.094),(90,1.00,.074),(100,1.12,.110),(112,1.00,.080),(76,1.12,.112),(104,1.06,.090),(126,1.06,.098),(54,1.06,.100)]):
    S.append(('Hair_Back_%02d'%(i+1),16+6*hsh(i,10),ph+4*(hsh(i,11)-.5),layer,zend,.0155,10*(hsh(i,12)-.5),None))   # 後ろ髪：やや長い

HT=.0022      # 半厚（フラットケーブルの厚み約4.4mm）
ARCH=.0030    # 幅方向の湾曲
U=[-1,-1/3,1/3,1]
def section(q,tan,out,hw,tw):
    out=(out-tan*out.dot(tan));out=out.normalized() if out.length>1e-6 else tan.orthogonal().normalized()
    wide=tan.cross(out).normalized()
    rot=Matrix.Rotation(math.radians(tw),3,tan);wide=rot@wide;out=rot@out
    ring=[q+wide*u*hw+out*(HT+ARCH*(1-u*u)) for u in U]
    ring+=[q+wide*u*hw+out*(-HT+ARCH*.6*(1-u*u)) for u in reversed(U)]
    return [tuple(p) for p in ring]
V=[];F=[];VG=[];TP=[]
def loft(rings,g,tv,cap):
    off=len(V);n=len(rings[0])
    for r,t in zip(rings,tv):
        for p in r:V.append(p);VG.append(g);TP.append(t)
    for k in range(len(rings)-1):
        for j in range(n):
            j2=(j+1)%n;F.append((off+k*n+j,off+k*n+j2,off+(k+1)*n+j2,off+(k+1)*n+j))
    c=len(V);V.append(tuple(cap));VG.append(g);TP.append(1.0);b=off+(len(rings)-1)*n
    for j in range(n):F.append((b+j,b+(j+1)%n,c))
    # 根元は外から見えないよう内側に埋めるため、蓋は付けない
report=[]
for gi,(name,t0,ph,layer,zend,hw,tw,te) in enumerate(S):
    p=math.radians(ph);pts=[]
    # 1) 球面上をつむじから外へ流れる
    te_=math.radians(te) if te else math.radians(104)
    n1=10 if te is None else 6
    for k in range(n1+1):
        th=math.radians(t0)+(te_-math.radians(t0))*k/n1
        q,_=vol_point(th,p+math.radians(3*math.sin(k*.7+gi)),layer if k>0 else 1.0)
        pts.append(q)
        if zend is not None and q.z<=zend:break
    # 2) 外形の赤道を越えたら重力で真下へ落ちる（軽く外へ膨らみ、先端は少し内へ）
    if zend is not None and pts[-1].z>zend:
        last=pts[-1];hdir=Vector((last.x-C.x,last.y-C.y,0)).normalized();drop=last.z-zend
        m=max(2,round(drop/.016))
        for k in range(1,m+1):
            t=k/m;pts.append(last+Vector((0,0,-drop*t))+hdir*(.006*math.sin(math.pi*t*.8)-.003*t*t))
    # 途中で筐体に近づき過ぎた点を外へ押し出す
    pts=[pts[0]]+[push_out(q,GAP*.6) for q in pts[1:]]
    # 根元は下の層へ潜り込ませる
    pts[0]=pts[0]-(pts[1]-pts[0]).normalized()*.004+(C-pts[0]).normalized()*.006
    L=[0.0]
    for k in range(1,len(pts)):L.append(L[-1]+(pts[k]-pts[k-1]).length)
    tv=[l/L[-1] for l in L]
    rings=[]
    for k,q in enumerate(pts):
        tan=(pts[min(k+1,len(pts)-1)]-pts[max(k-1,0)]).normalized();t=tv[k]
        w=hw*(.62+.55*math.sin(math.pi*min(t*1.15,1))**.8)*(1-.35*t)     # 根元はまとまり、中央で広がり、先端へ細く
        rings.append(section(q,tan,q-C,w,tw*math.sin(math.pi*t)))
    loft(rings,gi,tv,pts[-1]+(pts[-1]-pts[-2]).normalized()*.0015)
    report.append({'id':name,'layer':layer,'length_mm':round(L[-1]*1000,1),'tip_z':round(pts[-1].z,3),'segments':len(pts)-1})
bm.free()
me=bpy.data.meshes.new('ElseIf_HeadCableHair_Mesh_New');me.from_pydata(V,[],F);me.materials.append(M_HAIR)
for pl in me.polygons:pl.use_smooth=True
at=me.attributes.new('cable_t','FLOAT','POINT');at.data.foreach_set('value',TP)
me.validate();me.update()
b2=bmesh.new();b2.from_mesh(me);bmesh.ops.recalc_face_normals(b2,faces=b2.faces);b2.to_mesh(me);b2.free()
me.set_sharp_from_angle(angle=math.radians(55));me.update()
ob=bpy.data.objects.get('ElseIf_HeadCableHair')
if ob is None:ob=bpy.data.objects.new('ElseIf_HeadCableHair',me);col.objects.link(ob)
else:
    old=ob.data;ob.data=me
    if old.users==0:bpy.data.meshes.remove(old)
me.name='ElseIf_HeadCableHair_Mesh'
ob.vertex_groups.clear()
gs=[ob.vertex_groups.new(name=s[0]) for s in S]
by={}
for vi,g in enumerate(VG):by.setdefault(g,[]).append(vi)
for g,ids in by.items():gs[g].add(ids,1.0,'REPLACE')
ob.parent=root;ob.matrix_parent_inverse.identity();ob.location=(0,0,0);ob.rotation_euler=(0,0,0);ob.scale=(1,1,1)
bpy.context.view_layer.update()
assert sig(['ElseIf_HeadShell','ElseIf_FaceMonitor'])==before
def tri(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
heads=[o for o in col.objects if o.type=='MESH']
roles={}
for s in S:roles[s[0].rsplit('_',1)[0]]=roles.get(s[0].rsplit('_',1)[0],0)+1
res={'strands':len(S),'roles':roles,'hair_tris':tri(ob),'hair_vertices':len(me.vertices),'head_total_tris':sum(tri(o) for o in heads),
     'head_total_vertices':sum(len(o.data.vertices) for o in heads),'meshes':[o.name for o in heads],'hair_top_z':max(v.co.z for v in me.vertices),'detail':report}
json.dump(res,open(D+'/Hair_Blockout_Record.json','w'),indent=2,ensure_ascii=False);bpy.ops.wm.save_mainfile()
print(json.dumps({k:v for k,v in res.items() if k!='detail'},ensure_ascii=False))
