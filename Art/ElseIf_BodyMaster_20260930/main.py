# ElseIf Master 身体・衣装 ビルドエントリ
# exec(open(main.py).read(), {"__name__": "__main__", "ROOT": <dir>, "POSE": "BASE"|"NATURAL", "PARTS": [...]})
import os
for _f in ("lib.py", "body.py", "garments.py", "details.py"):
    _p = os.path.join(ROOT, _f)
    if os.path.exists(_p):
        exec(compile(open(_p, encoding="utf-8").read(), _p, "exec"), globals())

MAT = {}


def mats():
    MAT["skin"] = get_mat("M_Skin", (0.55, 0.36, 0.26), 0.55)
    MAT["jacket"] = get_mat("M_Jacket_White", (0.80, 0.79, 0.77), 0.85, sheen=0.3)
    MAT["jacket_in"] = get_mat("M_Jacket_Lining", (0.62, 0.62, 0.62), 0.9)
    MAT["black"] = get_mat("M_Trim_Black", (0.018, 0.018, 0.02), 0.6)
    MAT["shorts"] = get_mat("M_Shorts_Black", (0.025, 0.025, 0.028), 0.85, sheen=0.2)
    MAT["sock"] = get_mat("M_Sock_DarkGray", (0.055, 0.055, 0.06), 0.9, sheen=0.4)
    MAT["red"] = get_mat("M_Accent_Red", (0.45, 0.03, 0.03), 0.55)
    MAT["metal"] = get_mat("M_Metal", (0.55, 0.55, 0.56), 0.35, metal=1.0)
    MAT["gunmetal"] = get_mat("M_Metal_Dark", (0.08, 0.08, 0.09), 0.4, metal=1.0)
    MAT["shoe_black"] = get_mat("M_Shoe_Black", (0.02, 0.02, 0.022), 0.5)
    MAT["shoe_white"] = get_mat("M_Shoe_White", (0.78, 0.78, 0.77), 0.5)
    MAT["sole_white"] = get_mat("M_Sole_White", (0.72, 0.72, 0.71), 0.7)
    MAT["sole_gray"] = get_mat("M_Sole_Gray", (0.16, 0.16, 0.17), 0.8)
    MAT["cord"] = get_mat("M_Cord_Gray", (0.45, 0.45, 0.46), 0.8)
    MAT["panel_text"] = get_mat("M_BackPanel_Text", (0.85, 0.85, 0.9), 0.4, emit=(0.8, 0.8, 0.9), emit_str=0.6)


def finish(ob, coll, mat_list, subsurf=2, smooth=True):
    set_mats(ob, mat_list)
    if smooth:
        shade_smooth(ob)
    if subsurf:
        add_mod(ob, "SUBSURF", "Subdivision", levels=1, render_levels=subsurf)
    link(ob, coll)
    return ob


def build_body(pose, root):
    coll = get_coll(f"Body_{pose}", root)
    sk = [MAT["skin"]]
    finish(build_torso(), coll, sk)
    legs = build_leg()
    finish(legs, coll, sk)
    link(finish(mirror_x_copy(legs, "Body_Leg_R"), coll, sk), coll)
    ft = build_foot()
    finish(ft, coll, sk)
    finish(mirror_x_copy(ft, "Body_Foot_R"), coll, sk)
    arm = build_arm(pose)
    finish(arm, coll, sk)
    finish(mirror_x_copy(arm, "Body_Arm_R"), coll, sk)
    hand = join_meshes(build_hand(pose), "Body_Hand_L")
    finish(hand, coll, sk)
    finish(mirror_x_copy(hand, "Body_Hand_R"), coll, sk)
    return coll


def cloth(ob, thick=0.0045, even=True):
    # even=True は鋭い先端（袖の付け根ドーム）で頂点が遠くへ飛ぶので、袖では切る
    add_mod(ob, "SOLIDIFY", "Solidify", thickness=thick, offset=-1.0, use_rim=True, use_even_offset=even)
    return ob


JACKET_THICK = 0.0065
JACKET_VOXEL = 0.0028
JACKET_FILLET = 14


def build_jacket(pose, root):
    coll = get_coll(f"Jacket_{pose}", root)
    src = get_coll(f"JacketSrc_{pose}", coll)
    jm = [MAT["jacket"], MAT["jacket_zip"], MAT["print_glyph"], MAT["print_text"]]
    finish(cloth(build_jacket_shell(), JACKET_THICK), src, jm)
    finish(cloth(build_collar(), JACKET_THICK), src, jm)
    sl = build_sleeve(pose)
    finish(cloth(sl, JACKET_THICK, even=False), src, jm)
    sr = mirror_x_copy(sl, "Jacket_Sleeve_R")
    # 右袖は縦書きテキストのプリント
    sr.data.polygons.foreach_set("material_index", [3 if i == 2 else i for i in _mat_idx(sl)])
    finish(cloth(sr, JACKET_THICK, even=False), src, jm)
    ob = unify_jacket(src, coll, jm, pose)
    globals()["JACKET_OB"] = ob
    src.hide_render = True
    src.hide_viewport = True
    return coll


def _mat_idx(ob):
    a = [0] * len(ob.data.polygons)
    ob.data.polygons.foreach_get("material_index", a)
    return a


def bake_eval(obs, name):
    """モディファイア評価後のメッシュを1つに結合（原本は残す）"""
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    tmp = []
    for ob in obs:
        me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
        t = bpy.data.objects.new(ob.name + "_eval", me)
        t.matrix_world = ob.matrix_world
        tmp.append(t)
    return join_meshes(tmp, name)


def unify_jacket(src, coll, jm, pose):
    """身頃・襟・袖をSDFで一体化し、肩・脇の接合部をフィレットで布らしく繋ぐ。
    マテリアルとUVは元パーツから最近傍サンプリングで移す"""
    joined = bake_eval(list(src.objects), f"Jacket_SourceJoined_{pose}")
    link(joined, src)
    ob = bpy.data.objects.new("Jacket", bpy.data.meshes.new("Jacket"))
    link(ob, coll)
    for old_ng in [g for g in bpy.data.node_groups if g.name.startswith(f"GN_JacketUnify_{pose}")]:
        bpy.data.node_groups.remove(old_ng)
    ng = bpy.data.node_groups.new(f"GN_JacketUnify_{pose}", "GeometryNodeTree")
    ng.interface.new_socket("Geometry", in_out="INPUT", socket_type="NodeSocketGeometry")
    ng.interface.new_socket("Geometry", in_out="OUTPUT", socket_type="NodeSocketGeometry")
    N = ng.nodes
    L = ng.links
    gin = N.new("NodeGroupInput")
    gout = N.new("NodeGroupOutput")
    oi = N.new("GeometryNodeObjectInfo")
    oi.inputs["Object"].default_value = joined
    oi.transform_space = "RELATIVE"
    # パーツごとにSDF化してから和集合を取る（重なったメッシュを1つでSDF化すると符号が崩れてトゲが出る）
    bo = N.new("GeometryNodeSDFGridBoolean")
    bo.operation = "UNION"     # UNION時は複数入力ソケット（identifier "Grid 2"）に全部つなぐ
    for part in src.objects:
        if part is joined:
            continue
        poi = N.new("GeometryNodeObjectInfo")
        poi.transform_space = "RELATIVE"
        sdf = N.new("GeometryNodeMeshToSDFGrid")
        sdf.inputs["Voxel Size"].default_value = JACKET_VOXEL
        sdf.inputs["Band Width"].default_value = 3
        if part.name.startswith("Jacket_Sleeve_R"):
            # 右袖は左袖の鏡像からSDF化する（右袖を直接SDF化すると符号判定が漏れて背中側へヒレが出た）
            poi.inputs["Object"].default_value = next(o for o in src.objects if o.name.startswith("Jacket_Sleeve_L"))
            tf = N.new("GeometryNodeTransform")
            tf.inputs["Scale"].default_value = (-1.0, 1.0, 1.0)
            ff = N.new("GeometryNodeFlipFaces")
            L.new(poi.outputs["Geometry"], tf.inputs["Geometry"])
            L.new(tf.outputs["Geometry"], ff.inputs["Mesh"])
            L.new(ff.outputs["Mesh"], sdf.inputs["Mesh"])
        else:
            poi.inputs["Object"].default_value = part
            L.new(poi.outputs["Geometry"], sdf.inputs["Mesh"])
        L.new(sdf.outputs[0], next(s for s in bo.inputs if s.identifier == "Grid 2"))
    grid = bo.outputs[0]
    fil = N.new("GeometryNodeSDFGridFillet")
    fil.inputs["Iterations"].default_value = JACKET_FILLET
    g2m = N.new("GeometryNodeGridToMesh")
    g2m.inputs["Threshold"].default_value = 0.0   # SDFの等値面
    L.new(grid, fil.inputs["Grid"])
    L.new(fil.outputs[0], g2m.inputs["Grid"])
    # マテリアル番号
    smi = N.new("GeometryNodeSampleNearestSurface")
    smi.data_type = "INT"
    mi = N.new("GeometryNodeInputMaterialIndex")
    L.new(oi.outputs["Geometry"], smi.inputs["Mesh"])
    L.new(mi.outputs[0], smi.inputs["Value"])
    setm = N.new("GeometryNodeSetMaterialIndex")
    L.new(g2m.outputs["Mesh"], setm.inputs["Geometry"])
    L.new(smi.outputs["Value"], setm.inputs["Material Index"])
    # UV
    suv = N.new("GeometryNodeSampleNearestSurface")
    suv.data_type = "FLOAT_VECTOR"
    na = N.new("GeometryNodeInputNamedAttribute")
    na.data_type = "FLOAT_VECTOR"
    na.inputs["Name"].default_value = "UVMap"
    L.new(oi.outputs["Geometry"], suv.inputs["Mesh"])
    L.new(na.outputs["Attribute"], suv.inputs["Value"])
    st = N.new("GeometryNodeStoreNamedAttribute")
    st.data_type = "FLOAT2" if "FLOAT2" in [i.identifier for i in st.bl_rna.properties["data_type"].enum_items] else "FLOAT_VECTOR"
    st.domain = "CORNER"
    st.inputs["Name"].default_value = "UVMap"
    L.new(setm.outputs[0], st.inputs["Geometry"])
    L.new(suv.outputs["Value"], st.inputs["Value"])
    sm = N.new("GeometryNodeSetShadeSmooth")
    L.new(st.outputs[0], sm.inputs["Geometry"])
    L.new(sm.outputs[0], gout.inputs[0])
    md = ob.modifiers.new("JacketUnify", "NODES")
    md.node_group = ng
    set_mats(ob, jm)
    # 適用
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    me.name = "Jacket"
    bpy.data.meshes.remove(old)
    set_mats(ob, jm)
    if me.uv_layers and me.uv_layers[0].name != "UVMap":
        pass
    return ob


def build_lower(pose, root):
    coll = get_coll(f"Lower_{pose}", root)
    pel, leg = build_shorts()
    finish(cloth(pel, 0.004), coll, [MAT["shorts"]])
    finish(cloth(leg, 0.004), coll, [MAT["shorts"]])
    lr = mirror_x_copy(leg, "Shorts_Leg_R")
    finish(cloth(lr, 0.004), coll, [MAT["shorts"]])
    sk = build_sock()
    finish(cloth(sk, 0.003), coll, [MAT["sock"], MAT["sock_glyph"]])
    skr = mirror_x_copy(sk, "Sock_R")
    finish(cloth(skr, 0.003), coll, [MAT["sock"], MAT["sock_glyph"]])
    return coll


def build_shoes(pose, root):
    coll = get_coll(f"Shoes_{pose}", root)
    sh = place_shoe(build_shoe(), 1, coll, "Shoe_L")
    link(sh, coll)
    shade_smooth(sh)
    add_mod(sh, "SUBSURF", "Subdivision", levels=1, render_levels=2)
    shr = mirror_x_copy(sh, "Shoe_R")
    link(shr, coll)
    add_mod(shr, "SUBSURF", "Subdivision", levels=1, render_levels=2)
    return coll


def fabric_bump(m, scale=900.0, strength=0.12):
    """布目の細かい凹凸（オブジェクト座標のノイズ→バンプ）。何度呼んでも1つだけ"""
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.label == "fabric":
            nt.nodes.remove(n)
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tc = nt.nodes.new("ShaderNodeTexCoord"); tc.label = "fabric"
    nz = nt.nodes.new("ShaderNodeTexNoise"); nz.label = "fabric"
    nz.inputs["Scale"].default_value = scale
    nz.inputs["Detail"].default_value = 2.0
    bp = nt.nodes.new("ShaderNodeBump"); bp.label = "fabric"
    bp.inputs["Strength"].default_value = strength
    bp.inputs["Distance"].default_value = 0.0005
    nt.links.new(tc.outputs["Object"], nz.inputs["Vector"])
    nt.links.new(nz.outputs["Fac"], bp.inputs["Height"])
    nt.links.new(bp.outputs["Normal"], b.inputs["Normal"])


def run():
    mats()
    print_mats()
    jacket_shader_masks()
    for k in ("jacket", "jacket_zip", "print_glyph", "print_text", "shorts"):
        fabric_bump(MAT[k])
    fabric_bump(MAT["sock"], 1400.0, 0.18)
    fabric_bump(MAT["sock_glyph"], 1400.0, 0.18)
    root = get_coll(f"ElseIf_Master_{POSE}")
    # 非表示のコレクションは評価されない（SDF統合やレイキャストが空になる）ので表示に戻す
    root.hide_viewport = False
    root.hide_render = False
    for c in list(root.children):
        if c.name.split("_")[0] in PARTS or "ALL" in PARTS:
            clear_coll(c)
            bpy.data.collections.remove(c)
    if "Body" in PARTS or "ALL" in PARTS:
        build_body(POSE, root)
    for key, fn in (("Jacket", build_jacket), ("Lower", build_lower), ("Shoes", build_shoes), ("Details", build_details)):
        if key in PARTS or "ALL" in PARTS:
            if key == "Details" and "JACKET_OB" not in globals():
                globals()["JACKET_OB"] = bpy.data.objects["Jacket" if POSE == "BASE" else "Jacket_Natural"]
            fn(POSE, root)
    if POSE != "BASE":
        # 自然立ちの複製はオブジェクト名に _Natural を付ける
        for o in root.all_objects:
            base = o.name.split(".")[0]
            if not base.endswith("_Natural"):
                o.name = base + "_Natural"

    if "BUILD_EXTRA" in globals():
        BUILD_EXTRA(POSE, root)


run()
