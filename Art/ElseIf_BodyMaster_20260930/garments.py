# ElseIf 衣装（ジャケット・襟・萌え袖・ハーフパンツ・サイハイソックス・靴）
import random
import numpy as np


# ---------------- プリント用テクスチャ（手続き生成） ----------------
def _canvas(w, h, bg):
    a = np.zeros((h, w, 4), dtype=np.float32)
    a[..., :3] = bg
    a[..., 3] = 1.0
    return a


def _draw_glyph(a, x0, y0, s, kind, col, th):
    yy, xx = np.mgrid[0:s, 0:s]
    u = (xx + 0.5) / s * 2 - 1
    v = (yy + 0.5) / s * 2 - 1
    t = th / s * 2
    m = np.zeros((s, s), bool)
    if kind == 0:   # 四角枠
        m = (np.maximum(abs(u), abs(v)) < 0.62) & (np.maximum(abs(u), abs(v)) > 0.62 - t)
    elif kind == 1:  # 塗り四角
        m = np.maximum(abs(u), abs(v)) < 0.36
    elif kind == 2:  # 三角（A）
        edge = (v > -0.55) & (abs(u) < (0.6 - (v + 0.55) * 0.55))
        inner = (v > -0.55 + t) & (abs(u) < (0.6 - t * 1.6 - (v + 0.55) * 0.55))
        m = edge & ~inner
        m |= (abs(v + 0.1) < t * 0.5) & (abs(u) < 0.35)
    elif kind == 3:  # 丸枠
        r = np.sqrt(u * u + v * v)
        m = (r < 0.6) & (r > 0.6 - t)
    elif kind == 4:  # ×
        m = ((abs(u - v) < t * 0.8) | (abs(u + v) < t * 0.8)) & (np.maximum(abs(u), abs(v)) < 0.55)
    elif kind == 5:  # 二本線
        m = ((abs(v - 0.25) < t * 0.6) | (abs(v + 0.25) < t * 0.6)) & (abs(u) < 0.55)
    elif kind == 6:  # 口＋点
        m = (np.maximum(abs(u), abs(v)) < 0.58) & (np.maximum(abs(u), abs(v)) > 0.58 - t)
        m |= np.maximum(abs(u), abs(v)) < 0.14
    elif kind == 7:  # 斜線入り箱
        m = (np.maximum(abs(u), abs(v)) < 0.58) & (np.maximum(abs(u), abs(v)) > 0.58 - t)
        m |= (abs(u + v) < t * 0.7) & (np.maximum(abs(u), abs(v)) < 0.58)
    elif kind == 8:  # 山形
        m = (abs(v - (0.35 - abs(u) * 0.9)) < t * 0.7) & (abs(u) < 0.55)
        m |= (abs(v - (-0.15 - abs(u) * 0.9)) < t * 0.7) & (abs(u) < 0.55)
    a[y0:y0 + s, x0:x0 + s][m] = (*col, 1.0)


def make_glyph_image(name, cells=16, cell=128, fg=(0.9, 0.9, 0.9), bg=(0.02, 0.02, 0.02), seed=3):
    rnd = random.Random(seed)
    a = _canvas(cell, cell * cells, bg)
    for k in range(cells):
        _draw_glyph(a, 0, k * cell, cell, rnd.randrange(9), fg, 12)
    return _to_image(name, a)


def make_text_image(name, rows=64, w=96, fg=(0.03, 0.03, 0.03), bg=(0.80, 0.79, 0.77), seed=7):
    """縦書き風の文字列プリント（黒い細かいブロック）"""
    rnd = random.Random(seed)
    ch = 32
    a = _canvas(w, rows * ch, bg)
    for col in range(2):
        x0 = 14 + col * 40
        for r in range(rows):
            if rnd.random() < 0.12:
                continue
            y0 = r * ch + 4
            for k in range(rnd.randint(2, 4)):
                bx = x0 + rnd.randrange(0, 18)
                by = y0 + rnd.randrange(0, 20)
                a[by:by + rnd.randint(3, 8), bx:bx + rnd.randint(4, 14), :3] = fg
    return _to_image(name, a)


def _to_image(name, a):
    h, w = a.shape[:2]
    img = bpy.data.images.get(name)
    if img is None or img.size[0] != w or img.size[1] != h:
        if img:
            bpy.data.images.remove(img)
        img = bpy.data.images.new(name, w, h, alpha=True)
    img.pixels.foreach_set(a.ravel())
    img.pack()
    return img


def image_mat(name, img, rough=0.8, alpha=False, emit=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None) or nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Cubic"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if alpha:
        nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = rough
    return m


def print_mats():
    MAT["print_glyph"] = image_mat("M_Print_Glyph", make_glyph_image("T_Print_Glyph"))
    MAT["print_text"] = image_mat("M_Print_Text", make_text_image("T_Print_Text"))
    MAT["sock_glyph"] = image_mat("M_Sock_Glyph", make_glyph_image("T_Sock_Glyph", fg=(0.75, 0.75, 0.75), bg=(0.05, 0.05, 0.055), seed=11), 0.9)


# ---------------- ジャケット身頃 ----------------
JACKET_KEYS = [
    # z : [w, 前, 後, ex, cy]
    (0.700, [0.213, 0.152, 0.152, 2.3, 0.006]),
    (0.760, [0.208, 0.150, 0.149, 2.3, 0.006]),
    (0.840, [0.201, 0.148, 0.144, 2.3, 0.006]),
    (0.920, [0.193, 0.147, 0.137, 2.3, 0.005]),
    (1.000, [0.181, 0.145, 0.129, 2.3, 0.003]),
    (1.080, [0.173, 0.138, 0.123, 2.3, 0.001]),
    (1.160, [0.170, 0.132, 0.120, 2.3, 0.000]),
    (1.210, [0.174, 0.126, 0.117, 2.3, 0.000]),
    (1.250, [0.194, 0.120, 0.115, 2.4, 0.002]),
    (1.280, [0.208, 0.114, 0.113, 2.5, 0.004]),
    (1.305, [0.207, 0.108, 0.110, 2.6, 0.005]),
    (1.330, [0.194, 0.101, 0.106, 2.6, 0.006]),
    (1.348, [0.166, 0.093, 0.099, 2.4, 0.008]),
    (1.362, [0.128, 0.083, 0.089, 2.2, 0.010]),
    (1.372, [0.082, 0.074, 0.079, 2.0, 0.012]),
]
HEM_BASE = 0.700


def hem_z(th):
    s = math.sin(th)
    return (0.718 + 0.030 * max(s, 0) ** 1.4 - 0.026 * max(-s, 0) ** 1.2
            + 0.004 * math.sin(5 * th + 0.7) + 0.0025 * math.sin(9 * th + 2.1)
            # 左右非対称のごく小さな高さ差とたわみ（v02）
            + 0.005 * math.sin(th + 0.5) + 0.003 * math.sin(3 * th + 2.4))


def build_jacket_shell():
    def prof(z):
        v = interp_keys(JACKET_KEYS, z)
        return [v[0], v[0], v[1], v[2], v[3]]

    def cen(z):
        return Vector((0, interp_keys(JACKET_KEYS, z)[4], z))

    front_c = math.pi / 2

    def deform(z, th, p, t, f, l):
        c = cen(z)
        rad = Vector((p.x - c.x, p.y - c.y, 0))
        rl = rad.length
        rad = rad / rl if rl > 1e-6 else rad
        zip_keep = 1 - math.exp(-((th - front_c) / 0.22) ** 2)
        # 胸〜裾の縦のドレープ（下ほど深い）
        A = 0.011 * ss(1.17, 0.76, z)
        fold = math.sin(7 * th + 0.8 + 0.5 * math.sin(3 * th)) * 0.7 + math.sin(11 * th + 1.9) * 0.3
        p = p + rad * A * fold * zip_keep
        # 腹部〜裾: 大きく緩い布の流れ（左右非対称、v02）
        A2 = 0.009 * ss(1.08, 0.72, z)
        p = p + rad * A2 * (0.6 * math.sin(3 * th + 1.3) + 0.4 * math.sin(2 * th + 0.3)) * zip_keep
        p = Vector((p.x + 0.004 * ss(1.05, 0.72, z), p.y, p.z))
        # 脇下の圧縮しわ（横方向）
        side = abs(math.cos(th)) ** 6
        env = math.exp(-((z - 1.19) / 0.035) ** 2)
        p = p + rad * 0.0024 * side * env * math.sin((z - 1.15) / 0.021 * TAU)
        # 腰まわり: 背中側は骨盤で流れが変わり少し寄る
        back = max(-math.sin(th), 0)
        p = p - rad * 0.006 * back * math.exp(-((z - 0.95) / 0.06) ** 2)
        # 裾の高さ（前短め・後ろ長め・わずかな波）
        if z < 0.86:
            k = ss(0.86, HEM_BASE, z)
            p = Vector((p.x, p.y, p.z + (hem_z(th) - HEM_BASE) * k))
        return p

    def mat(i, j, s, th, c):
        # ファスナー帯の輪郭はシェーダー側（ZIP_HALF）で決めるので面は広めに割り当てる
        return 1 if (abs(c.x) < ZIP_HALF + 0.008 and c.y < 0) else 0

    zs = dense_samples(HEM_BASE, JACKET_KEYS[-1][0], 0.010)
    ob = build_loft("Jacket_Body", zs, 72, cen, prof, Vector((0, -1, 0)), deform=deform, mat_fn=mat)
    return ob


COLLAR_KEYS = [
    (1.345, [0.080, 0.077, 0.081, 2.2, 0.012]),
    (1.370, [0.075, 0.072, 0.077, 2.1, 0.012]),
    (1.400, [0.072, 0.070, 0.074, 2.0, 0.012]),
    (1.430, [0.075, 0.073, 0.076, 2.0, 0.012]),
    (1.450, [0.078, 0.076, 0.079, 2.0, 0.012]),
]


def build_collar():
    def prof(z):
        v = interp_keys(COLLAR_KEYS, z)
        return [v[0], v[0], v[1], v[2], v[3]]

    def cen(z):
        return Vector((0, 0.012, z))

    def deform(z, th, p, t, f, l):
        # 立ち襟の上端はわずかに後ろ高・前低
        p = Vector((p.x, p.y, p.z - 0.006 * max(math.sin(th), 0) ** 2 * ss(1.40, 1.45, z)))
        return p

    def mat(i, j, s, th, c):
        return 1 if (abs(c.x) < ZIP_HALF + 0.008 and c.y < 0) else 0
    zs = dense_samples(1.345, 1.450, 0.007)
    return build_loft("Jacket_Collar", zs, 64, cen, prof, Vector((0, -1, 0)), deform=deform, mat_fn=mat)


# ---------------- 萌え袖 ----------------
SLEEVE_KEYS = [
    # s : [下(脇側), 上(肩側), 前, 後]
    # 付け根は身頃の内側で小さなドームに絞って閉じる
    (-0.075, [0.010, 0.008, 0.010, 0.010]),
    (-0.060, [0.042, 0.022, 0.044, 0.046]),
    (-0.030, [0.070, 0.040, 0.062, 0.063]),
    (0.000, [0.076, 0.055, 0.065, 0.065]),
    (0.060, [0.072, 0.058, 0.063, 0.063]),
    (0.140, [0.065, 0.052, 0.058, 0.058]),
    (0.220, [0.060, 0.048, 0.055, 0.055]),
    (0.270, [0.059, 0.047, 0.055, 0.055]),
    (0.340, [0.059, 0.047, 0.054, 0.054]),
    (0.420, [0.063, 0.050, 0.058, 0.058]),
    (0.500, [0.070, 0.054, 0.066, 0.062]),
    (0.590, [0.076, 0.058, 0.072, 0.067]),
    (0.645, [0.071, 0.055, 0.067, 0.063]),
    (0.672, [0.060, 0.049, 0.057, 0.056]),
    (0.700, [0.057, 0.047, 0.054, 0.054]),
]
SLEEVE_END = 0.700
BAND_C = math.pi / 2 + 0.45     # プリント帯の中心角（前寄り外側）
BAND_HW = 0.34                  # 半角
BAND_W = 0.045                  # 帯の実幅(m)
BAND_S0, BAND_S1 = 0.07, 0.64   # 帯の範囲（肩からの距離）。肩の接合部はUVが混ざるので避ける
ZIP_HALF = 0.0165               # ファスナー帯の半幅


def build_sleeve(pose):
    arm_c = arm_center(pose)
    natural = POSES[pose]["abd"] < 20

    def prof(s):
        v = interp_keys(SLEEVE_KEYS, s)
        return [v[0], v[1], v[2], v[3], 2.0]

    def cen(s):
        return arm_c(s)

    def deform(s, th, p, t, f, l):
        c = arm_c(s)
        rad = (p - c)
        rl = rad.length
        rad = rad / rl if rl > 1e-6 else rad
        # 重力: 腕に垂直な成分だけ袖先を垂らす
        g = Vector((0, 0, -1))
        g = g - t * g.dot(t)
        droop = 0.026 * ss(0.30, SLEEVE_END, s) ** 1.4
        p = p + g * droop
        # 付け根は身頃の内側へ沈める（開口が肩上に出ないように）
        p = p + l * 0.020 * ss(0.0, -0.075, s)
        # 手首まわりのたるみ（輪状のしわ、角度で位相をずらす）
        env = ss(0.36, 0.46, s) * ss(0.66, 0.58, s)
        amp = 0.0055
        p = p + rad * amp * env * math.sin((s - 0.34) / 0.048 * TAU + 1.4 * math.sin(2 * th + s * 25))
        # 肘の内側で少し余る
        elb = math.exp(-((s - 0.265) / 0.03) ** 2) * max(math.sin(th), 0) ** 2
        p = p + rad * 0.0035 * elb * math.sin((s - 0.24) / 0.018 * TAU)
        # 脇（下側）の圧縮しわ
        under = max(math.cos(th), 0) ** 3
        p = p + rad * 0.003 * under * math.exp(-((s - 0.03) / 0.04) ** 2) * math.sin(s / 0.02 * TAU + th * 3)
        # 袖口のゴム（細かいリブ）
        cuff = ss(0.665, 0.678, s)
        p = p + rad * 0.0010 * cuff * math.sin(44 * th)
        return p

    def in_band(s, th):
        # 輪郭はシェーダー側（UV 0-1）で決めるので面は広めに割り当てる
        d = (th - BAND_C + math.pi) % TAU - math.pi
        return abs(d) < BAND_HW + 0.14 and BAND_S0 - 0.02 < s < BAND_S1 + 0.02

    def mat(i, j, s, th, c):
        return 2 if in_band(s, th) else 0

    def uv(s, th):
        d = (th - BAND_C + math.pi) % TAU - math.pi
        return (d / (2 * BAND_HW) + 0.5, s / (BAND_W * 16))

    ss_ = sorted(set(dense_samples(-0.075, SLEEVE_END, 0.0065)))
    # 付け根は蓋をして閉じる（開口のままだとSolidifyのリムが裏返って身頃の外へ突き出す）
    return build_loft("Jacket_Sleeve_L", ss_, 48, cen, prof, Vector((0, -1, 0)), deform=deform,
                      mat_fn=mat, uv_fn=uv, cap_start=True)


# ---------------- ハーフパンツ ----------------
SHORTS_KEYS = [
    (0.795, [0.128, 0.080, 0.090, 2.2, 0.006]),
    (0.830, [0.160, 0.097, 0.110, 2.3, 0.008]),
    (0.880, [0.166, 0.101, 0.116, 2.3, 0.010]),
    (0.940, [0.160, 0.099, 0.110, 2.3, 0.008]),
    (1.000, [0.140, 0.094, 0.096, 2.3, 0.004]),
    (1.045, [0.126, 0.089, 0.086, 2.3, 0.002]),
]
SHORTS_HEM = 0.695


def build_shorts():
    def prof(z):
        v = interp_keys(SHORTS_KEYS, z)
        return [v[0], v[0], v[1], v[2], v[3]]

    def cen(z):
        return Vector((0, interp_keys(SHORTS_KEYS, z)[4], z))
    zs = dense_samples(0.795, 1.045, 0.012)
    pel = build_loft("Shorts_Pelvis", zs, 48, cen, prof, Vector((0, -1, 0)), cap_start=True)

    def lprof(z):
        e = 0.018 + 0.012 * ss(0.86, SHORTS_HEM, z)
        return leg_prof(z, e)

    def ldef(z, th, p, t, f, l):
        c = leg_center(z)
        rad = Vector((p.x - c.x, p.y - c.y, 0)).normalized()
        k = ss(0.84, SHORTS_HEM, z)
        p = p + rad * 0.004 * k * math.sin(5 * th + 1.0)
        return p
    zs = dense_samples(SHORTS_HEM, 0.860, 0.010)
    leg = build_loft("Shorts_Leg_L", zs, 36, leg_center, lprof, Vector((0, -1, 0)), deform=ldef)
    return pel, leg


# ---------------- サイハイソックス ----------------
# 絶対領域: ショートパンツ裾(SHORTS_HEM=0.695)からソックス上端までの肌の帯を
# 脚の長さ（床〜股 約0.772m）の約5%≒0.039m にする（v03）。脚・パンツ丈は変えない
SOCK_TOP = SHORTS_HEM - 0.039
SOCK_BAND_C = 0.95


def build_sock(glyph_side="front"):
    def prof(z):
        e = 0.0035 + 0.0022 * ss(SOCK_TOP - 0.03, SOCK_TOP - 0.018, z)
        return leg_prof(z, e)

    def mat(i, j, s, th, c):
        d = (th - SOCK_BAND_C + math.pi) % TAU - math.pi
        return 1 if (abs(d) < 0.28 and 0.25 < s < 0.40) else 0

    def uv(s, th):
        d = (th - SOCK_BAND_C + math.pi) % TAU - math.pi
        return (d / 0.56 + 0.5, (s - 0.25) / (0.022 * 16))
    zs = dense_samples(0.095, SOCK_TOP, 0.008)
    return build_loft("Sock_L", zs, 36, leg_center, prof, Vector((0, -1, 0)), mat_fn=mat, uv_fn=uv)


# ---------------- 靴（ハイカット） ----------------
SHOE_POS = Vector((0.097, 0.014, 0.0))
SHOE_TOE_OUT = FOOT_TOE_OUT


def _rocker(y):
    return 0.018 * ss(-0.12, -0.234, y) ** 1.6 + 0.004 * ss(0.03, 0.068, y)


def _sole_hw(y):
    W = [(-0.19, [0.055]), (-0.13, [0.059]), (-0.06, [0.050]), (0.0, [0.046]), (0.028, [0.041])]
    if y > 0.028:
        return 0.041 * math.sqrt(max(0.0, 1 - ((y - 0.028) / 0.040) ** 2))
    if y < -0.19:
        return 0.055 * math.sqrt(max(0.0, 1 - ((y + 0.19) / 0.046) ** 2))
    return interp_keys(W, y)[0]


def build_shoe():
    parts = []
    # --- ソール ---
    M = 72
    ym, hl = (0.068 - 0.236) / 2, (0.068 + 0.236) / 2
    outline = []
    for k in range(M):
        ph = TAU * k / M
        y = ym + hl * math.cos(ph)
        x = math.copysign(_sole_hw(y), math.sin(ph)) if abs(math.sin(ph)) > 1e-6 else 0.0
        outline.append((x, y))
    levels = [(0.000, 0.95), (0.004, 0.985), (0.009, 1.0), (0.022, 1.0), (0.031, 1.004),
              (0.041, 1.0), (0.052, 0.99), (0.061, 0.972)]
    SW = 1.07
    rings = []
    for li, (z, sc) in enumerate(levels):
        r = []
        for k, (x, y) in enumerate(outline):
            g = 1.0
            if 0.009 < z < 0.052:
                g += 0.035 * max(0.0, math.sin(y * TAU / 0.024)) ** 2 * (abs(x) > 0.02)
            r.append(Vector((x * sc * SW * g, ym + (y - ym) * (sc * 1.02 if sc < 1 else 1.0), z + _rocker(y))))
        rings.append(r)

    def smat(i, j, c):
        z = levels[i][0]
        if z < 0.004:
            return 1   # 接地面
        if 0.022 <= z < 0.031:
            return 2   # 黒ライン
        return 0
    sole = mesh_from_rings("Shoe_Sole", rings, cap_start=True, cap_end=True, mat_fn=smat)
    parts.append((sole, [MAT["sole_white"], MAT["sole_gray"], MAT["shoe_black"]]))

    # --- アッパー（踵→つま先） ---
    UP = [
        (-0.068, [0.018, 0.018, 0.030, 0.018, 2.2, 0.108]),
        (-0.058, [0.031, 0.031, 0.046, 0.042, 2.6, 0.104]),
        (-0.040, [0.041, 0.041, 0.050, 0.044, 3.0, 0.104]),
        (-0.010, [0.047, 0.047, 0.052, 0.044, 3.0, 0.104]),
        (0.030, [0.050, 0.049, 0.046, 0.042, 3.0, 0.102]),
        (0.070, [0.052, 0.051, 0.036, 0.040, 3.0, 0.100]),
        (0.110, [0.057, 0.056, 0.033, 0.037, 2.9, 0.097]),
        (0.150, [0.059, 0.058, 0.030, 0.034, 2.8, 0.094]),
        (0.185, [0.056, 0.056, 0.027, 0.031, 2.7, 0.092]),
        (0.210, [0.047, 0.047, 0.023, 0.029, 2.5, 0.090]),
        (0.226, [0.031, 0.031, 0.017, 0.026, 2.3, 0.089]),
        (0.234, [0.012, 0.012, 0.007, 0.020, 2.0, 0.088]),
    ]

    def ucen(q):
        return Vector((0.0, -q, interp_keys(UP, q)[5] + _rocker(-q)))

    def uprof(q):
        return interp_keys(UP, q)[:5]

    def umat(i, j, s, th, c):
        # 白: つま先のバンパー・側面下部のフォクシング・踵のカウンター
        y = c.y
        rk = _rocker(y)
        # 境界がリング／列に沿うよう y と θ だけで判定（ギザを出さない）
        if s > 0.192:
            return 1
        if abs((th + math.pi / 2 + math.pi) % TAU - math.pi) < 0.95:
            return 1
        if s < -0.040:
            return 1
        return 0
    qs = sorted(set(dense_samples(-0.068, 0.234, 0.006) + [-0.040, 0.192]))
    upper = build_loft("Shoe_Upper", qs, 64, ucen, uprof, Vector((0, 0, 1)), mat_fn=umat,
                       cap_start=True, cap_end=True)
    parts.append((upper, [MAT["shoe_black"], MAT["shoe_white"]]))

    # --- 履き口（シャフト） ---
    SH = [
        (0.105, [0.047, 0.047, 0.052, 0.056, 2.2]),
        (0.150, [0.046, 0.046, 0.050, 0.055, 2.2]),
        (0.200, [0.047, 0.047, 0.050, 0.054, 2.1]),
        (0.232, [0.049, 0.049, 0.052, 0.056, 2.0]),
    ]

    def shdef(z, th, p, t, f, l):
        # 前（タン）を少し高く
        return Vector((p.x, p.y, p.z + 0.014 * max(math.sin(th), 0) ** 3 * ss(0.19, 0.232, z)))
    zs = dense_samples(0.105, 0.232, 0.009)
    shaft = build_loft("Shoe_Shaft", zs, 40, lambda z: Vector((0, 0.004, z)),
                       lambda z: interp_keys(SH, z), Vector((0, -1, 0)), deform=shdef)
    parts.append((shaft, [MAT["shoe_black"]]))
    # 履き口のパッド
    top = interp_keys(SH, 0.232)
    Np = 48
    pad_pts = []
    for k in range(Np + 1):
        th = -math.pi / 2 + TAU * k / Np
        x, y = ring_offset(th, top[0] - 0.004, top[1] - 0.004, top[2] - 0.004, top[3] - 0.004, 2.0)
        pad_pts.append(Vector((x, 0.004 - y, 0.232 + 0.014 * max(math.sin(th), 0) ** 3)))
    pad = build_loft("Shoe_Pad", list(range(Np + 1)), 10, lambda i: pad_pts[int(round(i))],
                     lambda i: [0.008, 0.008, 0.007, 0.007, 2.0], Vector((0, 0, 1)))
    parts.append((pad, [MAT["shoe_black"]]))

    # --- ストラップ2本＋バックル ---
    def strap(name, pts_a, pts_b, buckle_at, buckle_n):
        bm = bmesh.new()
        va = [bm.verts.new(p) for p in pts_a]
        vb = [bm.verts.new(p) for p in pts_b]
        for k in range(len(va) - 1):
            bm.faces.new((va[k], va[k + 1], vb[k + 1], vb[k]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        st = bpy.data.objects.new(name, me)
        add_mod(st, "SOLIDIFY", thickness=0.004, offset=1.0)
        parts.append((st, [MAT["shoe_white"]]))
        bk = box_mesh(name + "_Buckle", buckle_at, (0.007, 0.018, 0.016))
        parts.append((bk, [MAT["metal"]]))
    # 1本目: 履き口の前面を巻く（z=0.165付近）
    zc = 0.168
    v = interp_keys(SH, zc)
    ra, rb = [], []
    for k in range(25):
        th = -0.30 + (math.pi + 0.60) * k / 24 - math.pi / 2 + math.pi / 2
        # 前側半周: θ∈[-0.3, π+0.3]（θ=π/2が前）
        x, y = ring_offset(th, v[0] + 0.004, v[1] + 0.004, v[2] + 0.004, v[3] + 0.004, v[4])
        ra.append(Vector((x, 0.004 - y, zc - 0.011 - 0.006 * math.cos(th))))
        rb.append(Vector((x, 0.004 - y, zc + 0.011 - 0.006 * math.cos(th))))
    x, y = ring_offset(0.05, v[0] + 0.008, v[1], v[2], v[3], v[4])
    strap("Shoe_Strap_Upper", ra, rb, (x, 0.004 - y, zc), None)
    # 2本目: 甲を横切る（y=-0.085）
    sy = -0.085
    v = interp_keys(UP, -sy)
    cz = v[5] + _rocker(sy)
    ra, rb = [], []
    for k in range(25):
        th = -0.25 + (math.pi + 0.5) * k / 24
        x, zz = ring_offset(th, v[0] + 0.004, v[1] + 0.004, v[2] + 0.004, v[3] + 0.004, v[4])
        ra.append(Vector((-x, sy - 0.011, cz + zz)))
        rb.append(Vector((-x, sy + 0.011, cz + zz)))
    x, zz = ring_offset(math.pi - 0.15, v[0] + 0.008, v[1] + 0.008, v[2] + 0.008, v[3] + 0.008, v[4])
    strap("Shoe_Strap_Instep", ra, rb, (-x, sy, cz + zz), None)
    # タン上の赤い×
    for ang in (45, -45):
        rx = box_mesh("Shoe_RedX", (0, -0.058, 0.228), (0.024, 0.004, 0.006),
                      rot=Matrix.Rotation(math.radians(ang), 3, "Y") @ Matrix.Rotation(math.radians(-12), 3, "X"))
        parts.append((rx, [MAT["red"]]))
    # 踵のプルタブ
    parts.append((box_mesh("Shoe_PullTab", (0, 0.062, 0.232), (0.016, 0.008, 0.03)), [MAT["shoe_black"]]))
    return parts


def place_shoe(parts, side, coll, name):
    """ローカル靴パーツを左右に配置して1オブジェクトへ"""
    obs = []
    for ob, ms in parts:
        set_mats(ob, ms)
        for md in list(ob.modifiers):
            pass
        obs.append(ob)
    # モディファイア（ストラップのSolidify）を適用してから結合
    dg = bpy.context.evaluated_depsgraph_get()
    baked = []
    for ob in obs:
        tmp_coll = coll
        link(ob, coll)
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in obs:
        ev = ob.evaluated_get(dg)
        me = bpy.data.meshes.new_from_object(ev)
        nob = bpy.data.objects.new(ob.name, me)
        baked.append(nob)
    for ob in obs:
        old = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if old.users == 0:
            bpy.data.meshes.remove(old)
    shoe = join_meshes(baked, name)
    rot = Matrix.Rotation(math.radians(SHOE_TOE_OUT * side), 4, "Z")
    pos = Vector((SHOE_POS.x * side, SHOE_POS.y, 0))
    shoe.data.transform(Matrix.Translation(pos) @ rot)
    if side < 0:
        pass
    return shoe
