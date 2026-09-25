import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');fix=bpy.data.scenes['ElseIf_Underarm_Review'];orig=bpy.data.scenes['ElseIf_Optimized_Review'];frames=json.loads((P/'Pose_Frames.json').read_text());report={}
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=[p.co.copy() for p in m.vertices];e.to_mesh_clear();return v
parts=['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R']
for label in ['Base','A_Natural','B_Side90','G_WideOpen','D_AsymmetricAim','F_OneArmBack']:
 f=frames[label];bpy.context.window.scene=orig;orig.frame_set(f);bpy.context.view_layer.update();before={n:snap(bpy.data.objects['Opt__'+n]) for n in parts}
 bpy.context.window.scene=fix;fix.frame_set(f);bpy.context.view_layer.update();ds={k:[] for k in ['shoulder','underarm_bodice','lower_bodice','hem','outer_sleeve']}
 for n in parts:
  o=bpy.data.objects['Fix__'+n];vs=snap(o)
  for i,v in enumerate(o.data.vertices):
   p=v.co;d=vs[i]-before[n][i];inner=-.906308*(abs(p.x)-.08914964)-.422618*(p.z-1.35648966)
   if p.z>1.315:ds['shoulder'].append(d)
   if 'Sleeve' in n and inner<-.015:ds['outer_sleeve'].append(d)
   if 'Sleeve' not in n:
    if 1.15<p.z<1.25 and abs(p.x)>.08:ds['underarm_bodice'].append(d)
    if .9<p.z<1.1:ds['lower_bodice'].append(d)
    if p.z<.86:ds['hem'].append(d)
 report[label]={k:{'max_displacement_mm':max(d.length for d in vs)*1000,'mean_dz_mm':sum(d.z for d in vs)/len(vs)*1000,'min_dz_mm':min(d.z for d in vs)*1000} for k,vs in ds.items() if vs}
orig.frame_set(1);fix.frame_set(1);(P/'Motion_Measurement.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
