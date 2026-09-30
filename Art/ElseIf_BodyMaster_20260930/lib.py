# ElseIf Master 身体・衣装 共通ヘルパー（Blender内でexecして使う）
import bpy, bmesh, math
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

TAU = math.tau


def clamp01(x):
    return 0.0 if x < 0 else (1.0 if x > 1 else x)


def ss(a, b, x):
    """smoothstep（a→bで0→1。a>bなら逆向き）"""
    if a == b:
        return 1.0 if x >= a else 0.0
    t = clamp01((x - a) / (b - a))
    return t * t * (3 - 2 * t)


def interp_keys(keys, t):
    """keys=[(t,[v...]),...] を非等間隔Catmull-Rom(Hermite)で補間"""
    ts = [k[0] for k in keys]
    vs = [k[1] for k in keys]
    n = len(keys)
    if t <= ts[0]:
        return list(vs[0])
    if t >= ts[-1]:
        return list(vs[-1])
    i = 0
    while ts[i + 1] < t:
        i += 1
    m = len(vs[0])

    def tang(j):
        a = max(j - 1, 0)
        b = min(j + 1, n - 1)
        return [(vs[b][c] - vs[a][c]) / (ts[b] - ts[a]) for c in range(m)]

    h = ts[i + 1] - ts[i]
    u = (t - ts[i]) / h
    m0, m1 = tang(i), tang(i + 1)
    h00 = 2 * u ** 3 - 3 * u ** 2 + 1
    h10 = u ** 3 - 2 * u ** 2 + u
    h01 = -2 * u ** 3 + 3 * u ** 2
    h11 = u ** 3 - u ** 2
    return [h00 * vs[i][c] + h10 * h * m0[c] + h01 * vs[i + 1][c] + h11 * h * m1[c] for c in range(m)]


def se(c, e):
    """超楕円用の符号付きべき"""
    return math.copysign(abs(c) ** (2.0 / e), c)


def ring_offset(theta, wp, wn, df, db, ex):
    """θ=0が+l方向、θ=π/2が+f方向。戻り値(x,y)は(l,f)成分"""
    c, s = math.cos(theta), math.sin(theta)
    x = se(c, ex) * (wp if c >= 0 else wn)
    y = se(s, ex) * (df if s >= 0 else db)
    return x, y


def frames_along(centers, fref):
    """中心列から (t,f,l) フレーム列を作る。l = t×f"""
    out = []
    n = len(centers)
    for i in range(n):
        a = centers[max(i - 1, 0)]
        b = centers[min(i + 1, n - 1)]
        t = (b - a).normalized()
        f = (fref - t * fref.dot(t)).normalized()
        l = t.cross(f).normalized()
        out.append((t, f, l))
    return out


def build_loft(name, samples, N, center_fn, prof_fn, fref, deform=None,
               cap_start=False, cap_end=False, mat_fn=None, uv_fn=None, theta0=-math.pi / 2):
    """汎用ロフト。
    samples: パラメータsの列 / center_fn(s)->Vector / prof_fn(s)->[wp,wn,df,db,ex]
    deform(s,theta,p,t,f,l)->Vector（布の皺・垂れなど）
    mat_fn(i,j,s,theta,center)->material index / uv_fn(s,theta)->(u,v)"""
    centers = [center_fn(s) for s in samples]
    frames = frames_along(centers, fref)
    bm = bmesh.new()
    rings = []
    thetas = [theta0 + TAU * j / N for j in range(N + 1)]
    for i, s in enumerate(samples):
        t, f, l = frames[i]
        wp, wn, df, db, ex = prof_fn(s)[:5]
        ring = []
        for j in range(N):
            th = thetas[j]
            x, y = ring_offset(th, wp, wn, df, db, ex)
            p = centers[i] + l * x + f * y
            if deform:
                p = deform(s, th, p, t, f, l)
            ring.append(bm.verts.new(p))
        rings.append(ring)
    uvl = bm.loops.layers.uv.new("UVMap")
    for i in range(len(samples) - 1):
        for j in range(N):
            jn = (j + 1) % N
            vs = (rings[i][j], rings[i][jn], rings[i + 1][jn], rings[i + 1][j])
            fc = bm.faces.new(vs)
            if mat_fn:
                cen = sum((v.co for v in vs), Vector()) / 4
                fc.material_index = mat_fn(i, j, (samples[i] + samples[i + 1]) * 0.5,
                                           (thetas[j] + thetas[j + 1]) * 0.5, cen)
            corner = [(samples[i], thetas[j]), (samples[i], thetas[j + 1]),
                      (samples[i + 1], thetas[j + 1]), (samples[i + 1], thetas[j])]
            for lp, (s_, th_) in zip(fc.loops, corner):
                lp[uvl].uv = uv_fn(s_, th_) if uv_fn else ((th_ - theta0) / TAU, s_)
    if cap_start:
        fc = bm.faces.new(list(reversed(rings[0])))
        if mat_fn:
            fc.material_index = mat_fn(0, 0, samples[0], 0.0, centers[0])
    if cap_end:
        fc = bm.faces.new(rings[-1])
        if mat_fn:
            fc.material_index = mat_fn(len(samples) - 1, 0, samples[-1], 0.0, centers[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    return ob


def mesh_from_rings(name, rings, cap_start=False, cap_end=False, mat_fn=None):
    """点列リングの列からメッシュ。mat_fn(i_ring, j, center)->index"""
    bm = bmesh.new()
    vr = [[bm.verts.new(p) for p in r] for r in rings]
    N = len(rings[0])
    uvl = bm.loops.layers.uv.new("UVMap")
    for i in range(len(vr) - 1):
        for j in range(N):
            jn = (j + 1) % N
            q = (vr[i][j], vr[i][jn], vr[i + 1][jn], vr[i + 1][j])
            f = bm.faces.new(q)
            if mat_fn:
                f.material_index = mat_fn(i, j, sum((v.co for v in q), Vector()) / 4)
            for lp, uv in zip(f.loops, [(j / N, i), ((j + 1) / N, i), ((j + 1) / N, i + 1), (j / N, i + 1)]):
                lp[uvl].uv = uv
    if cap_start:
        f = bm.faces.new(list(reversed(vr[0])))
        if mat_fn:
            f.material_index = mat_fn(0, 0, vr[0][0].co)
    if cap_end:
        f = bm.faces.new(vr[-1])
        if mat_fn:
            f.material_index = mat_fn(len(vr) - 2, 0, vr[-1][0].co)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return bpy.data.objects.new(name, me)


def box_mesh(name, center, size, rot=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if rot is not None:
        bmesh.ops.rotate(bm, matrix=rot, verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(center), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return bpy.data.objects.new(name, me)


def lin(a, b, n):
    return [a + (b - a) * k / (n - 1) for k in range(n)]


def samples_from_keys(keys, per_seg):
    """キー位置を必ず含むサンプル列"""
    out = []
    for i in range(len(keys) - 1):
        a, b = keys[i][0], keys[i + 1][0]
        for k in range(per_seg):
            out.append(a + (b - a) * k / per_seg)
    out.append(keys[-1][0])
    return out


def dense_samples(a, b, step):
    n = max(2, int(round(abs(b - a) / step)) + 1)
    return lin(a, b, n)


# ---------- マテリアル ----------
def get_mat(name, color, rough=0.6, metal=0.0, emit=None, emit_str=0.0, sheen=0.0):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if "Sheen Weight" in bsdf.inputs:
        bsdf.inputs["Sheen Weight"].default_value = sheen
    if emit:
        bsdf.inputs["Emission Color"].default_value = (*emit, 1)
        bsdf.inputs["Emission Strength"].default_value = emit_str
    m.diffuse_color = (*color, 1)
    return m


def set_mats(ob, mats):
    # materials.clear() は material_index を失うので退避して戻す
    me = ob.data
    idx = [0] * len(me.polygons)
    me.polygons.foreach_get("material_index", idx)
    me.materials.clear()
    for m in mats:
        me.materials.append(m)
    me.polygons.foreach_set("material_index", idx)
    me.update()


def link(ob, coll):
    if ob.name not in coll.objects:
        coll.objects.link(ob)
    return ob


def get_coll(name, parent=None):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(c)
    return c


def clear_coll(c):
    for ob in list(c.all_objects):
        me = ob.data if ob.type == "MESH" else None
        bpy.data.objects.remove(ob, do_unlink=True)
        if me and me.users == 0:
            bpy.data.meshes.remove(me)
    for ch in list(c.children):
        clear_coll(ch)
        bpy.data.collections.remove(ch)


def add_mod(ob, kind, name=None, **kw):
    md = ob.modifiers.new(name or kind, kind)
    for k, v in kw.items():
        setattr(md, k, v)
    return md


def shade_smooth(ob):
    for p in ob.data.polygons:
        p.use_smooth = True


def mirror_x_copy(ob, name):
    """X反転コピー（法線も反転を戻す）"""
    me = ob.data.copy()
    me.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
    me.flip_normals()
    ob2 = bpy.data.objects.new(name, me)
    for md in ob.modifiers:
        pass
    return ob2


def join_meshes(obs, name):
    bm = bmesh.new()
    mats = []
    for ob in obs:
        me = ob.data
        idx_map = []
        for m in me.materials:
            if m not in mats:
                mats.append(m)
            idx_map.append(mats.index(m))
        tmp = bmesh.new()
        tmp.from_mesh(me)
        tmp.transform(ob.matrix_world)
        for f in tmp.faces:
            if idx_map:
                f.material_index = idx_map[f.material_index]
        me2 = bpy.data.meshes.new("_tmp")
        tmp.to_mesh(me2)
        tmp.free()
        bm.from_mesh(me2)
        bpy.data.meshes.remove(me2)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    out = bpy.data.objects.new(name, me)
    for ob in obs:
        old = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if old.users == 0:
            bpy.data.meshes.remove(old)
    return out


def bvh_of(ob, depsgraph=None):
    dg = depsgraph or bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    me = ev.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.transform(ob.matrix_world)
    tree = BVHTree.FromBMesh(bm)
    ev.to_mesh_clear()
    return tree, bm


def ribbon_on_surface(name, tree, pts, ray_dir, width, lift=0.0015, thick=0.0, up_hint=None, uv_len=True):
    """表面にレイキャストしてリボン（ストラップ・ライン）を作る。
    pts: レイ始点の列（ray_dir方向に飛ばす）"""
    hits = []
    for p in pts:
        loc, nor, idx, d = tree.ray_cast(Vector(p), Vector(ray_dir).normalized())
        if loc is None:
            continue
        if nor.dot(Vector(ray_dir)) > 0:
            nor = -nor
        hits.append((loc, nor))
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    rows = []
    acc = 0.0
    for i, (loc, nor) in enumerate(hits):
        a = hits[max(i - 1, 0)][0]
        b = hits[min(i + 1, len(hits) - 1)][0]
        tan = (b - a).normalized()
        side = tan.cross(nor).normalized()
        base = loc + nor * lift
        if i > 0:
            acc += (loc - hits[i - 1][0]).length
        row = [bm.verts.new(base - side * width * 0.5), bm.verts.new(base + side * width * 0.5)]
        if thick > 0:
            row += [bm.verts.new(base + side * width * 0.5 + nor * thick), bm.verts.new(base - side * width * 0.5 + nor * thick)]
        rows.append((row, acc))
    for i in range(len(rows) - 1):
        r0, a0 = rows[i]
        r1, a1 = rows[i + 1]
        k = len(r0)
        rng = range(k) if k > 2 else range(1)
        for j in rng:
            jn = (j + 1) % k
            f = bm.faces.new((r0[j], r0[jn], r1[jn], r1[j]))
            for lp, uv in zip(f.loops, [(j / k, a0), (jn / k if jn else 1.0, a0), (jn / k if jn else 1.0, a1), (j / k, a1)]):
                lp[uvl].uv = uv
    if thick > 0 and len(rows) > 1:
        bm.faces.new(list(reversed(rows[0][0])))
        bm.faces.new(rows[-1][0])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return bpy.data.objects.new(name, me)


def decal_on_surface(name, tree, center, ray_dir, right, up, w, h, nx=8, ny=8, lift=0.0015):
    """表面に貼り付く矩形デカール（UV 0-1）"""
    ray = Vector(ray_dir).normalized()
    right = Vector(right).normalized()
    up = Vector(up).normalized()
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    grid = []
    for iy in range(ny + 1):
        row = []
        for ix in range(nx + 1):
            u, v = ix / nx, iy / ny
            o = Vector(center) + right * (u - 0.5) * w + up * (v - 0.5) * h - ray * 0.3
            loc, nor, _, _ = tree.ray_cast(o, ray)
            if loc is None:
                loc, nor = o + ray * 0.3, -ray
            if nor.dot(ray) > 0:
                nor = -nor
            row.append((bm.verts.new(loc + nor * lift), (u, v)))
        grid.append(row)
    for iy in range(ny):
        for ix in range(nx):
            q = [grid[iy][ix], grid[iy][ix + 1], grid[iy + 1][ix + 1], grid[iy + 1][ix]]
            f = bm.faces.new([a[0] for a in q])
            for lp, a in zip(f.loops, q):
                lp[uvl].uv = a[1]
    # 法線はレイの逆向き（表側）へ
    for f in bm.faces:
        f.normal_update()
        if f.normal.dot(ray) > 0:
            f.normal_flip()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return bpy.data.objects.new(name, me)
