# 手（指）が袖の外へ突き抜けていないかを検査する。globals: R, POSE, JK(ジャケット名), HANDS(名前リスト)
import bpy, bmesh
from mathutils.bvhtree import BVHTree
g = {"__name__": "x", "ROOT": R}
for f in ("lib.py", "body.py"):
    exec(open(R + "/" + f, encoding="utf-8").read(), g)
ac = g["arm_center"](POSE)
jk = bpy.data.objects[JK]
bm = bmesh.new()
bm.from_mesh(jk.data)
tree = BVHTree.FromBMesh(bm)
res = {}
for hn in HANDS:
    h = bpy.data.objects[hn]
    sx = 1 if h.matrix_world.translation.x + sum(v.co.x for v in h.data.vertices[:50]) > 0 else -1
    axis = []
    for s in range(300, 720, 5):
        c = ac(s / 1000)
        axis.append(c.__class__((c.x * sx, c.y, c.z)))
    bad = 0
    for v in h.data.vertices:
        p = h.matrix_world @ v.co
        c = min(axis, key=lambda a: (a - p).length)
        d = p - c
        if d.length < 1e-6:
            continue
        loc, nor, i, dist = tree.ray_cast(c, d.normalized(), d.length)
        if loc is not None and dist < d.length - 0.0005:
            loc2, _, _, _ = tree.ray_cast(loc + d.normalized() * 0.0005, d.normalized(), d.length - dist)
            if loc2 is not None:
                bad += 1
    res[hn] = (bad, len(h.data.vertices))
bm.free()
print("hand outside:", res)
