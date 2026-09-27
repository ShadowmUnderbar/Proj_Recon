import bpy,json,hashlib,struct
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];root.rotation_euler=(0,0,0)
tex=bpy.data.materials['ElseIf_FaceMonitor'].node_tree.nodes['ExpressionTexture']
HEAD=[o.name for o in bpy.data.collections['ElseIf_HeadPart_Monitor01'].objects if o.type=='MESH']
def signature():
    h=hashlib.sha256()
    for n in HEAD:
        m=bpy.data.objects[n].data
        for v in m.vertices:h.update(struct.pack('3f',*v.co))
        for f in m.polygons:h.update(struct.pack('%di'%len(f.vertices),*f.vertices))
    return h.hexdigest()
SOLO='ElseIf_Head01_Standalone_Review';ASM='ElseIf_Head01_Assembly_Review'
jobs=[('Head_'+n,SOLO,'Claude_Head_Cam_'+n,'Normal',720,720) for n in ['Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle']]
jobs+=[('Assembly_'+n,ASM,'Head01_Assembly_Cam_'+n,'Normal',880,1120) for n in ['Front','ThreeQuarter','BackQuarter']]
jobs+=[('Assembly_HighAngle',ASM,'Game_Cam_Stress_HighAngle','Normal',880,1120)]
jobs+=[('Expression_'+e+'_'+v,SOLO,'Claude_Head_Cam_'+v,e,720,720) for e in ['Normal','Joy','Cry'] for v in ['Front','HighAngle']]
jobs+=[('Head_IllustrationMatched_ThreeQuarter',SOLO,'Claude_Head_Cam_ThreeQuarter','Normal',720,720)]
before=signature();man=[]
for label,sn,cam,ex,rx,ry in jobs:
    s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(1);s.camera=bpy.data.objects[cam]
    s.render.resolution_x=rx;s.render.resolution_y=ry;s.render.image_settings.file_format='PNG'
    tex.image=bpy.data.images['ElseIf_Expression_Test_'+ex];s.render.filepath=D+'/Renders/'+label+'.png';bpy.ops.render.render(write_still=True)
    assert signature()==before;man.append({'file':'Renders/'+label+'.png','scene':sn,'camera':cam,'expression':ex,'resolution':[rx,ry],'geometry_sha256':before})
tex.image=bpy.data.images['ElseIf_Expression_Test_Normal'];bpy.context.window.scene=bpy.data.scenes[ASM];bpy.context.scene.camera=bpy.data.objects['Game_Cam_Stress_HighAngle']
json.dump(man,open(D+'/Render_Manifest.json','w'),indent=2);bpy.ops.wm.save_mainfile();print(json.dumps({'renders':len(man),'geometry_sha256':before}))
