import bpy,bmesh
s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;s.frame_set(1)
o=bpy.data.objects['Opt__ElseIf_Jacket_Back'];bm=bmesh.new();bm.from_mesh(o.data);print('Shape layers',bm.verts.layers.shape.keys());print('Counts',len(bm.verts),len(bm.faces));bm.free()
