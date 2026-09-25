import bpy,json,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');s=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized_Corrected']
def sm(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
for o in col.objects:
 if o.type!='MESH':continue
 src=bpy.data.objects['Opt__'+o['master_source']]
 o.vertex_groups.clear()
 for g in src.vertex_groups:o.vertex_groups.new(name=g.name)
 for v in src.data.vertices:
  for g in v.groups:o.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
changed={}
for o in col.objects:
 if o.type!='MESH' or not o.get('master_source','').startswith(('ElseIf_Jacket_','EL_')):continue
 count=0
 for v in o.data.vertices:
  p=v.co;amount=.65*sm(-.012,.025,-.906308*(abs(p.x)-.08914964)-.422618*(p.z-1.35648966))*(1-sm(.165,.235,abs(p.x)))*(1-sm(1.195,1.315,p.z))*sm(1.075,1.135,p.z)
  if amount<.001:continue
  w={o.vertex_groups[g.group].name:g.weight for g in v.groups};take=0
  for n in list(w):
   if 'UpperArm' in n or 'Shoulder' in n:cut=w[n]*amount;w[n]-=cut;take+=cut
  if take<.00001:continue
  t=sm(1.17,1.28,p.z)
  for n,a in [('J_Bip_C_Chest',1-t),('J_Bip_C_UpperChest',t)]:w[n]=w.get(n,0)+take*a
  items=sorted(w.items(),key=lambda x:-x[1])[:4];total=sum(x[1] for x in items)
  for g in o.vertex_groups:g.remove([v.index])
  for n,a in items:
   group=o.vertex_groups.get(n) or o.vertex_groups.new(name=n);group.add([v.index],a/total,'REPLACE')
  count+=1
 if count:changed[o.name]=count
# 45度ポーズは独立した補正用Rigへ追加。
src=(P.parent/'GameStage1_20260924/pose_utils.py').read_text(encoding='utf-8-sig').replace('ElseIf_Game_Humanoid','ElseIf_Underarm_Humanoid');exec(src);POSES['K_Arms45']=((.7071068,0,-.7071068),(-.7071068,0,-.7071068),0,0);s.frame_set(111);set_pose('K_Arms45',111)
frames=json.loads((P.parent/'OptimizationTest_20260925/Pose_Frames.json').read_text());frames['K_Arms45']=111;(P/'Pose_Frames.json').write_text(json.dumps(frames,indent=2));s.frame_end=111;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Underarm_Corrected.blend'));print(changed)
