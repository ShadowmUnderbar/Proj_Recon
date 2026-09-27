import bpy,json,math
from mathutils.bvhtree import BVHTree
s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];cab=bpy.data.objects['ElseIf_HeadCables']
jk=bpy.data.objects['Mobile__Jacket'];e=jk.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
bt=BVHTree.FromPolygons([e.matrix_world@v.co for v in m.vertices],[tuple(t.vertices) for t in m.loop_triangles],all_triangles=True);e.to_mesh_clear()
cm=cab.data;cm.calc_loop_triangles();out={}
for pitch,yaw in [(30,270),(30,240),(30,300),(-30,90)]:
    root.rotation_euler=(math.radians(pitch),0,math.radians(yaw));bpy.context.view_layer.update()
    ht=BVHTree.FromPolygons([cab.matrix_world@v.co for v in cm.vertices],[tuple(t.vertices) for t in cm.loop_triangles],all_triangles=True)
    vs={i for a,b in ht.overlap(bt) for i in cm.loop_triangles[a].vertices}
    out[f'{pitch},{yaw}']=[[round(c,3) for c in cm.vertices[i].co] for i in sorted(vs)][:12]
root.rotation_euler=(0,0,0);bpy.context.view_layer.update();print(json.dumps(out))
