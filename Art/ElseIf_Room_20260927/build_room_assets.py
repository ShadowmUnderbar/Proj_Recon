# エルスイフの自室用アセットセットを生成し、アセットごとにFBXへ書き出す。
# 実行: blender.exe -b --factory-startup --python build_room_assets.py
# 座標系: Blender Z上、各アセットの正面は -Y（Unityでは +Z が正面になる）。原点は接地面の中心。
# 方針: メッシュは形・厚み・機能構造だけを持つ。傷・擦れ・汚れ・補修跡などはテクスチャ側で表現する。
import bpy, bmesh, math, os, json, random
import numpy as np
from mathutils import Vector, Matrix, Euler

OUT = os.path.dirname(os.path.abspath(__file__)).replace('\\', '/')
EXP = OUT + '/Export'
TEX = EXP + '/Textures'
REN = OUT + '/Renders'
os.makedirs(EXP, exist_ok=True)
os.makedirs(REN, exist_ok=True)
os.makedirs(TEX, exist_ok=True)
random.seed(7)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

# ---------------------------------------------------------------- マテリアル
def srgb(h):
    h = h.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple((x / 12.92) if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)

PALETTE = {}
def mat(name, hexcol, rough=0.7, metal=0.0, emit=None, strength=0.0):
    m = bpy.data.materials.new('M_Room_' + name)
    if m.node_tree is None:
        try:
            m.use_nodes = True
        except Exception:
            pass
    col = srgb(hexcol)
    m.diffuse_color = col if emit is None else srgb(emit)
    m.roughness = rough
    m.metallic = metal
    if m.node_tree:
        b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        b.inputs['Base Color'].default_value = col
        b.inputs['Roughness'].default_value = rough
        b.inputs['Metallic'].default_value = metal
        if emit:
            b.inputs['Emission Color'].default_value = srgb(emit)
            b.inputs['Emission Strength'].default_value = strength
    PALETTE[m.name] = {'base': hexcol, 'emission': emit, 'strength': strength, 'metallic': metal}
    return m

# 基本色：ダークグレー / 青みグレー。機械は少量のシアン発光、生活用品に少し暖色
DARK      = mat('DarkGrey',      '#2b2f36')
STEEL     = mat('BlueGrey',      '#3e4a56')
STEEL_LT  = mat('BlueGreyLight', '#6b7a88')
PANEL     = mat('PanelGrey',     '#8d99a4')
PAD       = mat('SeatPad',       '#323a44', rough=0.55)
RUBBER    = mat('Rubber',        '#15171a', rough=0.9)
METAL     = mat('Metal',         '#7d848c', rough=0.4, metal=0.6)
METAL_DK  = mat('MetalDark',     '#4a525b', rough=0.45, metal=0.5)
CYAN      = mat('CyanGlow',      '#1fb8cc', emit='#29e6ff', strength=3.0)
SCREEN    = mat('Screen',        '#0b1318', rough=0.25, emit='#1a6a80', strength=0.6)
ORANGE    = mat('AccentOrange',  '#d9772b')
SOFA      = mat('SofaFabric',    '#7b5b48', rough=0.95)
SOFA_DK   = mat('SofaBase',      '#4a3a32', rough=0.9)
CUSHION   = mat('CushionFabric', '#c9824f', rough=0.95)
BLANKET   = mat('BlanketFabric', '#a8503c', rough=0.95)
BLANKET_S = mat('BlanketStripe', '#e0c9a0', rough=0.95)
MUG       = mat('MugCeramic',    '#d9a441', rough=0.4)
CUP       = mat('CupWhite',      '#ece6da', rough=0.6)
CUP_RED   = mat('CupRed',        '#d2452f', rough=0.6)
LID       = mat('LidFoil',       '#c9ced4', rough=0.35, metal=0.3)
COFFEE    = mat('CoffeeStain',   '#3b2418', rough=0.3)
CAN       = mat('CanBody',       '#c8553d', rough=0.35, metal=0.3)
CREAM     = mat('Cream',         '#e3d8c3')
PAPER     = mat('Paper',         '#d8d2c4', rough=0.95)
LAMP      = mat('LampWarm',      '#ffe0b0', emit='#ffcf8a', strength=4.0)
CHOPSTICK = mat('Chopstick',     '#c4703a', rough=0.5)
FORK      = mat('ForkPlastic',   '#e8e4dc', rough=0.5)
CRATE     = mat('StorageBox',    '#5d6e7c', rough=0.7)
TABLE     = mat('TableResin',    '#6b7a88', rough=0.6)
DESK_TOP  = mat('DeskTop',       '#3a4049', rough=0.6)
PLASTIC_LT= mat('PlasticLight',  '#b9bec4', rough=0.6)
BIN       = mat('TrashBin',      '#4f5d6a', rough=0.7)
FRAME     = mat('WindowFrame',   '#5b6570', rough=0.45, metal=0.5)
DOOR      = mat('DoorPanel',     '#5c6874', rough=0.6)
BASEBOARD = mat('Baseboard',     '#2f353c', rough=0.7)
GLASS     = mat('Glass',         '#8fb3c4', rough=0.05)
GLASS.diffuse_color = srgb('#8fb3c4')[:3] + (0.3,)
try:
    GLASS.surface_render_method = 'BLENDED'
except Exception:
    pass
CEIL_LAMP = mat('CeilingLight',  '#eef3f6', emit='#eef6ff', strength=5.0)

# ---------------------------------------------------------------- メッシュ生成ヘルパー
def rot_m(rot):
    return Euler([math.radians(a) for a in rot], 'XYZ').to_matrix().to_4x4()

def bevel(bm, w, seg=1, min_angle=30):
    if w <= 0:
        return
    edges = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(min_angle)]
    if edges:
        bmesh.ops.bevel(bm, geom=edges, offset=w, offset_type='OFFSET', segments=seg,
                        profile=0.5, affect='EDGES', clamp_overlap=True)

def box(sx, sy, sz, bev=0.0, seg=1):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
    bevel(bm, bev, seg)
    return bm

def cyl(r1, r2, h, n=16, bev=0.0, seg=1, caps=True):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=caps, cap_tris=False, segments=n, radius1=r1, radius2=r2, depth=h)
    bevel(bm, bev, seg)
    return bm

def sphere(r, u=16, v=8):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    return bm

def ico(r, sub=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)
    return bm

def torus(R, r, nu=24, nv=8):
    bm = bmesh.new()
    rings = []
    for i in range(nu):
        a = 2 * math.pi * i / nu
        ring = []
        for j in range(nv):
            b = 2 * math.pi * j / nv
            d = R + r * math.cos(b)
            ring.append(bm.verts.new((d * math.cos(a), d * math.sin(a), r * math.sin(b))))
        rings.append(ring)
    for i in range(nu):
        for j in range(nv):
            a, b = rings[i], rings[(i + 1) % nu]
            bm.faces.new((a[j], b[j], b[(j + 1) % nv], a[(j + 1) % nv]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def plane_uv(w, h):
    """XZ平面の板。法線は-Y（正面向き）、UVは0-1。画面用。"""
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new('UVMap')
    vs = [bm.verts.new(p) for p in ((-w / 2, 0, -h / 2), (w / 2, 0, -h / 2), (w / 2, 0, h / 2), (-w / 2, 0, h / 2))]
    f = bm.faces.new(vs)
    for loop, c in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop[uv].uv = c
    return bm

def catmull(pts, k=6):
    pts = [Vector(p) for p in pts]
    if len(pts) < 3:
        return pts
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for s in range(k):
            t = s / k
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(pts[-1])
    return out

def tube(pts, r, n=8, k=6):
    bm = bmesh.new()
    pts = catmull(pts, k)
    rings, prev = [], None
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        if prev is None:
            up = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
            nrm = t.cross(up).normalized()
        else:
            nrm = (prev - t * prev.dot(t)).normalized()
        prev = nrm
        b = t.cross(nrm)
        rings.append([bm.verts.new(p + r * (math.cos(2 * math.pi * j / n) * nrm + math.sin(2 * math.pi * j / n) * b)) for j in range(n)])
    for i in range(len(rings) - 1):
        for j in range(n):
            bm.faces.new((rings[i][j], rings[i][(j + 1) % n], rings[i + 1][(j + 1) % n], rings[i + 1][j]))
    bm.faces.new(rings[0][::-1])
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def lathe(profile, n=24, seg_mat=None, cap_bottom=True):
    """profile=[(r, z), ...]を回転させた側面。seg_mat[i]は区間iの面のmaterial_index。"""
    bm = bmesh.new()
    rings = [[bm.verts.new((r * math.cos(2 * math.pi * j / n), r * math.sin(2 * math.pi * j / n), z)) for j in range(n)] for r, z in profile]
    for i in range(len(rings) - 1):
        for j in range(n):
            f = bm.faces.new((rings[i][j], rings[i][(j + 1) % n], rings[i + 1][(j + 1) % n], rings[i + 1][j]))
            f.material_index = seg_mat[i] if seg_mat else 0
    if cap_bottom:
        f = bm.faces.new(rings[0][::-1])
        f.material_index = seg_mat[0] if seg_mat else 0
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def apply_mod(bm, kind, **kw):
    """一時オブジェクトに修飾子を付けて評価し、結果のbmeshを返す（Subdivision / Solidify用）。"""
    me = bpy.data.meshes.new('_tmp')
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new('_tmp', me)
    scene.collection.objects.link(o)
    md = o.modifiers.new('m', kind)
    for k, v in kw.items():
        setattr(md, k, v)
    dg = bpy.context.evaluated_depsgraph_get()
    me2 = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    out = bmesh.new()
    out.from_mesh(me2)
    bpy.data.objects.remove(o)
    bpy.data.meshes.remove(me)
    bpy.data.meshes.remove(me2)
    return out

def open_top(bm):
    """上向きのキャップ面を削除して器にする。"""
    top = max(bm.faces, key=lambda f: f.calc_center_median().z)
    bmesh.ops.delete(bm, geom=[top], context='FACES_ONLY')
    return bm

def vessel(bm, t):
    return apply_mod(open_top(bm), 'SOLIDIFY', thickness=t, offset=-1.0, use_even_offset=True)

def soft(bm, levels=2):
    return apply_mod(bm, 'SUBSURF', levels=levels, render_levels=levels)

class Part:
    """1つのMesh（＝Unityで1 GameObject）を、複数の部品から組み立てる。"""
    def __init__(self):
        self.bm = bmesh.new()
        self.mats = []

    def add(self, piece, m, loc=(0, 0, 0), rot=(0, 0, 0), scale=None):
        M = Matrix.Translation(loc) @ rot_m(rot)
        if scale:
            M = M @ Matrix.Diagonal(Vector(scale).to_4d())
        return self.addm(piece, m, M)

    def addm(self, piece, m, M):
        bmesh.ops.transform(piece, matrix=M, verts=piece.verts)
        mats = m if isinstance(m, (list, tuple)) else [m]
        remap = []
        for mm in mats:
            if mm not in self.mats:
                self.mats.append(mm)
            remap.append(self.mats.index(mm))
        for f in piece.faces:
            f.material_index = remap[min(f.material_index, len(remap) - 1)] if len(mats) > 1 else remap[0]
        me = bpy.data.meshes.new('_piece')
        piece.to_mesh(me)
        piece.free()
        self.bm.from_mesh(me)
        bpy.data.meshes.remove(me)
        return self

    def beam(self, p0, p1, w, d, m):
        p0, p1 = Vector(p0), Vector(p1)
        v = p1 - p0
        M = Matrix.Translation((p0 + p1) / 2) @ v.normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
        return self.addm(box(w, d, v.length, bev=min(w, d) * 0.2), m, M)

    def rotate(self, axis, deg, center):
        bmesh.ops.rotate(self.bm, verts=self.bm.verts, cent=Vector(center), matrix=Matrix.Rotation(math.radians(deg), 3, axis))
        return self

    def ground(self):
        z = min(v.co.z for v in self.bm.verts)
        bmesh.ops.translate(self.bm, vec=(0, 0, -z), verts=self.bm.verts)
        return self

COLL = {}
PIV = {}
def collection(name):
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    COLL[name] = c
    return c

def empty(name, c, parent=None, loc=(0, 0, 0), size=0.1, kind='PLAIN_AXES'):
    o = bpy.data.objects.new(name, None)
    o.empty_display_type = kind
    o.empty_display_size = size
    c.objects.link(o)
    if parent:
        o.parent = parent
        o.location = Vector(loc) - Vector(PIV.get(parent.name, (0, 0, 0)))
    else:
        o.location = loc
    PIV[name] = tuple(loc)
    return o

SMART_UV = []
def box_uv(bm, scale=0.5):
    """建築用：面の向きごとにワールド座標(m)から投影。scale=0.5で1UV=2m（床1枚・壁1枚の幅）。"""
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for loop in f.loops:
            co = loop.vert.co
            if ax == 2:
                loop[uv].uv = (co.x * scale + 0.5, co.y * scale + 0.5)
            elif ax == 1:
                loop[uv].uv = (co.x * scale + 0.5, co.z * scale)
            else:
                loop[uv].uv = (co.y * scale + 0.5, co.z * scale)

def finalize(part, name, c, parent=None, pivot=(0, 0, 0), smooth_angle=35, uv='smart'):
    """uv: 'smart'=重ならないUV（テクスチャを描く用） / 'box'=2m周期のワールドUV（建築） / 'keep'=作成済みのUVを使う"""
    bm = part.bm
    if uv == 'box':
        box_uv(bm)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) != 2 or e.calc_face_angle(0) > math.radians(smooth_angle):
            e.smooth = False
    bmesh.ops.translate(bm, vec=-Vector(pivot), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in part.mats:
        me.materials.append(m)
    o = bpy.data.objects.new(name, me)
    c.objects.link(o)
    if parent:
        o.parent = parent
        o.location = Vector(pivot) - Vector(PIV.get(parent.name, (0, 0, 0)))
    else:
        o.location = pivot
    PIV[name] = tuple(pivot)
    if uv == 'smart':
        SMART_UV.append(o)
    return o

def asset_smart_meshes(root):
    return [o for o in [root] + list(root.children_recursive) if o in SMART_UV]

def mesh_area(objs):
    return sum(p.area for o in objs for p in o.data.polygons)

def bake_res(objs):
    a = mesh_area(objs)
    return 2048 if a > 2.0 else 1024 if a > 0.15 else 512

def smart_uv_all():
    """アセットごとに、全パーツのUVを1枚（0〜1）へまとめて詰める。テクスチャ1セットで済む。"""
    for name, root, group in ASSETS:
        objs = asset_smart_meshes(root)
        if not objs:
            continue
        for s_ in bpy.context.view_layer.objects:
            s_.select_set(False)
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=6 / bake_res(objs), correct_aspect=True, scale_to_bounds=False)
        bpy.ops.object.mode_set(mode='OBJECT')

def planar_uv(o, x0, w, z0, h):
    """窓ガラス用：開口全体を0〜1に対応させる（外の景色を1枚で貼れる）。"""
    me = o.data
    if not me.uv_layers:
        me.uv_layers.new(name='UVMap')
    lay = me.uv_layers[0]
    for poly in me.polygons:
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co + o.location
            lay.data[li].uv = ((co.x - x0) / w, (co.z - z0) / h)

ASSETS = []  # (FBXファイル名, ルートObject, 分類)
def register(root, group):
    ASSETS.append((root.name, root, group))
    return root

# ================================================================= 1. メインチェア
def build_chair():
    c = collection('Chair')
    R = empty('Chair', c, size=0.3)
    p = Part()  # 脚（台座＋支柱）
    p.add(cyl(0.30, 0.30, 0.05, 24, bev=0.012), DARK, (0, 0, 0.025))
    p.add(cyl(0.22, 0.26, 0.04, 24, bev=0.008), STEEL, (0, 0, 0.07))
    p.add(cyl(0.055, 0.055, 0.30, 16), METAL, (0, 0, 0.24))
    p.add(box(0.22, 0.22, 0.06, bev=0.012), STEEL, (0, 0, 0.36))
    finalize(p, 'Chair_Base', c, R)

    p = Part()  # 座面（シェル＋パッド＋ガムテープ補修）
    p.add(box(0.62, 0.62, 0.06, bev=0.015), STEEL, (0, 0, 0.42))
    p.add(box(0.56, 0.58, 0.10, bev=0.035, seg=2), PAD, (0, -0.01, 0.50))
    finalize(p, 'Chair_Seat', c, R)

    # 背もたれ：ヒンジ位置をPivotにし、Unity上でX回転させればリクライニングする
    hinge = (0, 0.28, 0.48)
    p = Part()
    p.add(box(0.58, 0.07, 0.80, bev=0.018), STEEL, (0, 0.34, 0.90))
    p.add(box(0.52, 0.10, 0.74, bev=0.035, seg=2), PAD, (0, 0.27, 0.92))
    for s in (-1, 1):
        p.add(box(0.07, 0.13, 0.58, bev=0.03, seg=2), PAD, (s * 0.275, 0.26, 0.97))
        p.add(cyl(0.013, 0.013, 0.08, 8), METAL, (s * 0.08, 0.33, 1.33))
    p.add(box(0.34, 0.12, 0.18, bev=0.04, seg=2), PAD, (0, 0.29, 1.43))
    # 太いケーブル用コネクタ（背面）
    p.add(box(0.22, 0.08, 0.18, bev=0.012), STEEL, (0, 0.405, 1.05))
    p.add(cyl(0.05, 0.05, 0.05, 20), DARK, (0, 0.46, 1.05), (-90, 0, 0))
    p.add(torus(0.052, 0.008, 20, 6), CYAN, (0, 0.487, 1.05), (90, 0, 0))
    p.add(box(0.016, 0.006, 0.016), CYAN, (0.08, 0.447, 1.12))
    p.rotate('X', -12, hinge)
    finalize(p, 'Chair_Back', c, R, pivot=hinge)

    # 挿さったままの太ケーブル（不要ならUnity側で非表示にできるよう別Mesh）
    # コネクタ側だけ背もたれの角度で回し、床側の点は床の上に固定する
    Rb = Matrix.Translation(hinge) @ Matrix.Rotation(math.radians(-12), 4, 'X') @ Matrix.Translation(-Vector(hinge))
    p = Part()
    p.addm(cyl(0.042, 0.042, 0.07, 16, bev=0.006), STEEL, Rb @ Matrix.Translation((0, 0.51, 1.05)) @ rot_m((-90, 0, 0)))
    near = [Rb @ Vector(v) for v in ((0, 0.54, 1.05), (0, 0.66, 1.0))]
    p.add(tube(near + [(0.05, 0.8, 0.75), (0.2, 0.8, 0.3), (0.35, 0.78, 0.06), (0.5, 0.8, 0.029)], 0.028, 10), RUBBER)
    finalize(p, 'Chair_Cable', c, R, pivot=hinge)  # 孫階層はFBX書き出しで位置が崩れるためルート直下。Chair_Backと同じPivot

    for side, s in (('L', 1), ('R', -1)):  # 座った本人から見た左が +X
        p = Part()
        p.add(box(0.06, 0.08, 0.18, bev=0.01), STEEL, (s * 0.31, 0.05, 0.54))
        p.add(box(0.09, 0.42, 0.05, bev=0.02, seg=2), PAD, (s * 0.32, 0.0, 0.655))
        if side == 'L':
            p.add(box(0.05, 0.1, 0.006), DARK, (0.32, -0.12, 0.681))
            p.add(box(0.012, 0.012, 0.005), CYAN, (0.32, -0.145, 0.685))
            p.add(box(0.012, 0.012, 0.005), CYAN, (0.32, -0.12, 0.685))
        finalize(p, 'Chair_Arm_' + side, c, R, pivot=(s * 0.31, 0.05, 0.45))

    hinge2 = (0, -0.30, 0.47)  # 脚置き：座面前縁のヒンジで回転
    p = Part()
    p.add(cyl(0.02, 0.02, 0.46, 12), METAL, hinge2, (0, 90, 0))
    p.add(box(0.50, 0.09, 0.40, bev=0.03, seg=2), PAD, (0, -0.33, 0.47 - 0.22))
    p.add(box(0.46, 0.15, 0.03, bev=0.008), DARK, (0, -0.37, 0.47 - 0.44))
    p.rotate('X', -35, hinge2)
    finalize(p, 'Chair_Legrest', c, R, pivot=hinge2)
    return register(R, 'Furniture')

# ================================================================= 2. ヘッドパーツ交換装置
RING_Z = 1.60
def build_machine():
    c = collection('HeadSwapMachine')
    R = empty('HeadSwapMachine', c, size=0.4)
    p = Part()  # 台座（キャスター・カウンターウェイト・操作箱）
    p.add(box(0.72, 0.60, 0.07, bev=0.015), DARK, (0, 0, 0.045))
    for x in (-0.3, 0.3):
        for y in (-0.24, 0.24):
            p.add(cyl(0.03, 0.03, 0.025, 12), RUBBER, (x, y, 0.03), (0, 90, 0))
            p.add(box(0.035, 0.05, 0.03), STEEL, (x, y, 0.06))
    p.add(box(0.30, 0.20, 0.12, bev=0.012), METAL_DK, (-0.17, 0.15, 0.14))
    p.add(box(0.18, 0.14, 0.22, bev=0.012), STEEL, (0.22, 0.12, 0.19))
    p.add(box(0.11, 0.006, 0.05), CYAN, (0.22, 0.049, 0.24))
    for i in range(3):
        p.add(cyl(0.008, 0.008, 0.01, 8), ORANGE if i == 0 else DARK, (0.19 + i * 0.03, 0.047, 0.16), (-90, 0, 0))
    finalize(p, 'HeadSwapMachine_Base', c, R)

    p = Part()  # 支柱＋給電ホース
    p.add(box(0.16, 0.16, 2.0, bev=0.02), STEEL, (0, 0.12, 1.08))
    p.add(box(0.10, 0.006, 1.6), DARK, (0, 0.038, 1.10))
    for z in (1.55, 1.62, 1.69):
        p.add(box(0.03, 0.006, 0.02), CYAN, (0, 0.034, z))
    p.add(tube([(0.12, 0.24, 0.09), (0.12, 0.25, 0.6), (0.10, 0.25, 1.5), (0.05, 0.24, 2.0), (0, 0.2, 2.2)], 0.025, 10), RUBBER)
    for z in (0.6, 1.2, 1.8):
        p.add(box(0.07, 0.06, 0.03), METAL_DK, (0.09, 0.225, z))
    finalize(p, 'HeadSwapMachine_Pillar', c, R)

    boom_pivot = (0, 0.12, 2.08)  # アーム：Z回転で椅子の上から退避できる
    p = Part()
    p.add(cyl(0.10, 0.10, 0.08, 20, bev=0.01), DARK, (0, 0.12, 2.12))
    p.add(box(0.14, 0.92, 0.12, bev=0.02), STEEL, (0, -0.26, 2.18))
    p.add(box(0.22, 0.22, 0.30, bev=0.02), STEEL, (0, -0.72, 2.12))
    for s in (-1, 1):
        p.add(box(0.006, 0.09, 0.02), CYAN, (s * 0.111, -0.72, 2.17))
    finalize(p, 'HeadSwapMachine_Boom', c, R, pivot=boom_pivot)

    hub = (0, -0.72, RING_Z + 0.10)  # リング：-Z移動で頭へ降りる（約0.2mまでロッドが筐体内に残る）
    p = Part()
    p.add(cyl(0.025, 0.025, 2.2 - hub[2], 12), METAL, (0, -0.72, (2.2 + hub[2]) / 2))
    p.add(box(0.10, 0.10, 0.06, bev=0.012), STEEL, hub)
    p.add(torus(0.19, 0.035, 32, 8), STEEL, (0, -0.72, RING_Z))
    p.add(torus(0.155, 0.01, 32, 6), CYAN, (0, -0.72, RING_Z))
    L = math.hypot(0.19, 0.10)
    a = math.degrees(math.atan2(0.10, 0.19))
    for s in (-1, 1):
        p.add(box(L, 0.035, 0.03), STEEL_LT, (s * 0.095, -0.72, RING_Z + 0.05), (0, s * a, 0))
        p.add(box(0.035, L, 0.03), STEEL_LT, (0, -0.72 + s * 0.095, RING_Z + 0.05), (-s * a, 0, 0))
        # 頭を挟むクランプ
        p.add(box(0.03, 0.08, 0.13, bev=0.008), DARK, (s * 0.17, -0.72, RING_Z - 0.08), (0, s * 10, 0))
        p.add(box(0.012, 0.07, 0.09), RUBBER, (s * 0.152, -0.72, RING_Z - 0.10), (0, s * 10, 0))
    finalize(p, 'HeadSwapMachine_Ring', c, R, pivot=hub)  # 同上。Boomを回すならUnity側でBoomの子にする
    return register(R, 'Furniture')

# ================================================================= 3. ヘッドパーツラック
SHELVES = (0.06, 0.46, 0.86, 1.26, 1.66)
def build_rack():
    c = collection('HeadRack')
    R = empty('HeadRack', c, size=0.3)
    p = Part()
    for x in (-0.23, 0.23):
        for y in (-0.17, 0.17):
            p.add(box(0.035, 0.035, 1.72, bev=0.004), METAL_DK, (x, y, 0.86))
    p.add(box(0.46, 0.01, 1.62), DARK, (0, 0.18, 0.87))
    for i, z in enumerate(SHELVES):
        p.add(box(0.50, 0.38, 0.025, bev=0.006), STEEL, (0, 0, z))
        if i < 4:
            top = z + 0.0125
            p.add(cyl(0.07, 0.08, 0.05, 20, bev=0.006), DARK, (0, -0.02, top + 0.025))
            p.add(cyl(0.034, 0.034, 0.002, 20), RUBBER, (0, -0.02, top + 0.0505))  # 首プラグの差込口
            p.add(torus(0.045, 0.006, 20, 6), CYAN if i != 3 else RUBBER, (0, -0.02, top + 0.05))
    finalize(p, 'HeadRack_Frame', c, R)
    for i, z in enumerate(SHELVES[:4]):
        empty('HeadSocket_%02d' % (i + 1), c, R, loc=(0, -0.02, z + 0.0125 + 0.05), size=0.05, kind='ARROWS')
    return register(R, 'Furniture')

# ================================================================= 4. ソファー
def build_sofa():
    c = collection('Sofa')
    R = empty('Sofa', c, size=0.4)
    p = Part()
    p.add(box(1.45, 0.74, 0.20, bev=0.02), SOFA_DK, (0, 0.01, 0.16))
    for x in (-0.66, 0.66):
        for y in (-0.3, 0.3):
            p.add(cyl(0.02, 0.016, 0.06, 10), METAL, (x, y, 0.03))
    for s in (-1, 1):
        p.add(box(0.16, 0.78, 0.36, bev=0.05, seg=3), SOFA, (s * 0.645, 0, 0.44))
    p.add(box(1.45, 0.18, 0.56, bev=0.05, seg=3), SOFA, (0, 0.30, 0.54))
    finalize(p, 'Sofa_Frame', c, R)
    for side, s in (('L', 1), ('R', -1)):
        p = Part()
        sag = side == 'R'  # 片側だけへたっている
        p.add(soft(box(0.56, 0.58, 0.15, bev=0.04)), SOFA, (s * 0.285, -0.06, 0.33 if sag else 0.335), (-2, 2, 0) if sag else (0, 0, 0))
        finalize(p, 'Sofa_SeatCushion_' + side, c, R, pivot=(s * 0.285, -0.06, 0.26))
        p = Part()
        p.add(soft(box(0.56, 0.16, 0.40, bev=0.04)), SOFA, (s * 0.285, 0.17, 0.62), (-10, 0, 0))
        finalize(p, 'Sofa_BackCushion_' + side, c, R, pivot=(s * 0.285, 0.17, 0.41))
    return register(R, 'Furniture')

# ================================================================= 5. ローテーブル
def build_lowtable():
    c = collection('LowTable')
    p = Part()
    p.add(box(0.90, 0.50, 0.03, bev=0.012, seg=2), TABLE, (0, 0, 0.365))
    for s in (-1, 1):
        for y in (-0.2, 0.2):
            p.add(box(0.03, 0.03, 0.33), METAL_DK, (s * 0.40, y, 0.185))
        p.add(box(0.03, 0.44, 0.03), METAL_DK, (s * 0.40, 0, 0.015))
        p.add(box(0.03, 0.44, 0.025), METAL_DK, (s * 0.40, 0, 0.3375))
    finalize(p, 'LowTable', c)
    return register(bpy.data.objects['LowTable'], 'Furniture')

# ================================================================= 6. 作業デスク
def build_desk():
    c = collection('Desk')
    p = Part()
    p.add(box(1.20, 0.60, 0.035, bev=0.008), DESK_TOP, (0, 0, 0.7225))
    p.add(box(1.0, 0.01, 0.004), CYAN, (0, -0.28, 0.703))
    p.add(box(0.035, 0.56, 0.705, bev=0.006), STEEL, (-0.57, 0, 0.3525))
    p.add(box(0.38, 0.56, 0.68, bev=0.008), STEEL, (0.39, 0, 0.35))
    for z in (0.58, 0.36, 0.14):
        p.add(box(0.34, 0.012, 0.19, bev=0.006), PANEL, (0.39, -0.285, z))
        p.add(box(0.14, 0.02, 0.015, bev=0.004), DARK, (0.39, -0.297, z + 0.06))
    p.add(box(0.76, 0.015, 0.30), STEEL, (-0.18, 0.27, 0.54))
    p.add(box(0.6, 0.08, 0.05), DARK, (-0.15, 0.2, 0.665))
    p.add(cyl(0.03, 0.03, 0.003, 16), RUBBER, (-0.4, 0.22, 0.7405))
    finalize(p, 'Desk', c)
    return register(bpy.data.objects['Desk'], 'Furniture')

# ================================================================= 7. モニター（画面は別Mesh・UV 0-1）
def build_monitor_a():
    c = collection('Monitor_A')
    R = empty('Monitor_A', c, size=0.15)
    p = Part()
    p.add(box(0.24, 0.18, 0.015, bev=0.006), DARK, (0, 0.02, 0.0075))
    p.add(box(0.05, 0.025, 0.30, bev=0.008), STEEL, (0, 0.075, 0.16))
    p.add(box(0.62, 0.03, 0.37, bev=0.01), DARK, (0, 0.03, 0.36))
    p.add(box(0.30, 0.03, 0.20, bev=0.01), DARK, (0, 0.055, 0.36))
    p.add(box(0.008, 0.004, 0.004), CYAN, (0.28, 0.014, 0.186))
    finalize(p, 'Monitor_A_Body', c, R)
    p = Part()
    p.add(plane_uv(0.59, 0.332), SCREEN, (0, 0.0145, 0.366))
    finalize(p, 'Monitor_A_Screen', c, R, uv='keep')
    return register(R, 'Furniture')

def build_monitor_b():
    c = collection('Monitor_B')  # 古めの小型サブモニター。少し上向き
    R = empty('Monitor_B', c, size=0.15)
    tilt_c = (0, 0.04, 0.26)
    p = Part()
    p.add(box(0.18, 0.16, 0.02, bev=0.008), STEEL, (0, 0.03, 0.01))
    p.add(cyl(0.018, 0.018, 0.2, 12), METAL, (0, 0.05, 0.12))
    T = Matrix.Translation(tilt_c) @ Matrix.Rotation(math.radians(-8), 4, 'X') @ Matrix.Translation(-Vector(tilt_c))
    p.addm(box(0.44, 0.06, 0.30, bev=0.015, seg=2), STEEL_LT, T @ Matrix.Translation((0, 0.04, 0.33)))
    p.addm(box(0.28, 0.04, 0.18, bev=0.012), STEEL_LT, T @ Matrix.Translation((0, 0.08, 0.33)))
    finalize(p, 'Monitor_B_Body', c, R)
    p = Part()
    p.add(plane_uv(0.39, 0.25), SCREEN, (0, 0.0095, 0.335))
    p.rotate('X', -8, tilt_c)
    finalize(p, 'Monitor_B_Screen', c, R, uv='keep')
    return register(R, 'Furniture')

# ================================================================= デスクライト
def build_desklight():
    c = collection('DeskLight')
    p = Part()
    p.add(cyl(0.07, 0.075, 0.025, 20, bev=0.005), DARK, (0, 0, 0.0125))
    p.add(box(0.03, 0.02, 0.008), CYAN, (0.0, -0.06, 0.026))
    a, b, h = Vector((0, 0.02, 0.03)), Vector((0, 0.08, 0.33)), Vector((0, -0.14, 0.42))
    p.beam(a, b, 0.022, 0.016, STEEL)
    p.beam(b, h, 0.02, 0.014, STEEL)
    p.add(sphere(0.02, 12, 6), DARK, tuple(b))
    p.add(sphere(0.016, 12, 6), DARK, tuple(a))
    d = Vector((0, -0.45, -1)).normalized()
    q = d.to_track_quat('-Z', 'Y').to_matrix().to_4x4()
    p.addm(cyl(0.058, 0.022, 0.08, 20, bev=0.004), DARK, Matrix.Translation(h + d * 0.02) @ q)
    p.addm(cyl(0.05, 0.05, 0.004, 20), LAMP, Matrix.Translation(h + d * 0.061) @ q)
    finalize(p, 'DeskLight', c)
    return register(bpy.data.objects['DeskLight'], 'Props')

# ================================================================= 8. カップ麺（部品を切り替えて使う）
NOODLE_TEX = TEX + '/CupNoodle_Contents.png'

def paint_noodle_texture(n=512):
    """スープ・麺・具を描いたトゥーン調のテクスチャ（UVの円＝カップの内径）。"""
    rnd = random.Random(3)
    def c(h):
        h = h.lstrip('#')
        return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1.0])
    img = np.zeros((n, n, 4))
    yy, xx = np.mgrid[0:n, 0:n]
    d = np.hypot(xx - n / 2, yy - n / 2) / (n / 2)
    img[:] = c('#8a4a22')
    img[d < 0.93] = c('#a65a2a')              # スープ（縁だけ一段暗く）
    img[(d < 0.55) & (d > 0.5)] = c('#b4692f')  # 油の輪
    def stamp(px, py, r, col):
        x0, x1 = max(int(px - r), 0), min(int(px + r) + 1, n)
        y0, y1 = max(int(py - r), 0), min(int(py + r) + 1, n)
        if x0 >= x1 or y0 >= y1:
            return
        sy, sx = np.mgrid[y0:y1, x0:x1]
        m = (sx - px) ** 2 + (sy - py) ** 2 <= r * r
        img[y0:y1, x0:x1][m] = col
    def noodle(ang, off, amp, freq, phase):
        dx, dy = math.cos(ang), math.sin(ang)
        pts = []
        for t in np.linspace(-0.8, 0.8, 90):
            w = amp * math.sin(freq * t * math.pi + phase)
            x = n / 2 + (t * dx + (off + w) * -dy) * n / 2
            y = n / 2 + (t * dy + (off + w) * dx) * n / 2
            if math.hypot(x - n / 2, y - n / 2) < n * 0.44:
                pts.append((x, y))
        for x, y in pts:
            stamp(x, y, 9, c('#c99a45'))
        for x, y in pts:
            stamp(x, y, 6.5, c('#f0d58a'))
    for i in range(16):
        noodle(rnd.uniform(0, math.pi), rnd.uniform(-0.45, 0.45), rnd.uniform(0.04, 0.09), rnd.uniform(2, 4), rnd.uniform(0, 6))
    # なると
    cx, cy = n * 0.62, n * 0.38
    stamp(cx, cy, 44, c('#d98a99'))
    stamp(cx, cy, 40, c('#f5eee6'))
    for k in range(160):
        t = k / 160
        a = t * 4 * math.pi
        stamp(cx + math.cos(a) * 34 * t, cy + math.sin(a) * 34 * t, 4, c('#e07a8f'))
    # ネギ・卵・肉
    for i in range(9):
        a = rnd.uniform(0, 2 * math.pi); rr = rnd.uniform(0.1, 0.75) * n / 2
        x, y = n / 2 + math.cos(a) * rr, n / 2 + math.sin(a) * rr
        stamp(x, y, 11, c('#3f7a34')); stamp(x, y, 7, c('#7cc05a'))
    for (x, y) in ((0.33, 0.35), (0.4, 0.66), (0.7, 0.68)):
        stamp(x * n, y * n, 20, c('#d9a326')); stamp(x * n - 3, y * n + 3, 15, c('#f7cf45'))
    for (x, y) in ((0.3, 0.55), (0.55, 0.72)):
        for k in range(-2, 3):
            stamp(x * n + k * 8, y * n, 17, c('#7a4a30'))
        for k in range(-2, 3):
            stamp(x * n + k * 8, y * n + 2, 13, c('#a8704a'))
    im = bpy.data.images.new('CupNoodle_Contents', n, n, alpha=False)
    im.pixels.foreach_set(img.astype(np.float32).ravel())
    im.filepath_raw = NOODLE_TEX
    im.file_format = 'PNG'
    os.makedirs(os.path.dirname(NOODLE_TEX), exist_ok=True)
    im.save()
    return im

def textured_mat(name, image):
    m = mat(name, '#a65a2a', rough=0.3)
    nt = m.node_tree
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = image
    b = next(nd for nd in nt.nodes if nd.type == 'BSDF_PRINCIPLED')
    nt.links.new(tex.outputs['Color'], b.inputs['Base Color'])
    nt.nodes.active = tex
    PALETTE[m.name]['texture'] = os.path.basename(image.filepath_raw)
    return m

NOODLE_TOP = textured_mat('NoodleContents', paint_noodle_texture())

def build_cupnoodle():
    c = collection('CupNoodle')
    R = empty('CupNoodle', c, size=0.05)
    p = Part()
    prof = [(0.045 + 0.15 * z, z) for z in (0.0, 0.04, 0.075, 0.1)]
    cup = apply_mod(lathe(prof, 24, [0, 1, 0]), 'SOLIDIFY', thickness=0.0025, offset=-1.0, use_even_offset=True)
    for f in cup.faces:  # 帯の色は外側の面だけ
        cc = f.calc_center_median()
        if f.normal.x * cc.x + f.normal.y * cc.y < 0 or abs(f.normal.z) > 0.9:
            f.material_index = 0
    p.add(cup, [CUP, CUP_RED])
    p.add(torus(0.06, 0.0025, 24, 6), CUP, (0, 0, 0.1))
    finalize(p, 'CupNoodle_Cup', c, R)

    def lid():
        l = Part()
        l.add(cyl(0.063, 0.063, 0.002, 24), LID, (0, 0, 0.101))
        l.add(box(0.026, 0.022, 0.002), LID, (0, -0.07, 0.101))
        return l
    finalize(lid(), 'CupNoodle_Lid_Sealed', c, R, pivot=(0, 0, 0.1))
    def half(keep_back):
        l = lid()
        n = (0, 1, 0) if keep_back else (0, -1, 0)
        bmesh.ops.bisect_plane(l.bm, geom=l.bm.verts[:] + l.bm.edges[:] + l.bm.faces[:], plane_co=(0, 0, 0), plane_no=n, clear_inner=True)
        bmesh.ops.holes_fill(l.bm, edges=[e for e in l.bm.edges if e.is_boundary], sides=0)
        return l
    back, flap = half(True), half(False)
    flap.rotate('X', -150, (0, 0, 0.102))  # 手前半分を剥がして奥へ折り返す
    me = bpy.data.meshes.new('_flap')
    flap.bm.to_mesh(me)
    back.bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    finalize(back, 'CupNoodle_Lid_Open', c, R, pivot=(0, 0, 0.1))

    def contents(z, r):
        bm = bmesh.new()
        bmesh.ops.create_circle(bm, cap_ends=True, segments=24, radius=r)
        uv = bm.loops.layers.uv.new('UVMap')
        for f in bm.faces:
            for loop in f.loops:
                loop[uv].uv = (loop.vert.co.x / (2 * r) + 0.5, loop.vert.co.y / (2 * r) + 0.5)
        q = Part()
        q.add(bm, NOODLE_TOP, (0, 0, z))
        return q
    finalize(contents(0.088, 0.0565), 'CupNoodle_Contents_Full', c, R, pivot=(0, 0, 0.086), uv='keep')
    finalize(contents(0.055, 0.0528), 'CupNoodle_Contents_Half', c, R, pivot=(0, 0, 0.053), uv='keep')
    return register(R, 'Props')

def build_chopsticks():
    c = collection('Utensil_Chopsticks')
    p = Part()
    for s, r in ((-1, -2.5), (1, 2.0)):
        p.add(cyl(0.0045, 0.0022, 0.22, 8), CHOPSTICK, (0, s * 0.007, 0.0045), (0, 90, r))
    finalize(p.ground(), 'Utensil_Chopsticks', c)
    return register(bpy.data.objects['Utensil_Chopsticks'], 'Props')

def build_fork():
    c = collection('Utensil_Fork')
    p = Part()
    p.add(box(0.012, 0.10, 0.004, bev=0.0015), FORK, (0, 0.045, 0.002))
    p.add(box(0.022, 0.035, 0.004, bev=0.0015), FORK, (0, -0.02, 0.002))
    for x in (-0.0075, 0, 0.0075):
        p.add(box(0.0045, 0.028, 0.004), FORK, (x, -0.05, 0.002))
    finalize(p.ground(), 'Utensil_Fork', c)
    return register(bpy.data.objects['Utensil_Fork'], 'Props')

# ================================================================= 追加の生活小物
def can_part():
    p = Part()
    p.add(cyl(0.028, 0.033, 0.01, 24), METAL, (0, 0, 0.005))
    p.add(lathe([(0.033, 0.01), (0.033, 0.074), (0.033, 0.096), (0.033, 0.12)], 24, [0, 1, 0], cap_bottom=False), [CAN, CREAM])
    p.add(cyl(0.033, 0.027, 0.01, 24, caps=False), METAL, (0, 0, 0.125))
    p.add(cyl(0.027, 0.027, 0.002, 20), METAL, (0, 0, 0.131))
    p.add(torus(0.027, 0.002, 20, 4), METAL, (0, 0, 0.132))
    p.add(box(0.012, 0.02, 0.0012), METAL, (0, 0.006, 0.1325))
    p.add(box(0.011, 0.012, 0.0006), RUBBER, (0, -0.013, 0.1323))  # 開いた飲み口
    return p

def build_can():
    c = collection('DrinkCan')
    finalize(can_part(), 'DrinkCan', c)
    return register(bpy.data.objects['DrinkCan'], 'Props')

def build_can_crushed():
    c = collection('DrinkCan_Crushed')
    p = can_part()
    for v in p.bm.verts:  # 縦に潰してねじる
        z = v.co.z
        a = math.atan2(v.co.y, v.co.x)
        k = 1 + 0.14 * math.sin(3 * a + z * 50) * math.sin(math.pi * min(z / 0.13, 1))
        v.co.x *= k
        v.co.y *= k * 0.9
        v.co.z = 0.01 + (z - 0.01) * 0.55 if z > 0.01 else z
        if z > 0.1:
            v.co.x += 0.006
    finalize(p.ground(), 'DrinkCan_Crushed', c)
    return register(bpy.data.objects['DrinkCan_Crushed'], 'Props')

def build_mug():
    c = collection('Mug')
    p = Part()
    p.add(vessel(cyl(0.04, 0.04, 0.09, 24), 0.005), MUG, (0, 0, 0.045))
    p.add(cyl(0.034, 0.034, 0.001, 20), COFFEE, (0, 0, 0.012))  # 底に残ったコーヒー
    h = torus(0.026, 0.007, 16, 8)
    bmesh.ops.delete(h, geom=[v for v in h.verts if v.co.x < -0.001], context='VERTS')
    p.add(h, MUG, (0.038, 0, 0.048), (90, 0, 0))
    finalize(p, 'Mug', c)
    return register(bpy.data.objects['Mug'], 'Props')

def build_controller():
    c = collection('GameController')
    p = Part()
    p.add(box(0.10, 0.055, 0.028, bev=0.012, seg=2), DARK, (0, 0, 0.02))
    for s in (-1, 1):
        p.add(cyl(0.018, 0.022, 0.07, 12, bev=0.004), DARK, (s * 0.042, 0.04, 0.018), (-95, 0, -s * 15))
        p.add(box(0.03, 0.012, 0.01, bev=0.003), STEEL, (s * 0.035, -0.03, 0.028))
    for (x, y) in ((-0.028, -0.006), (0.014, 0.014)):
        p.add(cyl(0.008, 0.008, 0.01, 12), RUBBER, (x, y, 0.036))
        p.add(cyl(0.011, 0.011, 0.004, 16), RUBBER, (x, y, 0.042))
    p.add(box(0.022, 0.007, 0.003), STEEL_LT, (-0.012, 0.014, 0.035))
    p.add(box(0.007, 0.022, 0.003), STEEL_LT, (-0.012, 0.014, 0.035))
    for (x, y) in ((0.03, -0.016), (0.03, 0.0), (0.022, -0.008), (0.038, -0.008)):
        p.add(cyl(0.0045, 0.0045, 0.004, 10), STEEL_LT, (x, y, 0.035))
    p.add(box(0.02, 0.004, 0.003), CYAN, (0, -0.02, 0.0345))
    finalize(p.ground(), 'GameController', c)
    return register(bpy.data.objects['GameController'], 'Props')

def build_mobile():
    c = collection('MobileDevice')
    R = empty('MobileDevice', c, size=0.03)
    p = Part()
    p.add(box(0.075, 0.155, 0.009, bev=0.004, seg=2), DARK, (0, 0, 0.0045))
    for x in (-1, 1):
        for y in (-1, 1):
            p.add(box(0.014, 0.014, 0.011, bev=0.003), ORANGE, (x * 0.034, y * 0.074, 0.0055))  # 安いケースの角
    p.add(cyl(0.007, 0.007, 0.002, 12), RUBBER, (-0.022, 0.06, -0.0005))
    finalize(p.ground(), 'MobileDevice_Body', c, R)
    p = Part()
    p.add(plane_uv(0.066, 0.143), SCREEN, (0, 0, 0.0092), (-90, 0, 0))
    finalize(p, 'MobileDevice_Screen', c, R, uv='keep')
    return register(R, 'Props')

def crumple(r, seed):
    bm = ico(r, 2)
    rnd = random.Random(seed)
    for v in bm.verts:
        v.co *= rnd.uniform(0.75, 1.1)
    return bm

def build_trashbin():
    c = collection('TrashBin')
    R = empty('TrashBin', c, size=0.1)
    p = Part()
    p.add(vessel(cyl(0.13, 0.15, 0.34, 24), 0.006), BIN, (0, 0, 0.17))
    p.add(torus(0.15, 0.008, 32, 6), DARK, (0, 0, 0.338))
    p.add(cyl(0.132, 0.132, 0.02, 24, caps=False), DARK, (0, 0, 0.012))
    finalize(p, 'TrashBin_Body', c, R)
    p = Part()
    for i, (x, y, z, r) in enumerate(((0.03, 0.02, 0.29, 0.045), (-0.05, -0.02, 0.28, 0.04), (0.0, -0.06, 0.305, 0.035))):
        p.add(crumple(r, i), PAPER, (x, y, z))
    p.add(box(0.1, 0.07, 0.004), CUP_RED, (0.04, 0.05, 0.315), (20, -15, 30))
    finalize(p, 'TrashBin_Contents', c, R, pivot=(0, 0, 0.25))
    return register(R, 'Props')

def build_storage():
    c = collection('StorageBox')
    R = empty('StorageBox', c, size=0.1)
    p = Part()
    b = vessel(box(0.42, 0.30, 0.22), 0.008)
    bevel(b, 0.003)
    p.add(b, CRATE, (0, 0, 0.11))
    p.add(box(0.426, 0.306, 0.016), DARK, (0, 0, 0.02))
    for s in (-1, 1):
        p.add(box(0.004, 0.10, 0.03), RUBBER, (s * 0.2105, 0, 0.17))
    finalize(p, 'StorageBox_Body', c, R)
    p = Part()
    p.add(box(0.44, 0.32, 0.025, bev=0.006), STEEL_LT, (0, 0, 0.2325))
    p.add(box(0.30, 0.18, 0.006), CRATE, (0, 0, 0.246))
    finalize(p, 'StorageBox_Lid', c, R, pivot=(0, 0, 0.22))
    return register(R, 'Props')

def build_blanket_folded():
    c = collection('Blanket_Folded')
    p = Part()
    b = box(0.45, 0.32, 0.09, bev=0.025)
    for x in (-0.155, -0.125, 0.125, 0.155):
        bmesh.ops.bisect_plane(b, geom=b.verts[:] + b.edges[:] + b.faces[:], plane_co=(x, 0, 0), plane_no=(1, 0, 0))
    b = soft(b, 1)
    for f in b.faces:
        f.material_index = 1 if 0.125 < abs(f.calc_center_median().x) < 0.155 else 0
    p.add(b, [BLANKET, BLANKET_S], (0, 0, 0.045))
    finalize(p, 'Blanket_Folded', c)
    return register(bpy.data.objects['Blanket_Folded'], 'Props')

def build_blanket_crumpled():
    """クロスシミュレーションで、雑に丸めて置いたブランケットを作る。"""
    c = collection('Blanket_Crumpled')
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=26, y_segments=34, size=0.5)
    ex = max(abs(v.co.x) for v in bm.verts)
    ey = max(abs(v.co.y) for v in bm.verts)
    for v in bm.verts:
        v.co.x *= 0.42 / ex
        v.co.y *= 0.55 / ey
    me = bpy.data.meshes.new('Blanket_Crumpled_sim')
    bm.to_mesh(me)
    bm.free()
    me.materials.append(BLANKET)
    me.materials.append(BLANKET_S)
    for poly in me.polygons:
        x = poly.center.x
        poly.material_index = 1 if any(abs(x - s) < 0.03 for s in (-0.3, 0.3)) else 0
    cloth = bpy.data.objects.new('Blanket_Crumpled', me)
    c.objects.link(cloth)
    cloth.location = (0.03, 0, 0.36)
    cloth.rotation_euler = (math.radians(6), math.radians(-8), math.radians(20))
    cols = []
    for (loc, dims) in (((0, 0, 0.09), (0.26, 0.24, 0.18)), ((0.12, 0.1, 0.06), (0.18, 0.16, 0.12)), ((-0.12, -0.1, 0.045), (0.16, 0.18, 0.09)), ((0, 0, -0.02), (3, 3, 0.04))):
        cm = bpy.data.meshes.new('_col')
        cb = soft(box(*dims, bev=min(dims) * 0.3), 2) if dims[0] < 1 else box(*dims)
        cb.to_mesh(cm)
        cb.free()
        co = bpy.data.objects.new('_col', cm)
        co.location = loc
        scene.collection.objects.link(co)
        co.modifiers.new('col', 'COLLISION')
        co.collision.thickness_outer = 0.006
        cols.append(co)
    md = cloth.modifiers.new('cloth', 'CLOTH')
    s = md.settings
    s.quality = 6
    s.mass = 0.25
    s.bending_stiffness = 0.2
    s.tension_stiffness = 12
    s.compression_stiffness = 12
    md.collision_settings.distance_min = 0.006
    md.collision_settings.use_self_collision = True
    md.collision_settings.self_distance_min = 0.006
    md.point_cache.frame_start = 1
    md.point_cache.frame_end = 90
    for f in range(1, 76):
        scene.frame_set(f)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = cloth.evaluated_get(dg)
    baked = bpy.data.meshes.new_from_object(ev)
    baked.transform(cloth.matrix_world)
    cloth.modifiers.clear()
    cloth.data = baked
    cloth.matrix_world = Matrix.Identity(4)
    for co in cols:
        m = co.data
        bpy.data.objects.remove(co)
        bpy.data.meshes.remove(m)
    bpy.data.meshes.remove(me)
    bm = bmesh.new()
    bm.from_mesh(baked)
    bm = apply_mod(bm, 'SMOOTH', factor=0.6, iterations=4)  # シミュレーションの細かい凹凸をならす
    bm = apply_mod(bm, 'SOLIDIFY', thickness=0.008, offset=0.0, use_even_offset=True)
    p = Part()
    p.bm.free()
    p.bm = bm
    p.mats = [BLANKET, BLANKET_S]
    p.ground()
    xs = [v.co.x for v in bm.verts]
    ys = [v.co.y for v in bm.verts]
    bmesh.ops.translate(bm, vec=(-(min(xs) + max(xs)) / 2, -(min(ys) + max(ys)) / 2, 0), verts=bm.verts)
    bpy.data.objects.remove(cloth)
    bpy.data.meshes.remove(baked)
    finalize(p, 'Blanket_Crumpled', c, smooth_angle=80)
    scene.frame_set(1)
    return register(bpy.data.objects['Blanket_Crumpled'], 'Props')

def build_cushion():
    c = collection('Cushion')
    bm = soft(box(0.42, 0.42, 0.17, bev=0.05), 2)
    for v in bm.verts:  # 縁ほど薄い枕形状
        e = max(abs(v.co.x), abs(v.co.y)) / 0.21
        v.co.z *= 1 - 0.55 * min(e, 1) ** 2
    p = Part()
    p.add(bm, CUSHION)
    finalize(p.ground(), 'Cushion', c, smooth_angle=80)
    return register(bpy.data.objects['Cushion'], 'Props')

def build_powerstrip():
    c = collection('PowerStrip')
    p = Part()
    p.add(box(0.30, 0.06, 0.035, bev=0.006, seg=2), PLASTIC_LT, (0, 0, 0.0175))
    for i in range(4):
        p.add(box(0.036, 0.032, 0.003), DARK, (-0.09 + i * 0.055, 0, 0.0355))
    p.add(box(0.03, 0.026, 0.008), CYAN, (0.125, 0, 0.037))
    p.add(tube([(-0.15, 0, 0.015), (-0.22, 0.03, 0.008), (-0.3, 0.02, 0.005), (-0.4, 0.06, 0.005)], 0.0045, 8), RUBBER)
    p.add(box(0.042, 0.048, 0.03, bev=0.005), CREAM, (-0.035, 0, 0.05))  # 挿さった充電器
    p.add(tube([(-0.035, 0.024, 0.055), (-0.03, 0.06, 0.04), (0.0, 0.1, 0.004), (0.06, 0.14, 0.003), (0.1, 0.12, 0.003)], 0.0022, 6), DARK)
    p.add(box(0.01, 0.022, 0.007), DARK, (0.11, 0.115, 0.0035), (0, 0, -30))
    finalize(p.ground(), 'PowerStrip', c)
    return register(bpy.data.objects['PowerStrip'], 'Props')

# ================================================================= 建築モジュール用テクスチャ（1枚＝2m四方でタイルする）
def blur_wrap(a, sigma):
    n = a.shape[0]
    f = np.fft.fftfreq(n)
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (f[:, None] ** 2 + f[None, :] ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g))

def noise(n, sigma, seed):
    a = blur_wrap(np.random.default_rng(seed).standard_normal((n, n)), sigma)
    return (a - a.mean()) / (a.std() + 1e-9)

def hexrgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])

def save_img(name, arr, noncolor=False):
    n = arr.shape[0]
    if arr.ndim == 2:
        arr = np.repeat(arr[:, :, None], 3, axis=2)
    rgba = np.concatenate([np.clip(arr, 0, 1), np.ones((n, n, 1))], axis=2)
    im = bpy.data.images.new(name, n, n, alpha=False)
    if noncolor:
        im.colorspace_settings.name = 'Non-Color'
    im.pixels.foreach_set(rgba.astype(np.float32).ravel())  # 行0が画像の下端（v=0）
    im.filepath_raw = TEX + '/' + name + '.png'
    im.file_format = 'PNG'
    im.save()
    return im

def normal_from_height(h, strength):
    """高さからNormal Mapを作る。Unityと同じOpenGL形式（緑=上）。"""
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) / 2
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) / 2
    nx, ny = -dx * strength, -dy * strength
    l = np.sqrt(nx ** 2 + ny ** 2 + 1)
    return np.stack([nx / l * 0.5 + 0.5, ny / l * 0.5 + 0.5, 1 / l * 0.5 + 0.5], axis=2)

def seam_lines(n, positions, w):
    idx = np.arange(n)
    m = np.zeros(n, bool)
    for p in positions:
        m |= np.abs(((idx - p + n / 2) % n) - n / 2) < w
    return m

def seg_mask(n, p0, p1, r):
    x0, x1 = int(max(min(p0[0], p1[0]) - r - 1, 0)), int(min(max(p0[0], p1[0]) + r + 2, n))
    y0, y1 = int(max(min(p0[1], p1[1]) - r - 1, 0)), int(min(max(p0[1], p1[1]) + r + 2, n))
    if x0 >= x1 or y0 >= y1:
        return None
    yy, xx = np.mgrid[y0:y1, x0:x1]
    ax, ay = p1[0] - p0[0], p1[1] - p0[1]
    t = np.clip(((xx - p0[0]) * ax + (yy - p0[1]) * ay) / max(ax * ax + ay * ay, 1e-9), 0, 1)
    d = np.hypot(xx - (p0[0] + t * ax), yy - (p0[1] + t * ay))
    return slice(y0, y1), slice(x0, x1), d <= r

def paint_seg(maps, p0, p1, r):
    """maps: [(配列, 関数(旧値)->新値), ...]"""
    sm = seg_mask(maps[0][0].shape[0], p0, p1, r)
    if sm is None:
        return
    ys, xs, mk = sm
    for arr, fn in maps:
        v = arr[ys, xs]
        v[mk] = fn(v[mk])

def floor_base(n=1024):
    col = hexrgb('#3d454e')[None, None, :] * (1 + 0.035 * noise(n, 60, 1) + 0.015 * noise(n, 6, 2))[:, :, None]
    h = 0.12 * noise(n, 1.5, 3)
    rough = 0.62 + 0.05 * noise(n, 30, 4)
    L = seam_lines(n, [0, n // 2], 2.5)  # タイル外周と1mごとの目地
    seam = L[None, :] | L[:, None]
    col[seam] *= 0.7
    h[seam] -= 1.0
    rough[seam] = 0.8
    return col, h, rough

def floor_damage(col, h, rough, seed=21):
    n = col.shape[0]
    rnd = np.random.default_rng(seed)
    scuff = noise(n, 18, seed + 1) > 1.6
    col[scuff] *= 0.84
    rough[scuff] += 0.1
    stain = noise(n, 30, seed + 2) > 1.9
    col[stain] = col[stain] * 0.5 + hexrgb('#4b4034') * 0.5
    light = hexrgb('#6c757e')
    for _ in range(70):  # 擦り傷
        x, y = rnd.uniform(0, n, 2)
        a, L = rnd.uniform(0, np.pi), rnd.uniform(15, 140)
        paint_seg([(col, lambda v: v * 0.5 + light * 0.5), (h, lambda v: v - 0.3), (rough, lambda v: v * 0 + 0.45)],
                  (x, y), (x + np.cos(a) * L, y + np.sin(a) * L), rnd.uniform(0.6, 1.4))
    # 補修プレート跡（テクスチャ上の四角い当て板とリベット）
    px0, px1, py0, py1 = int(0.58 * n), int(0.86 * n), int(0.14 * n), int(0.38 * n)
    col[py0:py1, px0:px1] = hexrgb('#4c5661') * (1 + 0.02 * noise(n, 8, seed + 3)[py0:py1, px0:px1, None])
    rough[py0:py1, px0:px1] = 0.5
    b = 5
    for sl in ((slice(py0, py0 + b), slice(px0, px1)), (slice(py1 - b, py1), slice(px0, px1)),
               (slice(py0, py1), slice(px0, px0 + b)), (slice(py0, py1), slice(px1 - b, px1))):
        col[sl] *= 0.8
        h[sl] += 0.8
    for (x, y) in ((px0 + 14, py0 + 14), (px1 - 14, py0 + 14), (px0 + 14, py1 - 14), (px1 - 14, py1 - 14),
                   ((px0 + px1) // 2, py0 + 14), ((px0 + px1) // 2, py1 - 14)):
        paint_seg([(col, lambda v: v * 0 + hexrgb('#6f7a85')), (h, lambda v: v + 1.2)], (x, y), (x, y), 5)
    # ひび
    x, y = 0.2 * n, 0.7 * n
    for _ in range(9):
        nx, ny = x + rnd.uniform(10, 40), y + rnd.uniform(-25, 25)
        paint_seg([(col, lambda v: v * 0.45), (h, lambda v: v - 0.8)], (x, y), (nx, ny), 1.2)
        x, y = nx, ny
    return col, h, np.clip(rough, 0, 1)

def wall_base(n=1024):
    col = hexrgb('#7c8794')[None, None, :] * (1 + 0.04 * noise(n, 80, 11) + 0.012 * noise(n, 5, 12))[:, :, None]  # 塗装ムラ
    h = 0.08 * noise(n, 1.2, 13)
    rough = 0.72 + 0.04 * noise(n, 40, 14)
    V = seam_lines(n, [0], 2.5)       # 壁モジュール同士の継ぎ目
    Hl = seam_lines(n, [0, n // 2], 2)  # 高さ1mごとのパネルライン
    seam = V[None, :] | Hl[:, None]
    col[seam] *= 0.78
    h[seam] -= 1.0
    return col, h, rough

def wall_repair(col, h, rough, seed=31):
    """補修跡（パテ埋めして色の合わない塗り直し）とビス穴跡。"""
    n = col.shape[0]
    yy, xx = np.mgrid[0:n, 0:n]
    wob = 6 * noise(n, 10, seed)
    m = (xx > 0.2 * n + wob) & (xx < 0.45 * n + wob) & (yy > 0.25 * n - wob) & (yy < 0.55 * n - wob)
    col[m] = col[m] * 0.4 + hexrgb('#95a0aa') * 0.6
    rough[m] = 0.6
    h[m] += 0.05 * noise(n, 3, seed + 1)[m]
    for (x, y) in ((0.62, 0.3), (0.62, 0.42), (0.75, 0.3), (0.75, 0.42)):
        paint_seg([(col, lambda v: v * 0 + hexrgb('#a3adb6')), (h, lambda v: v - 0.4)], (x * n, y * n), (x * n, y * n), 4)
    rnd = np.random.default_rng(seed + 2)
    for _ in range(12):
        x, y = rnd.uniform(0, n), rnd.uniform(0, 0.25 * n)
        a, L = rnd.uniform(-0.4, 0.4), rnd.uniform(20, 80)
        paint_seg([(col, lambda v: v * 0.8), (h, lambda v: v - 0.3)], (x, y), (x + np.cos(a) * L, y + np.sin(a) * L), 1.0)
    return col, h, rough

def ceiling_base(n=1024):
    col = hexrgb('#a2abb3')[None, None, :] * (1 + 0.02 * noise(n, 70, 41) + 0.01 * noise(n, 4, 42))[:, :, None]
    h = 0.06 * noise(n, 1.5, 43)
    rough = 0.85 + 0.03 * noise(n, 30, 44)
    L = seam_lines(n, [0], 2.5)
    seam = L[None, :] | L[:, None]
    col[seam] *= 0.8
    h[seam] -= 1.0
    return col, h, rough

def tex_mat(name, maps, avg_hex, strength=1.6):
    col, h, rough = maps
    bc = save_img('T_Room_%s_BaseColor' % name, col)
    nm = save_img('T_Room_%s_Normal' % name, normal_from_height(h, strength), True)
    rg = save_img('T_Room_%s_Roughness' % name, np.clip(rough, 0, 1), True)
    m = mat(name, avg_hex)
    nt = m.node_tree
    b = next(nd for nd in nt.nodes if nd.type == 'BSDF_PRINCIPLED')
    t1 = nt.nodes.new('ShaderNodeTexImage'); t1.image = bc
    t2 = nt.nodes.new('ShaderNodeTexImage'); t2.image = rg
    t3 = nt.nodes.new('ShaderNodeTexImage'); t3.image = nm
    nmap = nt.nodes.new('ShaderNodeNormalMap')
    nt.links.new(t1.outputs['Color'], b.inputs['Base Color'])
    nt.links.new(t2.outputs['Color'], b.inputs['Roughness'])
    nt.links.new(t3.outputs['Color'], nmap.inputs['Color'])
    nt.links.new(nmap.outputs['Normal'], b.inputs['Normal'])
    nt.nodes.active = t1
    PALETTE[m.name]['textures'] = [os.path.basename(i.filepath_raw) for i in (bc, nm, rg)]
    return m

FLOOR_M   = tex_mat('Floor', floor_base(), '#3d454e')
FLOOR_DMG = tex_mat('FloorDamaged', floor_damage(*floor_base()), '#3d454e')
WALL_M    = tex_mat('Wall', wall_base(), '#7c8794')
WALL_REP  = tex_mat('WallRepaired', wall_repair(*wall_base()), '#7c8794')  # Unity上で壁のマテリアルを差し替える用
CEIL_M    = tex_mat('Ceiling', ceiling_base(), '#a2abb3', strength=1.2)

# ================================================================= 建築モジュール
# 規格: グリッド2m。床・天井は2m×2m、壁は幅2m×高さ2.5m×厚さ0.15m。
# 床: 原点＝上面の中心。天井: 原点＝下面（見える面）の中心で、高さ2.5mに置く。
# 壁: 原点＝室内側の面の下端中央。厚みは原点から+Y（室外側）へ伸びる。室内側の面同士が角でぴったり接する。
# 窓・ドア・ドア枠: 原点が壁と同じなので、壁と同じ位置・回転に置けば開口に収まる。
GRID, WALL_H, WALL_T, FLOOR_T, CEIL_T = 2.0, 2.5, 0.15, 0.1, 0.06
OPEN_WIN = (-0.5, 0.5, 0.95, 1.85)
OPEN_WIN_L = (-0.8, 0.8, 0.6, 2.0)
OPEN_DOOR = (-0.5, 0.5, 0.0, 2.15)

def build_floor(name, fm, hatch=False):
    c = collection(name)
    p = Part()
    def slab(x0, x1, y0, y1):
        p.add(box(x1 - x0, y1 - y0, FLOOR_T, bev=0.004), fm, ((x0 + x1) / 2, (y0 + y1) / 2, -FLOOR_T / 2))
    if not hatch:
        slab(-1, 1, -1, 1)
        return register(finalize(p, name, c, uv='box'), 'Architecture')
    hx0, hx1, hy0, hy1 = 0.2, 0.8, -0.3, 0.3
    slab(-1, hx0, -1, 1); slab(hx1, 1, -1, 1); slab(hx0, hx1, -1, hy0); slab(hx0, hx1, hy1, 1)
    p.add(vessel(box(hx1 - hx0, hy1 - hy0, 0.25), 0.012), DARK, ((hx0 + hx1) / 2, 0, -0.125))  # 点検口の中（配線ピット）
    R = empty(name, c, size=0.3)
    finalize(p, name + '_Floor', c, R, uv='box')
    q = Part()  # 点検ハッチ：奥の辺がヒンジ（X回転で開く）
    q.add(box(hx1 - hx0 - 0.034, hy1 - hy0 - 0.034, 0.03, bev=0.004), fm, ((hx0 + hx1) / 2, 0, -0.015))
    q.add(box(0.12, 0.03, 0.004, bev=0.001), DARK, ((hx0 + hx1) / 2, hy0 + 0.06, 0.0015))
    finalize(q, name + '_Hatch', c, R, pivot=((hx0 + hx1) / 2, hy1 - 0.017, 0), uv='box')
    return register(R, 'Architecture')

def wall_part(opening=None):
    p = Part()
    def slab(x0, x1, z0, z1):
        if x1 - x0 > 1e-4 and z1 - z0 > 1e-4:
            p.add(box(x1 - x0, WALL_T, z1 - z0), WALL_M, ((x0 + x1) / 2, WALL_T / 2, (z0 + z1) / 2))
    if opening:
        x0, x1, z0, z1 = opening
        slab(-1, x0, 0, WALL_H); slab(x1, 1, 0, WALL_H); slab(x0, x1, 0, z0); slab(x0, x1, z1, WALL_H)
        segs = [(-1, 1)] if z0 > 0 else [(-1, x0), (x1, 1)]
    else:
        slab(-1, 1, 0, WALL_H)
        segs = [(-1, 1)]
    for a, b in segs:  # 巾木
        p.add(box(b - a, 0.012, 0.08), BASEBOARD, ((a + b) / 2, -0.006, 0.04))
    return p

def build_wall(name, opening=None, extra=None):
    c = collection(name)
    p = wall_part(opening)
    if extra:
        extra(p)
    return register(finalize(p, name, c, uv='box'), 'Architecture')

def wall_panel_extra(p):
    p.add(box(0.5, 0.03, 0.7, bev=0.006), STEEL, (0.45, -0.015, 1.35))     # 設備パネルの枠
    p.add(box(0.44, 0.01, 0.64, bev=0.004), PANEL, (0.45, -0.035, 1.35))   # 扉
    p.add(box(0.1, 0.006, 0.025, bev=0.002), DARK, (0.6, -0.041, 1.35))    # 取っ手
    p.add(box(0.03, 0.004, 0.012), CYAN, (0.29, -0.0415, 1.63))
    p.add(box(0.16, 0.03, 0.09, bev=0.006), STEEL, (-0.55, -0.015, 0.3))   # 配線口
    p.add(box(0.1, 0.004, 0.035), RUBBER, (-0.55, -0.031, 0.3))

def build_ceiling(name, light=False):
    c = collection(name)
    p = Part()
    p.add(box(GRID, GRID, CEIL_T, bev=0.004), CEIL_M, (0, 0, CEIL_T / 2))
    if light:
        p.add(box(1.2, 0.32, 0.05, bev=0.01), PLASTIC_LT, (0, 0, -0.025))
        p.add(box(1.12, 0.24, 0.006), CEIL_LAMP, (0, 0, -0.0515))
    return register(finalize(p, name, c, uv='box'), 'Architecture')

def build_window(name, opening, large=False):
    c = collection(name)
    R = empty(name, c, size=0.2)
    x0, x1, z0, z1 = opening
    w, h = x1 - x0, z1 - z0
    fw, d, yc = (0.06 if large else 0.05), 0.1, WALL_T / 2
    f, g = Part(), Part()
    f.add(box(fw, d, h, bev=0.004), FRAME, (x0 + fw / 2, yc, (z0 + z1) / 2))
    f.add(box(fw, d, h, bev=0.004), FRAME, (x1 - fw / 2, yc, (z0 + z1) / 2))
    f.add(box(w - 2 * fw, d, fw, bev=0.004), FRAME, ((x0 + x1) / 2, yc, z0 + fw / 2))
    f.add(box(w - 2 * fw, d, fw, bev=0.004), FRAME, ((x0 + x1) / 2, yc, z1 - fw / 2))
    f.add(box(w + 0.12, 0.07, 0.03, bev=0.006), FRAME, ((x0 + x1) / 2, -0.02, z0))  # 室内側の窓台
    tw = 0.04  # 室内側の額縁
    f.add(box(tw, 0.012, h + tw), FRAME, (x0 - tw / 2, -0.006, (z0 + z1 + tw) / 2))
    f.add(box(tw, 0.012, h + tw), FRAME, (x1 + tw / 2, -0.006, (z0 + z1 + tw) / 2))
    f.add(box(w + 2 * tw, 0.012, tw), FRAME, ((x0 + x1) / 2, -0.006, z1 + tw / 2))
    ix0, ix1, iz0, iz1 = x0 + fw, x1 - fw, z0 + fw, z1 - fw
    sp = 0.035
    def sash(sx0, sx1, sz0, sz1, y):
        f.add(box(sp, 0.03, sz1 - sz0), FRAME, (sx0 + sp / 2, y, (sz0 + sz1) / 2))
        f.add(box(sp, 0.03, sz1 - sz0), FRAME, (sx1 - sp / 2, y, (sz0 + sz1) / 2))
        f.add(box(sx1 - sx0 - 2 * sp, 0.03, sp), FRAME, ((sx0 + sx1) / 2, y, sz0 + sp / 2))
        f.add(box(sx1 - sx0 - 2 * sp, 0.03, sp), FRAME, ((sx0 + sx1) / 2, y, sz1 - sp / 2))
        g.add(box(sx1 - sx0 - 2 * sp, 0.006, sz1 - sz0 - 2 * sp), GLASS, ((sx0 + sx1) / 2, y, (sz0 + sz1) / 2))
    if not large:  # 引き違いの2枚窓
        mid = (ix0 + ix1) / 2
        sash(ix0, mid + 0.025, iz0, iz1, 0.055)
        sash(mid - 0.025, ix1, iz0, iz1, 0.095)
        f.add(box(0.015, 0.02, 0.09, bev=0.003), DARK, (mid + 0.008, 0.03, (iz0 + iz1) / 2))
    else:  # 上ははめ殺し、下は換気用の小窓
        tz = z0 + 0.42
        f.add(box(w - 2 * fw, d * 0.8, 0.05, bev=0.004), FRAME, ((x0 + x1) / 2, yc, tz))
        g.add(box(ix1 - ix0, 0.008, iz1 - (tz + 0.025)), GLASS, ((x0 + x1) / 2, yc, (tz + 0.025 + iz1) / 2))
        sash(ix0, ix1, iz0, tz - 0.025, 0.06)
        f.add(box(0.12, 0.02, 0.015, bev=0.003), DARK, ((x0 + x1) / 2, 0.035, tz - 0.07))
    finalize(f, name + '_Frame', c, R)
    go = finalize(g, name + '_Glass', c, R, uv='keep')
    planar_uv(go, x0, w, z0, h)
    return register(R, 'Architecture')

def build_doorframe():
    c = collection('DoorFrame')
    x0, x1, z0, z1 = OPEN_DOOR
    p = Part()
    lt = 0.02
    p.add(box(lt, WALL_T, z1), FRAME, (x0 + lt / 2, WALL_T / 2, z1 / 2))
    for ya, yb in ((0, 0.045), (0.105, WALL_T)):  # 戸袋側（+X）はドアが通る隙間を空ける
        p.add(box(lt, yb - ya, z1), FRAME, (x1 - lt / 2, (ya + yb) / 2, z1 / 2))
    p.add(box(x1 - x0, WALL_T, lt), FRAME, ((x0 + x1) / 2, WALL_T / 2, z1 - lt / 2))
    cw = 0.07
    for y in (-0.0075, WALL_T + 0.0075):  # 室内側・室外側の額縁
        p.add(box(cw, 0.015, z1 + cw), FRAME, (x0 - cw / 2, y, (z1 + cw) / 2))
        p.add(box(cw, 0.015, z1 + cw), FRAME, (x1 + cw / 2, y, (z1 + cw) / 2))
        p.add(box(x1 - x0, 0.015, cw), FRAME, ((x0 + x1) / 2, y, z1 + cw / 2))
    p.add(box(0.08, 0.02, 0.14, bev=0.005), DARK, (x1 + 0.16, -0.01, 1.1))  # 開閉パネル
    p.add(box(0.05, 0.004, 0.05), CYAN, (x1 + 0.16, -0.0215, 1.13))
    return register(finalize(p, 'DoorFrame', c), 'Architecture')

def build_door():
    c = collection('Door')  # 引き戸。ローカル+X方向へ約1m動かすと壁の中へ収まる
    yc = WALL_T / 2
    p = Part()
    p.add(box(0.96, 0.05, 2.12, bev=0.006), DOOR, (0, yc, 0.005 + 1.06))
    for s in (-1, 1):
        y = yc + s * 0.025
        p.add(box(0.8, 0.004, 0.95, bev=0.002), DOOR, (0, y + s * 0.002, 1.55))
        p.add(box(0.8, 0.004, 0.8, bev=0.002), DOOR, (0, y + s * 0.002, 0.55))
        p.add(box(0.03, 0.006, 0.32, bev=0.002), DARK, (-0.4, y + s * 0.003, 1.05))  # 引き手
    return register(finalize(p, 'Door', c), 'Architecture')

# ---- 壁付けProp（原点＝壁に接する背面の中心、正面は-Y）
def build_wallvent():
    c = collection('WallVent')
    W, H, D = 0.4, 0.25, 0.03
    p = Part()
    p.add(box(W, 0.004, H), DARK, (0, -0.002, 0))
    for sx, sz, x, z in ((W, 0.03, 0, H / 2 - 0.015), (W, 0.03, 0, -H / 2 + 0.015),
                         (0.03, H - 0.06, -W / 2 + 0.015, 0), (0.03, H - 0.06, W / 2 - 0.015, 0)):
        p.add(box(sx, D, sz, bev=0.004), STEEL_LT, (x, -D / 2, z))
    for i in range(5):
        p.add(box(W - 0.06, 0.004, 0.035), STEEL_LT, (0, -0.016, -0.075 + i * 0.0375), (35, 0, 0))
    return register(finalize(p, 'WallVent', c), 'ArchitectureProps')

def build_outlet():
    c = collection('WallOutlet')
    p = Part()
    p.add(box(0.075, 0.012, 0.12, bev=0.004), PLASTIC_LT, (0, -0.006, 0))
    for z in (-0.028, 0.028):
        p.add(box(0.042, 0.004, 0.036, bev=0.002), DARK, (0, -0.0125, z))
    p.add(box(0.008, 0.003, 0.004), CYAN, (0.028, -0.013, 0.052))
    return register(finalize(p, 'WallOutlet', c), 'ArchitectureProps')

def build_switch():
    c = collection('LightSwitch')
    p = Part()
    p.add(box(0.075, 0.012, 0.12, bev=0.004), PLASTIC_LT, (0, -0.006, 0))
    p.add(box(0.04, 0.008, 0.065, bev=0.003), PLASTIC_LT, (0, -0.014, 0), (6, 0, 0))
    p.add(box(0.006, 0.002, 0.006), CYAN, (0, -0.0185, 0.022))
    return register(finalize(p, 'LightSwitch', c), 'ArchitectureProps')

def build_cablecover():
    c = collection('CableCover')  # 1mの直線モール。原点は片端の背面中心で、+X方向へ伸びる（つなげる時は1mずつ動かす）
    p = Part()
    p.add(box(1.0, 0.02, 0.04, bev=0.006, seg=2), PLASTIC_LT, (0.5, -0.01, 0))
    return register(finalize(p, 'CableCover', c), 'ArchitectureProps')

ARCH_BUILDERS = [
    lambda: build_floor('Floor_Normal', FLOOR_M),
    lambda: build_floor('Floor_Damaged', FLOOR_DMG),
    lambda: build_floor('Floor_Service', FLOOR_M, hatch=True),
    lambda: build_wall('Wall_Normal'),
    lambda: build_wall('Wall_Panel', extra=wall_panel_extra),
    lambda: build_wall('Wall_Window', OPEN_WIN),
    lambda: build_wall('Wall_Window_Large', OPEN_WIN_L),
    lambda: build_wall('Wall_Door', OPEN_DOOR),
    lambda: build_ceiling('Ceiling_Normal'),
    lambda: build_ceiling('Ceiling_Light', light=True),
    lambda: build_window('Window_Normal', OPEN_WIN),
    lambda: build_window('Window_Large', OPEN_WIN_L, large=True),
    build_door, build_doorframe, build_wallvent, build_outlet, build_switch, build_cablecover,
]

# ================================================================= 生成
builders = [build_chair, build_machine, build_rack,
            build_sofa, build_lowtable, build_desk, build_monitor_a, build_monitor_b, build_desklight,
            build_cupnoodle, build_chopsticks, build_fork, build_can, build_can_crushed, build_mug, build_controller,
            build_mobile, build_trashbin, build_storage, build_blanket_folded, build_blanket_crumpled, build_cushion, build_powerstrip]
for b in ARCH_BUILDERS + builders:
    b()
smart_uv_all()

# ================================================================= 経年劣化テクスチャのベイク（家具・小物・窓枠・ドア・壁付けProp）
# 手続き的な劣化（角の塗装剥がれ・くぼみの汚れ・擦り傷・布目と上面の擦れ・テープなどの描き込み）をCyclesで焼き込み、
# アセットごとに BaseColor / Roughness / Normal の3枚を作る。メッシュには一切手を加えない。
SPECIAL = {'M_Room_CyanGlow', 'M_Room_LampWarm', 'M_Room_CeilingLight', 'M_Room_Screen', 'M_Room_Glass', 'M_Room_NoodleContents'}
CATEGORY = {
    'fabric': {'M_Room_SofaFabric', 'M_Room_SofaBase', 'M_Room_CushionFabric', 'M_Room_BlanketFabric', 'M_Room_BlanketStripe'},
    'leather': {'M_Room_SeatPad'},
    'metal': {'M_Room_Metal'},
    'plastic': {'M_Room_PlasticLight', 'M_Room_Rubber', 'M_Room_CupWhite', 'M_Room_CupRed', 'M_Room_ForkPlastic', 'M_Room_Chopstick',
                'M_Room_MugCeramic', 'M_Room_Cream', 'M_Room_AccentOrange', 'M_Room_StorageBox', 'M_Room_TrashBin', 'M_Room_TableResin',
                'M_Room_LidFoil', 'M_Room_CanBody', 'M_Room_Paper', 'M_Room_CoffeeStain'},
}
# 係数: 角の剥がれ / くぼみの汚れ / 擦り傷 / 上面の擦れ / 表面の粒度 / 粗さの基準 / 剥がれた所の色
WEAR = {
    'painted': dict(edge=0.8, dirt=0.55, scratch=0.6, worn=0.0, grain=0.15, rough=0.6, reveal='#8b949d'),
    'metal':   dict(edge=0.2, dirt=0.4, scratch=0.9, worn=0.0, grain=0.1, rough=0.42, reveal='#aab1b8'),
    'plastic': dict(edge=0.25, dirt=0.4, scratch=0.25, worn=0.0, grain=0.1, rough=0.6, reveal=None),
    'leather': dict(edge=0.8, dirt=0.5, scratch=0.25, worn=0.9, grain=0.45, rough=0.55, reveal=None),
    'fabric':  dict(edge=0.4, dirt=0.6, scratch=0.0, worn=1.0, grain=0.8, rough=0.95, reveal=None),
}
# 描き込み（アセット座標の軸平行な箱に入る面へ色を塗る）: オブジェクト名, 中心, 半径, 色
DECALS = [
    ('Chair_Seat', (0.1, -0.1, 0.551), (0.06, 0.1, 0.03), '#8e8f86'),               # ガムテープ補修
    ('Sofa_Frame', (0.73, -0.05, 0.46), (0.03, 0.075, 0.04), '#8e8f86'),
    ('Monitor_B_Body', (-0.19, 0.01, 0.45), (0.035, 0.04, 0.014), '#8e8f86'),
    ('HeadSwapMachine_Pillar', (0, 0.036, 0.72), (0.085, 0.03, 0.035), '#8e8f86'),
    ('HeadSwapMachine_Base', (0, -0.28, 0.083), (0.34, 0.02, 0.01), '#c9a13a'),     # 床置き機材の注意表示
    ('HeadSwapMachine_Boom', (0, 0.1, 2.243), (0.06, 0.1, 0.01), '#c9a13a'),
    ('StorageBox_Body', (-0.08, -0.151, 0.12), (0.05, 0.01, 0.03), '#d9c7a0'),      # 紙ラベル（文字なし）
]

def category(m):
    for k, v in CATEGORY.items():
        if m.name in v:
            return k
    return 'painted'

class NB:
    """シェーダーノードを短く組むための小さなヘルパー。"""
    def __init__(self, nt):
        self.nt = nt
    def n(self, kind, **props):
        nd = self.nt.nodes.new(kind)
        for k, v in props.items():
            setattr(nd, k, v)
        return nd
    def link(self, a, b):
        self.nt.links.new(a, b)
    def math(self, op, a, b=None, clamp=False):
        nd = self.n('ShaderNodeMath', operation=op, use_clamp=clamp)
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                nd.inputs[i].default_value = v
            else:
                self.link(v, nd.inputs[i])
        return nd.outputs[0]
    def remap(self, v, fmin, fmax, tmin=0.0, tmax=1.0):
        nd = self.n('ShaderNodeMapRange')
        self.link(v, nd.inputs['Value'])
        nd.inputs['From Min'].default_value, nd.inputs['From Max'].default_value = fmin, fmax
        nd.inputs['To Min'].default_value, nd.inputs['To Max'].default_value = tmin, tmax
        return nd.outputs['Result']
    def noise(self, vec, scale, detail=4.0, rough=0.55, stretch=None):
        if stretch:
            mp = self.n('ShaderNodeMapping')
            mp.inputs['Scale'].default_value = stretch
            self.link(vec, mp.inputs['Vector'])
            vec = mp.outputs['Vector']
        nd = self.n('ShaderNodeTexNoise')
        self.link(vec, nd.inputs['Vector'])
        nd.inputs['Scale'].default_value, nd.inputs['Detail'].default_value, nd.inputs['Roughness'].default_value = scale, detail, rough
        return nd.outputs['Fac']
    def mix(self, fac, a, b):
        nd = self.n('ShaderNodeMix', data_type='RGBA', blend_type='MIX')
        fi, ai, bi = nd.inputs[0], nd.inputs[6], nd.inputs[7]
        for sock, v in ((fi, fac), (ai, a), (bi, b)):
            if isinstance(v, (int, float)):
                sock.default_value = v
            elif isinstance(v, tuple):
                sock.default_value = v
            else:
                self.link(v, sock)
        return nd.outputs[2]

def scale_col(c, k, warm=0.0):
    return (min(c[0] * k + warm, 1), min(c[1] * k, 1), min(c[2] * k - warm * 0.5, 1), 1.0)

def make_bake_mat(src, obj):
    """元の単色マテリアルから、劣化を足したベイク用マテリアルを作る。"""
    m = bpy.data.materials.new(src.name + '__bake')
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    out = next(nd for nd in nt.nodes if nd.type == 'OUTPUT_MATERIAL')
    for nd in list(nt.nodes):
        if nd != out:
            nt.nodes.remove(nd)
    B = NB(nt)
    bsdf = B.n('ShaderNodeBsdfPrincipled')
    B.link(bsdf.outputs[0], out.inputs['Surface'])
    img_node = B.n('ShaderNodeTexImage')
    nt.nodes.active = img_node
    base = tuple(src.diffuse_color)
    if src.name in SPECIAL:  # 発光・画面・ガラスは劣化させない（最終的には元のマテリアルに戻す）
        bsdf.inputs['Base Color'].default_value = base
        return m, img_node
    w = WEAR[category(src)]
    co = B.n('ShaderNodeTexCoord').outputs['Object']
    geo = B.n('ShaderNodeNewGeometry')
    bev = B.n('ShaderNodeBevel', samples=8)
    bev.inputs['Radius'].default_value = 0.004
    ao = B.n('ShaderNodeAmbientOcclusion', samples=8, only_local=True)
    ao.inputs['Distance'].default_value = 0.04
    dot = B.n('ShaderNodeVectorMath', operation='DOT_PRODUCT')
    B.link(bev.outputs['Normal'], dot.inputs[0]); B.link(geo.outputs['Normal'], dot.inputs[1])
    edge = B.remap(dot.outputs['Value'], 0.985, 0.8)                     # 角ほど1
    cavity = B.remap(ao.outputs['AO'], 0.95, 0.45)                        # くぼみほど1
    nL = B.noise(co, 5.0, 6.0, 0.6)
    chip = B.math('MULTIPLY', edge, B.remap(B.noise(co, 14.0, 5.0, 0.7), 0.45, 0.62))
    nS = B.noise(co, 90.0, 2.0)
    sc_line = B.remap(B.math('ABSOLUTE', B.math('SUBTRACT', B.noise(co, 9.0, 2.0, 0.4), 0.5)), 0.0, 0.006, 1.0, 0.0)
    sc_mask = B.remap(B.noise(co, 3.0, 2.0), 0.52, 0.66)
    scratch = B.math('MULTIPLY', sc_line, sc_mask)
    up = B.n('ShaderNodeSeparateXYZ'); B.link(geo.outputs['Normal'], up.inputs[0])
    upm = B.math('MULTIPLY', B.remap(up.outputs['Z'], 0.4, 1.0), B.remap(B.noise(co, 4.0, 3.0), 0.35, 0.65))
    # 色
    col = B.mix(nL, scale_col(base, 0.9), scale_col(base, 1.08))
    col = B.mix(B.math('MULTIPLY', cavity, w['dirt']), col, scale_col(base, 0.5, 0.02))
    if w['worn']:
        col = B.mix(B.math('MULTIPLY', upm, w['worn']), col, scale_col(base, 1.25))
    if w['reveal']:
        col = B.mix(B.math('MULTIPLY', chip, w['edge']), col, srgb(w['reveal']))
    else:
        col = B.mix(B.math('MULTIPLY', chip, w['edge'] * 0.6), col, scale_col(base, 1.2))
    if w['scratch']:
        col = B.mix(B.math('MULTIPLY', scratch, w['scratch'] * 0.7), col, scale_col(base, 1.5) if not w['reveal'] else srgb(w['reveal']))
    piv = Vector(PIV.get(obj.name, (0, 0, 0)))
    for oname, c, hs, hexc in DECALS:
        if oname != obj.name:
            continue
        d = B.n('ShaderNodeVectorMath', operation='SUBTRACT')
        B.link(co, d.inputs[0]); d.inputs[1].default_value = tuple(Vector(c) - piv)
        a = B.n('ShaderNodeVectorMath', operation='ABSOLUTE'); B.link(d.outputs[0], a.inputs[0])
        sx = B.n('ShaderNodeSeparateXYZ'); B.link(a.outputs[0], sx.inputs[0])
        mk = B.math('MULTIPLY', B.math('MULTIPLY', B.math('LESS_THAN', sx.outputs['X'], hs[0]), B.math('LESS_THAN', sx.outputs['Y'], hs[1])),
                    B.math('LESS_THAN', sx.outputs['Z'], hs[2]))
        col = B.mix(mk, col, srgb(hexc))
    B.link(col, bsdf.inputs['Base Color'])
    # 粗さ
    r = B.math('ADD', w['rough'], B.math('MULTIPLY', B.math('SUBTRACT', nS, 0.5), 0.12))
    r = B.math('ADD', r, B.math('MULTIPLY', cavity, 0.1))
    r = B.math('SUBTRACT', r, B.math('MULTIPLY', B.math('ADD', chip, scratch), 0.25 if w['reveal'] else 0.1), clamp=True)
    B.link(r, bsdf.inputs['Roughness'])
    # 凹凸（Normal Map用。形は変えず、細かい面の情報だけ）
    h = B.math('MULTIPLY', nS, w['grain'])
    if category(src) == 'fabric':
        weave = B.n('ShaderNodeTexWave', wave_type='BANDS', bands_direction='DIAGONAL')
        B.link(co, weave.inputs['Vector']); weave.inputs['Scale'].default_value = 180.0
        h = B.math('ADD', h, B.math('MULTIPLY', weave.outputs['Fac'], 1.0))
    h = B.math('SUBTRACT', h, B.math('MULTIPLY', scratch, 0.6))
    h = B.math('SUBTRACT', h, B.math('MULTIPLY', chip, 0.3 * w['edge']))
    bump = B.n('ShaderNodeBump')
    bump.inputs['Strength'].default_value, bump.inputs['Distance'].default_value = 0.35, 0.002
    B.link(h, bump.inputs['Height']); B.link(bev.outputs['Normal'], bump.inputs['Normal'])
    B.link(bump.outputs['Normal'], bsdf.inputs['Normal'])
    return m, img_node

def setup_cycles():
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 16
    try:
        pref = bpy.context.preferences.addons['cycles'].preferences
        pref.compute_device_type = 'OPTIX'
        pref.get_devices()
        for d in pref.devices:
            d.use = d.type == 'OPTIX'
        scene.cycles.device = 'GPU'
    except Exception:
        scene.cycles.device = 'CPU'
    scene.render.bake.margin = 8
    scene.render.bake.margin_type = 'EXTEND'

TEX_ASSET = TEX + '/Assets'

def bake_asset(name, root):
    objs = asset_smart_meshes(root)
    if not objs:
        return None
    res = bake_res(objs)
    imgs = {k: bpy.data.images.new('T_%s_%s' % (name, k), res, res, alpha=False) for k in ('BaseColor', 'Roughness', 'Normal')}
    imgs['Roughness'].colorspace_settings.name = 'Non-Color'
    imgs['Normal'].colorspace_settings.name = 'Non-Color'
    originals, img_nodes = {}, []
    for o in objs:
        originals[o] = list(o.data.materials)
        for i, src in enumerate(originals[o]):
            bm_, node = make_bake_mat(src, o)
            o.data.materials[i] = bm_
            img_nodes.append(node)
    for s_ in bpy.context.view_layer.objects:
        s_.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    for key, kind, extra in (('BaseColor', 'DIFFUSE', dict(pass_filter={'COLOR'})), ('Roughness', 'ROUGHNESS', {}),
                             ('Normal', 'NORMAL', dict(normal_space='TANGENT'))):
        for nd in img_nodes:
            nd.image = imgs[key]
        bpy.ops.object.bake(type=kind, use_clear=True, margin=8, **extra)
        im = imgs[key]
        im.filepath_raw = '%s/T_%s_%s.png' % (TEX_ASSET, name, key)
        im.file_format = 'PNG'
        im.save()
    # 仕上げ：劣化を焼いたマテリアル1つ＋発光・画面などの特殊マテリアルだけにまとめる
    fm = bpy.data.materials.new('M_' + name)
    if fm.node_tree is None:
        fm.use_nodes = True
    nt = fm.node_tree
    b = next(nd for nd in nt.nodes if nd.type == 'BSDF_PRINCIPLED')
    t1 = nt.nodes.new('ShaderNodeTexImage'); t1.image = imgs['BaseColor']
    t2 = nt.nodes.new('ShaderNodeTexImage'); t2.image = imgs['Roughness']
    t3 = nt.nodes.new('ShaderNodeTexImage'); t3.image = imgs['Normal']
    nm = nt.nodes.new('ShaderNodeNormalMap')
    nt.links.new(t1.outputs['Color'], b.inputs['Base Color'])
    nt.links.new(t2.outputs['Color'], b.inputs['Roughness'])
    nt.links.new(t3.outputs['Color'], nm.inputs['Color'])
    nt.links.new(nm.outputs['Normal'], b.inputs['Normal'])
    nt.nodes.active = t1
    fm.diffuse_color = (0.18, 0.2, 0.23, 1.0)
    PALETTE[fm.name] = {'textures': [os.path.basename(i.filepath_raw) for i in imgs.values()], 'resolution': res}
    for o in objs:
        me = o.data
        new, remap = [], {}
        for i, src in enumerate(originals[o]):
            tgt = src if src.name in SPECIAL else fm
            if tgt not in new:
                new.append(tgt)
            remap[i] = new.index(tgt)
        idx = [remap[p.material_index] for p in me.polygons]
        me.materials.clear()
        for t in new:
            me.materials.append(t)
        for p, i in zip(me.polygons, idx):
            p.material_index = i
    for m in [m for m in bpy.data.materials if m.name.endswith('__bake')]:
        bpy.data.materials.remove(m)
    return res

BAKE_LOG = {}
if os.environ.get('ROOM_BAKE', '1') != '0':
    os.makedirs(TEX_ASSET, exist_ok=True)
    setup_cycles()
    for name, root, group in ASSETS:
        r = bake_asset(name, root)
        if r:
            BAKE_LOG[name] = r
            print('BAKED', name, r, flush=True)

# ================================================================= FBX書き出し（各アセット原点のまま）
def hier(root):
    return [root] + list(root.children_recursive)

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons) if o.type == 'MESH' else 0

bpy.context.view_layer.update()
stats = []
for name, root, group in ASSETS:
    objs = hier(root)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    path = '%s/%s/%s.fbx' % (EXP, group, name)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, mesh_smooth_type='OFF', use_mesh_modifiers=True,
                             add_leaf_bones=False, use_custom_props=False, path_mode='RELATIVE')
    pts = [o.matrix_world @ Vector(cn) for o in objs if o.type == 'MESH' for cn in o.bound_box]
    size = [round(max(p[i] for p in pts) - min(p[i] for p in pts), 3) for i in range(3)]
    stats.append({'asset': name, 'group': group, 'fbx': os.path.relpath(path, OUT).replace('\\', '/'),
                  'objects': [o.name for o in objs], 'tris': sum(tris(o) for o in objs),
                  'size_xyz_m': size, 'materials': sorted({m.name for o in objs if o.type == 'MESH' for m in o.data.materials})})

# ================================================================= プレビュー配置（Blend保存・レンダー用。FBXには影響しない）
def place(name, loc, rz=0, parent_obj=None):
    o = bpy.data.objects[name]
    o.location = loc
    o.rotation_euler = (0, 0, math.radians(rz))
    return o

def dup(name, loc, rz=0, hide=()):
    src = bpy.data.objects[name]
    c = collection(name + '_PreviewCopy')
    mp = {}
    for o in hier(src):
        n = o.copy()
        c.objects.link(n)
        mp[o] = n
    for o, n in mp.items():
        if o.parent in mp:
            n.parent = mp[o.parent]
        n.hide_render = n.hide_viewport = any(o.name.endswith(h) for h in hide)
    mp[src].location = loc
    mp[src].rotation_euler = (0, 0, math.radians(rz))
    return mp[src]

place('Chair', (0, 0, 0))
place('HeadSwapMachine', (0, 0.87, 0))
place('HeadRack', (1.15, 0.55, 0))
bpy.context.view_layer.update()
rack = bpy.data.objects['HeadRack']
REAL_HEAD = 'D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926/Export/ElseIf_HeadPart_Monitor01_NoCable.fbx'
if os.path.exists(REAL_HEAD):  # 収まり確認用。blendにだけ残り、FBXには含まない
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=REAL_HEAD)
    ch = collection('_RealHeadPreview')
    for o in set(bpy.data.objects) - before:
        for uc in list(o.users_collection):
            uc.objects.unlink(o)
        ch.objects.link(o)
        if o.parent is None:
            s = bpy.data.objects['HeadSocket_02']
            o.location = rack.location + s.location
place('Sofa', (2.75, 0.35, 0))
place('LowTable', (2.75, -0.55, 0))
place('Desk', (-1.75, 0.4, 0))
place('Monitor_A', (-1.95, 0.55, 0.74))
place('Monitor_B', (-1.42, 0.5, 0.74), -22)
place('DeskLight', (-2.25, 0.58, 0.74), 20)
place('Blanket_Crumpled', (3.0, 0.25, 0.405), 30)
place('Cushion', (2.33, 0.3, 0.41), 10)
props_row = ['CupNoodle', 'Utensil_Chopsticks', 'Utensil_Fork', 'DrinkCan', 'DrinkCan_Crushed', 'Mug', 'GameController',
             'MobileDevice', 'PowerStrip', 'TrashBin', 'StorageBox', 'Blanket_Folded']
x = -1.2
for n in props_row:
    w = next(s['size_xyz_m'][0] for s in stats if s['asset'] == n)
    x += w / 2
    place(n, (x, -1.6, 0))
    x += w / 2 + 0.12
bpy.data.objects['CupNoodle_Contents_Half'].hide_render = True
bpy.data.objects['CupNoodle_Lid_Open'].hide_render = True
bpy.data.objects['CupNoodle_Contents_Full'].hide_render = True
dup('CupNoodle', (-1.2, -1.85, 0), 0, hide=('Lid_Sealed', 'Contents_Full'))
dup('CupNoodle', (-1.05, -1.85, 0), 0, hide=('Lid_Sealed', 'Contents_Half'))
# 卓上にも少し置いた例
dup('CupNoodle', (2.62, -0.5, 0.38), 0, hide=('Lid_Sealed', 'Contents_Full'))
dup('Utensil_Chopsticks', (2.7, -0.58, 0.38), 25)
dup('DrinkCan_Crushed', (2.95, -0.45, 0.38))
dup('GameController', (-1.6, 0.25, 0.74), -15)
dup('Mug', (-1.2, 0.3, 0.74))

# 建築モジュールの接続テスト（4m×4mの一角。FPS視点の確認用で、レイアウトの提案ではない）
OX = -10.0
place('Floor_Normal', (OX + 1, 1, 0))
place('Floor_Damaged', (OX + 3, 1, 0))
place('Floor_Service', (OX + 1, 3, 0))
dup('Floor_Normal', (OX + 3, 3, 0))
place('Wall_Window', (OX + 1, 4, 0)); place('Window_Normal', (OX + 1, 4, 0))
place('Wall_Window_Large', (OX + 3, 4, 0)); place('Window_Large', (OX + 3, 4, 0))
place('Wall_Door', (OX, 1, 0), 90); place('Door', (OX, 1, 0), 90); place('DoorFrame', (OX, 1, 0), 90)
place('Wall_Panel', (OX, 3, 0), 90)
place('Wall_Normal', (OX + 4, 3, 0), -90)
place('Ceiling_Normal', (OX + 1, 1, WALL_H)); place('Ceiling_Light', (OX + 1, 3, WALL_H))
dup('Ceiling_Normal', (OX + 3, 1, WALL_H)); dup('Ceiling_Normal', (OX + 3, 3, WALL_H))
place('LightSwitch', (OX, 1.85, 1.1), 90)
place('WallOutlet', (OX + 2.25, 4, 0.3))
place('CableCover', (OX + 2.35, 4, 0.3))
place('WallVent', (OX, 3.55, 2.15), 90)
dup('Chair', (OX + 2.6, 2.5, 0), -20)
dup('HeadSwapMachine', (OX + 2.6 - 0.87 * math.sin(math.radians(-20)), 2.5 + 0.87 * math.cos(math.radians(-20)), 0), -20)

# ================================================================= レンダー（Workbench・輪郭線でトゥーン寄りに確認）
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_percentage = 100
sh = scene.display.shading
sh.light = 'STUDIO'
sh.color_type = 'TEXTURE'
sh.show_object_outline = True
sh.object_outline_color = (0.05, 0.05, 0.06)
sh.show_cavity = True
sh.cavity_type = 'BOTH'
sh.show_shadows = True
sh.shadow_intensity = 0.35
scene.display.render_aa = '16'
scene.view_settings.view_transform = 'Standard'
world = bpy.data.worlds.new('W')
world.color = srgb('#9aa4ae')[:3]
scene.world = world
scene.render.resolution_x = 1920
scene.render.resolution_y = 1080
floor_me = bpy.data.meshes.new('PreviewFloor')
fb = box(12, 12, 0.01)
fb.to_mesh(floor_me)
fb.free()
floor_me.materials.append(mat('PreviewFloor', '#aab2ba', rough=1))
PALETTE.pop('M_Room_PreviewFloor', None)
floor = bpy.data.objects.new('PreviewFloor', floor_me)
floor.location = (0, 0, -0.006)
cp = collection('_Preview')
cp.objects.link(floor)

cam_data = bpy.data.cameras.new('Cam')
cam = bpy.data.objects.new('Cam', cam_data)
cp.objects.link(cam)
scene.camera = cam

def shot_at(fname, loc, target, lens=24, hide_prefix=()):
    saved = {o: o.hide_render for o in scene.objects}
    for o in scene.objects:
        if any(o.name.startswith(h) for h in hide_prefix):
            o.hide_render = True
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = lens
    scene.render.filepath = '%s/%s.png' % (REN, fname)
    bpy.ops.render.render(write_still=True)
    for o, h in saved.items():
        o.hide_render = h

def shot(fname, target, dist, elev, azim, lens=50, only=None):
    saved = {o: o.hide_render for o in scene.objects}
    if only:
        keep = set()
        for n in only:
            keep.update(hier(bpy.data.objects[n]))
        for o in scene.objects:
            if o not in keep and o not in (floor, cam):
                o.hide_render = True
    t = Vector(target)
    d = Vector((math.sin(math.radians(azim)) * math.cos(math.radians(elev)),
                -math.cos(math.radians(azim)) * math.cos(math.radians(elev)),
                math.sin(math.radians(elev))))
    cam.location = t + d * dist
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = lens
    scene.render.filepath = '%s/%s.png' % (REN, fname)
    bpy.ops.render.render(write_still=True)
    for o, h in saved.items():
        o.hide_render = h

shot('Overview', (0.4, -0.2, 0.7), 8.5, 28, -20, 35)
shot('Chair_HeadSwapMachine', (0, 0.4, 1.05), 4.6, 18, -40, 40, only=['Chair', 'HeadSwapMachine'])
shot('Chair_Side', (0, 0.4, 1.05), 3.8, 5, -90, 40, only=['Chair', 'HeadSwapMachine'])
shot('Chair_Solo', (0, 0, 0.7), 2.8, 20, -35, 40, only=['Chair'])
shot('HeadRack', (1.15, 0.5, 0.85), 2.7, 10, -20, 40)
shot('HeadRack_RealHead', (1.15, 0.5, 0.62), 1.2, 12, -25, 50)
shot('Sofa_LowTable', (2.75, -0.1, 0.35), 3.3, 25, -30, 40)
shot('Desk_Monitors', (-1.75, 0.4, 0.75), 2.6, 22, -25, 40)
shot('Props', (-0.25, -1.65, 0.1), 2.3, 30, -10, 40)
shot('Props_Closeup_Left', (-0.9, -1.7, 0.05), 0.9, 30, -10, 50)
shot('CupNoodle_Top', (-1.12, -1.85, 0.08), 0.45, 60, -5, 50)
# 建築：外から（天井を外して）と、FPS目線（高さ1.55m・焦点距離24mm≒水平画角74°）
shot_at('Arch_Assembly_Outside', (OX + 6.5, -3.2, 5.2), (OX + 1.8, 2.2, 0.8), 30, hide_prefix=('Ceiling',))
shot_at('FPS_Room_Corner', (OX + 3.6, 0.35, 1.55), (OX + 0.5, 3.6, 1.25))
shot_at('FPS_Door_Wall', (OX + 3.3, 1.6, 1.55), (OX, 1.6, 1.2))
shot_at('FPS_Window', (OX + 2.0, 2.4, 1.55), (OX + 2.2, 4.0, 1.4))
shot_at('FPS_Ceiling', (OX + 2.4, 1.2, 1.55), (OX + 1.0, 3.0, 2.5))
shot_at('FPS_Floor_Service', (OX + 1.6, 1.8, 1.55), (OX + 1.5, 3.0, 0.0))
shot_at('FPS_Chair', (0.75, -0.95, 1.55), (0, 0.15, 0.75))
shot_at('FPS_Desk', (-1.65, -0.55, 1.55), (-1.75, 0.45, 0.8))
shot_at('FPS_Sofa_Table', (3.4, -1.5, 1.55), (2.75, -0.2, 0.4))

bpy.ops.wm.save_as_mainfile(filepath=OUT + '/ElseIf_Room_Assets.blend')
json.dump({'assets': stats, 'palette': PALETTE}, open(OUT + '/Asset_Stats.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
json.dump(BAKE_LOG, open(OUT + '/Bake_Log.json', 'w', encoding='utf-8'), indent=2)
print('DONE', len(stats), 'assets, total tris', sum(s['tris'] for s in stats), 'baked', len(BAKE_LOG))
