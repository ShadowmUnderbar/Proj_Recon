import bpy,bmesh,json,ast,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');code=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(code).body:
 if isinstance(fn,ast.FunctionDef) and fn.name=='snap':exec(compile(ast.Module(body=[fn],type_ignores=[]),'<snap>','exec'))
s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;o=bpy.data.objects['Mobile__Jacket'];m=o.data;bad=set()
for frame in [1,11,21,31,41,51,61,71,81,91,101,111]:
 s.frame_set(frame);bpy.context.view_layer.update();a=snap(o);m.calc_loop_triangles();polys=[t.polygon_index for t in m.loop_triangles]
 for bn in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']:
  b=snap(bpy.data.objects['Mobile__'+bn]);bad.update(polys[i] for i,j in a['tree'].overlap(b['tree']))
s.frame_set(1);bm=bmesh.new();bm.from_mesh(m);layer=bm.verts.layers.int.get("Mobile_ContactNew") or bm.verts.layers.int.new("Mobile_ContactNew");bm.faces.ensure_lookup_table();bm.verts.index_update();oldcount=len(bm.verts);oldpositions={tuple(round(c,8) for c in v.co) for v in bm.verts};edges={e for i in bad for e in bm.faces[i].edges};bmesh.ops.subdivide_edges(bm,edges=list(edges),cuts=2,use_grid_fill=True);bmesh.ops.triangulate(bm,faces=list(bm.faces))
for v in bm.verts:v[layer]=0 if tuple(round(c,8) for c in v.co) in oldpositions else 1
bm.to_mesh(m);bm.free();m.update();src=bpy.data.objects['LOD0__Jacket'];sm=src.data;sm.calc_loop_triangles();ca=sm.attributes['DLHN_Component'];sv=[v.co.copy() for v in sm.vertices];faces={};trees={}
for cid in set(v.value for v in ca.data):
 faces[cid]=[tuple(t.vertices) for t in sm.loop_triangles if ca.data[t.vertices[0]].value==cid];trees[cid]=BVHTree.FromPolygons(sv,faces[cid],all_triangles=True)
count=0
for v in m.vertices:
 if not m.attributes['Mobile_ContactNew'].data[v.index].value:continue
 cid=m.attributes['DLHN_Component'].data[v.index].value;hit=trees[cid].find_nearest(v.co);tri=faces[cid][hit[2]];a,b,c=[sv[i] for i in tri];ab=b-a;ac=c-a;ap=hit[0]-a;d00=ab.dot(ab);d01=ab.dot(ac);d11=ac.dot(ac);d20=ap.dot(ab);d21=ap.dot(ac);den=d00*d11-d01*d01
 if abs(den)<1e-20:u=0;vv=0
 else:u=(d11*d20-d01*d21)/den;vv=(d00*d21-d01*d20)/den
 ws=[1-u-vv,u,vv];base=hit[0];groups={}
 for i,w in zip(tri,ws):
  for g in sm.vertices[i].groups:
   name=src.vertex_groups[g.group].name;groups[name]=groups.get(name,0)+w*g.weight
 top=sorted([(n,w) for n,w in groups.items() if w>1e-8],key=lambda x:-x[1])[:4];total=sum(w for n,w in top)
 for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
 for n,w in top:o.vertex_groups[n].add([v.index],w/total,'REPLACE')
 for k in m.shape_keys.key_blocks:
  sk=sm.shape_keys.key_blocks[k.name];basis=sm.shape_keys.key_blocks[0];delta=sum(((sk.data[i].co-basis.data[i].co)*w for i,w in zip(tri,ws)),Vector());k.data[v.index].co=base+delta
 count+=1
assert count<=len(m.vertices)-oldcount,(count,len(m.vertices),oldcount)
m.update();(P/'Contact_Retopology_Final.json').write_text(json.dumps({'faces_refined':len(bad),'vertices_added':count},indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print({'faces_refined':len(bad),'vertices_added':count})



