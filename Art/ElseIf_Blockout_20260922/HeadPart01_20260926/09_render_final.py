import bpy,json,hashlib,struct
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');root=bpy.data.objects['HeadPart_ElseIf_Monitor01'];root.rotation_euler=(0,0,0);monitor=bpy.data.materials['ElseIf_FaceMonitor'];tex=monitor.node_tree.nodes['ExpressionTexture'];normal=bpy.data.images['ElseIf_Expression_Test_Normal'];tex.image=normal
oldcenter=Vector((0,.02384366095,1.4681533575+.089));center=Vector((0,.01584366095,1.5461533575))
for name in ['Front','Side','Back','ThreeQuarter','HighAngle']:
 c=bpy.data.objects['Head01_Cam_'+name];delta=c.location-oldcenter;c.location=center+delta;c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=.33
jobs=[('Head_'+n,'ElseIf_Head01_Standalone_Review','Head01_Cam_'+n,'Normal') for n in ['Front','Side','Back','ThreeQuarter','HighAngle']]+[('Assembly_'+n,'ElseIf_Head01_Assembly_Review','Head01_Assembly_Cam_'+n,'Normal') for n in ['Front','ThreeQuarter','BackQuarter']]+[('Assembly_HighAngle','ElseIf_Head01_Assembly_Review','Game_Cam_Stress_HighAngle','Normal')]+[('Expression_'+n,'ElseIf_Head01_Standalone_Review','Head01_Cam_Front',n) for n in ['Joy','Cry']]
def signature():
 h=hashlib.sha256()
 for name in ['ElseIf_HeadShell','ElseIf_FaceMonitor']:
  m=bpy.data.objects[name].data
  for v in m.vertices:h.update(struct.pack('3f',*v.co))
  for f in m.polygons:h.update(struct.pack('%di'%len(f.vertices),*f.vertices))
 return h.hexdigest()
before=signature();manifest=[]
for label,sn,cam,expression in jobs:
 s=bpy.data.scenes[sn];bpy.context.window.scene=s;s.frame_set(1);s.camera=bpy.data.objects[cam];tex.image=bpy.data.images['ElseIf_Expression_Test_'+expression];s.render.image_settings.file_format='PNG';s.render.image_settings.compression=75;s.render.filepath=str(P/(label+'.png'));bpy.ops.render.render(write_still=True);assert signature()==before;manifest.append({'file':label+'.png','scene':sn,'camera':cam,'expression':expression,'resolution':[s.render.resolution_x,s.render.resolution_y],'geometry_sha256':before});(P/'Render_Manifest.json').write_text(json.dumps(manifest,indent=2))
tex.image=normal;bpy.context.window.scene=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.scene.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_MobileVR_HeadPart01.blend'));print('11 final renders complete; same head geometry for all expressions')
