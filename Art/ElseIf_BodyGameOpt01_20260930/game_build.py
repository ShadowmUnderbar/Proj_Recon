# ElseIf_Body_GameOpt01 ジオメトリ生成
# Master（Art/ElseIf_BodyMaster_20260930）のビルダー関数を「捕捉」して同じ形状関数を低解像度で評価する。
# Master のオブジェクトは読み取り専用（BVH参照・形状評価）で、一切書き換えない。
# globals: MROOT(Master作業フォルダ), GROOT(このフォルダ)
import os
import bpy, bmesh, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

for _f in ("lib.py", "body.py", "garments.py", "details.py"):
    _p = os.path.join(MROOT, _f)
    exec(compile(open(_p, encoding="utf-8").read(), _p, "exec"), globals())


class _MatDict(dict):
    """Masterビルダーが参照するMATの代用（捕捉中に作られる使い捨てオブジェクト用）"""
    def __missing__(self, k):
        return bpy.data.materials.get("M_Metal") or bpy.data.materials.new("_GameOpt_tmp")


MAT = _MatDict()
G = globals()
POSE_G = "BASE"          # Gameモデルは A-pose で作り、自然立ちはリグで付ける

# 部位ID（ウェイト・UV優先度に使う）
P_SHELL, P_COLLAR, P_SLEEVE, P_LIP_HEM, P_LIP_CUFF, P_LIP_COLLAR = 1, 2, 3, 4, 5, 6
P_PANEL, P_STRAP_HEM, P_TAG_CUFF, P_HARNESS, P_TAG_CHEST = 7, 8, 9, 13, 14
P_SHORTS_PELVIS, P_SHORTS_LEG, P_LIP_SHORTS = 10, 11, 12
P_LEG = 20
P_SOLE, P_UPPER, P_SHAFT, P_LIP_SHAFT = 30, 31, 32, 33
P_PALM, P_FINGER = 40, 41
P_BELT, P_BELT_PARTS = 50, 51
P_SOCKET = 60

PART_NAMES = {
    "Jacket": (1, 9), "Jacket_Details": (13, 14), "ShortPants": (10, 12), "Legs_Socks": (20, 20),
    "Shoes": (30, 33), "Hands": (40, 41), "ThighBelt": (50, 51), "HeadSocket": (60, 60),
}


# ---------------- 捕捉と低解像度評価 ----------------
def capture(fn, *a, **k):
    calls = []
    real = G["build_loft"]

    def rec(name, samples, N, center_fn, prof_fn, fref, **kw):
        calls.append(dict(name=name, samples=list(samples), N=N, cen=center_fn, prof=prof_fn, fref=fref, **kw))
        return None
    G["build_loft"] = rec
    try:
        fn(*a, **k)
    finally:
        G["build_loft"] = real
    return calls


def eval_pt(c, s, th, eps=2e-4):
    cen = c["cen"]
    lo, hi = c["samples"][0], c["samples"][-1]
    a, b = max(lo, s - eps), min(hi, s + eps)
    if b - a < 1e-9:
        a, b = s - eps, s + eps
    t = (cen(b) - cen(a)).normalized()
    fr = c["fref"]
    f = (fr - t * fr.dot(t)).normalized()
    l = t.cross(f).normalized()
    wp, wn, df, db, ex = c["prof"](s)[:5]
    x, y = ring_offset(th, wp, wn, df, db, ex)
    p = cen(s) + l * x + f * y
    d = c.get("deform")
    if d:
        p = d(s, th, p, t, f, l)
    return p


def lp_rings(c, s_list, N, filt=(3, 3), theta0=-math.pi / 2):
    """各頂点をセル内の平均で評価（低域通過）。細かいシワはここで落ち、ノーマルマップへ回す"""
    fs, ft = filt
    out = []
    n = len(s_list)
    for i, s in enumerate(s_list):
        if i == 0 or i == n - 1:
            ss_ = [s]
        else:
            lo = (s_list[i - 1] + s) / 2
            hi = (s + s_list[i + 1]) / 2
            ss_ = [lo + (hi - lo) * (k + 0.5) / fs for k in range(fs)]
        ring = []
        for j in range(N):
            th = theta0 + TAU * j / N
            acc = Vector()
            m = 0
            for s_ in ss_:
                for k in range(ft):
                    acc += eval_pt(c, s_, th + (TAU / N) * ((k + 0.5) / ft - 0.5))
                    m += 1
            ring.append(acc / m)
        out.append(ring)
    return out


# ---------------- 部位ビルダー（頂点属性つき） ----------------
class PB:
    def __init__(self):
        self.bm = bmesh.new()
        L = self.bm.verts.layers
        self.l_part = L.int.new("part")
        self.l_s = L.float.new("s")
        self.l_side = L.int.new("side")
        self.l_fing = L.int.new("finger")

    def v(self, co, part, s=0.0, side=0, finger=-1):
        v = self.bm.verts.new(co)
        v[self.l_part] = part
        v[self.l_s] = s
        v[self.l_side] = side
        v[self.l_fing] = finger
        return v

    def rings(self, rings, part, s_list, side=0, finger=-1, cap_start=False, cap_end=False, parts=None):
        vr = []
        for i, (r, s) in enumerate(zip(rings, s_list)):
            pp = parts[i] if parts else part
            vr.append([self.v(p, pp, s, side, finger) for p in r])
        N = len(rings[0])
        for i in range(len(vr) - 1):
            for j in range(N):
                jn = (j + 1) % N
                self.bm.faces.new((vr[i][j], vr[i][jn], vr[i + 1][jn], vr[i + 1][j]))
        if cap_start:
            self.bm.faces.new(list(reversed(vr[0])))
        if cap_end:
            self.bm.faces.new(vr[-1])
        return vr

    def box(self, corners8, part, s=0.0, side=0):
        vs = [self.v(c, part, s, side) for c in corners8]
        for q in ((0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)):
            self.bm.faces.new([vs[i] for i in q])

    def to_object(self, name):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        orient_outward(self.bm)
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        self.bm.free()
        return bpy.data.objects.new(name, me)


def orient_outward(bm):
    """開いたメッシュでも外向きになるよう、連結島ごとに (法線・重心からの向き) の面積加重和で判定して反転"""
    bm.faces.ensure_lookup_table()
    seen = set()
    for f0 in bm.faces:
        if f0.index in seen:
            continue
        isl = []
        stack = [f0]
        seen.add(f0.index)
        while stack:
            f = stack.pop()
            isl.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g.index not in seen:
                        seen.add(g.index)
                        stack.append(g)
        cs = [f.calc_center_median() for f in isl]
        ar = [f.calc_area() for f in isl]
        cen = sum((c * a for c, a in zip(cs, ar)), Vector()) / max(sum(ar), 1e-12)
        score = sum(f.normal.dot(c - cen) * a for f, c, a in zip(isl, cs, ar))
        if score < 0:
            for f in isl:
                f.normal_flip()


def mirror_obj(ob, name):
    """X反転コピー（side属性も反転）"""
    me = ob.data.copy()
    me.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
    me.flip_normals()
    a = me.attributes["side"].data
    vals = [0] * len(a)
    a.foreach_get("value", vals)
    a.foreach_set("value", [-x if x else -1 for x in vals])
    return bpy.data.objects.new(name, me)


def obb_corners(points, pad=0.0):
    P = np.array([tuple(p) for p in points])
    c = P.mean(axis=0)
    w, V = np.linalg.eigh(np.cov((P - c).T))
    loc = (P - c) @ V
    mn, mx = loc.min(axis=0) - pad, loc.max(axis=0) + pad
    cs = []
    for (a, b, cc) in ((0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)):
        q = np.array([mn[0] if not a else mx[0], mn[1] if not b else mx[1], mn[2] if not cc else mx[2]])
        cs.append(Vector(c + V @ q))
    return cs


# ---------------- Master 参照 ----------------
def master_outer_bvh():
    """Masterジャケットの外側面だけのBVH（法線方向へ飛ばして自分に当たる面=内側面を除く）"""
    jk = bpy.data.objects["Jacket"]
    me = jk.data
    bm = bmesh.new()
    bm.from_mesh(me)
    full = BVHTree.FromBMesh(bm)
    bm.faces.ensure_lookup_table()
    kill = []
    for f in bm.faces:
        c = f.calc_center_median()
        n = f.normal
        hit = full.ray_cast(c + n * 0.0008, n, 0.30)
        if hit[0] is not None:
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    tree = BVHTree.FromBMesh(bm)
    return tree, bm


# ---------------- ジャケット ----------------
SHELL_Z = [0.700, 0.716, 0.736, 0.762, 0.800, 0.850, 0.910, 0.970, 1.030, 1.090, 1.140, 1.180,
           1.215, 1.245, 1.270, 1.290, 1.310, 1.330, 1.348, 1.362, 1.372]
COLLAR_Z = [1.392, 1.418, 1.436, 1.450]
# 根元リングは身頃の外側から始める（内側に入ると袖ぐりのブリッジ面が折り返す）
SLEEVE_S = [0.125, 0.170, 0.215, 0.250, 0.275, 0.300, 0.340,
            0.395, 0.450, 0.505, 0.560, 0.610, 0.645, 0.672, 0.700]
# 袖ぐり: 身頃グリッド上の 4×4 面の長方形（周囲16頂点）＝袖の根元リング16頂点と1対1
ARM_ROWS = (11, 15)            # SHELL_Z の 1.180〜1.290
ARM_COLS = {1: (7, 11), -1: (25, 29)}   # 列番号（θ=0/πの左右±20°）
N_SHELL, N_SLEEVE = 36, 16
SHELL_VR = [None]


def _ring_poly(c, s, M=48):
    """変形なしの断面多角形（中心, l, f, 2D点列）"""
    cen = c["cen"]
    t = (cen(s + 2e-4) - cen(s - 2e-4)).normalized()
    fr = c["fref"]
    f = (fr - t * fr.dot(t)).normalized()
    l = t.cross(f).normalized()
    wp, wn, df, db, ex = c["prof"](s)[:5]
    poly = [ring_offset(-math.pi / 2 + TAU * k / M, wp, wn, df, db, ex) for k in range(M)]
    return cen(s), t, l, f, poly


def _in_poly(x, y, poly):
    ins = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            ins = not ins
    return ins


def build_jacket_game(outer_tree):
    c_sh = capture(build_jacket_shell)[0]
    c_co = capture(build_collar)[0]
    c_sl = capture(build_sleeve, POSE_G)[0]

    # 身頃＋襟を1本のロフトに
    def cen(z):
        return c_sh["cen"](min(z, 1.372)) if z <= 1.372 else c_co["cen"](z)

    def prof(z):
        return c_sh["prof"](z) if z <= 1.372 else c_co["prof"](z)

    def deform(z, th, p, t, f, l):
        return c_sh["deform"](z, th, p, t, f, l) if z <= 1.372 else c_co["deform"](z, th, p, t, f, l)
    c_body = dict(cen=cen, prof=prof, deform=deform, fref=Vector((0, -1, 0)), samples=SHELL_Z + COLLAR_Z)
    zs = SHELL_Z + COLLAR_Z
    pb = PB()
    rings = lp_rings(c_body, zs, N_SHELL)
    parts = [P_SHELL if z <= 1.372 else P_COLLAR for z in zs]
    SHELL_VR[0] = pb.rings(rings, P_SHELL, zs, parts=parts)

    # 袖（左）→ 右は鏡像
    sl_rings = lp_rings(c_sl, SLEEVE_S, N_SLEEVE)
    bm = pb.bm
    sv = pb_rings_ret = None
    shell_vr = SHELL_VR[0]
    for side in (1, -1):
        rr = sl_rings if side > 0 else [[Vector((-p.x, p.y, p.z)) for p in r] for r in sl_rings]
        vr = pb.rings(rr, P_SLEEVE, SLEEVE_S, side=side)
        r0, r4 = ARM_ROWS
        cf, cb = (ARM_COLS[side][1], ARM_COLS[side][0]) if side > 0 else (ARM_COLS[side][0], ARM_COLS[side][1])
        # 袖ぐりの面を削除
        c_lo, c_hi = ARM_COLS[side]
        kill = []
        for i in range(r0, r4):
            for j in range(c_lo, c_hi):
                q = {shell_vr[i][j], shell_vr[i][j + 1], shell_vr[i + 1][j + 1], shell_vr[i + 1][j]}
                f = [fc for fc in shell_vr[i][j].link_faces if set(fc.verts) == q]
                kill += f
        bmesh.ops.delete(bm, geom=kill, context="FACES_ONLY")
        # 周囲16頂点（後ろ中段→下→前→上→後ろ）: 袖リング j=0後ろ, 4脇, 8前, 12上 に対応
        step = -1 if cb > cf else 1
        cols_b2f = list(range(cb, cf + step, step))          # 後ろ列→前列
        rm = (r0 + r4) // 2
        loop = [shell_vr[r][cb] for r in range(rm, r0, -1)]
        loop += [shell_vr[r0][c] for c in cols_b2f]
        loop += [shell_vr[r][cf] for r in range(r0 + 1, r4 + 1)]
        loop += [shell_vr[r4][c] for c in reversed(cols_b2f[:-1])]
        loop += [shell_vr[r][cb] for r in range(r4 - 1, rm, -1)]
        assert len(loop) == N_SLEEVE, len(loop)
        root = vr[0]
        for k in range(N_SLEEVE):
            kn = (k + 1) % N_SLEEVE
            bm.faces.new((loop[k], loop[kn], root[kn], root[k]))
    ob = pb.to_object("GO_Jacket")
    return ob, c_sl


def boundary_loops(bm):
    edges = [e for e in bm.edges if e.is_boundary]
    seen = set()
    loops = []
    for e in edges:
        if e.index in seen:
            continue
        loop = []
        stack = [e]
        while stack:
            x = stack.pop()
            if x.index in seen:
                continue
            seen.add(x.index)
            loop.append(x)
            for v in x.verts:
                for e2 in v.link_edges:
                    if e2.is_boundary and e2.index not in seen:
                        stack.append(e2)
        loops.append(loop)
    return loops


def bridge_armholes(ob):
    """身頃の穴と袖の根元の境界ループを Bridge Edge Loops でつなぐ"""
    vl = bpy.context.view_layer
    coll = bpy.context.scene.collection
    coll.objects.link(ob)
    for o in vl.objects:
        o.select_set(False)
    vl.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for side in (1, -1):
        bm = bmesh.from_edit_mesh(ob.data)
        bm.edges.ensure_lookup_table()
        lp = bm.verts.layers.int["part"]
        loops = boundary_loops(bm)
        hole = None
        root = None
        for lo in loops:
            vs = {v for e in lo for v in e.verts}
            cz = sum(v.co.z for v in vs) / len(vs)
            cx = sum(v.co.x for v in vs) / len(vs)
            parts = {v[lp] for v in vs}
            if cx * side <= 0.05 or not (1.05 < cz < 1.40):
                continue
            if parts == {P_SHELL}:
                hole = lo
            elif parts == {P_SLEEVE}:
                # 袖口ではなく根元（肩に近い方）
                if cz > 1.1:
                    root = lo
        for e in bm.edges:
            e.select_set(False)
        for v in bm.verts:
            v.select_set(False)
        if hole is None or root is None:
            print("bridge: loop not found", side, hole is None, root is None)
            continue
        for e in hole + root:
            e.select_set(True)
        bmesh.update_edit_mesh(ob.data)
        bpy.ops.mesh.bridge_edge_loops(number_cuts=1, interpolation="SURFACE", smoothness=0.6)
    bpy.ops.object.mode_set(mode="OBJECT")
    coll.objects.unlink(ob)


def conform_junction(ob, outer_tree, radius=0.10, iters=6):
    """肩・脇の接合部だけ Master 外側面へ吸着させて整える（他はロフトのまま）"""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    lp = bm.verts.layers.int["part"]
    # 接合部 = 身頃と袖の両方に隣接する辺の頂点（ブリッジで生まれた面の頂点）
    lside = bm.verts.layers.int["side"]
    # ブリッジで生まれた頂点（part=0）と、身頃・袖の両方に接する頂点が起点
    seeds = []
    for v in bm.verts:
        nb = {w[lp] for f in v.link_faces for w in f.verts}
        if v[lp] == 0 or (P_SHELL in nb and P_SLEEVE in nb) or (0 in nb and v[lp] in (P_SHELL, P_SLEEVE)):
            seeds.append(v)
    for v in bm.verts:
        if v[lp] == 0:
            v[lp] = P_SLEEVE
            v[lside] = 1 if v.co.x > 0 else -1
    seeds_co = [v.co.copy() for v in seeds]
    wts = {}
    for v in bm.verts:
        if v[lp] not in (P_SHELL, P_SLEEVE, 0):
            continue
        d = min(((v.co - s).length for s in seeds_co), default=1.0)
        if d < radius:
            wts[v] = 1.0 - ss(0.0, radius, d)
    for it in range(iters):
        # ラプラシアン平滑化→外側面へ吸着
        new = {}
        for v, w in wts.items():
            nb = [e.other_vert(v).co for e in v.link_edges]
            if not nb:
                continue
            avg = sum(nb, Vector()) / len(nb)
            new[v] = v.co.lerp(avg, 0.5 * w)
        for v, co in new.items():
            v.co = co
        for v, w in wts.items():
            loc, nor, _, _ = outer_tree.find_nearest(v.co)
            if loc is not None:
                v.co = v.co.lerp(loc, w)
    bm.to_mesh(ob.data)
    bm.free()
    return len(seeds)


def add_lip(ob, select_fn, inward, along_fn, part):
    """境界ループを内側へ折り返して最小限の厚みを作る（外から断面が見える所だけ）"""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bm.normal_update()
    L = bm.verts.layers
    lp, ls, lside, lf = L.int["part"], L.float["s"], L.int["side"], L.int["finger"]
    loops = boundary_loops(bm)
    made = 0
    for lo in loops:
        vs = list({v for e in lo for v in e.verts})
        if not select_fn(vs):
            continue
        rim = {}
        deep = {}
        for v in vs:
            n = v.normal
            u = along_fn(v)
            r = bm.verts.new(v.co - n * inward)
            d = bm.verts.new(v.co - n * inward + u)
            for nv in (r, d):
                nv[lp] = part
                nv[ls] = v[ls]
                nv[lside] = v[lside]
                nv[lf] = v[lf]
            rim[v] = r
            deep[v] = d
        for e in lo:
            a, b = e.verts
            # 元の面と向きを揃えるため、隣接面のループ順を見る
            f0 = e.link_faces[0]
            la = next(l for l in f0.loops if l.vert == a)
            if la.link_loop_next.vert != b:
                a, b = b, a
            bm.faces.new((b, a, rim[a], rim[b]))
            bm.faces.new((rim[b], rim[a], deep[a], deep[b]))
        made += 1
    bm.to_mesh(ob.data)
    bm.free()
    return made


# ---------------- ハーフパンツ ----------------
def build_shorts_game():
    c_pel, c_leg = capture(build_shorts)
    pb = PB()
    zs = [0.795, 0.820, 0.850, 0.885, 0.925, 0.965]
    vr = pb.rings(lp_rings(c_pel, zs, 24), P_SHORTS_PELVIS, zs, cap_start=True, cap_end=True)
    # 股下の平らな蓋は中心を少し下げて丸める
    bot = set(vr[0])
    cap = [f for f in pb.bm.faces if len(f.verts) == 24 and set(f.verts) == bot]
    res = bmesh.ops.poke(pb.bm, faces=cap)
    for v in res["verts"]:
        v.co.z -= 0.010
        v[pb.l_part] = P_SHORTS_PELVIS
    zl = [SHORTS_HEM, 0.715, 0.750, 0.800, 0.860]
    lr = lp_rings(c_leg, zl, 16)
    pb.rings(lr, P_SHORTS_LEG, zl, side=1)
    pb.rings([[Vector((-p.x, p.y, p.z)) for p in r] for r in lr], P_SHORTS_LEG, zl, side=-1)
    return pb.to_object("GO_Shorts")


# ---------------- 脚（ソックス＋絶対領域の肌） ----------------
LEG_SOCK_Z = [0.100, 0.140, 0.200, 0.270, 0.340, 0.400, 0.450, 0.480, 0.510, 0.560, 0.610, 0.640, SOCK_TOP]
LEG_SKIN_Z = [SOCK_TOP, 0.672, 0.700, 0.738]


def build_legs_game():
    c_sock = capture(build_sock)[0]
    c_skin = dict(cen=leg_center, prof=lambda z: leg_prof(z, 0.0), fref=Vector((0, -1, 0)),
                  samples=[0.09, 0.88])
    pb = PB()
    r1 = lp_rings(c_sock, LEG_SOCK_Z, 16)
    r2 = lp_rings(c_skin, LEG_SKIN_Z, 16)
    rings = r1 + r2
    zs = LEG_SOCK_Z + LEG_SKIN_Z
    for side in (1, -1):
        rr = rings if side > 0 else [[Vector((-p.x, p.y, p.z)) for p in r] for r in rings]
        pb.rings(rr, P_LEG, zs, side=side)
    return pb.to_object("GO_Legs")


# ---------------- 靴 ----------------
def build_shoe_game():
    calls = {c["name"]: c for c in capture(build_shoe)}
    pb = PB()
    # ソール（接地面は1枚の面・側面の段差や溝はテクスチャへ）
    M = 22
    ym, hl = (0.068 - 0.236) / 2, (0.068 + 0.236) / 2
    outline = []
    for k in range(M):
        ph = TAU * k / M
        y = ym + hl * math.cos(ph)
        x = math.copysign(_sole_hw(y), math.sin(ph)) if abs(math.sin(ph)) > 1e-6 else 0.0
        outline.append((x, y))
    levels = [(0.000, 0.955), (0.009, 1.0), (0.050, 0.995), (0.061, 0.972)]
    rings = []
    for z, sc in levels:
        rings.append([Vector((x * sc * 1.07, ym + (y - ym) * (sc * 1.02 if sc < 1 else 1.0), z + _rocker(y))) for x, y in outline])
    pb.rings(rings, P_SOLE, [l[0] for l in levels], cap_start=True)
    # アッパー
    cu = calls["Shoe_Upper"]
    qs = [-0.068, -0.058, -0.040, -0.010, 0.030, 0.070, 0.110, 0.150, 0.185, 0.210, 0.226, 0.234]
    pb.rings(lp_rings(cu, qs, 16), P_UPPER, qs, cap_start=True, cap_end=True)
    # 履き口
    cs = calls["Shoe_Shaft"]
    zs = [0.105, 0.150, 0.200, 0.232]
    pb.rings(lp_rings(cs, zs, 16), P_SHAFT, zs)
    ob = pb.to_object("GO_Shoe_L")
    # 履き口のパッド＝内側への折り返し
    add_lip(ob, lambda vs: min(v.co.z for v in vs) > 0.2, 0.009, lambda v: Vector((0, 0, -0.022)), P_LIP_SHAFT)
    rot = Matrix.Rotation(math.radians(SHOE_TOE_OUT), 4, "Z")
    ob.data.transform(Matrix.Translation((SHOE_POS.x, SHOE_POS.y, 0)) @ rot)
    set_side(ob, 1)
    return ob


def set_side(ob, side):
    a = ob.data.attributes["side"].data
    a.foreach_set("value", [side] * len(a))


# ---------------- 手 ----------------
FINGER_ORDER = ["Thumb", "Index", "Middle", "Ring", "Pinky"]


def finger_acc(name):
    if name == "Thumb":
        segs = [0.038, 0.029, 0.024]
        acc = [0.0]
    else:
        segs = FINGERS[name][3]
        acc = [0.0, 0.014]
    for L in segs:
        acc.append(acc[-1] + L)
    return acc


def build_hand_game():
    calls = capture(build_hand, POSE_G)
    pb = PB()
    for c in calls:
        nm = c["name"].replace("Hand_", "")
        if nm == "Palm":
            ss_ = [-0.02, 0.010, 0.050, 0.085, 0.100]
            pb.rings(lp_rings(c, ss_, 8), P_PALM, ss_, side=1, cap_start=True, cap_end=True)
            continue
        acc = finger_acc(nm)
        total = c["samples"][-1]
        S = {0.0, total - 0.005, total}
        for a in acc[1:-1]:
            S |= {a - 0.004, a, a + 0.004}
        ss_ = sorted(x for x in S if 0 <= x <= total)
        pb.rings(lp_rings(c, ss_, 6), P_FINGER, ss_, side=1, finger=FINGER_ORDER.index(nm),
                 cap_start=True, cap_end=True)
    return pb.to_object("GO_Hand_L")


# ---------------- ディテール（シルエットに効くものだけジオメトリで） ----------------
OBB_DETAILS = [
    # (Masterオブジェクト名, 部位ID)
    ("Hem_Strap_L", P_STRAP_HEM), ("Hem_StrapTip_L", P_STRAP_HEM),
    ("Hem_Strap_R", P_STRAP_HEM), ("Hem_StrapTip_R", P_STRAP_HEM),
    ("Cuff_Tag_L", P_TAG_CUFF), ("Cuff_TagTip_L", P_TAG_CUFF),
    ("Cuff_Tag_R", P_TAG_CUFF), ("Cuff_TagTip_R", P_TAG_CUFF),
    ("Harness_Hang_L", P_HARNESS), ("Harness_DRing_L", P_HARNESS),
    ("Harness_Hang_R", P_HARNESS), ("Harness_DRing_R", P_HARNESS),
    ("Tag_Chest_Red", P_TAG_CHEST),
    ("ThighBelt_Buckle", P_BELT_PARTS), ("ThighBelt_Hang", P_BELT_PARTS), ("ThighBelt_Tag", P_BELT_PARTS),
]


def build_details_game(master_tree):
    pb = PB()
    for name, part in OBB_DETAILS:
        mo = bpy.data.objects.get(name)
        if mo is None:
            print("missing detail", name)
            continue
        pts = [mo.matrix_world @ v.co for v in mo.data.vertices]
        side = 1 if sum(p.x for p in pts) > 0 else -1
        pb.box(obb_corners(pts), part, side=side)
    # 背面パネル（厚みのある板。画面・文字はテクスチャ）
    px0, px1, pz0, pz1 = -0.072 - 0.008, 0.072 + 0.008, 0.985 - 0.008, 1.225 + 0.008
    nx, nz = 3, 4
    top, low = [], []
    for iz in range(nz + 1):
        rt, rl = [], []
        for ix in range(nx + 1):
            x = px0 + (px1 - px0) * ix / nx
            z = pz0 + (pz1 - pz0) * iz / nz
            loc, nor, _, _ = master_tree.ray_cast(Vector((x, 0.6, z)), Vector((0, -1, 0)))
            if loc is None:
                loc, nor = Vector((x, 0.13, z)), Vector((0, 1, 0))
            rt.append(pb.v(loc + nor * 0.0095, P_PANEL))
            rl.append(pb.v(loc + nor * 0.0010, P_PANEL))
        top.append(rt)
        low.append(rl)
    for iz in range(nz):
        for ix in range(nx):
            pb.bm.faces.new((top[iz][ix], top[iz][ix + 1], top[iz + 1][ix + 1], top[iz + 1][ix]))
    ring_t = [top[0][i] for i in range(nx + 1)] + [top[i][nx] for i in range(1, nz + 1)] + \
             [top[nz][i] for i in range(nx - 1, -1, -1)] + [top[i][0] for i in range(nz - 1, 0, -1)]
    ring_l = [low[0][i] for i in range(nx + 1)] + [low[i][nx] for i in range(1, nz + 1)] + \
             [low[nz][i] for i in range(nx - 1, -1, -1)] + [low[i][0] for i in range(nz - 1, 0, -1)]
    for i in range(len(ring_t)):
        j = (i + 1) % len(ring_t)
        pb.bm.faces.new((ring_l[i], ring_l[j], ring_t[j], ring_t[i]))
    # 不要な下側グリッド頂点を消す
    used = set(ring_l)
    for row in low:
        for v in row:
            if v not in used:
                pb.bm.verts.remove(v)
    return pb.to_object("GO_Details")


def build_belt_game():
    pb = PB()
    z0, z1 = 0.628, 0.650
    rings, zs = [], []
    for z, e in ((z0, 0.0045), (z0, 0.0098), (z1, 0.0098), (z1, 0.0045)):
        c = leg_center(z)
        v = leg_prof(z, e)
        r = []
        for j in range(16):
            th = -math.pi / 2 + TAU * j / 16
            x, y = ring_offset(th, *v)
            r.append(Vector((-(c.x + x), c.y - y, z)))
        rings.append(r)
        zs.append(z)
    pb.rings(rings, P_BELT, zs, side=-1)
    return pb.to_object("GO_Belt")


def build_socket_game():
    pb = PB()
    cy, z = 0.012, SOCKET_Z

    def ring(r, zz, N=16):
        return [Vector((r * math.cos(TAU * k / N), cy + r * math.sin(TAU * k / N), zz)) for k in range(N)]
    pb.rings([ring(0.047, z - 0.010), ring(0.045, z + 0.002)], P_SOCKET, [0, 0], cap_end=True)
    rs = [(0.045, z + 0.001), (0.056, z - 0.006), (0.066, z - 0.016), (0.071, z - 0.024)]
    pb.rings([ring(r, zz) for r, zz in rs], P_SOCKET, [0.2] * 4)
    return pb.to_object("GO_Socket")


# ---------------- 全体 ----------------
def build_all(coll):
    outer_tree, outer_bm = master_outer_bvh()
    jk, c_sl = build_jacket_game(outer_tree)
    nseed = conform_junction(jk, outer_tree)
    # 開口部の最小限の厚み（裾・袖口・襟）
    add_lip(jk, lambda vs: max(v.co.z for v in vs) < 0.80, 0.007, lambda v: Vector((0, 0, 0.035)), P_LIP_HEM)
    arm_c = arm_center(POSE_G)
    tend = (arm_c(SLEEVE_END) - arm_c(SLEEVE_END - 0.02)).normalized()

    def cuff_sel(vs):
        return all(abs(v.co.x) > 0.45 for v in vs)

    def cuff_along(v):
        t = Vector((tend.x * (1 if v.co.x > 0 else -1), tend.y, tend.z))
        return -t * 0.030
    add_lip(jk, cuff_sel, 0.006, cuff_along, P_LIP_CUFF)
    add_lip(jk, lambda vs: min(v.co.z for v in vs) > 1.42, 0.006, lambda v: Vector((0, 0, -0.025)), P_LIP_COLLAR)

    parts = [jk, build_shorts_game(), build_legs_game()]
    sh = build_shoe_game()
    parts += [sh, mirror_obj(sh, "GO_Shoe_R")]
    hd = build_hand_game()
    parts += [hd, mirror_obj(hd, "GO_Hand_R")]
    parts += [build_details_game(outer_tree), build_belt_game(), build_socket_game()]
    sho = parts[1]
    # ハーフパンツ裾の折り返し
    add_lip(sho, lambda vs: max(v.co.z for v in vs) < SHORTS_HEM + 0.01, 0.005, lambda v: Vector((0, 0, 0.018)), P_LIP_SHORTS)
    outer_bm.free()
    ob = join_meshes(parts, "ElseIf_Body_GameOpt01")
    link(ob, coll)
    print("junction seeds", nseed)
    return ob
