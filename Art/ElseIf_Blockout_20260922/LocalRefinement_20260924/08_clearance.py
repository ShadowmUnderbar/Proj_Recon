import bpy,ast,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];nat=bpy.data.scenes['ElseIf_NaturalPose_Validation']
src=ast.parse((OUT/'04_refine.py').read_text(encoding='utf-8-sig'))
for name in ['snap','smooth']:
 fn=next(n for n in src.body if isinstance(n,ast.FunctionDef) and n.name==name);exec(compile(ast.Module(body=[fn],type_ignores=[]),'<helper>','exec'))
jackets=['ElseIf_Jacket_Front_L','ElseIf_Jacket_Front_R','ElseIf_Jacket_Back','ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Sleeve_R'];old={}
for scene,prefix in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 for n in jackets:old[prefix+n]=snap(bpy.data.objects[prefix+n])
bpy.context.window.scene=base;bpy.context.view_layer.update();bodies=[snap(bpy.data.objects[n])[2] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']]
# 元の衣装が人体に近い支持部では補正を弱め、既存のクリアランスを守る。
for n in jackets:
 original=bpy.data.objects[n];bk=original.data.shape_keys.reference_key;secondary=original.data.shape_keys.key_blocks['Secondary_Cloth_Weight_20260924'];factors=[]
 for i,v in enumerate(bk.data):
  p=original.matrix_world@(v.co+(secondary.data[i].co-v.co)*secondary.value);distance=min(t.find_nearest(p)[3] for t in bodies);factors.append(smooth(.005,.014,distance))
 for prefix in ['', 'NaturalPose__']:
  o=bpy.data.objects[prefix+n];key=o.data.shape_keys.key_blocks['Local_Shoulder_Armhole_20260924'];basis=o.data.shape_keys.reference_key
  for i,v in enumerate(basis.data):key.data[i].co=v.co+(key.data[i].co-v.co)*factors[i%len(factors)]
new={}
for scene,prefix in [(base,''),(nat,'NaturalPose__')]:
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 for n in jackets:new[prefix+n]=snap(bpy.data.objects[prefix+n])
 for o in bpy.data.collections['ElseIf_Design_NaturalPose' if prefix else 'ElseIf_Design_Basis'].objects:
  if o.type!='MESH' or 'EL_Shoe_' in o.name:continue
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if p.z<1.16 or abs(p.x)<.071 or abs(p.x)>.25:continue
   hits=[(old[prefix+n][2].find_nearest(p),prefix+n) for n in jackets];hit,n=min(hits,key=lambda a:a[0][3]);loc,normal,idx,dist=hit
   if dist>.035:continue
   ov,faces,_=old[n];nv,_,_=new[n];a,b,c=faces[idx];mapped=barycentric_transform(loc,ov[a],ov[b],ov[c],nv[a],nv[b],nv[c]);v.co+=mapped-loc
  o.data.update()
bpy.context.window.scene=base;bpy.context.view_layer.update();print('Shoulder support clearance preserved by measured surface distance mask.')
