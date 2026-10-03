# Blender内: GameOpt01 を最初から再生成（形状→UV→ベイク→リグ→ウェイト）。Masterは読み取りのみ
# globals: MROOT, GROOT, STEPS(["geo","uv","bake","rig"])
import bpy, time, traceback
LOG = []
try:
    g = {"__name__": "__main__", "MROOT": MROOT, "GROOT": GROOT}
    exec(open(GROOT + "/game_build.py", encoding="utf-8").read(), g)
    exec(open(GROOT + "/game_rig.py", encoding="utf-8").read(), g)
    b = {"GROOT": GROOT}
    exec(open(GROOT + "/game_bake.py", encoding="utf-8").read(), b)
    coll = bpy.data.collections.get("ElseIf_GameOpt01")
    if coll is None:
        coll = bpy.data.collections.new("ElseIf_GameOpt01")
        bpy.context.scene.collection.children.link(coll)
    coll.hide_viewport = False
    coll.hide_render = False
    t = time.time()
    if "geo" in STEPS:
        old = bpy.data.objects.get("ElseIf_Body_GameOpt01")
        if old:
            me = old.data
            bpy.data.objects.remove(old, do_unlink=True)
            bpy.data.meshes.remove(me)
        ob = g["build_all"](coll)
        for p in ob.data.polygons:
            p.use_smooth = True
        LOG.append(("geo", round(time.time() - t, 1)))
    ob = bpy.data.objects["ElseIf_Body_GameOpt01"]
    if "uv" in STEPS:
        # UV展開・ベイクはレストポーズで（リグ解除）
        for md in [m for m in ob.modifiers if m.type == "ARMATURE"]:
            ob.modifiers.remove(md)
        ob.parent = None
        LOG.append(("uv islands", b["unwrap"](ob)))
        m, imgs = b["game_material"](2048)
        ob.data.materials.clear()
        ob.data.materials.append(m)
    if "bake" in STEPS:
        for md in ob.modifiers:
            md.show_render = False
        imgs = {k: bpy.data.images[f"T_ElseIf_Body_GameOpt01_{k}"] for k in ("BaseColor", "Normal")}
        t = time.time()
        LOG.append(("bake src", b["bake"](ob, imgs), round(time.time() - t, 1)))
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
