# ElseIf 衣装ディテール（ワッペン・ロゴ・ポケット・コード・タグ・背面パネル/ハーネス・太腿ベルト・HeadSocket）


# ---------------- デカール用テクスチャ ----------------
def _logo_AO(a, x0, y0, w, h, col, th):
    """ΔO ロゴ（三角と丸の線画）"""
    H, W = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    u = xx / w
    v = yy / h
    # 左の三角
    cx, base, top, hw = 0.26, 0.18, 0.82, 0.22
    t = th / w
    inside = (v > base) & (v < top) & (abs(u - cx) < hw * (top - v) / (top - base))
    inner = (v > base + t * 1.4) & (v < top - t * 2.5) & (abs(u - cx) < (hw * (top - v) / (top - base) - t * 1.8))
    m = inside & ~inner
    m |= (abs(v - 0.40) < t * 0.6) & (abs(u - cx) < hw * (top - 0.40) / (top - base) - t)
    # 右の丸
    r = np.sqrt(((u - 0.72) * w / h) ** 2 + (v - 0.5) ** 2)
    m |= (r < 0.32) & (r > 0.32 - t * w / h * 1.1)
    a[y0:y0 + h, x0:x0 + w][m] = (*col, 1.0)


def make_chest_patch_image(name):
    a = _canvas(256, 400, (0.86, 0.85, 0.83))
    # 下: 黒タグ（白い斜線と点）
    a[0:150, :, :3] = 0.03
    yy, xx = np.mgrid[0:150, 0:256]
    m = abs((xx - 128) - (yy - 75) * 1.3) < 6
    m &= (abs(xx - 128) < 70) & (abs(yy - 75) < 50)
    a[0:150][m] = (0.9, 0.9, 0.9, 1)
    a[20:30, 30:60, :3] = 0.9
    a[20:30, 70:90, :3] = 0.9
    # 上: 白地に ΔO ＋ 枠
    _logo_AO(a, 28, 190, 200, 160, (0.03, 0.03, 0.03), 14)
    a[150:158, :, :3] = 0.03
    a[392:400, :, :3] = 0.03
    a[150:400, 0:8, :3] = 0.03
    a[150:400, 248:256, :3] = 0.03
    return _to_image(name, a)


def make_hem_logo_image(name):
    a = np.zeros((200, 320, 4), np.float32)
    _logo_AO(a, 10, 10, 300, 180, (0.03, 0.03, 0.03), 16)
    # 下に細い文字列風ライン
    return _to_image(name, a)


def make_sleeve_patch_image(name):
    """DLHN パッチ（黒地・白ブロック文字）"""
    a = _canvas(256, 200, (0.03, 0.03, 0.03))
    # 簡易ブロック体 D L H N
    def bar(x, y, w, h):
        a[y:y + h, x:x + w, :3] = 0.9
    ox, oy, s = 22, 110, 8
    # D
    bar(ox, oy, s, 60); bar(ox, oy, 34, s); bar(ox, oy + 52, 34, s); bar(ox + 34, oy + 8, s, 44)
    # L
    ox += 56; bar(ox, oy, s, 60); bar(ox, oy, 36, s)
    # H
    ox += 52; bar(ox, oy, s, 60); bar(ox + 32, oy, s, 60); bar(ox, oy + 26, 40, s)
    # N
    ox += 56
    bar(ox, oy, s, 60); bar(ox + 32, oy, s, 60)
    for k in range(52):
        a[oy + 58 - k:oy + 60 - k, ox + 6 + int(k * 0.55):ox + 12 + int(k * 0.55), :3] = 0.9
    # 下段の細字
    for i in range(6):
        bar(22 + i * 36, 60, 26, 6)
    for i in range(4):
        bar(22 + i * 50, 36, 38, 6)
    return _to_image(name, a)


def make_panel_image(name):
    """背面パネル: 黒フレーム内のスクリーン（イラスト風の濃淡）"""
    W, H = 256, 384
    a = _canvas(W, H, (0.02, 0.02, 0.022))
    yy, xx = np.mgrid[0:H, 0:W]
    scr = (xx > 18) & (xx < W - 18) & (yy > 90) & (yy < H - 18)
    g = 0.30 + 0.12 * np.sin(xx / 17.0) * np.cos(yy / 23.0) + 0.25 * np.exp(-(((xx - 128) / 55.0) ** 2 + ((yy - 250) / 80.0) ** 2))
    a[scr, 0] = g[scr] * 0.9
    a[scr, 1] = g[scr] * 0.9
    a[scr, 2] = g[scr] * 1.0
    # 下部の操作プレート
    pl = (xx > 18) & (xx < W - 18) & (yy > 18) & (yy < 80)
    a[pl, :3] = 0.55
    for i in range(5):
        x0 = 30 + i * 42
        a[34:62, x0:x0 + 26, :3] = 0.12 if i % 2 else 0.25
    return _to_image(name, a)


def decal_mat(name, img, alpha=False, rough=0.7, metal=0.0):
    m = image_mat(name, img, rough, alpha=alpha)
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Metallic"].default_value = metal
    return m


def jacket_shader_masks():
    """ユニオン後メッシュの面単位の境界ギザを避けるため、ファスナー帯とプリント帯は
    シェーダー側のマスク（オブジェクト座標／UV範囲）で輪郭を決める"""
    white = MAT["jacket"].node_tree.nodes
    wcol = next(n for n in white if n.type == "BSDF_PRINCIPLED").inputs["Base Color"].default_value[:]
    # ファスナー
    m = bpy.data.materials.get("M_Jacket_Zip") or bpy.data.materials.new("M_Jacket_Zip")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type not in ("BSDF_PRINCIPLED", "OUTPUT_MATERIAL"):
            nt.nodes.remove(n)
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ab = nt.nodes.new("ShaderNodeMath"); ab.operation = "ABSOLUTE"
    lt = nt.nodes.new("ShaderNodeMath"); lt.operation = "LESS_THAN"; lt.inputs[1].default_value = ZIP_HALF
    mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"
    mix.inputs["A"].default_value = wcol
    mix.inputs["B"].default_value = (0.018, 0.018, 0.02, 1)
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    nt.links.new(sep.outputs["X"], ab.inputs[0])
    nt.links.new(ab.outputs[0], lt.inputs[0])
    nt.links.new(lt.outputs[0], mix.inputs["Factor"])
    nt.links.new(mix.outputs["Result"], b.inputs["Base Color"])
    rr = nt.nodes.new("ShaderNodeMix"); rr.data_type = "FLOAT"
    rr.inputs["A"].default_value = 0.85
    rr.inputs["B"].default_value = 0.45
    nt.links.new(lt.outputs[0], rr.inputs["Factor"])
    nt.links.new(rr.outputs["Result"], b.inputs["Roughness"])
    MAT["jacket_zip"] = m
    # プリント帯（UVが0-1の外は白地）
    for key in ("print_glyph", "print_text"):
        pm = MAT[key]
        nt = pm.node_tree
        b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
        for n in list(nt.nodes):
            if n.label == "mask":
                nt.nodes.remove(n)
        uv = nt.nodes.new("ShaderNodeUVMap"); uv.label = "mask"
        sep = nt.nodes.new("ShaderNodeSeparateXYZ"); sep.label = "mask"
        nt.links.new(uv.outputs[0], sep.inputs[0])
        nt.links.new(uv.outputs[0], tex.inputs["Vector"])

        def rng(src, lo, hi):
            g = nt.nodes.new("ShaderNodeMath"); g.operation = "GREATER_THAN"; g.inputs[1].default_value = lo; g.label = "mask"
            l = nt.nodes.new("ShaderNodeMath"); l.operation = "LESS_THAN"; l.inputs[1].default_value = hi; l.label = "mask"
            mm = nt.nodes.new("ShaderNodeMath"); mm.operation = "MULTIPLY"; mm.label = "mask"
            nt.links.new(src, g.inputs[0]); nt.links.new(src, l.inputs[0])
            nt.links.new(g.outputs[0], mm.inputs[0]); nt.links.new(l.outputs[0], mm.inputs[1])
            return mm.outputs[0]
        mu = rng(sep.outputs["X"], 0.0, 1.0)
        mv = rng(sep.outputs["Y"], BAND_S0 / (BAND_W * 16), BAND_S1 / (BAND_W * 16))
        mm = nt.nodes.new("ShaderNodeMath"); mm.operation = "MULTIPLY"; mm.label = "mask"
        nt.links.new(mu, mm.inputs[0]); nt.links.new(mv, mm.inputs[1])
        mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.label = "mask"
        mix.inputs["A"].default_value = wcol
        nt.links.new(tex.outputs["Color"], mix.inputs["B"])
        nt.links.new(mm.outputs[0], mix.inputs["Factor"])
        nt.links.new(mix.outputs["Result"], b.inputs["Base Color"])


# ---------------- 配置ユーティリティ ----------------
def tube_along(name, pts, r, N=8):
    acc = [0.0]
    for i in range(1, len(pts)):
        acc.append(acc[-1] + (pts[i] - pts[i - 1]).length)
    tot = acc[-1]

    def cen(s):
        i = 0
        while i < len(acc) - 2 and acc[i + 1] < s:
            i += 1
        t = (s - acc[i]) / max(acc[i + 1] - acc[i], 1e-6)
        return pts[i].lerp(pts[i + 1], clamp01(t))
    return build_loft(name, dense_samples(0, tot, 0.004), N, cen, lambda s: [r, r, r, r, 2.0],
                      Vector((0, -1, 0.001)), cap_start=True, cap_end=True)


def surf_path(tree, pts2d, ray, lift):
    """(x,z) or 3D始点列を ray 方向に投げて表面上の点列にする"""
    out = []
    ray = Vector(ray).normalized()
    for p in pts2d:
        o = Vector(p) - ray * 0.5
        loc, nor, _, _ = tree.ray_cast(o, ray)
        if loc is not None:
            if nor.dot(ray) > 0:
                nor = -nor
            out.append(loc + nor * lift)
    return out


def hanging_strap(name, top, down, length, width=0.016, thick=0.0025, twist=0.0):
    """上端topから垂れる平たいストラップ（重力方向＋少し外へ）"""
    down = Vector(down).normalized()
    side = down.cross(Vector((0, 0, 1)))
    if side.length < 1e-4:
        side = Vector((1, 0, 0))
    side = side.normalized()
    pts = [Vector(top) + down * (length * k / 6) for k in range(7)]
    bm = bmesh.new()
    rows = []
    for i, p in enumerate(pts):
        sd = side
        n = sd.cross(down).normalized()
        rows.append([bm.verts.new(p - sd * width / 2), bm.verts.new(p + sd * width / 2),
                     bm.verts.new(p + sd * width / 2 + n * thick), bm.verts.new(p - sd * width / 2 + n * thick)])
    for i in range(len(rows) - 1):
        for j in range(4):
            bm.faces.new((rows[i][j], rows[i][(j + 1) % 4], rows[i + 1][(j + 1) % 4], rows[i + 1][j]))
    bm.faces.new(list(reversed(rows[0])))
    bm.faces.new(rows[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return bpy.data.objects.new(name, me), pts[-1], side


def text_mesh(name, body, size, loc, rot_euler, extrude=0.0006):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = body
    cu.size = size
    cu.extrude = extrude
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    tob = bpy.data.objects.new(name + "_txt", cu)
    bpy.context.scene.collection.objects.link(tob)
    bpy.context.view_layer.update()
    me = bpy.data.meshes.new_from_object(tob.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    bpy.data.objects.remove(tob, do_unlink=True)
    bpy.data.curves.remove(cu)
    ob = bpy.data.objects.new(name, me)
    ob.location = loc
    ob.rotation_euler = rot_euler
    return ob


def bake_obj_transform(ob):
    ob.data.transform(ob.matrix_basis)
    ob.matrix_basis = Matrix.Identity(4)


# ---------------- ディテール本体 ----------------
def build_details(pose, root):
    coll = get_coll(f"Details_{pose}", root)
    jk = JACKET_OB
    tree, _bm = bvh_of(jk)
    FR = (0, 1, 0)      # 正面から奥へ（前面に当てる）
    BK = (0, -1, 0)     # 背面から手前へ
    MAT["chest_patch"] = decal_mat("M_Patch_Chest", make_chest_patch_image("T_Patch_Chest"))
    MAT["hem_logo"] = decal_mat("M_Print_HemLogo", make_hem_logo_image("T_Print_HemLogo"), alpha=True)
    MAT["sleeve_patch"] = decal_mat("M_Patch_DLHN", make_sleeve_patch_image("T_Patch_DLHN"))
    MAT["panel"] = decal_mat("M_BackPanel_Screen", make_panel_image("T_BackPanel_Screen"), rough=0.35)

    def add(ob, mats, sub=0, smooth=True):
        set_mats(ob, mats)
        if smooth:
            shade_smooth(ob)
        if sub:
            add_mod(ob, "SUBSURF", levels=1, render_levels=sub)
        link(ob, coll)
        return ob

    # 胸ワッペン（キャラ左胸）＋赤タグ
    add(decal_on_surface("Patch_Chest", tree, (0.062, -0.3, 1.215), FR, (1, 0, 0), (0, 0, 1), 0.050, 0.078, 6, 8, 0.0018),
        [MAT["chest_patch"]], smooth=False)
    ob, tip, _ = hanging_strap("Tag_Chest_Red", surf_path(tree, [(0.062, -0.3, 1.178)], FR, 0.003)[0], (0, -0.12, -1), 0.034, 0.011, 0.002)
    add(ob, [MAT["red"]])
    # 襟の三角ロゴ（キャラ左）
    add(decal_on_surface("Logo_Collar", tree, (0.036, -0.3, 1.415), FR, (1, 0, 0), (0, 0, 1), 0.024, 0.016, 4, 3, 0.0015),
        [MAT["hem_logo"]], smooth=False)
    # 裾の ΔO プリント（キャラ左前）
    add(decal_on_surface("Logo_Hem", tree, (0.115, -0.3, 0.805), FR, (1, 0, 0), (0, 0, 1), 0.075, 0.045, 8, 5, 0.0015),
        [MAT["hem_logo"]], smooth=False)

    # 両脇の斜めポケット（玉縁）
    for sx in (1, -1):
        a = Vector((0.098 * sx, 0, 1.015))
        b = Vector((0.152 * sx, 0, 0.905))
        pts = [a.lerp(b, k / 12) + Vector((0, -0.5, 0)) for k in range(13)]
        add(ribbon_on_surface(f"Pocket_Welt_{'L' if sx > 0 else 'R'}", tree, pts, FR, 0.016, 0.0012, 0.0022),
            [MAT["jacket"]])
        add(ribbon_on_surface(f"Pocket_Slit_{'L' if sx > 0 else 'R'}", tree, pts, FR, 0.004, 0.0038),
            [MAT["black"]], smooth=False)

    # 襟元のドローコード（キャラ右）＋コードストッパー
    top = surf_path(tree, [(-0.030, -0.3, 1.395)], FR, 0.004)[0]
    pts = [top]
    for k in range(1, 14):
        z = 1.395 - k * 0.0105
        q = surf_path(tree, [(-0.030 - 0.002 * k, -0.3, z)], FR, 0.005)
        if q:
            pts.append(q[0])
    add(tube_along("Collar_Cord", pts, 0.0028), [MAT["cord"]])
    end = pts[-1]
    add(box_mesh("Collar_CordStopper", end + Vector((0, -0.004, -0.004)), (0.010, 0.008, 0.016)), [MAT["black"]], sub=2)
    add(box_mesh("Collar_CordTip", end + Vector((0, -0.004, -0.018)), (0.006, 0.006, 0.012)), [MAT["red"]], sub=2)

    # 裾左右の垂れストラップ（赤い先端）
    for sx in (1, -1):
        th = math.pi / 2 - sx * 1.25
        x = 0.205 * math.cos(math.pi / 2 - th) * sx
        z0 = hem_z(th) + 0.02
        pt = surf_path(tree, [(0.19 * sx, -0.5, z0)], FR, 0.003)
        if not pt:
            continue
        ob, tip, _ = hanging_strap(f"Hem_Strap_{'L' if sx > 0 else 'R'}", pt[0], (0.05 * sx, -0.06, -1), 0.095, 0.014, 0.0025)
        add(ob, [MAT["black"]])
        ob2, _, _ = hanging_strap(f"Hem_StrapTip_{'L' if sx > 0 else 'R'}", tip, (0.05 * sx, -0.06, -1), 0.022, 0.0145, 0.003)
        add(ob2, [MAT["red"]])

    # 袖口の垂れタグ（左右）
    arm_c = arm_center(pose)
    for sx in (1, -1):
        c = arm_c(SLEEVE_END - 0.03)
        c = Vector((c.x * sx, c.y, c.z))
        o = c + Vector((0, -0.2, 0.0))
        hit = tree.ray_cast(o, Vector((0, 1, 0)))
        if hit[0] is None:
            continue
        ob, tip, _ = hanging_strap(f"Cuff_Tag_{'L' if sx > 0 else 'R'}", hit[0] + Vector((0, -0.003, 0)), (0, -0.02, -1), 0.05, 0.012, 0.002)
        add(ob, [MAT["black"]])
        ob2, _, _ = hanging_strap(f"Cuff_TagTip_{'L' if sx > 0 else 'R'}", tip, (0, -0.02, -1), 0.018, 0.0125, 0.0026)
        add(ob2, [MAT["red"]])

    # 右袖（背面寄り）の DLHN パッチ
    c = arm_c(0.16)
    c = Vector((-c.x, c.y, c.z))
    d1, _ = arm_dirs(pose)
    up = -Vector((-d1.x, d1.y, d1.z))
    right = Vector((-up.z, 0, up.x)).normalized()
    add(decal_on_surface("Patch_Sleeve_DLHN", tree, c, BK, right, up, 0.052, 0.042, 6, 5, 0.0018),
        [MAT["sleeve_patch"]], smooth=False)

    # 背面パネル＋ハーネス
    build_back_harness(tree, coll, add)
    # 太腿ベルト（キャラ右脚）
    build_thigh_belt(coll, add)
    # HeadSocket
    build_head_socket(coll, add)
    _bm.free()
    return coll


def build_back_harness(tree, coll, add):
    # パネル（背中中央 1.00〜1.23）
    px0, px1, pz0, pz1 = -0.072, 0.072, 0.985, 1.225
    add(decal_on_surface("BackPanel_Screen", tree, (0, 0.5, (pz0 + pz1) / 2), (0, -1, 0), (-1, 0, 0), (0, 0, 1),
                         px1 - px0, pz1 - pz0, 8, 12, 0.009), [MAT["panel"]], smooth=False)
    # パネルの枠（厚み）: 同形状の少し大きい黒い板
    fr = decal_on_surface("BackPanel_Frame", tree, (0, 0.5, (pz0 + pz1) / 2), (0, -1, 0), (-1, 0, 0), (0, 0, 1),
                          px1 - px0 + 0.016, pz1 - pz0 + 0.016, 8, 12, 0.003)
    add_mod(fr, "SOLIDIFY", thickness=0.0065, offset=1.0)
    add(fr, [MAT["black"]], smooth=False)
    # 肩から降りる2本のストラップ
    for sx in (1, -1):
        pts = []
        for k in range(15):
            t = k / 14
            x = (0.088 + (0.060 - 0.088) * t) * sx
            z = 1.372 + (pz1 + 0.004 - 1.372) * t
            pts.append((x, 0.5, z))
        add(ribbon_on_surface(f"Harness_Shoulder_{'L' if sx > 0 else 'R'}", tree, pts, (0, -1, 0), 0.024, 0.0015, 0.003),
            [MAT["black"]])
        # パネル下の垂れストラップ＋Dリング
        hit = surf_path(tree, [(0.060 * sx, 0.5, pz0 - 0.004)], (0, -1, 0), 0.004)
        if hit:
            ob, tip, _ = hanging_strap(f"Harness_Hang_{'L' if sx > 0 else 'R'}", hit[0], (0, 0.03, -1), 0.07, 0.016, 0.003)
            add(ob, [MAT["black"]])
            add(box_mesh(f"Harness_DRing_{'L' if sx > 0 else 'R'}", tip + Vector((0, 0.002, -0.006)), (0.020, 0.004, 0.012)),
                [MAT["metal"]], sub=2)
    # 中央の縦ストラップ（パネル上〜裾）
    pts = [(0, 0.5, 1.37 - k * 0.01) for k in range(0, 15)]
    add(ribbon_on_surface("Harness_Center_Top", tree, pts, (0, -1, 0), 0.02, 0.0015, 0.003), [MAT["black"]])
    pts = [(0, 0.5, pz0 - 0.002 - k * 0.012) for k in range(0, 24)]
    add(ribbon_on_surface("Harness_Center_Low", tree, pts, (0, -1, 0), 0.022, 0.0015, 0.003), [MAT["black"]])
    for z in (0.90, 0.80):
        hit = surf_path(tree, [(0, 0.5, z)], (0, -1, 0), 0.005)
        if hit:
            add(box_mesh(f"Harness_Buckle_{int(z * 100)}", hit[0], (0.028, 0.004, 0.014)), [MAT["metal"]], sub=2)
    # パネル文字
    hit = surf_path(tree, [(0, 0.5, pz0 + 0.075)], (0, -1, 0), 0.0105)
    if hit:
        tx = text_mesh("BackPanel_Text", "LEVEL UP.", 0.024, hit[0], (math.radians(90), 0, math.radians(180)))
        bake_obj_transform(tx)
        add(tx, [MAT["panel_text"]], smooth=False)


def build_thigh_belt(coll, add):
    # ベルト全体をソックス上端(SOCK_TOP≈0.656)より下に収め、絶対領域の肌に重ねない（v04）
    z0, z1 = 0.628, 0.650

    def prof(z):
        return leg_prof(z, 0.0035 + 0.0035)
    zs = dense_samples(z0, z1, 0.0055)
    belt = build_loft("ThighBelt_R", zs, 40, leg_center, prof, Vector((0, -1, 0)))
    add_mod(belt, "SOLIDIFY", thickness=0.0028, offset=1.0)
    belt.data.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
    belt.data.flip_normals()
    add(belt, [MAT["black"]])
    # バックル（前外側）
    c = leg_center((z0 + z1) / 2)
    v = leg_prof((z0 + z1) / 2, 0.01)
    th = math.pi / 2 + 0.55     # 前やや外
    x, y = ring_offset(th, v[0], v[1], v[2], v[3], v[4])
    p = Vector((-(c.x + x), c.y - y, (z0 + z1) / 2))
    add(box_mesh("ThighBelt_Buckle", p, (0.022, 0.006, 0.026)), [MAT["metal"]], sub=2)
    ob, tip, _ = hanging_strap("ThighBelt_Hang", p + Vector((0, -0.003, -0.012)), (0, -0.05, -1), 0.075, 0.013, 0.0025)
    add(ob, [MAT["black"]])
    ob2, _, _ = hanging_strap("ThighBelt_Tag", tip, (0, -0.05, -1), 0.03, 0.0135, 0.003)
    add(ob2, [MAT["red"]])


def build_head_socket(coll, add):
    """頭部交換用ソケット。Empty 'HeadSocket' の位置・向きに Head Part の首下端を合わせる"""
    cy = 0.012
    z = SOCKET_Z
    # 取付プレート（首の上端に載る金属リング）
    def cyl(name, r0, r1, za, zb, N=40):
        rings = []
        for zz, rr in ((za, r0), (zb, r1)):
            rings.append([Vector((rr * math.cos(TAU * k / N), cy + rr * math.sin(TAU * k / N), zz)) for k in range(N)])
        return mesh_from_rings(name, rings, cap_start=True, cap_end=True)
    add(cyl("HeadSocket_Plate", 0.047, 0.045, z - 0.010, z + 0.002), [MAT["gunmetal"]], sub=0, smooth=False)
    add(cyl("HeadSocket_Ring", 0.036, 0.036, z + 0.002, z + 0.008), [MAT["metal"]], sub=0, smooth=False)
    add(cyl("HeadSocket_Pin", 0.012, 0.012, z + 0.002, z + 0.011), [MAT["black"]], sub=0, smooth=False)
    # 襟元シール: ソケット外周から襟の内側まで塞ぐ黒いリング（上から首の肌が見えないように）
    rings = []
    for rr, zz in ((0.045, z + 0.001), (0.056, z - 0.006), (0.066, z - 0.016), (0.071, z - 0.024)):
        rings.append([Vector((rr * math.cos(TAU * k / 48), cy + rr * math.sin(TAU * k / 48), zz)) for k in range(48)])
    seal = mesh_from_rings("HeadSocket_NeckSeal", rings)
    add(seal, [MAT["shorts"]], sub=2)
    e = bpy.data.objects.get("HeadSocket")
    if e is None or coll not in e.users_collection:
        e = bpy.data.objects.new("HeadSocket", None)
        coll.objects.link(e)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = 0.08
    e.location = (0, cy, z + 0.008)
    e["note"] = "Head Part の首下端をここに合わせる（Z上/-Y前）。頭部は別パーツ"
