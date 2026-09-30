# ElseIf 人体素体（首から下）。lib.py と同じ名前空間でexecする
# 座標: Z上 / 正面は-Y / +Xがキャラクターの左。単位m。
# 左半身を作ってX反転で右を作る

FOOT_Z = 0.058          # 靴内の足裏高さ（厚底ソール分）
FOOT_TOE_OUT = 6.0      # つま先の外向き角（deg）
SOCKET_Z = 1.392        # HeadSocket（首付け根・頭部パーツの首下端）
SHOULDER = Vector((0.148, 0.010, 1.305))   # 肩関節（左）
L_UPPER = 0.270
L_FORE = 0.232

POSES = {
    # Base Pose: A-pose（リグ前提）。腕を下げ42°
    "BASE": dict(abd=42.0, fwd=4.0, elbow=12.0),
    # 自然立ち
    "NATURAL": dict(abd=14.0, fwd=6.0, elbow=14.0),
}


def arm_dirs(pose):
    p = POSES[pose]
    a = math.radians(p["abd"])
    fw = math.radians(p["fwd"])
    d1 = Vector((math.sin(a) * math.cos(fw), -math.sin(fw), -math.cos(a) * math.cos(fw))).normalized()
    # 肘の屈曲: 前腕を前方(-Y)へ倒す
    e = math.radians(p["elbow"])
    fperp = Vector((0, -1, 0))
    fperp = (fperp - d1 * fperp.dot(d1)).normalized()
    d2 = (d1 * math.cos(e) + fperp * math.sin(e)).normalized()
    return d1, d2


def arm_center(pose):
    d1, d2 = arm_dirs(pose)
    elbow = SHOULDER + d1 * L_UPPER

    # 付け根の逃がし方向。曲がりが急だとリング同士が交差する（SDF化でトゲになる）ので
    # 下向き成分を持たせ、長めの区間で腕の向きへつなぐ
    root = Vector((1.0, 0.0, -0.35)).normalized()

    def c(s):
        # 肘付近は2本の直線をなめらかにつなぐ
        k = ss(L_UPPER - 0.05, L_UPPER + 0.05, s)
        p1 = SHOULDER + d1 * s
        p2 = elbow + d2 * (s - L_UPPER)
        p = p1.lerp(p2, k)
        if s < 0.07:
            # 肩関節より内側はポーズに関係なく胴体の内側へ逃がす（肩上へ突き出さない）
            k0 = ss(-0.065, 0.07, s)
            p = (SHOULDER + root * s).lerp(p, k0)
        return p
    return c


def wrist_frame(pose):
    d1, d2 = arm_dirs(pose)
    W = SHOULDER + d1 * L_UPPER + d2 * L_FORE
    h = d2
    v = Vector((-1, 0, 0))
    n = (v - h * v.dot(h)).normalized()      # 手のひら法線（内側）
    b = n.cross(h).normalized()               # 親指側（前）
    return W, h, n, b


# ---------------- 胴体 ----------------
TORSO_KEYS = [
    # z : [w(左右), 前, 後, ex, cy]
    (0.772, [0.070, 0.050, 0.058, 2.0, 0.006]),
    (0.800, [0.113, 0.074, 0.084, 2.2, 0.006]),
    (0.840, [0.143, 0.085, 0.098, 2.3, 0.008]),
    (0.880, [0.151, 0.088, 0.104, 2.3, 0.010]),
    (0.930, [0.146, 0.087, 0.098, 2.3, 0.008]),
    (0.980, [0.131, 0.084, 0.087, 2.3, 0.005]),
    (1.030, [0.117, 0.080, 0.078, 2.3, 0.002]),
    (1.080, [0.119, 0.083, 0.080, 2.3, 0.000]),
    (1.130, [0.126, 0.090, 0.085, 2.3, 0.000]),
    (1.180, [0.132, 0.100, 0.090, 2.3, -0.002]),
    (1.220, [0.137, 0.097, 0.092, 2.4, -0.001]),
    (1.260, [0.142, 0.087, 0.090, 2.4, 0.001]),
    (1.300, [0.140, 0.074, 0.082, 2.4, 0.004]),
    (1.335, [0.108, 0.061, 0.068, 2.3, 0.008]),
    (1.358, [0.066, 0.051, 0.057, 2.1, 0.011]),
    (1.372, [0.051, 0.048, 0.050, 2.0, 0.012]),
    (SOCKET_Z, [0.046, 0.045, 0.045, 2.0, 0.012]),
]


def build_torso():
    def prof(z):
        v = interp_keys(TORSO_KEYS, z)
        return [v[0], v[0], v[1], v[2], v[3]]

    def cen(z):
        return Vector((0, interp_keys(TORSO_KEYS, z)[4], z))

    def deform(z, th, p, t, f, l):
        # 控えめな胸（左右の二つの膨らみ）: 正面±35°付近
        for side in (-1, 1):
            dth = th - (math.pi / 2 - side * 0.62)
            g = math.exp(-(dth / 0.38) ** 2) * math.exp(-((z - 1.18) / 0.045) ** 2)
            p = p + f * 0.008 * g
        # 肩甲骨の平面感
        return p

    zs = dense_samples(TORSO_KEYS[0][0], TORSO_KEYS[-1][0], 0.012)
    ob = build_loft("Body_Torso", zs, 40, cen, prof, Vector((0, -1, 0)), deform=deform,
                    cap_start=True, cap_end=True)
    return ob


# ---------------- 脚 ----------------
LEG_PTS = [  # z, x, y（左脚の中心線）
    (0.090, 0.096, 0.012),
    (0.130, 0.096, 0.010),
    (0.300, 0.095, 0.008),
    (0.480, 0.093, 0.000),
    (0.680, 0.090, 0.004),
    (0.880, 0.085, 0.008),
]
LEG_KEYS = [
    # z : [外, 内, 前, 後, ex]
    (0.090, [0.032, 0.032, 0.040, 0.042, 2.0]),
    (0.130, [0.030, 0.029, 0.030, 0.034, 2.0]),
    (0.180, [0.031, 0.030, 0.029, 0.038, 2.0]),
    (0.260, [0.037, 0.035, 0.033, 0.043, 2.0]),
    (0.340, [0.045, 0.042, 0.037, 0.051, 2.0]),
    (0.400, [0.045, 0.043, 0.039, 0.047, 2.0]),
    (0.460, [0.044, 0.043, 0.043, 0.040, 2.2]),
    (0.500, [0.047, 0.045, 0.046, 0.041, 2.2]),
    (0.560, [0.053, 0.051, 0.050, 0.046, 2.0]),
    (0.640, [0.060, 0.057, 0.055, 0.054, 2.0]),
    (0.720, [0.066, 0.062, 0.060, 0.061, 2.0]),
    (0.800, [0.071, 0.064, 0.064, 0.067, 2.0]),
    (0.880, [0.068, 0.052, 0.058, 0.064, 2.0]),
]


def leg_center(z):
    k = [(p[0], [p[1], p[2]]) for p in LEG_PTS]
    x, y = interp_keys(k, z)
    return Vector((x, y, z))


def leg_prof(z, extra=0.0):
    v = interp_keys(LEG_KEYS, z)
    return [v[0] + extra, v[1] + extra, v[2] + extra, v[3] + extra, v[4]]


def build_leg():
    zs = dense_samples(0.090, 0.880, 0.012)
    return build_loft("Body_Leg_L", zs, 28, leg_center, leg_prof, Vector((0, -1, 0)),
                      cap_start=True, cap_end=True)


# ---------------- 足（靴の中） ----------------
FOOT_KEYS = [
    # y : [内側(-X), 外側(+X), 上, 下, ex, cz]
    (0.050, [0.020, 0.020, 0.028, 0.020, 2.0, FOOT_Z + 0.030]),
    (0.035, [0.030, 0.030, 0.040, 0.030, 2.4, FOOT_Z + 0.038]),
    (0.000, [0.033, 0.033, 0.055, 0.030, 2.4, FOOT_Z + 0.040]),
    (-0.060, [0.040, 0.038, 0.034, 0.024, 2.6, FOOT_Z + 0.030]),
    (-0.130, [0.045, 0.041, 0.020, 0.013, 2.6, FOOT_Z + 0.020]),
    (-0.175, [0.040, 0.034, 0.015, 0.010, 2.4, FOOT_Z + 0.016]),
    (-0.198, [0.020, 0.018, 0.010, 0.008, 2.0, FOOT_Z + 0.014]),
]


def build_foot():
    def cen(y):
        v = interp_keys(FOOT_KEYS, -y)
        return Vector((0.096, y, v[5]))

    def prof(y):
        return interp_keys([(-k[0], k[1]) for k in FOOT_KEYS], -y)

    keys = [(-k[0], k[1]) for k in FOOT_KEYS]
    ys = [-s for s in dense_samples(-0.050, 0.198, 0.012)]

    def cen2(y):
        return Vector((0.096, y, interp_keys(keys, -y)[5]))

    def prof2(y):
        v = interp_keys(keys, -y)
        return v[:5]
    ob = build_loft("Body_Foot_L", ys, 20, cen2, prof2, Vector((0, 0, 1)),
                    cap_start=True, cap_end=True)
    # 靴と同じだけつま先を外へ向ける（足首まわりで回転）
    piv = Vector((0.097, 0.014, 0.0))
    ob.data.transform(Matrix.Translation(piv) @ Matrix.Rotation(math.radians(FOOT_TOE_OUT), 4, "Z") @ Matrix.Translation(-piv))
    return ob


# ---------------- 腕 ----------------
ARM_KEYS = [
    # s : [内下(l+), 外上(l-), 前, 後, ex]
    (-0.050, [0.032, 0.029, 0.033, 0.033, 2.0]),
    (0.000, [0.041, 0.045, 0.045, 0.045, 2.0]),
    (0.050, [0.039, 0.046, 0.043, 0.043, 2.0]),
    (0.120, [0.036, 0.040, 0.039, 0.040, 2.0]),
    (0.200, [0.033, 0.035, 0.035, 0.037, 2.0]),
    (0.265, [0.030, 0.032, 0.031, 0.033, 2.0]),
    (0.310, [0.032, 0.033, 0.036, 0.034, 2.0]),
    (0.380, [0.028, 0.028, 0.030, 0.028, 2.0]),
    (0.460, [0.020, 0.020, 0.026, 0.026, 2.2]),
    (0.505, [0.018, 0.018, 0.025, 0.025, 2.2]),
]


def build_arm(pose):
    c = arm_center(pose)
    ss_ = dense_samples(-0.050, 0.505, 0.012)
    return build_loft("Body_Arm_L", ss_, 24, c, lambda s: interp_keys(ARM_KEYS, s),
                      Vector((0, -1, 0)), cap_start=True, cap_end=True)


# ---------------- 手（指まで） ----------------
FINGERS = {
    # name: (付け根a, c(親指側+), 開き角deg, [節長], 半径, [累積曲げdeg])
    "Index":  (0.092, 0.025, 5.0, [0.037, 0.023, 0.018], 0.0090, [10, 30, 45]),
    "Middle": (0.095, 0.008, 0.0, [0.041, 0.026, 0.019], 0.0093, [14, 36, 52]),
    "Ring":   (0.091, -0.009, -5.0, [0.038, 0.024, 0.018], 0.0088, [18, 42, 58]),
    "Pinky":  (0.084, -0.025, -11.0, [0.029, 0.018, 0.016], 0.0076, [24, 50, 66]),
}


def build_hand(pose):
    W, h, n, b = wrist_frame(pose)

    def P(a, c, e):
        return W + h * a + b * c + n * e

    parts = []
    # 手のひら（l=n:手のひら側、f=b:親指側）
    PALM = [
        (-0.020, [0.016, 0.016, 0.024, 0.024, 2.4]),
        (0.010, [0.015, 0.013, 0.031, 0.029, 2.8]),
        (0.050, [0.014, 0.012, 0.039, 0.036, 3.0]),
        (0.085, [0.012, 0.011, 0.041, 0.037, 3.2]),
        (0.100, [0.010, 0.010, 0.038, 0.034, 3.0]),
    ]
    palm = build_loft("Hand_Palm", dense_samples(-0.02, 0.10, 0.008), 20,
                      lambda a: P(a, 0.001, 0.0), lambda a: interp_keys(PALM, a), b,
                      cap_start=True, cap_end=True)
    parts.append(palm)
    for nm, (a0, c0, spread, segs, r0, curl) in FINGERS.items():
        sp = math.radians(spread)
        base_dir = (h * math.cos(sp) + b * math.sin(sp)).normalized()
        pts = [P(a0 - 0.014, c0, -0.001)]
        pts.append(P(a0, c0, -0.001))
        cur = pts[-1]
        for L, cd in zip(segs, curl):
            ang = math.radians(cd)
            d = (base_dir * math.cos(ang) + n * math.sin(ang)).normalized()
            cur = cur + d * L
            pts.append(cur)
        parts.append(finger_loft("Hand_" + nm, pts, r0, b))
    # 親指
    tdirs = [(0.85, 0.30, 0.40), (0.92, 0.10, 0.34), (0.90, -0.05, 0.40)]
    tl = [0.038, 0.029, 0.024]
    cur = P(0.010, 0.019, 0.007)
    pts = [cur]
    for (ka, kb, kn), L in zip(tdirs, tl):
        d = (h * ka + b * kb + n * kn).normalized()
        cur = cur + d * L
        pts.append(cur)
    parts.append(finger_loft("Hand_Thumb", pts, 0.0118, b, taper=0.78))
    return parts


def finger_loft(name, pts, r0, fref, taper=0.74):
    # 折れ線を弧長パラメータ化し、関節を少し膨らませる
    acc = [0.0]
    for i in range(1, len(pts)):
        acc.append(acc[-1] + (pts[i] - pts[i - 1]).length)
    total = acc[-1]

    def cen(s):
        s = max(0.0, min(total, s))
        i = 0
        while i < len(acc) - 2 and acc[i + 1] < s:
            i += 1
        t = (s - acc[i]) / max(acc[i + 1] - acc[i], 1e-6)
        return pts[i].lerp(pts[i + 1], t)

    def prof(s):
        u = s / total
        r = r0 * (1 - (1 - taper) * u)
        for a in acc[2:-1]:
            r *= 1 + 0.07 * math.exp(-((s - a) / 0.004) ** 2)
        tip = total - s
        if tip < r:
            r *= math.sqrt(max(0.05, 1 - ((r - tip) / r) ** 2))
        return [r * 0.92, r * 0.92, r, r, 2.0]
    sm = dense_samples(0.0, total, 0.004)
    return build_loft(name, sm, 12, cen, prof, fref, cap_start=True, cap_end=True)
