import bpy,json,time
from mathutils import Vector
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
solo=bpy.data.scenes['ElseIf_Head01_Standalone_Review'];bpy.context.window.scene=solo;solo.frame_set(1)
sock=bpy.data.objects['Mobile__LOD0__Backup__Fix__HeadSocket'];center=sock.matrix_world.translation+Vector((0,0,.12))
# 比較用カメラは新規作成（既存カメラは変更しない）。Current/IllustrationMatchedで同一構図
def cam(name,delta,scale):
    o=bpy.data.objects.get(name)
    if o is None:
        d=bpy.data.cameras.new(name);d.type='ORTHO';o=bpy.data.objects.new(name,d);solo.collection.objects.link(o)
    o.data.ortho_scale=scale;o.location=center+Vector(delta);o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler();return o
views={'Front':(0,-2,0),'Side':(2,0,0),'Back':(0,2,0),'ThreeQuarter':(1.6,-2,.8),'BackQuarter':(1.6,2,.8),'HighAngle':(1.2,-1.7,2.5)}
for n,dl in views.items():cam('Claude_Head_Cam_'+n,dl,.42)
tex=bpy.data.materials['ElseIf_FaceMonitor'].node_tree.nodes['ExpressionTexture'];tex.image=bpy.data.images['ElseIf_Expression_Test_Normal']
solo.camera=bpy.data.objects['Claude_Head_Cam_ThreeQuarter'];solo.render.resolution_x=solo.render.resolution_y=720
solo.render.image_settings.file_format='PNG';solo.render.filepath=D+'/Renders/Head_Current_ThreeQuarter.png';t=time.time();bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_mainfile()
print(json.dumps({'engine':solo.render.engine,'samples':solo.cycles.samples,'device':solo.cycles.device,'sec':round(time.time()-t,1),'file':bpy.data.filepath}))
