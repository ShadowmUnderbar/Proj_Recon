import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
bpy.context.window.scene=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.view_layer.update()
for side in ['L','R']:
 target=bpy.data.objects['ElseIf_Shoe_'+side+'_Tongue'];e=target.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[target.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices) for p in m.polygons];tree=BVHTree.FromPolygons(vs,fs);e.to_mesh_clear()
 for prefix in ['', 'NaturalPose__']:
  o=bpy.data.objects[prefix+'EL_Shoe_'+side+'_RedCross']
  for v in o.data.vertices:
   p=o.matrix_world@v.co;hit=tree.ray_cast(Vector((p.x,-.3,p.z)),Vector((0,1,0)))
   if hit[0] is not None:v.co=o.matrix_world.inverted()@(hit[0]+Vector((0,-.0015,0)))
  o.data.update()
print('Existing red shoe markings placed on refined tongue surface.')
