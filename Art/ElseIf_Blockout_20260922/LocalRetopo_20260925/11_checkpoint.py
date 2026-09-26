import bpy,json
from pathlib import Path
s=bpy.data.scenes.get('ElseIf_Topology_Review')
if s:
 bpy.context.window.scene=s;s.frame_set(1)
path='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925/ElseIf_LocalRetopo_Unfinished_Checkpoint.blend'
bpy.ops.wm.save_as_mainfile(filepath=path,copy=True)
print(json.dumps({'checkpoint':path,'active_file':bpy.data.filepath,'status':'unfinished; awaiting clarification; not promoted to Latest','bodies':{o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.data.objects if o.name in ['Topo__ElseIf_Mannequin','Topo__ElseIf_Anatomy_Restoration']}}))
