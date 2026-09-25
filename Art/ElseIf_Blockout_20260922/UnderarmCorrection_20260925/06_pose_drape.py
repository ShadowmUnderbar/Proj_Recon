import bpy,json
from pathlib import Path
from mathutils import Matrix,Vector
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');s=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=s;col=bpy.data.collections['ElseIf_Game_Optimized_Corrected'];rig=bpy.data.objects['ElseIf_Underarm_Humanoid'];s.frame_set(21);bpy.context.view_layer.update()
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=[x.co.copy() for x in m.vertices];e.to_mesh_clear();return v
objects=[o for o in col.objects if o.type=='MESH' and o.get('master_source','').startswith(('ElseIf_Jacket_','EL_'))];desired={o.name:snap(o) for o in objects}
for o in objects:
 src=bpy.data.objects['Opt__'+o['master_source']];o.vertex_groups.clear()
 for g in src.vertex_groups:o.vertex_groups.new(name=g.name)
 for v in src.data.vertices:
  for g in v.groups:o.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
bpy.context.view_layer.update();trans={b.name:rig.pose.bones[b.name].matrix@b.matrix_local.inverted() for b in rig.data.bones};targets={}
for f in [21,71]:
 s.frame_set(f);targets[f]={side:list(rig.pose.bones['J_Bip_'+side+'_UpperArm'].rotation_quaternion) for side in ['L','R']}
s.frame_set(21);bpy.context.view_layer.update()
def drive(k,side):
 d=k.driver_add('value').driver;d.type='SCRIPTED'
 for i in range(4):
  v=d.variables.new();v.name='q'+str(i);v.type='SINGLE_PROP';v.targets[0].id=rig;v.targets[0].data_path='pose.bones["J_Bip_'+side+'_UpperArm"].rotation_quaternion['+str(i)+']'
 terms=['('+ '+'.join(f'{targets[f][side][i]:.6f}*q{i}' for i in range(4))+')**2' for f in [21,71]];d.expression='min(1,max(0,(max('+','.join(terms)+')-.90)/.07))'
report=[]
for o in objects:
 vs=snap(o);updates={'L':{},'R':{}}
 for i,(want,now) in enumerate(zip(desired[o.name],vs)):
  d=want-now
  if d.length<.00003:continue
  # 通常のウェイトを保ち、身頃が下へ残る成分を主体にする。
  d.x*=.45;d.y*=.80
  mx=Matrix(((0,0,0,0),)*4)
  for g in o.data.vertices[i].groups:
   n=o.vertex_groups[g.group].name
   if n in trans:mx+=trans[n]*g.weight
  updates['L' if o.data.vertices[i].co.x>=0 else 'R'][i]=mx.to_3x3().inverted_safe()@d
 if not any(updates.values()):continue
 if not o.data.shape_keys:o.shape_key_add(name='Basis',from_mix=False)
 for side,upd in updates.items():
  if not upd:continue
  k=o.shape_key_add(name='DLHN_Underarm_Drape_'+side,from_mix=False)
  for i,d in upd.items():k.data[i].co+=d
  drive(k,side);report.append([o.name,side,len(upd),max(d.length for d in upd.values())*1000])
s.frame_set(1);(P/'Drape_Keys.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Underarm_Corrected.blend'));print(report)
