import bpy,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');scene=bpy.data.scenes['ElseIf_Game_Backup_Review'];bpy.context.window.scene=scene;scene.frame_set(1);bpy.context.view_layer.update();o=bpy.data.objects['Backup__ElseIf_Mannequin'];rig=bpy.data.objects['ElseIf_Backup_Humanoid'];r=json.loads((P/'Body_Deletion_Record.json').read_text())['LOD0__ElseIf_Mannequin'];prot=set(r['protected_vertices']);report={};vs=[];fs=[]
for ob in bpy.data.collections['ElseIf_Game_Backup'].objects:
 if ob.type!='MESH' or not ob.get('master_source','').startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_')):continue
 e=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();off=len(vs);vs += [v.co.copy() for v in m.vertices];fs += [tuple(i+off for i in f.vertices) for f in m.polygons];e.to_mesh_clear()
t=BVHTree.FromPolygons(vs,fs);dirs=[Vector((x,y,z)).normalized() for x in [-1,0,1] for y in [-1,0,1] for z in [-1,0,1] if x or y or z]
for bone in ['UpperArm','LowerArm','Foot','UpperLeg','LowerLeg','Chest']:
 ids={g.index for g in o.vertex_groups if g.name.endswith('_'+bone)};verts=[v for v in o.data.vertices if any(g.group in ids and g.weight>.5 for g in v.groups)];bad=[]
 for v in verts:
  missing=[list(d) for d in dirs if t.ray_cast(v.co+d*.00005,d,2)[0] is None]
  if missing:bad.append({'p':list(v.co),'missing':missing[:2]})
 report[bone]={'vertices':len(verts),'protected':sum(v.index in prot for v in verts),'ray_uncovered':len(bad),'samples':bad[:3]}
(P/'Hidden_Diagnostic.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));scene.frame_set(1)
