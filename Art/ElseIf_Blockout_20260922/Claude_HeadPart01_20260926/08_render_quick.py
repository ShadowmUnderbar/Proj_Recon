import bpy,json,sys
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
# 反復確認用の頭部単体クイックレンダー（6方向）
solo=bpy.data.scenes['ElseIf_Head01_Standalone_Review'];bpy.context.window.scene=solo;solo.frame_set(1)
solo.render.resolution_x=solo.render.resolution_y=720;solo.render.image_settings.file_format='PNG'
out=[]
for n in ['Front','Side','Back','ThreeQuarter','BackQuarter','HighAngle']:
    solo.camera=bpy.data.objects['Claude_Head_Cam_'+n];solo.render.filepath=D+'/Renders/Iter_Head_'+n+'.png';bpy.ops.render.render(write_still=True);out.append(n)
print(json.dumps(out))
