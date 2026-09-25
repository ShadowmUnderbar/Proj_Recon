import bpy,math,json,ast
from pathlib import Path
from mathutils import Vector,Matrix
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);rig=bpy.data.objects['ElseIf_Game_Humanoid'];col=bpy.data.collections['ElseIf_Game']
s=(OUT/'03_weights.py').read_text(encoding='utf-8-sig');s=s.replace("arm=smooth(.070,.152,x)*smooth(1.150,1.280,p.z)","arm=smooth(.046,.140,x)*smooth(1.105,1.205,p.z)")
s=s.replace("smooth(.155,.235,s)","smooth(.110,.180,s)").replace("else:arm*=smooth(1.17,1.22,p.z)","else:arm*=smooth(1.105,1.205,p.z)").replace("shoulder=.45*","shoulder=.25*")
(OUT/'weight_utils.py').write_text(s[:s.index('count=0')],encoding='utf-8')
for fn in [n for n in ast.parse(s).body if isinstance(n,ast.FunctionDef)]:exec(compile(ast.Module(body=[fn],type_ignores=[]),'<weights>','exec'))
for o in col.objects:
 if o.type!='MESH' or o['master_source']=='ElseIf_Mannequin':continue
 o.vertex_groups.clear();groups={}
 for v in o.data.vertices:
  w=weights(o.matrix_world@v.co,o['master_source'])
  for n,value in w.items():
   if n not in groups:groups[n]=o.vertex_groups.new(name=n)
   groups[n].add([v.index],value,'REPLACE')
scene.cycles.samples=24;scene.camera=bpy.data.objects['Game_Cam_Stress_ThreeQuarter'];frames=json.loads((OUT/'Pose_Frames.json').read_text())
for name in ['B_Side90','D_AsymmetricAim','E_Elbow90']:
 scene.frame_set(frames[name]);scene.render.filepath=str(OUT/('WeightsV2_'+name+'.png'));bpy.ops.render.render(write_still=True)
scene.frame_set(1);scene.cycles.samples=48;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Shoulder/upper-arm follow weights revised and tested.')
