import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');src=bpy.data.collections['ElseIf_Game_Optimized_Test'];base=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=base;base.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_Underarm_Correction.blend'),copy=True)
col=bpy.data.collections.new('ElseIf_Game_Optimized_Corrected');col.use_fake_user=True;mp={}
for o in src.objects:
 n=o.copy();n.name='Fix__'+o.name.removeprefix('Opt__');col.objects.link(n)
 if o.data:n.data=o.data.copy()
 if n.animation_data and n.animation_data.action:n.animation_data.action=n.animation_data.action.copy()
 mp[o]=n
for o,n in mp.items():
 if o.parent in mp:n.parent=mp[o.parent]
 for m in n.modifiers:
  if m.type=='ARMATURE' and m.object in mp:m.object=mp[m.object]
 cs=list(n.constraints)+([c for pb in n.pose.bones for c in pb.constraints] if n.type=='ARMATURE' else [])
 for c in cs:
  if hasattr(c,'target') and c.target in mp:c.target=mp[c.target]
 for data in [n,n.data.shape_keys if n.type=='MESH' else None]:
  if data and data.animation_data:
   for fc in data.animation_data.drivers:
    for v in fc.driver.variables:
     for t in v.targets:
      if t.id in mp:t.id=mp[t.id]
rig=mp[bpy.data.objects['ElseIf_Optimized_Humanoid']];rig.name='ElseIf_Underarm_Humanoid'
s=base.copy();s.name='ElseIf_Underarm_Review';s.use_fake_user=True
for c in list(s.collection.children):s.collection.children.unlink(c)
s.collection.children.link(bpy.data.collections['ElseIf_Review_Stage']);s.collection.children.link(col);bpy.context.window.scene=s;s.frame_set(1);bpy.context.view_layer.update()
rep={'bones':{},'weights':{}}
for b in rig.data.bones:
 if any(t in b.name for t in ['Shoulder','Clavicle','UpperArm','Chest','Spine']):rep['bones'][b.name]={'head':list(b.head_local),'tail':list(b.tail_local)}
for prefix in ['Game__','Opt__']:
 for part in ['Front_L','Front_R','Back']:
  o=bpy.data.objects[prefix+'ElseIf_Jacket_'+part];bins=[]
  for lo,hi in [(1.05,1.10),(1.10,1.15),(1.15,1.20),(1.20,1.25),(1.25,1.30)]:
   vals=[]
   for v in o.data.vertices:
    if lo<=v.co.z<hi and .08<abs(v.co.x)<.20:
     vals.append(sum(g.weight for g in v.groups if any(t in o.vertex_groups[g.group].name for t in ['UpperArm','Shoulder'])))
   if vals:bins.append({'z':[lo,hi],'count':len(vals),'arm_mean':sum(vals)/len(vals),'arm_max':max(vals)})
  rep['weights'][o.name]=bins
(P/'Diagnosis.json').write_text(json.dumps(rep,indent=2));print(json.dumps(rep));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Underarm_Corrected.blend'))
