import bpy,json,math
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];o=bpy.data.objects['ElseIf_HeadShell'];m=o.data;m.calc_loop_triangles();b=bpy.data.objects['Mobile__Jacket'];e=b.evaluated_get(bpy.context.evaluated_depsgraph_get());bm=e.to_mesh();bm.calc_loop_triangles();bt=BVHTree.FromPolygons([e.matrix_world@v.co for v in bm.vertices],[tuple(t.vertices) for t in bm.loop_triangles],all_triangles=True);e.to_mesh_clear();rows=[]
for pitch in [-30,30]:
 root.rotation_euler=(math.radians(pitch),0,0);bpy.context.view_layer.update();ht=BVHTree.FromPolygons([o.matrix_world@v.co for v in m.vertices],[tuple(t.vertices) for t in m.loop_triangles],all_triangles=True);ids={a for a,b in ht.overlap(bt)};vs={i for t in ids for i in m.loop_triangles[t].vertices};rows.append({'pitch':pitch,'triangles':len(ids),'local_z':[min(m.vertices[i].co.z for i in vs),max(m.vertices[i].co.z for i in vs)] if vs else [],'ids':sorted(vs)})
root.rotation_euler=(0,0,0);bpy.context.view_layer.update();print(json.dumps(rows));(P/'Rotation_Contact_Diagnosis.json').write_text(json.dumps(rows,indent=2))
