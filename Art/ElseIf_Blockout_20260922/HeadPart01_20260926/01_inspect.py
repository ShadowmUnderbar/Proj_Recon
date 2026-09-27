import bpy,json,hashlib,struct,ast
from pathlib import Path
from mathutils import Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926')
def info(o):
 return {'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'parent_type':o.parent_type,'parent_bone':o.parent_bone,'location':list(o.location),'rotation_euler':list(o.rotation_euler),'scale':list(o.scale),'matrix_world':[list(r) for r in o.matrix_world],'dimensions':list(o.dimensions),'collections':[c.name for c in o.users_collection]}
report={'file':bpy.data.filepath,'scene':bpy.context.scene.name,'sockets':[info(o) for o in bpy.data.objects if 'Socket' in o.name],'scenes':[s.name for s in bpy.data.scenes]}
col=bpy.data.collections.get('ElseIf_Game_MobileVR_Test');report['mobile_objects']=[info(o) for o in col.objects] if col else []
rig=bpy.data.objects.get('ElseIf_MobileVR_Humanoid')
if rig:report['bones']=[{'name':b.name,'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)} for b in rig.data.bones if any(t in b.name for t in ['Head','Neck','Chest'])]
report['cameras']=[info(o) for o in bpy.data.objects if o.type=='CAMERA' and ('ElseIf_Cam' in o.name or 'HighAngle' in o.name)]
(P/'Initial_Inspection.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
