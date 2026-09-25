import bpy,json,hashlib,struct
from pathlib import Path
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/DesignTransfer_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.window.scene=base
# 再開時点で残っている既存形状・Shape Key・骨・撮影条件を記録する。
def fp(o):
 r={'type':o.type,'matrix':[list(x) for x in o.matrix_world]}
 if o.type=='MESH':
  r['geometry']=hashlib.sha256(b''.join(struct.pack('<3f',*v.co) for v in o.data.vertices)+repr([tuple(p.vertices) for p in o.data.polygons]).encode()+repr([[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]).encode()).hexdigest()
  r['shapes']={k.name:{'value':k.value,'hash':hashlib.sha256(b''.join(struct.pack('<3f',*v.co) for v in k.data)).hexdigest()} for k in o.data.shape_keys.key_blocks} if o.data.shape_keys else {}
 if o.type=='ARMATURE':r['bones']=repr([(b.name,list(b.head_local),list(b.tail_local),list(o.pose.bones[b.name].matrix_basis)) for b in o.data.bones])
 if o.type=='CAMERA':r['camera']=[o.data.type,o.data.lens,o.data.ortho_scale]
 if o.type=='LIGHT':r['light']=[o.data.type,o.data.energy,list(o.data.color),getattr(o.data,'size',0)]
 return r
protected={o.name:fp(o) for o in bpy.data.objects if not o.name.startswith(('EL_','NaturalPose__EL_'))}
(OUT/'Recovered_Protected_Audit.json').write_text(json.dumps(protected,ensure_ascii=False,indent=2),encoding='utf-8')
# 元の材質データと承認済み形状はメモリに残っているため、無地状態を別名で保全。
objects=[o for o in bpy.data.objects if o.type=='MESH' and (o.name.startswith(('ElseIf_Jacket_','NaturalPose__ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_')))]
slots={o.name:list(o.data.materials) for o in objects};cols=[bpy.data.collections[n] for n in ['ElseIf_Design_Basis','ElseIf_Design_NaturalPose']];flags=[(c.hide_render,c.hide_viewport) for c in cols]
try:
 for c in cols:c.hide_render=True;c.hide_viewport=True
 for o in objects:
  name=o.name.replace('NaturalPose__','');mat=None
  if name.startswith('ElseIf_Jacket_'):
   if 'Construction_Seams' in name:mat='ElseIf_Construction_Fold.002'
   elif 'Closure_Tape' in name:mat='ElseIf_Closure_Tape.001'
   elif 'Turnback' in name:mat='ElseIf_Facing_Inner.001'
   else:mat='ElseIf_Plain_WarmWhite'
  elif name.startswith('ElseIf_Shorts'):mat='ElseIf_Plain_Charcoal'
  elif name.startswith('ElseIf_Sock_'):mat='ElseIf_Plain_Socks'
  elif name.startswith('ElseIf_Shoe_'):
   mat='ElseIf_Sole_OffWhite' if name.endswith('Sole') else 'ElseIf_Shoe_Collar' if 'AnkleCollar' in name else 'ElseIf_Shoe_Tongue' if 'Tongue' in name else 'ElseIf_Shoe_Graphite'
  if mat and bpy.data.materials.get(mat):o.data.materials.clear();o.data.materials.append(bpy.data.materials[mat])
 base['recovery_note']='元ファイルが見当たらなくなったため、メモリ内の承認済み形状・旧材質から無地状態を再保存。追加デザインは非表示。元ファイルそのものの復元ではない。'
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Approved_SecondaryCloth_Recovered.blend'),copy=True)
finally:
 for o in objects:
  o.data.materials.clear()
  for m in slots[o.name]:o.data.materials.append(m)
 for c,(render,viewport) in zip(cols,flags):c.hide_render=render;c.hide_viewport=viewport
 base['recovery_note']='再開後、消失していたディスク保存をメモリから再保存。復元用Shape Keyと隠しバックアップは保持。'
# 参照図柄は背面カメラから読めるようU方向だけ反転。
for name in ['EL_Back_Reference_Artwork','NaturalPose__EL_Back_Reference_Artwork']:
 ob=bpy.data.objects[name];uv=ob.data.uv_layers.active.data;lo=min(x.uv.x for x in uv);hi=max(x.uv.x for x in uv)
 for x in uv:x.uv.x=lo+hi-x.uv.x
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Design_Transfer.blend'))
print('Recovered neutral backup and protected-state audit saved; back artwork orientation corrected')
