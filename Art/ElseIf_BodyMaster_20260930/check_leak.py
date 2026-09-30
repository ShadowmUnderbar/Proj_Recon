# SDF一体化で元パーツから離れた頂点（トゲ・ヒレ）が出ていないか。globals: SUF, SRC
import bpy, bmesh
from mathutils.bvhtree import BVHTree
s = bpy.data.objects[SRC]
bm = bmesh.new(); bm.from_mesh(s.data); t = BVHTree.FromBMesh(bm)
j = bpy.data.objects["Jacket" + SUF]
far = [v.co.copy() for v in j.data.vertices if t.find_nearest(v.co)[3] > 0.005]
print("leak verts", SUF or "BASE", len(far), [tuple(round(c, 3) for c in p) for p in far[:3]])
bm.free()
