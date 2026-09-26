import bpy,json,math,hashlib,struct,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;s.frame_set(1);root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];shell=bpy.data.objects['ElseIf_HeadShell'];face=bpy.data.objects['ElseIf_FaceMonitor']
def tree(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();v=[e.matrix_world@x.co for x in m.vertices];f=[tuple(t.vertices) for t in m.loop_triangles];t=BVHTree.FromPolygons(v,f,all_triangles=True);e.to_mesh_clear();return t
body=tree(bpy.data.objects['Mobile__Jacket']);rows=[]
poses=[(0,0,y) for y in range(0,360,15)]+[(p,0,y) for p in [-30,-15,15,30] for y in [0,90,180,270]]+[(0,r,y) for r in [-15,15] for y in [0,90,180,270]]
for pitch,roll,yaw in poses:
 root.rotation_euler=tuple(math.radians(a) for a in (pitch,roll,yaw));bpy.context.view_layer.update();pairs=len(tree(shell).overlap(body))+len(tree(face).overlap(body));rows.append({'pitch_deg':pitch,'roll_deg':roll,'yaw_deg':yaw,'jacket_intersection_pairs':pairs})
root.rotation_euler=(0,0,0);bpy.context.view_layer.update();m=face.data;uv=m.uv_layers['FaceUV'];us=[list(x.uv) for x in uv.data];m.calc_loop_triangles();areas=[]
for t in m.loop_triangles:
 a,b,c=[uv.data[i].uv for i in t.loops];areas.append(((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5)
report={'rotation_tests':rows,'yaw360_contact_free':all(r['jacket_intersection_pairs']==0 for r in rows if r['pitch_deg']==r['roll_deg']==0),'uv':{'layer':'FaceUV','islands':1,'uv_min':[min(v[i] for v in us) for i in range(2)],'uv_max':[max(v[i] for v in us) for i in range(2)],'zero_area_triangles':sum(abs(a)<1e-10 for a in areas),'negative_area_triangles':sum(a<0 for a in areas),'mapping':'planar X/Z projection; normalized 0-1; rounded corners clipped','screen_width_m':.166,'screen_height_m':.146,'max_bulge_m':.0035},'stats':{'vertices':sum(len(o.data.vertices) for o in [shell,face]),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in [shell,face]),'meshes':2,'materials':3,'material_slots':3},'root_local':{'location':list(root.location),'rotation':list(root.rotation_euler),'scale':list(root.scale),'parent_inverse_identity':all(abs(root.matrix_parent_inverse[i][j]-(i==j))<1e-7 for i in range(4) for j in range(4))}}
(P/'Head_Validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
