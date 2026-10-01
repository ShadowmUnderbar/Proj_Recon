# Blender内: 再ビルド＋WIPレンダー
# 事前に globals に R, POSE, PARTS, VIEWS_TO, TAG, (ZOOM) を入れて exec する
import bpy, traceback
try:
    g = {"__name__": "__main__", "ROOT": R, "POSE": POSE, "PARTS": PARTS}
    exec(open(R + "/main.py", encoding="utf-8").read(), g)
    r = {}
    exec(open(R + "/render.py", encoding="utf-8").read(), r)
    r["stage"]()
    r["show_only"]([f"ElseIf_Master_{POSE}"])
    if VIEWS_TO:
        print(r["render"](VIEWS_TO, R + "/renders/wip", TAG, res_pct=globals().get("PCT", 40), samples=16))
except Exception:
    print(traceback.format_exc())
