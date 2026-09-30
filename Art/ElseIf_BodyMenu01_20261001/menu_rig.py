# ElseIf_Body_Menu01: Humanoid リグ（VRoid 系 J_Bip 命名）と手続きウェイト
# menu_build.py と同じ名前空間で exec する（arm_center, wrist_frame, FINGERS 等を使う）
import bpy, bmesh, math
import numpy as np
from mathutils import Vector, Matrix

ARM_NAME = "ElseIf_Body_Menu01_Armature"
MAX_INFL = 4


def finger_points(pose="BASE"):
    """body.build_hand と同じ計算で指の関節点を出す"""
    W, h, n, b = wrist_frame(pose)

    def P(a, c, e):
        return W + h * a + b * c + n * e
    out = {}
    for nm, (a0, c0, spread, segs, r0, curl) in FINGERS.items():
        sp = math.radians(spread)
        base_dir = (h * math.cos(sp) + b * math.sin(sp)).normalized()
        cur = P(a0, c0, -0.001)
        pts = [cur]
        for L, cd in zip(segs, curl):
            ang = math.radians(cd)
            d = (base_dir * math.cos(ang) + n * math.sin(ang)).normalized()
            cur = cur + d * L
            pts.append(cur)
        out[nm] = pts
    tdirs = [(0.85, 0.30, 0.40), (0.92, 0.10, 0.34), (0.90, -0.05, 0.40)]
    tl = [0.038, 0.029, 0.024]
    cur = P(0.010, 0.019, 0.007)
    pts = [cur]
    for (ka, kb, kn), L in zip(tdirs, tl):
        cur = cur + (h * ka + b * kb + n * kn).normalized() * L
        pts.append(cur)
    out["Thumb"] = pts
    return out, (W, h, n, b)


FNAME = {"Thumb": "Thumb", "Index": "Index", "Middle": "Middle", "Ring": "Ring", "Pinky": "Little"}


def bone_table(pose="BASE"):
    """name -> (head, tail, parent)。左を作って右は鏡像"""
    d1, d2 = arm_dirs(pose)
    S = SHOULDER
    E = S + d1 * L_UPPER
    fp, (W, h, n, b) = finger_points(pose)
    T = {}
    T["J_Bip_C_Hips"] = (Vector((0, 0.006, 0.870)), Vector((0, 0.006, 0.950)), None)
    T["J_Bip_C_Spine"] = (Vector((0, 0.006, 0.950)), Vector((0, 0.002, 1.070)), "J_Bip_C_Hips")
    T["J_Bip_C_Chest"] = (Vector((0, 0.002, 1.070)), Vector((0, 0.000, 1.190)), "J_Bip_C_Spine")
    T["J_Bip_C_UpperChest"] = (Vector((0, 0.000, 1.190)), Vector((0, 0.010, 1.330)), "J_Bip_C_Chest")
    T["J_Bip_C_Neck"] = (Vector((0, 0.010, 1.330)), Vector((0, 0.012, SOCKET_Z)), "J_Bip_C_UpperChest")
    T["J_Bip_C_Head"] = (Vector((0, 0.012, SOCKET_Z)), Vector((0, 0.012, SOCKET_Z + 0.16)), "J_Bip_C_Neck")
    L = {}
    L["Shoulder"] = (Vector((0.022, 0.006, 1.315)), S.copy(), "J_Bip_C_UpperChest")
    L["UpperArm"] = (S.copy(), E, "Shoulder")
    L["LowerArm"] = (E, W, "UpperArm")
    L["Hand"] = (W, W + h * 0.085, "LowerArm")
    for nm, pts in fp.items():
        fn = FNAME[nm]
        par = "Hand"
        for k in range(3):
            bn = f"{fn}{k + 1}"
            L[bn] = (pts[k], pts[k + 1], par)
            par = bn
    hip = Vector((0.085, 0.008, 0.870))
    knee = leg_center(0.480)
    ank = leg_center(0.125)
    L["UpperLeg"] = (hip, knee, "J_Bip_C_Hips")
    L["LowerLeg"] = (knee, ank, "UpperLeg")
    rot = Matrix.Rotation(math.radians(SHOE_TOE_OUT), 3, "Z")
    toe = Vector((SHOE_POS.x, SHOE_POS.y, 0)) + rot @ Vector((0, -0.150, 0.075))
    tip = Vector((SHOE_POS.x, SHOE_POS.y, 0)) + rot @ Vector((0, -0.225, 0.075))
    L["Foot"] = (ank, toe, "LowerLeg")
    L["ToeBase"] = (toe, tip, "Foot")
    for side, tag in ((1, "L"), (-1, "R")):
        for k, (hd, tl, par) in L.items():
            m = lambda v: Vector((v.x * side, v.y, v.z))
            pn = par if par.startswith("J_Bip_C") else f"J_Bip_{tag}_{par}"
            T[f"J_Bip_{tag}_{k}"] = (m(hd), m(tl), pn)
    return T


def build_armature(coll, pose="BASE"):
    old = bpy.data.objects.get(ARM_NAME)
    if old:
        arm_data = old.data
        bpy.data.objects.remove(old, do_unlink=True)
        bpy.data.armatures.remove(arm_data)
    ad = bpy.data.armatures.new(ARM_NAME)
    ao = bpy.data.objects.new(ARM_NAME, ad)
    coll.objects.link(ao)
    ad.display_type = "STICK"
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    vl.objects.active = ao
    ao.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    T = bone_table(pose)
    for name, (hd, tl, par) in T.items():
        eb = ad.edit_bones.new(name)
        eb.head = hd
        eb.tail = tl
        eb.roll = 0.0
    for name, (hd, tl, par) in T.items():
        if par:
            eb = ad.edit_bones[name]
            eb.parent = ad.edit_bones[par]
            eb.use_connect = False
    # 腕・指のロールは手のひら法線（-Y 前向き基準）に合わせる
    for name in T:
        eb = ad.edit_bones[name]
        if any(k in name for k in ("Arm", "Hand", "Thumb", "Index", "Middle", "Ring", "Little")):
            eb.align_roll(Vector((0, -1, 0)))
    bpy.ops.object.mode_set(mode="OBJECT")
    return ao, T


# ---------------- ウェイト ----------------
def _seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-12)))
    return (p - (a + ab * t)).length, t


def allowed_bones(part, side, finger, z, T):
    tag = "L" if side > 0 else "R"
    C = lambda n: "J_Bip_C_" + n
    S = lambda n, t=tag: f"J_Bip_{t}_{n}"
    if part in (1, 4, 7, 8, 13, 14):          # 身頃・裾・背面パネル・ストラップ
        bs = {C("Hips"): 1.0, C("Spine"): 1.0, C("Chest"): 1.0, C("UpperChest"): 1.0}
        if z > 1.12:
            # 脇下の身頃は上腕に引かれすぎると胴がくびれるので、肩の高さに近いほど上腕を効かせる
            ua = 0.9 * (0.25 + 0.75 * ss(1.20, 1.28, z))
            for t in ("L", "R"):
                bs[S("Shoulder", t)] = 1.0
                bs[S("UpperArm", t)] = ua
        if z < 0.90 and part in (1, 4, 8):
            for t in ("L", "R"):
                bs[S("UpperLeg", t)] = 0.30
        return bs
    if part in (2, 6):                        # 襟
        return {C("Neck"): 1.0, C("UpperChest"): 1.0}
    if part in (3, 5, 9):                     # 袖・袖口・袖口タグ
        return {S("Shoulder"): 1.0, S("UpperArm"): 1.0, S("LowerArm"): 1.0, S("Hand"): 0.45, C("UpperChest"): 0.8}
    if part in (10,):                          # ハーフパンツ骨盤
        return {C("Hips"): 1.0, S("UpperLeg", "L"): 0.6, S("UpperLeg", "R"): 0.6}
    if part in (11, 12):
        return {C("Hips"): 0.7, S("UpperLeg"): 1.0}
    if part == 20:                             # 脚（ソックス・絶対領域）
        return {S("UpperLeg"): 1.0, S("LowerLeg"): 1.0, S("Foot"): 1.0, C("Hips"): 0.4}
    if part in (30, 31, 32, 33):               # 靴
        return {S("Foot"): 1.0, S("ToeBase"): 1.0, S("LowerLeg"): 0.5}
    if part == 40:                             # 手のひら
        return {S("Hand"): 1.0, S("LowerArm"): 0.4, S("Index1"): 0.3, S("Middle1"): 0.3, S("Ring1"): 0.3,
                S("Little1"): 0.3, S("Thumb1"): 0.5}
    if part == 41:                             # 指（その指の3本＋手）
        fn = FNAME[FINGER_ORDER[finger]] if finger >= 0 else "Index"
        return {S(f"{fn}1"): 1.0, S(f"{fn}2"): 1.0, S(f"{fn}3"): 1.0, S("Hand"): 0.6}
    if part in (50, 51):                        # 太腿ベルト（右脚）
        return {S("UpperLeg", "R"): 1.0}
    if part == 60:
        return {C("Neck"): 1.0, C("UpperChest"): 0.5}
    return {C("Hips"): 1.0}


def compute_weights(ob, T, power=4.0, smooth_iters=3, arm_r=0.075, arm_iters=14):
    me = ob.data
    part = [0] * len(me.vertices)
    side = [0] * len(me.vertices)
    fing = [0] * len(me.vertices)
    me.attributes["part"].data.foreach_get("value", part)
    me.attributes["side"].data.foreach_get("value", side)
    me.attributes["finger"].data.foreach_get("value", fing)
    names = list(T.keys())
    idx = {n: i for i, n in enumerate(names)}
    segs = [(T[n][0], T[n][1]) for n in names]
    W = np.zeros((len(me.vertices), len(names)), np.float32)
    # 袖ぐり（身頃と袖をつなぐ辺）からの距離。近い所は上腕の効きを落とさず、あとで広く平滑化する
    co = np.array([tuple(v.co) for v in me.vertices])
    jv = set()
    for e in me.edges:
        a, b = e.vertices
        if {part[a], part[b]} == {1, 3}:
            jv |= {a, b}
    J = co[list(jv)] if jv else np.zeros((1, 3))
    dj = np.full(len(co), 9.0)
    for k in range(0, len(co), 2048):
        blk = co[k:k + 2048]
        dj[k:k + 2048] = np.sqrt(((blk[:, None, :] - J[None, :, :]) ** 2).sum(-1)).min(axis=1)
    for vi, v in enumerate(me.vertices):
        p = v.co
        sd = side[vi] if side[vi] else (1 if p.x > 0 else -1)
        allow = allowed_bones(part[vi], sd, fing[vi], p.z, T)
        if part[vi] in (1, 4, 8) and p.y > 0:
            # 背面の裾は座ったときに太腿へ引かれて垂れないよう太腿の効きを弱める（前面は残す）
            for t in ("L", "R"):
                k = f"J_Bip_{t}_UpperLeg"
                if k in allow:
                    allow[k] *= 0.25 * (1 - ss(0.0, 0.12, p.y)) + 0.05
        if part[vi] in (1, 3) and dj[vi] < arm_r:
            for t in ("L", "R"):
                k = f"J_Bip_{t}_UpperArm"
                if k in allow:
                    allow[k] = max(allow[k], 0.9)
        for bn, sc in allow.items():
            if bn not in idx:
                continue
            a, b = segs[idx[bn]]
            d, t = _seg_dist(p, a, b)
            W[vi, idx[bn]] = sc / (d + 0.012) ** power
    W /= np.maximum(W.sum(axis=1, keepdims=True), 1e-12)
    # 近傍平均でなめらかに（同じ部位グループ内）
    adj = [[] for _ in me.vertices]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    for _ in range(smooth_iters):
        W2 = W.copy()
        for vi in range(len(adj)):
            nb = [j for j in adj[vi] if part[j] // 10 == part[vi] // 10]
            if nb:
                W2[vi] = 0.5 * W[vi] + 0.5 * W[nb].mean(axis=0)
        W = W2
    # 袖ぐり周辺だけ追加で平滑化（腕を上げたときに脇の面が折れないように）
    near = [vi for vi in range(len(adj)) if dj[vi] < arm_r and part[vi] in (1, 3, 4)]
    fall = {vi: 1.0 - ss(0.0, arm_r, dj[vi]) for vi in near}
    for _ in range(arm_iters):
        W2 = W.copy()
        for vi in near:
            nb = [j for j in adj[vi] if part[j] in (1, 3, 4)]
            if nb:
                a = 0.6 * fall[vi]
                W2[vi] = (1 - a) * W[vi] + a * W[nb].mean(axis=0)
        W = W2
    # 上位 MAX_INFL 本に制限して正規化
    order = np.argsort(-W, axis=1)[:, :MAX_INFL]
    groups = {}
    for n in names:
        groups[n] = ob.vertex_groups.get(n) or ob.vertex_groups.new(name=n)
    for vi in range(len(adj)):
        ids = order[vi]
        ws = W[vi, ids]
        ws = np.where(ws < 0.01, 0.0, ws)
        s = ws.sum()
        if s <= 0:
            continue
        ws /= s
        for i, w in zip(ids, ws):
            if w > 0:
                groups[names[i]].add([vi], float(w), "REPLACE")
    return len(names)


def bind(ob, ao):
    for md in [m for m in ob.modifiers if m.type == "ARMATURE"]:
        ob.modifiers.remove(md)
    ob.vertex_groups.clear()
    ob.parent = ao
    md = ob.modifiers.new("Armature", "ARMATURE")
    md.object = ao
    return md


# ---------------- ポーズ ----------------
# pb.matrix は評価タイミング次第で未計算（ゼロ行列）になるので使わない。
# レスト行列から自前でワールド姿勢を追跡し、最後に matrix_basis を計算して入れる。
class PoseCtx:
    def __init__(self, ao):
        self.ao = ao
        self.W = {b.name: b.matrix_local.copy() for b in ao.data.bones}

    def _desc(self, name):
        out = []
        st = list(self.ao.data.bones[name].children)
        while st:
            b = st.pop()
            out.append(b.name)
            st += list(b.children)
        return out

    def apply_delta(self, name, D):
        for n in [name] + self._desc(name):
            self.W[n] = D @ self.W[n]

    def aim(self, name, target_dir):
        cur = self.W[name]
        head = cur.translation.copy()
        ydir = (cur.to_3x3() @ Vector((0, 1, 0))).normalized()
        R = ydir.rotation_difference(Vector(target_dir).normalized()).to_matrix().to_4x4()
        self.apply_delta(name, Matrix.Translation(head) @ R @ Matrix.Translation(-head))

    def commit(self):
        for b in self.ao.data.bones:
            pb = self.ao.pose.bones[b.name]
            rest = b.matrix_local
            if b.parent:
                prest = b.parent.matrix_local
                pw = self.W[b.parent.name]
                basis = (prest.inverted() @ rest).inverted() @ pw.inverted() @ self.W[b.name]
            else:
                basis = rest.inverted() @ self.W[b.name]
            pb.matrix_basis = basis
        bpy.context.view_layer.update()


def reset_pose(ao):
    for pb in ao.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def pose_natural(ao):
    """Master の NaturalPose と同じ腕角度（abd14 fwd6 elbow14）"""
    reset_pose(ao)
    P = PoseCtx(ao)
    for tag in ("L", "R"):
        _natural_arm(P, tag)
    P.commit()


def _natural_arm(P, tag):
    sx = 1 if tag == "L" else -1
    d1n, d2n = arm_dirs("NATURAL")
    Wn, hn, nn, bn = wrist_frame("NATURAL")
    m = lambda v: Vector((v.x * sx, v.y, v.z))
    P.aim(f"J_Bip_{tag}_UpperArm", m(d1n))
    P.aim(f"J_Bip_{tag}_LowerArm", m(d2n))
    P.aim(f"J_Bip_{tag}_Hand", m(hn))


def pose_test(ao, kind):
    reset_pose(ao)
    P = PoseCtx(ao)
    if kind == "ArmsWide":            # 左右に大きく開く（水平）
        P.aim("J_Bip_L_UpperArm", (1, 0, 0.05))
        P.aim("J_Bip_L_LowerArm", (1, -0.05, 0.05))
        P.aim("J_Bip_R_UpperArm", (-1, 0, 0.05))
        P.aim("J_Bip_R_LowerArm", (-1, -0.05, 0.05))
    elif kind == "ArmForward":        # 片腕を前へ
        P.aim("J_Bip_L_UpperArm", (0.15, -1, 0.0))
        P.aim("J_Bip_L_LowerArm", (0.1, -1, 0.0))
        _natural_arm(P, "R")
    elif kind == "ArmBack":           # 片腕を後ろへ
        P.aim("J_Bip_L_UpperArm", (0.35, 0.75, -0.55))
        P.aim("J_Bip_L_LowerArm", (0.3, 0.7, -0.65))
        _natural_arm(P, "R")
    elif kind == "Elbow90":           # 肘90度（上腕は下ろし、前腕を前へ）
        for tag, sx in (("L", 1), ("R", -1)):
            P.aim(f"J_Bip_{tag}_UpperArm", (0.22 * sx, -0.05, -1))
            P.aim(f"J_Bip_{tag}_LowerArm", (0.05 * sx, -1, 0.0))
    elif kind == "Asymmetric":        # 左右で異なる方向（左は斜め前上、右は横やや後ろ）
        P.aim("J_Bip_L_UpperArm", (0.55, -0.7, 0.45))
        P.aim("J_Bip_L_LowerArm", (0.45, -0.8, 0.4))
        P.aim("J_Bip_R_UpperArm", (-0.85, 0.35, -0.1))
        P.aim("J_Bip_R_LowerArm", (-0.8, 0.2, 0.3))
    elif kind == "Seated":          # 座り（メインメニューで座っている想定の確認）: 股関節・膝 90°
        for tag in ("L", "R"):
            sx = 1 if tag == "L" else -1
            P.aim(f"J_Bip_{tag}_UpperLeg", (0.08 * sx, -1, -0.06))
            P.aim(f"J_Bip_{tag}_LowerLeg", (0.02 * sx, 0.05, -1))
            P.aim(f"J_Bip_{tag}_Foot", (0.05 * sx, -1, -0.35))
            _natural_arm(P, tag)
    P.commit()
