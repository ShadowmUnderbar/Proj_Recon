import bpy,json
from pathlib import Path
from mathutils import Vector
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924')
r={'file':bpy.data.filepath,'scene':bpy.context.scene.name,'bones':{},'objects':{}}
rig=bpy.data.objects['ElseIf_Humanoid']
for b in rig.pose.bones:
 if any(t in b.name for t in ['Shoulder','UpperArm','LowerArm','Neck','Chest']):r['bones'][b.name]={'head':list(rig.matrix_world@b.head),'tail':list(rig.matrix_world@b.tail)}
for o in bpy.data.objects:
 if o.type=='MESH' and (o.name.startswith('ElseIf_Jacket') or o.name.startswith('ElseIf_Shoe') or o.name.startswith('EL_Shoe') or 'Shoulder' in o.name):
  r['objects'][o.name]={'verts':len(o.data.vertices),'bounds':[list(o.matrix_world@Vector(v)) for v in o.bound_box],'mods':[(m.name,m.type) for m in o.modifiers],'keys':list(o.data.shape_keys.key_blocks.keys()) if o.data.shape_keys else [],'props':{k:str(v) for k,v in o.items()},'materials':[m.name if m else '' for m in o.data.materials]}
(out/'01_inspect.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
print(json.dumps(r))
