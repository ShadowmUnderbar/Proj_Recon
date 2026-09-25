import bpy,json,hashlib,struct,ast,math
from pathlib import Path
from mathutils import Vector,Matrix
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.window.scene=base;bpy.context.view_layer.update()
# Master全オブジェクトの指紋。以後編集対象はGameの独立データのみ。
src=(OUT.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
(OUT/'Master_Audit_Before.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects},ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'Master_Collections_Before.json').write_text(json.dumps({c.name:sorted(o.name for o in c.objects) for c in bpy.data.collections},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Master_Approved_Backup.blend'),copy=True)
inv=json.loads((OUT/'01_inventory.json').read_text(encoding='utf-8'));source=[bpy.data.objects[x['name']] for x in inv['objects'] if x['name']!='Review_Floor']
scene=base.copy();scene.name='ElseIf_Game_Validation'
for c in list(scene.collection.children):scene.collection.children.unlink(c)
for o in list(scene.collection.objects):scene.collection.objects.unlink(o)
scene.collection.children.link(bpy.data.collections['ElseIf_Review_Stage'])
col=bpy.data.collections.new('ElseIf_Game');scene.collection.children.link(col)
rig0=bpy.data.objects['ElseIf_Humanoid'];rig=rig0.copy();rig.data=rig0.data.copy();rig.name='ElseIf_Game_Humanoid';col.objects.link(rig);rig.animation_data_clear()
# 現在の承認済みA姿勢をゲーム用バインド姿勢として、同じ骨名・親子関係の複製へ焼き込む。
pose={b.name:rig0.pose.bones[b.name].matrix.copy() for b in rig0.data.bones};lengths={b.name:b.length for b in rig0.data.bones}
bpy.context.window.scene=scene
for o in bpy.context.selected_objects:o.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:
 b.use_connect=False;b.matrix=pose[b.name];b.length=lengths[b.name]
for side in ['L','R']:
 fore=rig.data.edit_bones['J_Bip_'+side+'_LowerArm'];hand=rig.data.edit_bones['J_Bip_'+side+'_Hand'];b=rig.data.edit_bones.new('DLHN_SleeveTip_'+side);b.head=hand.head;b.tail=b.head+(fore.tail-fore.head).normalized()*.14;b.parent=fore;b.use_deform=True
bpy.ops.object.mode_set(mode='OBJECT')
for pb in rig.pose.bones:
 pb.matrix_basis=Matrix.Identity(4)
 for c in list(pb.constraints):pb.constraints.remove(c)
for side in ['L','R']:
 pb=rig.pose.bones['DLHN_SleeveTip_'+side];c=pb.constraints.new('COPY_ROTATION');c.name='Soft_Wrist_Follow_12pct';c.target=rig;c.subtarget='J_Bip_'+side+'_Hand';c.target_space='LOCAL';c.owner_space='LOCAL';c.mix_mode='AFTER';c.influence=.12
rig['rig_stage']='DLHN Stage1 / existing Humanoid hierarchy + independent sleeve-tip helpers; no automatic weights'
# Masterの評価済み外観を複製。共有メッシュ・Shape Keyは一切編集しない。
bpy.context.window.scene=base;bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();mapping={}
for original in source:
 me=bpy.data.meshes.new_from_object(original.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);o=bpy.data.objects.new('Game__'+original.name,me);o.matrix_world=original.matrix_world.copy();col.objects.link(o);o['master_source']=original.name
 for vg in original.vertex_groups:o.vertex_groups.new(name=vg.name)
 mapping[original.name]=o.name
# HeadSocketを複製し、複製Humanoidへ向け直す。
so=bpy.data.objects['HeadSocket'];socket=so.copy();socket.name='Game__HeadSocket';col.objects.link(socket)
for c in socket.constraints:
 if getattr(c,'target',None)==rig0:c.target=rig
socket.matrix_world=so.matrix_world.copy()
bpy.context.window.scene=scene;bpy.context.view_layer.update();rig.select_set(False)
(OUT/'Game_Mapping.json').write_text(json.dumps(mapping,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Independent Game scene and collection created, 124 meshes, 74-bone cloned Humanoid.')
