# 確認レンダー用のステージ（ライト・床・カメラ）とレンダー関数
import bpy, math, os
from mathutils import Vector

VIEWS = {
    # name: (方位deg: 0=正面(-Y側から), 90=キャラ左側(+X側から), 仰角deg, 距離, レンズ, 注視z)
    "Front": (0, 4, 8.0, 115, 0.84),
    "Side": (90, 4, 8.0, 115, 0.84),
    "Back": (180, 4, 8.0, 115, 0.84),
    "ThreeQuarter": (35, 8, 8.0, 115, 0.84),
    "BackQuarter": (215, 8, 8.0, 115, 0.84),
    "HighAngle": (20, 62, 8.0, 115, 0.78),
}


def stage():
    sc = bpy.context.scene
    coll = bpy.data.collections.get("Stage")
    if coll is None:
        coll = bpy.data.collections.new("Stage")
        sc.collection.children.link(coll)
    for n in ("Light", "Camera"):
        o = bpy.data.objects.get(n)
        if o and o.users_collection and coll not in o.users_collection:
            bpy.data.objects.remove(o, do_unlink=True)
    w = sc.world or bpy.data.worlds.new("World")
    sc.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.78, 0.79, 0.81, 1)
    bg.inputs[1].default_value = 0.35

    def light(name, kind, loc, rot, energy, size=1.0, color=(1, 1, 1)):
        o = bpy.data.objects.get(name)
        if o is None:
            ld = bpy.data.lights.new(name, kind)
            o = bpy.data.objects.new(name, ld)
            coll.objects.link(o)
        o.location = loc
        o.rotation_euler = [math.radians(a) for a in rot]
        o.data.energy = energy
        o.data.color = color
        if kind == "AREA":
            o.data.size = size
        if kind == "SUN":
            o.data.angle = math.radians(8)
        return o
    light("Key_Sun", "SUN", (2, -3, 5), (48, 0, 28), 1.5)
    light("Fill_Area", "AREA", (-3.5, -2.5, 2.0), (70, 0, -55), 160, 3.0, (0.92, 0.95, 1.0))
    light("Rim_Area", "AREA", (1.5, 3.5, 2.5), (-65, 0, 160), 220, 2.5)

    fl = bpy.data.objects.get("Floor")
    if fl is None:
        me = bpy.data.meshes.new("Floor")
        s = 6
        me.from_pydata([(-s, -s, 0), (s, -s, 0), (s, s, 0), (-s, s, 0)], [], [(0, 1, 2, 3)])
        fl = bpy.data.objects.new("Floor", me)
        coll.objects.link(fl)
        m = bpy.data.materials.new("M_Floor")
        m.use_nodes = True
        b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        b.inputs["Base Color"].default_value = (0.45, 0.46, 0.48, 1)
        b.inputs["Roughness"].default_value = 0.9
        me.materials.append(m)

    tgt = bpy.data.objects.get("CamTarget")
    if tgt is None:
        tgt = bpy.data.objects.new("CamTarget", None)
        coll.objects.link(tgt)
    cam = bpy.data.objects.get("RenderCam")
    if cam is None:
        cam = bpy.data.objects.new("RenderCam", bpy.data.cameras.new("RenderCam"))
        coll.objects.link(cam)
        c = cam.constraints.new("TRACK_TO")
        c.target = tgt
        c.track_axis = "TRACK_NEGATIVE_Z"
        c.up_axis = "UP_Y"
    sc.camera = cam
    try:
        sc.render.engine = "BLENDER_EEVEE"
    except TypeError:
        pass
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1620
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "AgX" if "AgX" in [i.identifier for i in sc.view_settings.bl_rna.properties["view_transform"].enum_items] else "Standard"
    try:
        sc.eevee.use_shadows = True
    except Exception:
        pass
    return cam, tgt


def set_view(name, zoom=1.0):
    az, el, dist, lens, tz = VIEWS[name]
    light_rig_follow(az)
    cam = bpy.data.objects["RenderCam"]
    tgt = bpy.data.objects["CamTarget"]
    tgt.location = (0, 0, tz)
    a, e = math.radians(az), math.radians(el)
    d = dist
    cam.location = (math.sin(a) * math.cos(e) * d, -math.cos(a) * math.cos(e) * d, tz + math.sin(e) * d)
    cam.data.lens = lens * zoom
    cam.data.sensor_fit = "AUTO"
    cam.data.clip_end = 100


def show_only(coll_names):
    for c in bpy.context.scene.collection.children:
        if c.name == "Stage":
            continue
        c.hide_render = c.name not in coll_names
        c.hide_viewport = c.name not in coll_names


def render(views, out_dir, prefix, res_pct=50, samples=32):
    sc = bpy.context.scene
    sc.render.resolution_percentage = res_pct
    try:
        sc.eevee.taa_render_samples = samples
    except Exception:
        pass
    os.makedirs(out_dir, exist_ok=True)
    outs = []
    for v in views:
        set_view(v)
        p = os.path.join(out_dir, f"{prefix}{v}.png")
        sc.render.filepath = p
        bpy.ops.render.render(write_still=True)
        outs.append(p)
    return outs


def light_rig_follow(az):
    """ライトをカメラ方位に追従させ、どの向きでも同じ照明にする"""
    rig = bpy.data.objects.get("LightRig")
    coll = bpy.data.collections["Stage"]
    if rig is None:
        rig = bpy.data.objects.new("LightRig", None)
        coll.objects.link(rig)
        for n in ("Key_Sun", "Fill_Area", "Rim_Area"):
            o = bpy.data.objects[n]
            o.parent = rig
    rig.rotation_euler = (0, 0, math.radians(az))


def shot(out, loc, target, lens=85, pct=50):
    sc = bpy.context.scene
    cam = bpy.data.objects["RenderCam"]
    bpy.data.objects["CamTarget"].location = target
    cam.location = loc
    cam.data.lens = lens
    sc.render.resolution_percentage = pct
    d = Vector(loc) - Vector(target)
    light_rig_follow(math.degrees(math.atan2(d.x, -d.y)))
    sc.render.filepath = out
    bpy.ops.render.render(write_still=True)
    return out
