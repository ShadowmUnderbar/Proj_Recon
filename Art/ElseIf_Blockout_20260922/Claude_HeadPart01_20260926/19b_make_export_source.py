import bpy,json,math,bmesh
from pathlib import Path
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_FlatCable_v2.blend')
# 書き出し専用ファイルを先に別名で確定させ、v2ファイルは変更しない
bpy.ops.wm.save_as_mainfile(filepath=D+'/Claude_HeadPart01_NoCable_Export.blend',compress=False)
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_NoCable_Export.blend')
cab=bpy.data.objects.get('ElseIf_HeadCables')
if cab:
    me=cab.data;bpy.data.objects.remove(cab)
    if me.users==0:bpy.data.meshes.remove(me)
s=bpy.data.scenes['ElseIf_Head01_Standalone_Review'];bpy.context.window.scene=s
shell=bpy.data.objects['ElseIf_HeadShell'];face=bpy.data.objects['ElseIf_FaceMonitor']
for o in s.objects:o.select_set(False)
shell.hide_set(False);shell.select_set(True);bpy.context.view_layer.objects.active=shell
me=shell.data
for l in list(me.uv_layers):me.uv_layers.remove(l)
me.uv_layers.new(name='UVMap')
# 筐体のUV0：角度で島を分け、重なりなしで0〜1に詰める
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.01,area_weight=0.0,correct_aspect=True,scale_to_bounds=False)
bpy.ops.uv.export_layout(filepath=D+'/Export/ElseIf_HeadShell_UVLayout.png',export_all=True,modified=False,mode='PNG',size=(1024,1024),opacity=0.25)
bpy.ops.object.mode_set(mode='OBJECT')
# 品質確認：UV面積0／反転、範囲、島の数、面積比（テクセル密度のばらつき）
bm=bmesh.new();bm.from_mesh(me);uv=bm.loops.layers.uv.active
zero=0;flip=0;ratios=[];us=[]
for f in bm.faces:
    pts=[l[uv].uv.copy() for l in f.loops];us+=pts
    a=0.0
    for i in range(len(pts)):a+=pts[i].x*pts[(i+1)%len(pts)].y-pts[(i+1)%len(pts)].x*pts[i].y
    a*=.5
    if abs(a)<1e-12:zero+=1
    elif a<0:flip+=1
    if f.calc_area()>1e-10 and abs(a)>1e-12:ratios.append(abs(a)/f.calc_area())
# 島数（UV上で連結している面の塊）
bm.faces.ensure_lookup_table();seen=set();islands=0
key=lambda l:(round(l[uv].uv.x,6),round(l[uv].uv.y,6),l.vert.index)
for f in bm.faces:
    if f.index in seen:continue
    islands+=1;stack=[f];seen.add(f.index)
    while stack:
        g=stack.pop()
        for l in g.loops:
            for e_l in l.edge.link_loops:
                h=e_l.face
                if h.index in seen:continue
                # 同じ辺でUV座標が一致すれば同じ島
                a1={(round(x[uv].uv.x,5),round(x[uv].uv.y,5)) for x in (l,l.link_loop_next)}
                a2={(round(x[uv].uv.x,5),round(x[uv].uv.y,5)) for x in (e_l,e_l.link_loop_next)}
                if a1==a2:seen.add(h.index);stack.append(h)
ratios.sort();med=ratios[len(ratios)//2]
res={'shell_uv':me.uv_layers.keys(),'face_uv':face.data.uv_layers.keys(),'islands':islands,'zero_area':zero,'flipped':flip,
     'uv_min':[round(min(p.x for p in us),4),round(min(p.y for p in us),4)],'uv_max':[round(max(p.x for p in us),4),round(max(p.y for p in us),4)],
     'texel_density_p10_p90_vs_median':[round(ratios[len(ratios)//10]/med,2),round(ratios[len(ratios)*9//10]/med,2)]}
bm.free();bpy.ops.wm.save_mainfile();print(json.dumps(res))
