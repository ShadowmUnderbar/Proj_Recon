import bpy,bmesh,math,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);rig=bpy.data.objects['ElseIf_Game_Humanoid'];col=bpy.data.collections['ElseIf_Game']
s=(OUT/'weight_utils.py').read_text(encoding='utf-8');exec(compile(ast.Module(body=[n for n in ast.parse(s).body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'<weights>','exec'))
seams={}
for side in ['L','R']:
 source=bpy.data.objects['ElseIf_Jacket_Sleeve_'+side];target=bpy.data.objects['Game__ElseIf_Jacket_Sleeve_'+side];bm=bmesh.new();bm.from_mesh(source.data);bm.verts.ensure_lookup_table();ids=[v.index for v in bm.verts if v.is_boundary and v.co.z>1.19];bm.free();pts=[target.data.vertices[i].co.copy() for i in ids];tree=KDTree(len(pts))
 for i,p in enumerate(pts):tree.insert(p,i)
 tree.balance();seams[side]=(tree,pts)
for o in col.objects:
 if o.type!='MESH' or o['master_source']=='ElseIf_Mannequin':continue
 name=o['master_source'];o.vertex_groups.clear();groups={}
 for v in o.data.vertices:
  p=o.matrix_world@v.co;w=weights(p,name)
  if p.z>1.14 and .055<abs(p.x)<.24 and ('Jacket' in name or name.startswith('EL_')):
   side='L' if p.x>=0 else 'R';tree,pts=seams[side];q,idx,dist=tree.find(p);blend=1-smooth(.006,.047,dist)
   if blend>0:
    common=weights(q,'Jacket_Seam');w={n:w.get(n,0)*(1-blend)+common.get(n,0)*blend for n in set(w)|set(common)}
  items=sorted(w.items(),key=lambda a:-a[1])[:4];total=sum(value for n,value in items)
  for n,value in items:
   if value<.0001:continue
   if n not in groups:groups[n]=o.vertex_groups.new(name=n)
   groups[n].add([v.index],value/total,'REPLACE')
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print('Armhole seam weights unified across bodice, sleeve, and surface markings.')
