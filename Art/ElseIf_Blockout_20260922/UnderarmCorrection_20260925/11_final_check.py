import bpy,json,hashlib,ast,struct
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/UnderarmCorrection_20260925');s=bpy.data.scenes['ElseIf_Underarm_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized_Corrected'];changes=[];stats={'vertices':0,'triangles':0,'materials':set(),'new_keys':0};normal=[]
for o in col.objects:
 if o.type!='MESH':continue
 src=bpy.data.objects['Opt__'+o['master_source']];m=o.data;m.calc_loop_triangles();stats['vertices']+=len(m.vertices);stats['triangles']+=len(m.loop_triangles);stats['materials'].update(x.name for x in m.materials if x)
 if len(m.vertices)!=len(src.data.vertices) or [tuple(f.vertices) for f in m.polygons]!=[tuple(f.vertices) for f in src.data.polygons]:changes.append(o.name)
 if any((a.co-b.co).length>1e-7 for a,b in zip(m.vertices,src.data.vertices)):changes.append(o.name+' base geometry')
 if m.shape_keys:stats['new_keys']+=sum(k.name.startswith('DLHN_Underarm_Drape_') for k in m.shape_keys.key_blocks)
for frame in [1,11,111,21,71,41,61]:
 s.frame_set(frame);bpy.context.view_layer.update();normal.append({'frame':frame,'values':{k.name:k.value for k in bpy.data.objects['Fix__ElseIf_Jacket_Back'].data.shape_keys.key_blocks if k.name.startswith('DLHN_Underarm_Drape_')}})
s.frame_set(1)
for sc in bpy.data.scenes:bpy.context.window.scene=sc;bpy.context.view_layer.update()
src=(P.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'));before=json.loads((P.parent/'GameStage1_20260924/Master_Audit_Before.json').read_text());master=[n for n,r in before.items() if n not in bpy.data.objects or fp(bpy.data.objects[n])!=r]
stats['material_count']=len(stats.pop('materials'));stats['topology_or_basis_changes']=changes;stats['master_changes']=master;stats['activation']=normal;stats['bones']=len(bpy.data.objects['ElseIf_Underarm_Humanoid'].data.bones);(P/'Final_Check.json').write_text(json.dumps(stats,indent=2));bpy.context.window.scene=s;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Underarm_Corrected.blend'));print(stats)
