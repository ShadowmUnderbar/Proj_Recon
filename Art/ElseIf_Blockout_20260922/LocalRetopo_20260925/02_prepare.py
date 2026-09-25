import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRetopo_20260925');src=bpy.data.collections['ElseIf_Game_Optimized_Test'];base=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=base;base.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_LocalRetopo.blend'),copy=True)
col=bpy.data.collections.new('ElseIf_Game_LocalRetopo');col.use_fake_user=True;mp={}
for o in src.objects:
 n=o.copy();n.name='Topo__'+o.name.removeprefix('Opt__');col.objects.link(n)
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
rig=mp[bpy.data.objects['ElseIf_Optimized_Humanoid']];rig.name='ElseIf_Topology_Humanoid'
s=base.copy();s.name='ElseIf_Topology_Review';s.use_fake_user=True
for c in list(s.collection.children):s.collection.children.unlink(c)
s.collection.children.link(bpy.data.collections['ElseIf_Review_Stage']);s.collection.children.link(col);bpy.context.window.scene=s;s.frame_set(1);bpy.context.view_layer.update()

# 以前の肩用補正を積み重ねず、肘と手首の検証済み補正は維持。
for o in col.objects:
 if o.type!='MESH' or not o.data.shape_keys:continue
 for k in list(o.data.shape_keys.key_blocks):
  if k.name.startswith('DLHN_Wide_ClothEase') or (k.name.startswith('DLHN_Contact') and 'J_WristTwist' not in k.name):
   k.driver_remove('value');o.shape_key_remove(k)
source=(P.parent/'GameStage1_20260924/pose_utils.py').read_text(encoding='utf-8-sig').replace('ElseIf_Game_Humanoid','ElseIf_Topology_Humanoid');exec(source);POSES['K_Arms45']=((.7071068,0,-.7071068),(-.7071068,0,-.7071068),0,0);s.frame_set(111);set_pose('K_Arms45',111)
frames=json.loads((P.parent/'UnderarmCorrection_20260925/Pose_Frames.json').read_text());(P/'Pose_Frames.json').write_text(json.dumps(frames,indent=2));s.frame_end=111;s.frame_set(1)
print({o.name:[[min(v.co[i] for v in o.data.vertices),max(v.co[i] for v in o.data.vertices)] for i in range(3)] for o in col.objects if o.type=='MESH' and any(t in o.name for t in ['Shorts','Sock_L','Shoe_L_Upper'])})
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_LocalRetopo.blend'))
