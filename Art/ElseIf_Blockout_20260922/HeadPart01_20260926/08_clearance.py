import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926');shell=bpy.data.objects['ElseIf_HeadShell'];face=bpy.data.objects['ElseIf_FaceMonitor'];backup={o.name:[list(v.co) for v in o.data.vertices] for o in [shell,face]};(P/'Head_PreClearance_Coordinates.json').write_text(json.dumps(backup));lift=.012
for v in shell.data.vertices:
 if v.index<624 or v.index>=912:v.co.z+=lift
 elif 624<=v.index<848:v.co.z+=lift*max(0,min(1,(v.co.z+.012)/.035))
for v in face.data.vertices:v.co.z+=lift
for o in [shell,face]:o.data.update()
(P/'Clearance_Adjustment.json').write_text(json.dumps({'shell_lift_m':lift,'neck_extension_m':lift,'body_changed':False}));print('Head housing lifted 12 mm, upper neck extended; socket and body unchanged')
