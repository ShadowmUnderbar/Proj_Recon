import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');o=bpy.data.objects['LOD0__Jacket'];uv=o.data.uv_layers['DesignUV'];o.data.uv_layers.active=uv;uv.active_render=True
manifest=json.loads((P/'Render_Manifest.json').read_text());s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s
for job in manifest:
 if not job['file'].startswith('Optimized_'):continue
 s.frame_set(job['frame']);s.camera=bpy.data.objects[job['camera']];s.render.filepath=str(P/job['file']);bpy.ops.render.render(write_still=True)
s.frame_set(1);s.camera=bpy.data.objects['Game_Cam_Stress_HighAngle'];bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'));(P/'UV_Preservation.json').write_text(json.dumps({'object':o.name,'active_uv':o.data.uv_layers.active.name,'render_uv':uv.name,'source_uv':'Backup__EL_Back_Reference_Artwork / DesignUV','geometry_or_material_changes':False},indent=2));print('Back graphic UV restored and eight optimized views refreshed.')
