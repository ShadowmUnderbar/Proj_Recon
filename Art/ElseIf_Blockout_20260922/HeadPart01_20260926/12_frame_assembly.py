import bpy,json,numpy as np
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');s=bpy.data.scenes['ElseIf_Head01_Assembly_Review'];bpy.context.window.scene=s;s.frame_set(1);deps=bpy.context.evaluated_depsgraph_get();pts=[]
for cn in ['ElseIf_Game_MobileVR_Test','ElseIf_HeadPart_Monitor01']:
 for o in bpy.data.collections[cn].objects:
  if o.type!='MESH':continue
  e=o.evaluated_get(deps);m=e.to_mesh();coords=np.ones((len(m.vertices),4));coords[:,:3]=np.array([list(v.co) for v in m.vertices]);pts.extend((coords@np.array(e.matrix_world).T).tolist());e.to_mesh_clear()
pts=np.array(pts);report=[]
for name in ['Front','ThreeQuarter','BackQuarter']:
 cam=bpy.data.objects['Head01_Assembly_Cam_'+name];v=pts@np.array(cam.matrix_world.inverted()).T;lo=v[:,:2].min(axis=0);hi=v[:,:2].max(axis=0);center=(lo+hi)*.5;rot=cam.matrix_world.to_3x3();cam.location+=rot@Vector((center[0],center[1],0));p=cam.calc_matrix_camera(deps,x=s.render.resolution_x,y=s.render.resolution_y);extent=np.array([2/p[0][0],2/p[1][1]]);cam.data.ortho_scale*=float(max((hi-lo)/extent))*1.12;bpy.context.view_layer.update();s.camera=cam;s.render.filepath=str(P/('Assembly_'+name+'.png'));bpy.ops.render.render(write_still=True);report.append({'camera':cam.name,'ortho_scale':cam.data.ortho_scale,'location':list(cam.location)})
s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];(P/'Assembly_Framing.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_MobileVR_HeadPart01.blend'));print('Three assembly views reframed; existing game camera unchanged')
