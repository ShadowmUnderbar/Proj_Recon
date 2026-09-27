import bpy,json,math,ast,hashlib,struct
from pathlib import Path
from mathutils.bvhtree import BVHTree
D=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926');CX=D.parent/'HeadPart01_20260926'
s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;s.frame_set(1)
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];heads=[o for o in bpy.data.collections['ElseIf_HeadPart_Monitor01'].objects if o.type=='MESH']
def tree(o):
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    t=BVHTree.FromPolygons([e.matrix_world@x.co for x in m.vertices],[tuple(x.vertices) for x in m.loop_triangles],all_triangles=True);e.to_mesh_clear();return t
body=tree(bpy.data.objects['Mobile__Jacket'])
poses=[(0,0,y) for y in range(0,360,15)]+[(p,0,y) for p in [-30,-15,15,30] for y in [0,90,180,270]]+[(0,r,y) for r in [-15,15] for y in [0,90,180,270]]
rows=[]
for pitch,roll,yaw in poses:
    root.rotation_euler=tuple(math.radians(a) for a in (pitch,roll,yaw));bpy.context.view_layer.update()
    per={o.name:len(tree(o).overlap(body)) for o in heads};rows.append({'pitch':pitch,'roll':roll,'yaw':yaw,**per,'total':sum(per.values())})
root.rotation_euler=(0,0,0);bpy.context.view_layer.update()
# 頭部パーツ同士（画面と筐体）の貫通も確認
self_face_shell=len(tree(bpy.data.objects['ElseIf_FaceMonitor']).overlap(tree(bpy.data.objects['ElseIf_HeadShell'])))
m=bpy.data.objects['ElseIf_FaceMonitor'].data;uv=m.uv_layers['FaceUV'];m.calc_loop_triangles();areas=[]
for t in m.loop_triangles:
    a,b,c=[uv.data[i].uv for i in t.loops];areas.append(((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5)
us=[tuple(x.uv) for x in uv.data]
code=(D.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(code).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
for sc in list(bpy.data.scenes):bpy.context.window.scene=sc;bpy.context.view_layer.update()
bpy.context.window.scene=s
before=json.loads((CX/'Protected_Before.json').read_text());changed=[n for n,v in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=v]
rep={'rotation_poses':len(rows),'rotation_contact_poses':[r for r in rows if r['total']],'max_pairs':max(r['total'] for r in rows),
 'face_vs_shell_overlap_pairs':self_face_shell,
 'uv':{'uv_layers':[l.name for l in m.uv_layers],'min':[min(u[i] for u in us) for i in range(2)],'max':[max(u[i] for u in us) for i in range(2)],'zero_area':sum(abs(a)<1e-10 for a in areas),'negative_area':sum(a<0 for a in areas)},
 'root_local':{'loc':list(root.location),'rot':list(root.rotation_euler),'scale':list(root.scale),'parent':root.parent.name},
 'children':{o.name:{'parent':o.parent.name,'loc':list(o.location),'rot':list(o.rotation_euler),'scale':list(o.scale),'mats':[x.name for x in o.data.materials]} for o in heads},
 'protected_checked':len(before),'protected_changed':changed,
 'world_top':max((o.matrix_world@v.co).z for o in heads for v in o.data.vertices)}
(D/'Head_Validation.json').write_text(json.dumps({'summary':rep,'rows':rows},indent=2));print(json.dumps(rep))
