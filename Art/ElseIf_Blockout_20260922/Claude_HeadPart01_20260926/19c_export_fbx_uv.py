import bpy,json,os
from mathutils import Matrix
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926';OUT=D+'/Export'
from pathlib import Path
assert Path(bpy.data.filepath)==Path(D+'/Claude_HeadPart01_NoCable_Export.blend'),bpy.data.filepath
s=bpy.data.scenes['ElseIf_Head01_Standalone_Review'];bpy.context.window.scene=s
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];objs=[root,bpy.data.objects['ElseIf_HeadShell'],bpy.data.objects['ElseIf_FaceMonitor']]
# 単体Head PartとしてRootを原点・親なしにする（このファイルは保存しない）
sock=root.parent;root.parent=None;root.matrix_world=Matrix.Identity(4);bpy.context.view_layer.update()
for o in s.objects:o.select_set(False)
for o in objs:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=root
fbx=OUT+'/ElseIf_HeadPart_Monitor01_NoCable.fbx'
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'EMPTY','MESH'},apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',axis_up='Y',use_space_transform=True,bake_space_transform=False,mesh_smooth_type='FACE',use_mesh_modifiers=True,
    add_leaf_bones=False,path_mode='COPY',embed_textures=True,use_custom_props=True)
# 表情Textureは別ファイルでも同梱する（FaceMonitorは発光側で使うため、FBXのMaterialには自動で割り当たらない場合がある）
tex=[]
for n in ['Normal','Joy','Cry']:
    im=bpy.data.images['ElseIf_Expression_Test_'+n];p=OUT+'/ElseIf_Expression_Test_'+n+'.png'
    im.save(filepath=p) if im.packed_file is None else open(p,'wb').write(im.packed_file.data);tex.append(os.path.basename(p))
tris={o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in objs[1:]}
print(json.dumps({'fbx':fbx,'size_kb':round(os.path.getsize(fbx)/1024,1),'objects':[o.name for o in objs],'tris':tris,'total':sum(tris.values()),
 'verts':sum(len(o.data.vertices) for o in objs[1:]),'materials':sorted({m.name for o in objs[1:] for m in o.data.materials}),'uv':objs[2].data.uv_layers.keys(),'textures':tex}))
# 書き出し専用ファイルには装着状態（HeadSocketの子）を残したいので、保存せずに元へ戻す
root.parent=sock;root.matrix_parent_inverse=Matrix.Identity(4);root.location=(0,0,0);root.rotation_euler=(0,0,0);root.scale=(1,1,1)
