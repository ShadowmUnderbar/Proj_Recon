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
 se(.0960,.0830,4.3,-.0685),   # 後部筐体：フレームから自然につなぎ、後ろへ向けて幅と高さを絞る
 se(.0950,.0825,4.2,-.0300,ZC-.001),
 se(.0920,.0805,4.0, .0150,ZC-.003),
 se(.0880,.0775,3.8, .0460,ZC-.005),
 se(.0840,.0740,3.6, .0620,ZC-.006),   # 背面の角落とし
 se(.0780,.0690,3.6, .0715,ZC-.006),
 se(.0700,.0620,3.6, .0755,ZC-.006),   # 背面パネル外周
 se(.0665,.0585,3.6, .0757,ZC-.006),   # 背面パネルの継ぎ目
 se(.0650,.0570,3.6, .0740,ZC-.006),
 se(.0635,.0555,3.6, .0742,ZC-.006)]
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

# ---- ケーブル状パーツは廃止（2026-09-26 ユーザー指示）----
shell_ob.data=shell_me;face_ob.data=face_me
cab=bpy.data.objects.get('ElseIf_HeadCables')
if cab:
    me=cab.data;bpy.data.objects.remove(cab)
    if me and me.users==0:bpy.data.meshes.remove(me)
for me in list(bpy.data.meshes):
    if me.users==0 and not me.use_fake_user and me.name.startswith(('ElseIf_HeadShell_Mesh','ElseIf_FaceMonitor_Mesh','ElseIf_HeadCables_Mesh')):bpy.data.meshes.remove(me)
for o,n in [(shell_ob,'ElseIf_HeadShell_Mesh'),(face_ob,'ElseIf_FaceMonitor_Mesh')]:o.data.name=n
bpy.context.view_layer.update()
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
objs=[shell_ob,face_ob]
allv=[o.matrix_local@v.co for o in objs for v in o.data.vertices]
res={'tris':{o.name:tris(o) for o in objs},'total_tris':sum(tris(o) for o in objs),'head_objects':[o.name for o in col.objects],
     'local_bounds':[[min(v[i] for v in allv) for i in range(3)],[max(v[i] for v in allv) for i in range(3)]]}
json.dump(res,open(D+'/Build_Record.json','w'),indent=2);bpy.ops.wm.save_mainfile();print(json.dumps(res))
