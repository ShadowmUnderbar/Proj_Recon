import bpy,json,ast,hashlib,struct,bmesh
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924');base=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.window.scene=base
src=(out.parent/'DesignTransfer_20260924/07_preserve_recover.py').read_text(encoding='utf-8-sig');fn=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name=='fp');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<fp>','exec'))
(out/'Before_Audit.json').write_text(json.dumps({o.name:fp(o) for o in bpy.data.objects},ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ElseIf_Approved_Design_Backup.blend'),copy=True)
r={};vs=[];fs=[]
for name in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
 o=bpy.data.objects[name];e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();off=len(vs);vs += [o.matrix_world@v.co for v in m.vertices];fs += [tuple(off+i for i in p.vertices) for p in m.polygons];e.to_mesh_clear()
tree=BVHTree.FromPolygons(vs,fs)
for label,origin,direction in [('clavicle_front',(.055,-.3,1.365),(0,1,0)),('shoulder_tip',(.4,.02525,1.355),(-1,0,0)),('shoulder_top',(.08915,.02525,1.55),(0,0,-1)),('upper_arm_front',(.125,-.3,1.285),(0,1,0)),('axilla',(.105,.02525,1.20),(0,0,1))]:
 h=tree.ray_cast(Vector(origin),Vector(direction));r[label]=list(h[0]) if h[0] is not None else None
for name in ['ElseIf_Jacket_Sleeve_L','ElseIf_Jacket_Front_L','ElseIf_Shoe_L_Upper','ElseIf_Shoe_L_Sole','ElseIf_Shoe_L_AnkleCollar','ElseIf_Shoe_L_Tongue']:
 o=bpy.data.objects[name];bm=bmesh.new();bm.from_mesh(o.data);boundary=[list(o.matrix_world@v.co) for v in bm.verts if v.is_boundary];bm.free();r[name]={'boundary':boundary[:300],'vertices':[list(o.matrix_world@v.co) for v in o.data.vertices] if 'Shoe' in name else [],'matrix':[list(x) for x in o.matrix_world]}
(out/'02_anatomy_topology.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in r.items() if not k.startswith('ElseIf')}));print('Backup and anatomy/topology inspection saved')
