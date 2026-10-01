# Blender内: Master / Menu01 比較レンダー・近接ショット・変形テスト
# globals: MROOT, GROOT, WHAT(["compare","close","poses"]), PCT
import bpy, os, math, traceback
from mathutils import Vector
try:
    r = {}
    exec(open(MROOT + "/render.py", encoding="utf-8").read(), r)
    g = {"__name__": "__main__", "MROOT": MROOT, "GROOT": GROOT}
    exec(open(GROOT + "/menu_build.py", encoding="utf-8").read(), g)
    exec(open(GROOT + "/menu_rig.py", encoding="utf-8").read(), g)
    r["stage"]()
    ao = bpy.data.objects["ElseIf_Body_Menu01_Armature"]
    ob = bpy.data.objects["ElseIf_Body_Menu01"]
    pct = globals().get("PCT", 70)
    base_views = ["Front", "Side", "Back", "ThreeQuarter", "BackQuarter", "HighAngle"]
    nat_views = ["Front", "ThreeQuarter", "HighAngle"]
    if "compare" in WHAT:
        out = os.path.join(GROOT, "Renders", "Compare")
        os.makedirs(out, exist_ok=True)
        r["show_only"](["ElseIf_Master_BASE"])
        r["render"](base_views, out, "Master_", res_pct=pct, samples=32)
        r["show_only"](["ElseIf_Master_NATURAL"])
        r["render"](nat_views, out, "Master_NaturalPose_", res_pct=pct, samples=32)
        r["show_only"](["ElseIf_Menu01"])
        g["reset_pose"](ao)
        r["render"](base_views, out, "Menu01_", res_pct=pct, samples=32)
        g["pose_natural"](ao)
        r["render"](nat_views, out, "Menu01_NaturalPose_", res_pct=pct, samples=32)
        g["reset_pose"](ao)
    if "close" in WHAT:
        # 一人称で近づいたときの見え方（Master と同じカメラで並べる）
        out = os.path.join(GROOT, "Renders", "Close")
        os.makedirs(out, exist_ok=True)
        shots = {
            # name: (pose, loc, target, lens)
            "Shoulder": ("BASE", (0.75, -1.3, 1.55), (0.17, 0, 1.26), 110),
            "ShoulderBack": ("BASE", (0.75, 1.3, 1.55), (0.17, 0, 1.26), 110),
            "Collar": ("BASE", (0.25, -0.6, 1.9), (0, 0, 1.38), 85),
            "CuffInside": ("NATURAL", (0.55, -0.9, 0.45), (0.27, -0.12, 0.70), 110),
            "Elbow": ("NATURAL", (0.95, -0.55, 1.05), (0.24, -0.04, 1.00), 100),
            "Hem": ("BASE", (0.3, -1.0, 0.55), (0.0, 0, 0.72), 85),
            "ThighBorder": ("BASE", (0.15, -1.2, 0.72), (0.0, 0, 0.67), 85),
            "Shoe": ("BASE", (0.7, -0.9, 0.3), (0.1, -0.05, 0.1), 80),
        }
        for nm, (pose, loc, tgt, lens) in shots.items():
            r["show_only"](["ElseIf_Master_BASE" if pose == "BASE" else "ElseIf_Master_NATURAL"])
            r["shot"](os.path.join(out, f"Master_{nm}.png"), loc, tgt, lens, pct)
            r["show_only"](["ElseIf_Menu01"])
            if pose == "BASE":
                g["reset_pose"](ao)
            else:
                g["pose_natural"](ao)
            r["shot"](os.path.join(out, f"Menu01_{nm}.png"), loc, tgt, lens, pct)
        g["reset_pose"](ao)
        # 手（袖の中に隠れるので手だけを取り出してワイヤーフレーム付きで）
        me = ob.data.copy()
        hand = bpy.data.objects.new("_HandOnly", me)
        bpy.data.collections["ElseIf_Menu01"].objects.link(hand)
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(me)
        lp = bm.verts.layers.int["part"]
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if v[lp] not in (40, 41) or v.co.x < 0], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        wf = bpy.data.objects.new("_HandWF", me)
        bpy.data.collections["ElseIf_Menu01"].objects.link(wf)
        md = wf.modifiers.new("w", "WIREFRAME")
        md.thickness = 0.0004
        md.use_replace = True
        wm = bpy.data.materials.get("_WFmat") or bpy.data.materials.new("_WFmat")
        wm.use_nodes = True
        next(n for n in wm.node_tree.nodes if n.type == "BSDF_PRINCIPLED").inputs["Base Color"].default_value = (0.9, 0.1, 0.05, 1)
        md.material_offset = 1
        me.materials.append(wm)
        ob.hide_render = True
        c = sum((v.co for v in me.vertices), Vector()) / max(len(me.vertices), 1)
        r["shot"](os.path.join(out, "Menu01_HandTopology.png"), tuple(c + Vector((0.18, -0.22, 0.06))), tuple(c), 110, pct)
        r["shot"](os.path.join(out, "Menu01_HandTopology_Palm.png"), tuple(c + Vector((-0.05, -0.12, -0.24))), tuple(c), 110, pct)
        ob.hide_render = False
        for o in (hand, wf):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.meshes.remove(me)
    if "poses" in WHAT:
        pout = os.path.join(GROOT, "Renders", "PoseTest")
        os.makedirs(pout, exist_ok=True)
        r["show_only"](["ElseIf_Menu01"])
        for kind in ("ArmsWide", "ArmForward", "ArmBack", "Elbow90", "Asymmetric", "Seated"):
            g["pose_test"](ao, kind)
            r["render"](["ThreeQuarter", "BackQuarter", "HighAngle"], pout, f"{kind}_", res_pct=pct, samples=24)
        g["reset_pose"](ao)
except Exception:
    print(traceback.format_exc())
