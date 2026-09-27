import bpy,json
from mathutils import Matrix
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];sock=root.parent;col=bpy.data.collections['ElseIf_HeadPart_Monitor01']
# 再ビルドで付いた連番（.001）を外し、正式名にそろえる
for n in [o.name for o in col.objects if o.type=='MESH']:
    me=bpy.data.objects[n].data;want=n+'_Mesh';other=bpy.data.meshes.get(want)
    if other and other!=me:other.name=want+'_Unused'
    me.name=want
imgs={bpy.data.images['ElseIf_Expression_Test_'+e] for e in ['Normal','Joy','Cry']}
# 単体ファイルはRootを原点・親なし・Scale 1で書き出す。メモリ上の親子付けは直後に元へ戻す
root.parent=None;root.matrix_world=Matrix.Identity(4)
try:
    bpy.data.libraries.write(D+'/Claude_HeadPart_Monitor01.blend',{col}|imgs,fake_user=False,path_remap='NONE',compress=False)
finally:
    root.parent=sock;root.matrix_parent_inverse=Matrix.Identity(4);root.location=(0,0,0);root.rotation_euler=(0,0,0);root.scale=(1,1,1)
bpy.context.view_layer.update()
with bpy.data.libraries.load(D+'/Claude_HeadPart_Monitor01.blend',link=False) as (data,_):
    r={k:list(getattr(data,k)) for k in ['objects','meshes','materials','images','armatures','cameras','collections']}
r['root_restored_parent']=root.parent.name;r['root_world']=list(root.matrix_world.translation)
bpy.ops.wm.save_mainfile();print(json.dumps(r))
