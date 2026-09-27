import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
# 筐体の上側だけを延長する。FaceMonitor・首・台座・下半分は従来どおり
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];col=bpy.data.collections['ElseIf_HeadPart_Monitor01']
shell_ob=bpy.data.objects['ElseIf_HeadShell'];face_ob=bpy.data.objects['ElseIf_FaceMonitor']
def sig(o):
    h=hashlib.sha256();m=o.data
    for v in m.vertices:h.update(struct.pack('3f',*v.co))
    for f in m.polygons:h.update(struct.pack('%di'%len(f.vertices),*f.vertices))
    return h.hexdigest()
face_before=sig(face_ob)
M_SHELL=bpy.data.materials['ElseIf_HeadShell'];M_JOINT=bpy.data.materials['ElseIf_HeadJoint'];M_FACE=bpy.data.materials['ElseIf_FaceMonitor']
M_ACC=bpy.data.materials['ElseIf_HeadAccent']

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
E=.026      # Monitorより上の筐体を上方向へ延長する量（額と頭頂の丸みを作る）
def se2(a,b,bt,n,nt,y,zc=ZC):
    # 下半分は従来の超楕円、上半分だけ高さbt・指数ntで延長する（Monitorの縁より内側のリングは延長しない）
    pts=[]
    for i in range(N):
        t=2*math.pi*i/N;c=math.cos(t);s=math.sin(t)
        if s>=0:
            w=s*s   # 赤道(w=0)では従来形状、頭頂(w=1)へ向けて丸い指数ntへ移行
            x=a*((1-w)*math.copysign(abs(c)**(2/n),c)+w*math.copysign(abs(c)**(2/nt),c))
            pts.append((x,y,zc+bt*s**(2/nt)))
        else:pts.append((a*math.copysign(abs(c)**(2/n),c),y,zc-b*abs(s)**(2/n)))
    return pts
shell_rings=[
 se(.0790,.0670,4.0,-.0930),   # パッキン奥（画面の縁）※Monitor周りは変更しない
 se(.0825,.0705,4.0,-.0975),   # パッキン手前
 se(.0845,.0725,4.0,-.1005),   # フレーム内縁
 se2(.0910,.0780,.0780+E-.005,4.2,3.8,-.1025),   # フレーム前面：上側が額になる
 se2(.0955,.0825,.0825+E-.002,4.4,3.6,-.1000),
 se2(.0975,.0845,.0845+E,4.5,3.4,-.0940),
 se2(.0975,.0845,.0845+E,4.5,3.4,-.0800),
 se2(.0945,.0815,.0815+E,4.5,3.4,-.0775),       # 分割溝
 se2(.0945,.0815,.0815+E,4.5,3.4,-.0715),
 se2(.0960,.0830,.0830+E,4.3,3.2,-.0685),       # 後部筐体：頭頂は後ろへ向けて丸く下げる
 se2(.0950,.0825,.0825+E-.002,4.2,3.0,-.0300,ZC-.001),
 se2(.0920,.0805,.0805+E-.007,4.0,2.8, .0150,ZC-.003),
 se2(.0880,.0775,.0775+E-.014,3.8,2.6, .0460,ZC-.005),
 se2(.0840,.0740,.0740+E-.019,3.6,2.5, .0620,ZC-.006),
 se2(.0780,.0690,.0690+E-.022,3.6,2.5, .0715,ZC-.006),
 se2(.0700,.0620,.0620+E-.022,3.6,2.6, .0755,ZC-.006),   # 背面パネル外周
 se2(.0665,.0585,.0585+E-.022,3.6,2.6, .0757,ZC-.006),
 se2(.0650,.0570,.0570+E-.022,3.6,2.6, .0740,ZC-.006),
 se2(.0635,.0555,.0555+E-.022,3.6,2.6, .0742,ZC-.006)]
loft(V,F,Mi,shell_rings,[J,J,S,S,S,S,J,J,J,S,S,S,S,S,S,J,J,S],cap_end=((0,.0748,ZC-.006),S))
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
A=2
def ycirc(r,y,cz,n=16):return [(r*math.cos(2*math.pi*i/n),y,cz+r*math.sin(2*math.pi*i/n)) for i in range(n)]
PZ=ZC-.030   # 背面コネクタの高さ（背面パネル下寄り）
loft(V,F,Mi,[ycirc(.0150,.0735,PZ),ycirc(.0150,.0790,PZ),ycirc(.0125,.0795,PZ),ycirc(.0085,.0795,PZ),ycirc(.0085,.0770,PZ)],[J,J,J,J],cap_end=((0,.0770,PZ),J))
def xbox(x0,x1,y,z,hy,hz):
    # X方向へ張り出す小さな角丸タブ
    ring=lambda x,s:[(x,y+hy*s*math.cos(2*math.pi*(i+.5)/8)/.924,z+hz*s*math.sin(2*math.pi*(i+.5)/8)/.924) for i in range(8)]
    return [ring(x0,1),ring(x1-.0012,1),ring(x1,.8)]
loft(V,F,Mi,xbox(.0935,.1000,-.052,ZC+.030,.0085,.0050),[A,A],cap_end=((.1000,-.052,ZC+.030),A))
shell_me=build_mesh('ElseIf_HeadShell_Mesh',V,F,Mi,[M_SHELL,M_JOINT,M_ACC])
old=shell_ob.data;shell_ob.data=shell_me
if old.users==0 and not old.use_fake_user:bpy.data.meshes.remove(old)
shell_me.name='ElseIf_HeadShell_Mesh'
# v2のケーブルは髪型ブロックアウトで置き換えるため、この作業ファイルからは外す（v2は別ファイルに保持済み）
cab=bpy.data.objects.get('ElseIf_HeadCables')
if cab:
    me=cab.data;bpy.data.objects.remove(cab)
    if me.users==0:bpy.data.meshes.remove(me)
bpy.context.view_layer.update()
assert sig(face_ob)==face_before
zs=[v.co.z for v in shell_me.vertices];ys=[v.co.y for v in shell_me.vertices];xs=[v.co.x for v in shell_me.vertices]
res={'shell_tris':sum(len(p.vertices)-2 for p in shell_me.polygons),'shell_top_z':max(zs),'x':[min(xs),max(xs)],'y':[min(ys),max(ys)],'face_unchanged':True}
bpy.ops.wm.save_mainfile();print(json.dumps(res))
