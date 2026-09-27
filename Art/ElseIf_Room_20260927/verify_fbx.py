import bpy, json, glob, os
from mathutils import Vector
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Room_20260927'
stats={s['asset']:s for s in json.load(open(D+'/Asset_Stats.json',encoding='utf-8'))['assets']}
bpy.ops.wm.read_homefile(use_empty=True)
out=[]
for f in sorted(glob.glob(D+'/Export/*/*.fbx')):
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    bpy.ops.import_scene.fbx(filepath=f)
    objs=list(bpy.context.scene.objects)
    meshes=[o for o in objs if o.type=='MESH']
    pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    mn=[min(p[i] for p in pts) for i in range(3)]; mx=[max(p[i] for p in pts) for i in range(3)]
    size=[round(mx[i]-mn[i],3) for i in range(3)]
    name=os.path.basename(f)[:-4]; exp=stats[name]['size_xyz_m']
    nouv=[o.name for o in meshes if not o.data.uv_layers]
    uvout=[]
    for o in meshes:
        if o.data.uv_layers and not any(k in o.name for k in ('Floor','Wall','Ceiling','Hatch')):
            us=[d.uv for d in o.data.uv_layers[0].data]
            if us and (min(u.x for u in us)<-0.001 or max(u.x for u in us)>1.001 or min(u.y for u in us)<-0.001 or max(u.y for u in us)>1.001): uvout.append(o.name)
    imgs=sorted({n.image.filepath.split('/')[-1].split(chr(92))[-1] for o in meshes for m in o.data.materials if m and m.node_tree for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image})
    missing=[n.image.filepath for o in meshes for m in o.data.materials if m and m.node_tree for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and not n.image.has_data]
    out.append({'a':name,'sizeOK':all(abs(size[i]-exp[i])<0.003 for i in range(3)),'minZ':round(mn[2],3),'noUV':nouv,'uvOut':uvout,'imgs':imgs,'missing':missing})
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    for im in list(bpy.data.images): bpy.data.images.remove(im)
print(json.dumps(out,ensure_ascii=False))
