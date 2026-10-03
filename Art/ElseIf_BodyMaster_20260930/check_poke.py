# 人体（胴・腕・手）がジャケットの外へ露出していないか検査。globals: R, POSE, SUF("" or "_Natural")
# 各頂点から法線方向へレイを飛ばし、ジャケットに当たらなければ「覆われていない（突き抜け）」
import bpy, bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
jk = bpy.data.objects["Jacket" + SUF]
bm = bmesh.new()
bm.from_mesh(jk.data)
tree = BVHTree.FromBMesh(bm)
res = {}
for name, zr in (("Body_Arm_L", None), ("Body_Arm_R", None), ("Body_Hand_L", None), ("Body_Hand_R", None),
                 ("Body_Torso", (0.80, 1.355))):
    ob = bpy.data.objects[name + SUF]
    mw = ob.matrix_world
    nm = mw.to_3x3().inverted().transposed()
    bad = []
    for v in ob.data.vertices:
        p = mw @ v.co
        if zr and not (zr[0] < p.z < zr[1]):
            continue
        n = (nm @ v.normal).normalized()
        loc, _, _, _ = tree.ray_cast(p + n * 0.0002, n, 0.6)
        if loc is None:
            bad.append(p)
    if bad:
        m = sum(bad, Vector()) / len(bad)
        res[name] = (len(bad), tuple(round(x, 3) for x in m))
    else:
        res[name] = 0
bm.free()
print(f"[{POSE}] body not covered by jacket:", res)
