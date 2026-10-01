# Blender内: Menu01 を最初から再生成（形状→UV→ベイク→リグ→ウェイト）。Masterは読み取りのみ
# globals: MROOT, GROOT, STEPS(["geo","uv","bake","rig"])
import bpy, time, traceback
LOG = []
try:
    g = {"__name__": "__main__", "MROOT": MROOT, "GROOT": GROOT}
    exec(open(GROOT + "/menu_build.py", encoding="utf-8").read(), g)
    exec(open(GROOT + "/menu_rig.py", encoding="utf-8").read(), g)
    b = {"GROOT": GROOT}
    exec(open(GROOT + "/menu_bake.py", encoding="utf-8").read(), b)
    coll = bpy.data.collections.get("ElseIf_Menu01")
    if coll is None:
        coll = bpy.data.collections.new("ElseIf_Menu01")
        bpy.context.scene.collection.children.link(coll)
    coll.hide_viewport = False
    coll.hide_render = False
    t = time.time()
    if "geo" in STEPS:
        old = bpy.data.objects.get("ElseIf_Body_Menu01")
        if old:
            me = old.data
            bpy.data.objects.remove(old, do_unlink=True)
            bpy.data.meshes.remove(me)
        ob = g["build_all"](coll)
        for p in ob.data.polygons:
            p.use_smooth = True
        LOG.append(("geo", round(time.time() - t, 1)))
    ob = bpy.data.objects["ElseIf_Body_Menu01"]
    if "uv" in STEPS:
        # UV展開・ベイクはレストポーズで（リグ解除）
        for md in [m for m in ob.modifiers if m.type == "ARMATURE"]:
            ob.modifiers.remove(md)
        ob.parent = None
        LOG.append(("uv islands", b["unwrap"](ob)))
        m, imgs = b["game_material"](4096)
        ob.data.materials.clear()
        ob.data.materials.append(m)
    if "bake" in STEPS:
        for md in ob.modifiers:
            md.show_render = False
        imgs = {k: bpy.data.images[f"T_ElseIf_Body_Menu01_{k}"] for k in ("BaseColor", "Normal")}
        t = time.time()
        LOG.append(("bake src", b["bake"](ob, imgs), round(time.time() - t, 1)))
        # 2回目: 開口部の裏地（首元のソケット等を拾わないよう小さなケージ）と、袖の中の手（手だけから）
        b["bake_parts"](ob, imgs, {4, 5, 6, 12, 33}, 0.006, 0.015)
        b["bake_parts"](ob, imgs, {40, 41}, 0.004, 0.010, src_prefix="Body_Hand_")
        LOG.append(("bake inner+hands", round(time.time() - t, 1)))
        for md in ob.modifiers:
            md.show_render = True
    if "rig" in STEPS:
        ao, T = g["build_armature"](coll)
        g["bind"](ob, ao)
        g["compute_weights"](ob, T)
        ao.hide_render = True
        LOG.append(("bones", len(ao.data.bones)))
    GX = g
except Exception:
    LOG.append(traceback.format_exc())
print(LOG)
