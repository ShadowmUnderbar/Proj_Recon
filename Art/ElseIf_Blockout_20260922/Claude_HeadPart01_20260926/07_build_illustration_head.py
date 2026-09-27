import bpy,bmesh,json,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
from pathlib import Path
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
# イラスト準拠の頭部。座標はRoot(=HeadSocket)ローカル、前方-Y、上方+Z、単位m
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];col=bpy.data.collections['ElseIf_HeadPart_Monitor01']
shell_ob=bpy.data.objects['ElseIf_HeadShell'];face_ob=bpy.data.objects['ElseIf_FaceMonitor']
# Codex版メッシュは比較用に名前を変えて保持する
for ob,tag in [(shell_ob,'ElseIf_HeadShell_Mesh'),(face_ob,'ElseIf_FaceMonitor_Mesh')]:
    old=bpy.data.meshes.get(tag)
    if old and not bpy.data.meshes.get(tag+'_Codex'):old.name=tag+'_Codex';old.use_fake_user=True
M_SHELL=bpy.data.materials['ElseIf_HeadShell'];M_JOINT=bpy.data.materials['ElseIf_HeadJoint'];M_FACE=bpy.data.materials['ElseIf_FaceMonitor']
M_ACC=bpy.data.materials.get('ElseIf_HeadAccent')
if M_ACC is None:M_ACC=bpy.data.materials.new('ElseIf_HeadAccent')
M_ACC.use_nodes=True;nt=M_ACC.node_tree;nt.nodes.clear()
p=nt.nodes.new('ShaderNodeBsdfPrincipled');o=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(p.outputs['BSDF'],o.inputs['Surface'])
p.inputs['Base Color'].default_value=(.42,.035,.03,1);p.inputs['Roughness'].default_value=.45;M_ACC.diffuse_color=(.42,.035,.03,1)

ZC=.125     # 画面中心の高さ
N=48        # 断面の分割数（全リング共通）
def se(a,b,n,y,zc=ZC):
    # 超楕円リング。n大で角丸矩形、n小で楕円。ブラウン管の膨らみを表現する
    pts=[]
    for i in range(N):
        t=2*math.pi*i/N;c=math.cos(t);s=math.sin(t)
        pts.append((a*math.copysign(abs(c)**(2/n),c),y,zc+b*math.copysign(abs(s)**(2/n),s)))
    return pts
def loft(V,F,Mi,rings,mats,cap_end=None,cap_start=None,close=True):
    off=len(V);cnt=len(rings[0])
    for r in rings:V.extend(r)
    for k in range(len(rings)-1):
        for j in range(cnt if close else cnt-1):
            j2=(j+1)%cnt;F.append((off+k*cnt+j,off+k*cnt+j2,off+(k+1)*cnt+j2,off+(k+1)*cnt+j));Mi.append(mats[k])
    for cap,idx in [(cap_start,0),(cap_end,len(rings)-1)]:
        if cap is None:continue
        c=len(V);V.append(cap[0]);base=off+idx*cnt
        for j in range(cnt):F.append((base+j,base+(j+1)%cnt,c));Mi.append(cap[1])
def build_mesh(name,V,F,Mi,mats):
    me=bpy.data.meshes.new(name);me.from_pydata(V,[],F)
    for m in mats:me.materials.append(m)
    for p,mi in zip(me.polygons,Mi):p.material_index=mi;p.use_smooth=True
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-6);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    me.set_sharp_from_angle(angle=math.radians(38));me.update();return me

# ---- 筐体：前面フレーム → 分割溝 → 後頭部ドーム ----
V=[];F=[];Mi=[]
S,J=0,1
shell_rings=[
 se(.0790,.0670,4.0,-.0930),   # パッキン奥（画面の縁）
 se(.0825,.0705,4.0,-.0975),   # パッキン手前
 se(.0845,.0725,4.0,-.1005),   # フレーム内縁
 se(.0910,.0780,4.2,-.1025),   # フレーム前面
 se(.0955,.0825,4.4,-.1000),   # 外周の丸め
 se(.0975,.0845,4.5,-.0940),
 se(.0975,.0845,4.5,-.0800),   # フレームの帯
 se(.0945,.0815,4.5,-.0775),   # 分割溝
 se(.0945,.0815,4.5,-.0715),
 se(.0960,.0830,4.3,-.0685),   # 後部筐体
 se(.0960,.0840,4.0,-.0350),
 se(.0925,.0820,3.4, .0100),
 se(.0830,.0760,2.8, .0450),
 se(.0650,.0620,2.4, .0700),
 se(.0370,.0360,2.2, .0845)]
loft(V,F,Mi,shell_rings,[J,J,S,S,S,S,J,J,J,S,S,S,S,S],cap_end=((0,.0895,ZC),S))
# 画面下の台座（モニタースタンド）。黒い帯で上下を分ける
def rr(ax,ay,r,z,cy=0,n=6):
    pts=[]
    for cx,cyy,st in [(ax-r,ay-r,0),(-ax+r,ay-r,90),(-ax+r,-ay+r,180),(ax-r,-ay+r,270)]:
        for j in range(n):a=math.radians(st+j*90/n);pts.append((cx+r*math.cos(a),cy+cyy+r*math.sin(a),z))
    return pts
loft(V,F,Mi,[rr(.034,.026,.008,.050),rr(.034,.026,.008,.036),rr(.032,.024,.007,.034),rr(.032,.024,.007,.030),rr(.035,.027,.008,.028),rr(.033,.025,.008,.021)],[S,J,J,J,S],cap_end=((0,0,.021),S))
# 首とコネクタ。細い首を節で区切り、下端のフランジが身体側コネクタへ差し込まれる形
def circ(r,z,n=24):return [(r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z) for i in range(n)]
loft(V,F,Mi,[circ(.019,.0225),circ(.019,.0135),circ(.0225,.0120),circ(.0225,.0075),circ(.019,.0060),circ(.019,.0040),
             circ(.034,.0030),circ(.036,.0010),circ(.036,-.0010),circ(.034,-.0035),circ(.030,-.0045),circ(.030,-.0300)],
     [J,J,J,J,J,J,S,S,J,J,J],cap_end=((0,0,-.0300),J))
shell_me=build_mesh('ElseIf_HeadShell_Mesh',V,F,Mi,[M_SHELL,M_JOINT])

# ---- 画面：浅い凸面、FaceUVは正面投影を0〜1へ正規化した単一アイランド ----
SA,SB,SN,SY,BULGE=.0790,.0670,4.0,-.0930,.0060
V=[(0,SY-BULGE,ZC)];UV=[(.5,.5)];F=[]
edge=se(SA,SB,SN,0)
for t in [.25,.5,.75,1.0]:
    for x,_,z in edge:
        xx=x*t;zz=(z-ZC)*t;b=BULGE*max(0,1-(xx/SA)**2)*max(0,1-(zz/SB)**2)
        V.append((xx,SY-b,ZC+zz));UV.append((xx/(2*SA)+.5,zz/(2*SB)+.5))
for j in range(N):F.append((0,1+(j+1)%N,1+j))
for k in range(3):
    a=1+k*N;b=a+N
    for j in range(N):F.append((a+j,a+(j+1)%N,b+(j+1)%N,b+j))
face_me=bpy.data.meshes.new('ElseIf_FaceMonitor_Mesh');face_me.from_pydata(V,[],F);face_me.materials.append(M_FACE)
uvl=face_me.uv_layers.new(name='FaceUV')
for p in face_me.polygons:
    p.use_smooth=True
    for li,vi in zip(p.loop_indices,p.vertices):uvl.data[li].uv=UV[vi]
if sum(p.normal.y for p in face_me.polygons)>0:
    bm=bmesh.new();bm.from_mesh(face_me);bmesh.ops.reverse_faces(bm,faces=bm.faces);bm.to_mesh(face_me);bm.free()
face_me.update()

# ---- ケーブルの髪（簡略版）：頭頂〜側面〜後頭部から垂れる房。先端はアクセント色 ----
shell_ob.data=shell_me;face_ob.data=face_me;bpy.context.view_layer.update()
bm=bmesh.new();bm.from_mesh(shell_me);bm.faces.ensure_lookup_table()
bvh=BVHTree.FromBMesh(bm)
C=Vector((0,-.005,ZC))
def surface(d):
    hit=bvh.ray_cast(C+d*.4,-d)
    return (hit[0],hit[1]) if hit[0] else (None,None)
def hsh(a,b):
    # 決定的な擬似乱数（0〜1）。毎回同じ形を再現するため
    x=math.sin(a*12.9898+b*78.233)*43758.5453;return x-math.floor(x)
groups=[  # (極角deg, 本数, 前方除外の半角deg, 先端高さ, 外側への張り出し)
 (4,6,0,.170,.040),(22,11,0,.130,.040),(42,14,38,.100,.036),(62,14,58,.075,.030),(82,11,78,.060,.024),(102,7,120,.050,.020)]
strands=[]
for gi,(pol,cnt,excl,zend,out) in enumerate(groups):
    for i in range(cnt):
        az=2*math.pi*(i+.5*(gi%2)+.3*(hsh(gi,i)-.5))/cnt   # az=0を+X、-Y(前)は-90度
        rel=math.degrees(math.atan2(math.sin(az),math.cos(az)))
        front_off=abs(((rel+90)+180)%360-180)                     # 前方(-Y)からの角度差
        fringe=gi in (2,3) and front_off<excl
        if fringe and (gi==3 or front_off<14):continue         # 画面中央の前髪は作らない
        pp=pol+6*(hsh(i,gi)-.5)
        d=Vector((math.sin(math.radians(pp))*math.cos(az),math.sin(math.radians(pp))*math.sin(az),math.cos(math.radians(pp)))).normalized()
        p,n=surface(d)
        if p is None:continue
        jz=.018*(hsh(gi+7,i)-.5)
        strands.append((p,n,d,.212 if fringe else zend+jz,.010 if fringe else out*(.8+.4*hsh(i+3,gi)),gi,i,fringe,front_off))
R0,R1,SIDES=.0088,.0068,6
FLAT=.62   # 断面の扁平率（頭の表面方向に広い平たいケーブル）
V=[];F=[];Mi=[]
def push_out(q,r,clear):
    # 筐体へ食い込む点を外側へ押し出す
    loc,nor,_,dist=bvh.find_nearest(q)
    if loc is None:return q
    if (q-loc).dot(nor)<r+clear:q=loc+nor*(r+clear)
    return q
ts=[0,.18,.36,.54,.70,.84,.91,1.0]
AX=Vector((0,-.005,0));dropped=[]
for (p,n,d,zend,out,gi,i,fringe,front_off) in strands:
    h=Vector((d.x,d.y,0));h=h.normalized() if h.length>1e-4 else Vector((math.cos(2*math.pi*i/6),math.sin(2*math.pi*i/6),0))
    start=p-n*.005
    up=.030 if gi==0 else (.018 if gi==1 else .006)
    endr=(Vector((p.x,p.y,0))-AX).length+out
    end=AX+h*endr+Vector((0,0,zend))
    if not fringe and front_off<100:end.y=max(end.y,-.062)   # 側面の房はフレームより後ろへ垂らす
    if fringe:end.y=max(end.y,-.084)
    c1=p+n*(.022 if fringe else .034)+Vector((0,0,up))
    c2=Vector((end.x,end.y,0))+Vector((0,0,max(end.z+.03,(c1.z+end.z)*.5+.012)))
    pts=[]
    for k,t in enumerate(ts):
        a=(1-t)**3*start+3*(1-t)**2*t*c1+3*(1-t)*t*t*c2+t**3*end
        r=R0+(R1-R0)*t
        pts.append((push_out(a,r,.002+.005*math.sin(math.pi*min(t,.99))) if t>0 else a,r))
    # 画面の前に垂れる房・フレームより前へ突き出す房は作らない（表情の視認性を優先）
    # 筐体の下へ回り込み、首・襟に近づく房は作らない（首振り時の襟干渉対策）
    if any(q.z<.050 and (Vector((q.x,q.y,0))-AX).length<.090 for q,_ in pts):dropped.append((gi,i));continue
    if any((q.y<-.088 and abs(q.x)<.100 and q.z<.214) or q.y<-.106 for q,_ in pts):dropped.append((gi,i));continue
    # 平行移動フレームで断面を配置。広い面を頭の表面側へ向ける
    ring_list=[]
    for k,(q,r) in enumerate(pts):
        tan=(pts[min(k+1,len(pts)-1)][0]-pts[max(k-1,0)][0]).normalized()
        radial=(Vector((q.x,q.y,q.z-ZC*.8))).normalized()
        nrm=(radial-tan*radial.dot(tan));nrm=nrm.normalized() if nrm.length>1e-5 else tan.orthogonal().normalized()
        wide=tan.cross(nrm).normalized()
        ring_list.append([tuple(q+(wide*math.cos(2*math.pi*s/SIDES)+nrm*FLAT*math.sin(2*math.pi*s/SIDES))*r) for s in range(SIDES)])
    tipped=hsh(i+11,gi*3)<.72
    mats=[0]*(len(ts)-1)
    if tipped:mats[-2]=1;mats[-1]=2   # 黒い継ぎ輪 → 赤い先端
    loft(V,F,Mi,ring_list,mats,cap_end=(tuple(pts[-1][0]+(pts[-1][0]-pts[-2][0]).normalized()*.002),2 if tipped else 0))
bm.free()
cab_me=build_mesh('ElseIf_HeadCables_Mesh',V,F,Mi,[M_SHELL,M_JOINT,M_ACC])
cab=bpy.data.objects.get('ElseIf_HeadCables')
if cab is None:cab=bpy.data.objects.new('ElseIf_HeadCables',cab_me);col.objects.link(cab)
else:
    old=cab.data;cab.data=cab_me
    if old.users==0:bpy.data.meshes.remove(old)
cab.parent=root;cab.matrix_parent_inverse.identity();cab.location=(0,0,0);cab.rotation_euler=(0,0,0);cab.scale=(1,1,1)
# 再ビルド時の古いメッシュを掃除
for me in list(bpy.data.meshes):
    if me.users==0 and not me.use_fake_user and me.name.startswith(('ElseIf_HeadShell_Mesh','ElseIf_FaceMonitor_Mesh','ElseIf_HeadCables_Mesh')):bpy.data.meshes.remove(me)
bpy.context.view_layer.update()
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
objs=[shell_ob,face_ob,cab]
allv=[o.matrix_local@v.co for o in objs for v in o.data.vertices]
res={'strands':len(strands)-len(dropped),'dropped':dropped,'tris':{o.name:tris(o) for o in objs},'total_tris':sum(tris(o) for o in objs),
     'local_bounds':[[min(v[i] for v in allv) for i in range(3)],[max(v[i] for v in allv) for i in range(3)]]}
json.dump(res,open(D+'/Build_Record.json','w'),indent=2);bpy.ops.wm.save_mainfile();print(json.dumps(res))
