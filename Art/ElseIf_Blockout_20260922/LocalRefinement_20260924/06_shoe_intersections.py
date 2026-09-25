import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924');bpy.context.window.scene=bpy.data.scenes['ElseIf_Basis_Construction']
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices) for p in m.polygons];e.to_mesh_clear();return vs,fs,BVHTree.FromPolygons(vs,fs)
v,f,t=snap(bpy.data.objects['ElseIf_Mannequin']);r={}
for value in [0,1]:
 for o in bpy.data.objects:
  if o.name.startswith('ElseIf_Shoe_'):o.data.shape_keys.key_blocks['Local_StreetShoe_Form_20260924'].value=value
 bpy.context.view_layer.update();r[value]={}
 for part in ['Upper','Sole']:
  o=bpy.data.objects['ElseIf_Shoe_L_'+part];vv,ff,tt=snap(o);pairs=tt.overlap(t);ps=[sum((vv[i] for i in ff[a]),Vector())/len(ff[a]) for a,b in pairs];r[value][part]={'pairs':len(pairs),'bounds':[[min(p[i] for p in ps),max(p[i] for p in ps)] for i in range(3)],'samples':[list(p) for p in ps[::max(1,len(ps)//8)]]}
print(json.dumps(r));(out/'06_shoe_intersections.json').write_text(json.dumps(r,indent=2))
