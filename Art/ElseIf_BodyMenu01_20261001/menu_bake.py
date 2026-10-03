# ElseIf_Body_Menu01: UV展開とMasterからのベイク（BaseColor / Normal）
# globals: GROOT, RES
import bpy, bmesh, math, os
from mathutils import Vector

GAME = "ElseIf_Body_Menu01"
MASTER_COLLS = ["Body_BASE", "Jacket_BASE", "Lower_BASE", "Shoes_BASE", "Details_BASE"]
# UV上の優先度（見えにくい所は小さく）。部位ID→倍率
# Menu: 近距離で見える裏地・手はBattleより大きく取る
UV_PRIORITY = {4: 0.7, 5: 0.7, 6: 0.8, 12: 0.6, 33: 0.7, 30: 0.85, 40: 0.8, 41: 0.8, 10: 0.55, 60: 0.6}


def _edit(ob):
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    vl.objects.active = ob
    ob.select_set(True)


def unwrap(ob):
    _edit(ob)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58), island_margin=0.003, area_weight=0.0,
                             correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    # 部位ごとの優先度で UV 島を縮める（島中心まわり）
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv.active
    lp = bm.verts.layers.int["part"]
    islands = uv_islands(bm, uvl)
    for isl in islands:
        parts = [f.verts[0][lp] for f in isl]
        k = min(UV_PRIORITY.get(p, 1.0) for p in parts)
        if k >= 1.0:
            continue
        pts = [l[uvl].uv.copy() for f in isl for l in f.loops]
        c = sum(pts, Vector((0, 0))) / len(pts)
        for f in isl:
            for l in f.loops:
                l[uvl].uv = c + (l[uvl].uv - c) * k
    bm.to_mesh(me)
    bm.free()
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, margin=0.004, scale=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    return len(islands)


def uv_islands(bm, uvl):
    bm.faces.ensure_lookup_table()
    key = lambda l: (l.vert.index, round(l[uvl].uv.x, 5), round(l[uvl].uv.y, 5))
    seen = set()
    out = []
    for f0 in bm.faces:
        if f0.index in seen:
            continue
        isl = []
        st = [f0]
        seen.add(f0.index)
        while st:
            f = st.pop()
            isl.append(f)
            for l in f.loops:
                e = l.edge
                for l2 in e.link_loops:
                    g = l2.face
                    if g.index in seen:
                        continue
                    # 同じ辺でUVが一致していれば同じ島
                    a = {key(l), key(l.link_loop_next)}
                    b = {key(l2), key(l2.link_loop_next)}
                    if a == b:
                        seen.add(g.index)
                        st.append(g)
        out.append(isl)
    return out


def game_material(res):
    m = bpy.data.materials.get("M_ElseIf_Body_Menu01") or bpy.data.materials.new("M_ElseIf_Body_Menu01")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type not in ("BSDF_PRINCIPLED", "OUTPUT_MATERIAL"):
            nt.nodes.remove(n)
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Roughness"].default_value = 0.75
    imgs = {}
    for key, cs in (("BaseColor", "sRGB"), ("Normal", "Non-Color")):
        name = f"T_ElseIf_Body_Menu01_{key}"
        img = bpy.data.images.get(name)
        if img is None or img.size[0] != res:
            if img:
                bpy.data.images.remove(img)
            img = bpy.data.images.new(name, res, res, alpha=False, float_buffer=False)
        img.colorspace_settings.name = cs
        imgs[key] = img
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.name = key
        tex.label = key
        tex.image = img
    nt.links.new(nt.nodes["BaseColor"].outputs["Color"], b.inputs["Base Color"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(nt.nodes["Normal"].outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m, imgs


# 衣服の下に隠れる人体はベイク元にしない（低ポリ面がMaster外表面より内側に入った所で肌色を拾う）
BODY_KEEP = ("Body_Leg_", "Body_Hand_")
# 平面デカールは色だけ。ノーマルに入れると板の縁が段差として写る
FLAT_DECALS = ("Patch_", "Logo_", "BackPanel_Screen", "BackPanel_Text")


def master_objects(for_normal=False):
    obs = []
    for cn in MASTER_COLLS:
        c = bpy.data.collections.get(cn)
        if c is None:
            continue
        for o in c.objects:
            if o.type != "MESH" or o.hide_render:
                continue
            if o.name.startswith("Body_") and not o.name.startswith(BODY_KEEP):
                continue
            if for_normal and o.name.startswith(FLAT_DECALS):
                continue
            obs.append(o)
    return obs


def _fabric_bump(strength=None):
    """Masterの布目バンプ（2048に対して細かすぎる）をベイク中だけ切る。戻り値で元に戻す"""
    saved = []
    for m in bpy.data.materials:
        if not m.use_nodes:
            continue
        for n in m.node_tree.nodes:
            if n.type == "BUMP" and n.label == "fabric":
                saved.append((n, n.inputs["Strength"].default_value))
                n.inputs["Strength"].default_value = 0.0
    return saved


def _metal_off():
    """Cycles の拡散色ベイクは Metallic=1 だと黒になる。ベイク中だけ金属度を0にする"""
    saved = []
    for m in bpy.data.materials:
        if not m.use_nodes or m.name.startswith("M_ElseIf_Body_Menu01"):
            continue
        for n in m.node_tree.nodes:
            if n.type == "BSDF_PRINCIPLED" and n.inputs["Metallic"].default_value > 0:
                saved.append((n, n.inputs["Metallic"].default_value))
                n.inputs["Metallic"].default_value = 0.0
    return saved


def _srgb(c):
    return 12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def _alpha_fix():
    """透過デカールの透明部分は RGB=0 なので、ベイク中だけ下地色にしておく（画像はsRGBなので変換）"""
    saved = []
    jw = tuple(_srgb(c) for c in (0.80, 0.79, 0.77))
    for name, bg in (("T_Print_HemLogo", jw),):
        img = bpy.data.images.get(name)
        if img is None:
            continue
        px = list(img.pixels)
        saved.append((img, px))
        new = px[:]
        for i in range(0, len(new), 4):
            a = new[i + 3]
            new[i] = new[i] * a + bg[0] * (1 - a)
            new[i + 1] = new[i + 1] * a + bg[1] * (1 - a)
            new[i + 2] = new[i + 2] * a + bg[2] * (1 - a)
            new[i + 3] = 1.0
        img.pixels.foreach_set(new)
    return saved


def bake(ob, imgs, cage=0.022, dist=0.045, samples=4):
    sc = bpy.context.scene
    old_engine = sc.render.engine
    sc.render.engine = "CYCLES"
    sc.cycles.samples = samples
    sc.cycles.device = "CPU"
    bk = sc.render.bake
    bk.use_selected_to_active = True
    bk.cage_extrusion = cage
    bk.max_ray_distance = dist
    bk.margin = 6
    for c in MASTER_COLLS:
        bpy.data.collections[c].hide_render = False
        bpy.data.collections[c].hide_viewport = False
    root = bpy.data.collections["ElseIf_Master_BASE"]
    root.hide_viewport = False
    root.hide_render = False
    vl = bpy.context.view_layer
    nt = ob.active_material.node_tree

    def select_src(for_normal):
        src = master_objects(for_normal)
        for o in vl.objects:
            o.select_set(False)
        for o in src:
            o.select_set(True)
        ob.select_set(True)
        vl.objects.active = ob
        return src
    # ベイク先の画像をシェーダーから外しておく（循環参照の回避）
    for l in list(nt.links):
        if l.from_node.type == "TEX_IMAGE":
            nt.links.remove(l)
    saved_b = _fabric_bump()
    saved_a = _alpha_fix()
    saved_m = _metal_off()
    try:
        for key in ("BaseColor", "Normal"):
            src = select_src(key == "Normal")
            for n in nt.nodes:
                n.select = False
            node = nt.nodes[key]
            node.select = True
            nt.nodes.active = node
            if key == "BaseColor":
                bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True,
                                    cage_extrusion=cage, max_ray_distance=dist, margin=6)
            else:
                bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", use_selected_to_active=True,
                                    cage_extrusion=cage, max_ray_distance=dist, margin=6)
            img = imgs[key]
            img.filepath_raw = os.path.join(GROOT, "Textures", img.name + ".png")
            img.file_format = "PNG"
            img.save()
    finally:
        for n, v in saved_b:
            n.inputs["Strength"].default_value = v
        for img, px in saved_a:
            img.pixels.foreach_set(px)
        for n, v in saved_m:
            n.inputs["Metallic"].default_value = v
        sc.render.engine = old_engine
        b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        nm = next(n for n in nt.nodes if n.type == "NORMAL_MAP")
        nt.links.new(nt.nodes["BaseColor"].outputs["Color"], b.inputs["Base Color"])
        nt.links.new(nt.nodes["Normal"].outputs["Color"], nm.inputs["Color"])
    return len(src)


def bake_parts(ob, imgs, parts, cage, dist, src_prefix=None):
    """指定部位の面だけを取り出した一時オブジェクトで、同じ画像へ上書きベイクする。
    袖の中の手・開口部の裏地は、通常のケージだとレイが袖や首元の別パーツを拾うため"""
    import bmesh
    me = ob.data.copy()
    tmp = bpy.data.objects.new("_BakeParts", me)
    bpy.context.scene.collection.objects.link(tmp)
    bm = bmesh.new()
    bm.from_mesh(me)
    lp = bm.verts.layers.int["part"]
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.verts[0][lp] not in parts], context="FACES")
    bm.to_mesh(me)
    bm.free()
    sc = bpy.context.scene
    old_engine = sc.render.engine
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 4
    bk = sc.render.bake
    old_clear = bk.use_clear
    bk.use_clear = False
    vl = bpy.context.view_layer
    nt = tmp.active_material.node_tree
    for l in list(nt.links):
        if l.from_node.type == "TEX_IMAGE":
            nt.links.remove(l)
    saved_b = _fabric_bump()
    saved_a = _alpha_fix()
    saved_m = _metal_off()
    try:
        for key in ("BaseColor", "Normal"):
            src = master_objects(key == "Normal")
            if src_prefix:
                src = [o for o in src if o.name.startswith(src_prefix)]
            for o in vl.objects:
                o.select_set(False)
            for o in src:
                o.select_set(True)
            tmp.select_set(True)
            vl.objects.active = tmp
            for n in nt.nodes:
                n.select = False
            node = nt.nodes[key]
            node.select = True
            nt.nodes.active = node
            if key == "BaseColor":
                bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True,
                                    cage_extrusion=cage, max_ray_distance=dist, margin=4, use_clear=False)
            else:
                bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", use_selected_to_active=True,
                                    cage_extrusion=cage, max_ray_distance=dist, margin=4, use_clear=False)
            imgs[key].save()
    finally:
        for n, v in saved_b:
            n.inputs["Strength"].default_value = v
        for img, px in saved_a:
            img.pixels.foreach_set(px)
        for n, v in saved_m:
            n.inputs["Metallic"].default_value = v
        bk.use_clear = old_clear
        sc.render.engine = old_engine
        b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        nm = next(n for n in nt.nodes if n.type == "NORMAL_MAP")
        nt.links.new(nt.nodes["BaseColor"].outputs["Color"], b.inputs["Base Color"])
        nt.links.new(nt.nodes["Normal"].outputs["Color"], nm.inputs["Color"])
        bpy.data.objects.remove(tmp, do_unlink=True)
        bpy.data.meshes.remove(me)
