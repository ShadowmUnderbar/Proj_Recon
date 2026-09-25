import bpy,json
from mathutils.bvhtree import BVHTree
from pathlib import Path
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LocalRefinement_20260924');bpy.context.window.scene=bpy.data.scenes['ElseIf_Basis_Construction']
def snap(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices) for p in m.polygons];gs=[[(g.group,g.weight) for g in v.groups] for v in m.vertices];e.to_mesh_clear();return vs,fs,gs,BVHTree.FromPolygons(vs,fs)
body=bpy.data.objects['ElseIf_Mannequin'];v,f,g,t=snap(body);r={'collisions':{}}
for side in ['L','R']:
 ids={g.index for g in body.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(w in g.name for w in ['Foot','Toe'])};points=[p for p,gs in zip(v,g) if any(i in ids and w>.1 for i,w in gs)];r[side]={'count':len(points),'bounds':[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)]}
 for part in ['Sole','Upper','AnkleCollar','Tongue']:
  o=bpy.data.objects['ElseIf_Shoe_'+side+'_'+part];vv,ff,gg,tt=snap(o);pairs=tt.overlap(t);r['collisions'][o.name]=len(pairs)
(out/'05_foot_check.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
