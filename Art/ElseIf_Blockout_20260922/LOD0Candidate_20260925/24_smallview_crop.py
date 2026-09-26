import bpy,json,math,numpy as np
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');manifest=json.loads((P/'Render_Manifest.json').read_text());r={};s=bpy.data.scenes['ElseIf_Game_Backup_Review'];bpy.context.window.scene=s
for job in manifest:
 if not job['file'].startswith('Current_'):continue
 s.frame_set(job['frame']);s.camera=bpy.data.objects[job['camera']];bpy.context.view_layer.update();coords=[]
 for o in bpy.data.collections['ElseIf_Game_Backup'].objects:
  if o.type!='MESH':continue
  e=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
  m=e.to_mesh();a=np.empty(len(m.vertices)*3,np.float32);m.vertices.foreach_get('co',a);a=a.reshape(-1,3);a=np.concatenate([a,np.ones((len(a),1),np.float32)],axis=1);proj=s.camera.calc_matrix_camera(bpy.context.evaluated_depsgraph_get(),x=s.render.resolution_x,y=s.render.resolution_y);clip=a@np.array(proj@s.camera.matrix_world.inverted()@e.matrix_world).T;xy=clip[:,:2]/clip[:,3:4];px=(xy[:,0]+1)*.5*s.render.resolution_x;py=(1-xy[:,1])*.5*s.render.resolution_y;coords.extend([(float(px.min()),float(py.min())),(float(px.max()),float(py.max()))]);e.to_mesh_clear()
 x0=max(0,math.floor(min(p[0] for p in coords))-5);x1=min(s.render.resolution_x,math.ceil(max(p[0] for p in coords))+5);y0=max(0,math.floor(min(p[1] for p in coords))-5);y1=min(s.render.resolution_y,math.ceil(max(p[1] for p in coords))+5);r[job['file'].removeprefix('Current_').removesuffix('.png')]=[x0,y0,x1-x0,y1-y0]
s.frame_set(1);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];bpy.context.window.scene=bpy.data.scenes['ElseIf_LOD0_Review'];(P/'SmallView_Crops.json').write_text(json.dumps(r,indent=2));print(r)
