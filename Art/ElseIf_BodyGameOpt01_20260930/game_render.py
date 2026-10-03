# Blender内: Master / GameOpt01 比較レンダーと変形テスト。globals: MROOT, GROOT, WHAT(["compare","poses"]), PCT
import bpy, os, traceback
try:
    r = {}
    exec(open(MROOT + "/render.py", encoding="utf-8").read(), r)
    g = {"__name__": "__main__", "MROOT": MROOT, "GROOT": GROOT}
    exec(open(GROOT + "/game_build.py", encoding="utf-8").read(), g)
    exec(open(GROOT + "/game_rig.py", encoding="utf-8").read(), g)
    r["stage"]()
    ao = bpy.data.objects["ElseIf_Body_GameOpt01_Armature"]
    out = os.path.join(GROOT, "Renders", "Compare")
    os.makedirs(out, exist_ok=True)
    pct = globals().get("PCT", 70)
    base_views = ["Front", "Side", "Back", "ThreeQuarter", "BackQuarter", "HighAngle"]
    nat_views = ["Front", "ThreeQuarter", "HighAngle"]
    if "compare" in WHAT:
        r["show_only"](["ElseIf_Master_BASE"])
        r["render"](base_views, out, "Master_", res_pct=pct, samples=32)
        r["show_only"](["ElseIf_Master_NATURAL"])
        r["render"](nat_views, out, "Master_NaturalPose_", res_pct=pct, samples=32)
        r["show_only"](["ElseIf_GameOpt01"])
        g["reset_pose"](ao)
        r["render"](base_views, out, "GameOpt01_", res_pct=pct, samples=32)
        g["pose_natural"](ao)
        r["render"](nat_views, out, "GameOpt01_NaturalPose_", res_pct=pct, samples=32)
        g["reset_pose"](ao)
    if "poses" in WHAT:
        pout = os.path.join(GROOT, "Renders", "PoseTest")
        os.makedirs(pout, exist_ok=True)
        r["show_only"](["ElseIf_GameOpt01"])
        for kind in ("ArmsWide", "ArmForward", "ArmBack", "Elbow90", "Asymmetric"):
            g["pose_test"](ao, kind)
            r["render"](["ThreeQuarter", "BackQuarter", "HighAngle"], pout, f"{kind}_", res_pct=pct, samples=24)
        g["reset_pose"](ao)
except Exception:
    print(traceback.format_exc())
