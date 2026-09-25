import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.kdtree import KDTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925');s=bpy.data.scenes['ElseIf_Optimized_Review'];bpy.context.window.scene=s;c=bpy.data.collections['ElseIf_Game_Optimized_Test'];rig=bpy.data.objects['ElseIf_Optimized_Humanoid'];targets={}
for f in [21,71]:
 s.frame_set(f);targets[f]={side:list(rig.pose.bones['J_Bip_'+side+'_UpperArm'].rotation_quaternion) for side in ['L','R']}
s.frame_set(21);bpy.context.view_layer.update();trans={b.name:rig.pose.bones[b.name].matrix@b.matrix_local.inverted() for b in rig.data.bones}
def driver(k,side):
 d=k.driver_add('value').driver;d.type='SCRIPTED';terms=[]
 for i in range(4):
  v=d.variables.new();v.name='q'+str(i);v.type='SINGLE_PROP';v.targets[0].id=rig;v.targets[0].data_path='pose.bones["J_Bip_'+side+'_UpperArm"].rotation_quaternion['+str(i)+']'
 for f in [21,71]:terms.append('('+ '+'.join(f'{targets[f][side][i]:.6f}*q{i}' for i in range(4))+')**2')
 d.expression='min(1,max(0,(max('+','.join(terms)+')-.86)/.10))'
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
points={side:[] for side in ['L','R']};deltas={side:[] for side in ['L','R']};report=[]
for o in c.objects:
 if o.type!='MESH' or o.get('master_source','') not in ['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R']:continue
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();vs=[v.co.copy() for v in m.vertices];ev.to_mesh_clear();orig=[v.copy() for v in vs];adj=[[] for _ in vs]
 for e in o.data.edges:a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 for it in range(24):
  vs=[v*.55+sum((vs[j] for j in adj[i]),Vector())*(.45/len(adj[i])) if adj[i] else v for i,v in enumerate(vs)]
 if not o.data.shape_keys:o.shape_key_add(name='Basis',from_mix=False)
 basis=o.data.shape_keys.reference_key;ks={side:o.shape_key_add(name='DLHN_Wide_ClothEase_'+side,from_mix=False) for side in ['L','R']};maxd=0
 for i,b in enumerate(basis.data):
  p=b.co;mask=smooth(.04,.075,abs(p.x))*(1-smooth(.20,.27,abs(p.x)))*smooth(1.10,1.19,p.z)*(1-smooth(1.35,1.41,p.z))
  if mask<.001:continue
  d=(vs[i]-orig[i])*.85*mask
  # 脇の布に、張った面を下へ落とす弱い余りを与える。
  ax=math.exp(-((abs(p.x)-.135)/.055)**2-((p.z-1.235)/.055)**2)
  d.z-=.009*ax;d.y+=.004*ax*(1 if p.y>.025 else -1)
  if d.length>.014:d*=.014/d.length
  mx=Matrix(((0,0,0,0),)*4)
  for g in o.data.vertices[i].groups:
   n=o.vertex_groups[g.group].name
   if n in trans:mx+=trans[n]*g.weight
  rd=mx.to_3x3().inverted_safe()@d;side='L' if p.x>=0 else 'R';ks[side].data[i].co+=rd;points[side].append(p.copy());deltas[side].append(rd);maxd=max(maxd,d.length)
 for side,k in ks.items():driver(k,side)
 report.append([o.name,maxd*1000])
for side in ['L','R']:
 kd=KDTree(len(points[side]))
 for i,p in enumerate(points[side]):kd.insert(p,i)
 kd.balance()
 for o in c.objects:
  if o.type!='MESH' or not o.get('master_source','').startswith('EL_'):continue
  upd={}
  for v in o.data.vertices:
   if (v.co.x>=0)!=(side=='L'):continue
   ns=kd.find_n(v.co,4)
   if ns[0][2]>.02:continue
   tot=sum(1/(d+.001)**2 for p,i,d in ns);upd[v.index]=sum((deltas[side][i]/(d+.001)**2 for p,i,d in ns),Vector())/tot
  if not upd:continue
  if not o.data.shape_keys:o.shape_key_add(name='Basis',from_mix=False)
  k=o.shape_key_add(name='DLHN_Wide_ClothEase_'+side,from_mix=False)
  for i,d in upd.items():k.data[i].co+=d
  driver(k,side)
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Optimization_Test.blend'));(P/'Shoulder_Corrective.json').write_text(json.dumps(report,indent=2));print(report)
