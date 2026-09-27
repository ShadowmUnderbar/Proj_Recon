import bpy,json
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
sc=bpy.data.scenes.new('FBX_Verify');bpy.context.window.scene=sc
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=D+'/Export/ElseIf_HeadPart_Monitor01_NoCable.fbx')
new=[o for o in bpy.data.objects if o not in before];bpy.context.view_layer.update()
r={}
for o in new:
    e={'type':o.type,'parent':o.parent.name if o.parent else None,'loc':[round(v,5) for v in o.matrix_world.translation],'scale':[round(v,4) for v in o.matrix_world.to_scale()]}
    if o.type=='MESH':
        ws=[o.matrix_world@v.co for v in o.data.vertices]
        e.update({'tris':sum(len(p.vertices)-2 for p in o.data.polygons),'uv':o.data.uv_layers.keys(),'mats':[m.name for m in o.data.materials],
                  'zmin':round(min(v.z for v in ws),4),'zmax':round(max(v.z for v in ws),4),'ymin':round(min(v.y for v in ws),4),'ymax':round(max(v.y for v in ws),4)})
    r[o.name]=e
print(json.dumps(r))
