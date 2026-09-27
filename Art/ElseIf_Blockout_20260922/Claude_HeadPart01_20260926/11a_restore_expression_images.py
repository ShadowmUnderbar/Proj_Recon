import bpy,json
CX='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926'
out={}
for n in ['Normal','Joy','Cry']:
    name='ElseIf_Expression_Test_'+n;im=bpy.data.images.get(name)
    if im is None:
        im=bpy.data.images.load(CX+'/'+name+'.png',check_existing=False);im.name=name
    if not im.packed_file:im.pack()
    im.filepath='//'+name+'.png';im.use_fake_user=True   # 元フォルダへの参照を残さない
    im.colorspace_settings.name='sRGB';out[name]={'size':list(im.size),'packed':bool(im.packed_file)}
bpy.ops.wm.save_mainfile();print(json.dumps(out))
