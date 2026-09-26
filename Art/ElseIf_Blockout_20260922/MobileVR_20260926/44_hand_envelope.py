import bpy,bmesh,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');old=P.parent/'LOD0Candidate_20260925';code=(old/'20_final_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(code).body:
 if isinstance(fn,ast.FunctionDef) and fn.name in ['closed','inside']:exec(compile(ast.Module(body=[fn],type_ignores=[]),'<envelope>','exec'))
s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);o=bpy.data.objects['Mobile__Jacket'];src=bpy.data.objects['LOD0__Jacket'];sm=src.data;sm.calc_loop_triangles();cm={int(k):v for k,v in json.loads((old/'Component_Map.json').read_text()).items()};selected={}
for side in ['L','R']:
 names=['ElseIf_Jacket_Sleeve_'+side,'ElseIf_Jacket_Cuff_'+side];trees={};flags={}
 for cid,meta in cm.items():
  if meta['source'] not in names:continue
  tris=[t for t in sm.loop_triangles if sm.attributes['DLHN_Component'].data[t.vertices[0]].value==cid];count=len(bpy.data.objects[meta['source']].data.vertices);flags[cid]=[all(sm.attributes['DLHN_ComponentVertex'].data[i].value<count for i in t.vertices) for t in tris];trees[cid]=BVHTree.FromPolygons([v.co for v in sm.vertices],[tuple(t.vertices) for t in tris],all_triangles=True)
 ids=[];o.data.calc_loop_triangles()
 for ti,t in enumerate(o.data.loop_triangles):
  cid=o.data.attributes['DLHN_Component'].data[t.vertices[0]].value
  if cid not in trees:continue
  p=sum((o.data.vertices[i].co for i in t.vertices),Vector())/3;hit=trees[cid].find_nearest(p)
  if flags[cid][hit[2]]:ids.append(ti)
 selected[side]=ids
report={};body=bpy.data.objects['Mobile__ElseIf_Mannequin']
for label,frame in json.loads((P/'Pose_Frames.json').read_text()).items():
 s.frame_set(frame);bpy.context.view_layer.update();e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles();vs=[v.co.copy() for v in m.vertices];fs=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear();eb=body.evaluated_get(bpy.context.evaluated_depsgraph_get());bm=eb.to_mesh();row={}
 for side,ids in selected.items():
  ff=[fs[i] for i in ids];used=sorted({i for f in ff for i in f});mp={i:j for j,i in enumerate(used)};tree=closed([vs[i] for i in used],[tuple(mp[i] for i in f) for f in ff]);groups={g.index for g in body.vertex_groups if 'J_Bip_'+side+'_' in g.name and any(n in g.name for n in ['Hand','Thumb','Index','Middle','Ring','Little'])};pts=[v.co.copy() for v in bm.vertices if any(g.group in groups and g.weight>.1 for g in v.groups)];outside=sum(not inside(tree,p) for p in pts);row[side]={'hand_points':len(pts),'outside_closed_sleeve_envelope':outside}
 eb.to_mesh_clear();report[label]=row
s.frame_set(1);(P/'Hand_Envelope_Final.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
