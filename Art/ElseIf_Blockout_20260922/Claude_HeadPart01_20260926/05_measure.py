import bpy,json,numpy as np
from mathutils import Vector
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
# 04で作業ファイルを開いた状態を前提とする（open直後は同一スクリプト内でwindowがNoneになるため分割）
from pathlib import Path
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_Work.blend'),bpy.data.filepath
s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;s.frame_set(1)
deps=bpy.context.evaluated_depsgraph_get()
sock=bpy.data.objects['Mobile__LOD0__Backup__Fix__HeadSocket'];sp=sock.matrix_world.translation
r={'socket':list(sp),'socket_parent':sock.parent.name if sock.parent else None,'socket_parent_type':sock.parent_type,'socket_bone':sock.parent_bone}
def wverts(o):
    e=o.evaluated_get(deps);m=e.to_mesh();a=np.array([list(e.matrix_world@v.co) for v in m.vertices]);e.to_mesh_clear();return a
jk=wverts(bpy.data.objects['Mobile__Jacket'])
rel=jk-np.array(sp);near=rel[(rel[:,2]>-0.12)&(np.hypot(rel[:,0],rel[:,1])<0.16)]
r['jacket_near_socket_count']=len(near)
# 高さごとに襟の内径（ソケット軸からの最小水平距離）と外径
rows=[]
for z0 in np.arange(-0.10,0.08,0.01):
    b=near[(near[:,2]>=z0)&(near[:,2]<z0+0.01)]
    if len(b):h=np.hypot(b[:,0],b[:,1]);rows.append([round(float(z0),3),len(b),round(float(h.min()),4),round(float(h.max()),4)])
r['collar_rings_rel_socket']=rows
r['jacket_top_rel']=float(rel[:,2].max())
col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];allv=np.vstack([wverts(o) for o in col.objects if o.type=='MESH'])
r['body_bounds']=[allv.min(0).tolist(),allv.max(0).tolist()]
for n in ['ElseIf_HeadShell','ElseIf_FaceMonitor']:
    o=bpy.data.objects[n];a=np.array([list(v.co) for v in o.data.vertices]);r[n]={'local_min':a.min(0).tolist(),'local_max':a.max(0).tolist(),'mats':[m.name for m in o.data.materials]}
r['cams']={sc.name:[o.name for o in sc.objects if o.type=='CAMERA'] for sc in bpy.data.scenes if 'Head01' in sc.name}
r['head_col_objs']=[o.name for o in bpy.data.collections['ElseIf_HeadPart_Monitor01'].objects]
r['assembly_scene_colls']=[c.name for c in s.collection.children]
print(json.dumps(r))
