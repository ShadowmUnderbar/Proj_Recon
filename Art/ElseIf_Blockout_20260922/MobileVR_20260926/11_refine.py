import bpy,bmesh,json,ast,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');code=(P/'08_stress.py').read_text(encoding='utf-8-sig')
for fn in ast.parse(code).body:
 if isinstance(fn,ast.FunctionDef) and fn.name=='snap':exec(compile(ast.Module(body=[fn],type_ignores=[]),'<snap>','exec'))
frames=[1,41,51,61,71,101];bad={n:set() for n in ['Jacket','Socks_L','Socks_R']};log={}
for frame in frames:
 bpy.context.window.scene=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();olds={n:snap(bpy.data.objects['LOD0__'+n]) for n in bad}
 bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
 for n,b in olds.items():
  ob=bpy.data.objects['Mobile__'+n];a=snap(ob);trees={cid:BVHTree.FromPolygons(b['v'],[f for f,c in zip(b['f'],b['c']) if c==cid],all_triangles=True) for cid in set(b['c'])}
  # 全面は三角形に確定済み。
  ob.data.calc_loop_triangles();polyids=[t.polygon_index for t in ob.data.loop_triangles]
  for fi,(f,c) in enumerate(zip(a['f'],a['c'])):
   p=sum((a['v'][i] for i in f),Vector())/3
   if trees[c].find_nearest(p)[3]>.0025:bad[n].add(polyids[fi])
for sn in ['ElseIf_LOD0_Review','ElseIf_MobileVR_Review']:bpy.data.scenes[sn].frame_set(1)
bpy.context.window.scene=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.view_layer.update()
for n,ids in bad.items():
 o=bpy.data.objects['Mobile__'+n];src=bpy.data.objects['LOD0__'+n];m=o.data;before=len(m.vertices);bm=bmesh.new();bm.from_mesh(m);bm.faces.ensure_lookup_table();edges={e for i in ids for e in bm.faces[i].edges};bmesh.ops.subdivide_edges(bm,edges=list(edges),cuts=1,use_grid_fill=True);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();m.update()
 sm=src.data;sm.calc_loop_triangles();ca=sm.attributes['DLHN_Component'];trees={};faces={};sv=[v.co.copy() for v in sm.vertices]
 for cid in set(a.value for a in ca.data):
  faces[cid]=[tuple(t.vertices) for t in sm.loop_triangles if ca.data[t.vertices[0]].value==cid];trees[cid]=BVHTree.FromPolygons(sv,faces[cid],all_triangles=True)
 ii=[];ww=[];coords=[]
 for v in m.vertices:
  cid=m.attributes['DLHN_Component'].data[v.index].value;hit=trees[cid].find_nearest(v.co);tri=faces[cid][hit[2]];a,b,c=[sv[j] for j in tri];ab=b-a;ac=c-a;ap=hit[0]-a;d00=ab.dot(ab);d01=ab.dot(ac);d11=ac.dot(ac);d20=ap.dot(ab);d21=ap.dot(ac);den=d00*d11-d01*d01
  if abs(den)<1e-20:u=0;vv=0
  else:u=(d11*d20-d01*d21)/den;vv=(d00*d21-d01*d20)/den
  weights=[1-u-vv,u,vv];ii.append(tri);ww.append(weights);coords.append(list(hit[0]));groups={}
  for j,w in zip(tri,weights):
   for g in sm.vertices[j].groups:
    name=src.vertex_groups[g.group].name;groups[name]=groups.get(name,0)+g.weight*w
  top=sorted([(gn,w) for gn,w in groups.items() if w>1e-8],key=lambda x:-x[1])[:4];total=sum(w for _,w in top)
  for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
  for gn,w in top:
   vg=o.vertex_groups.get(gn) or o.vertex_groups.new(name=gn);vg.add([v.index],w/total,'REPLACE')
 ii=np.array(ii);ww=np.array(ww);base=np.array(coords,np.float32);m.vertices.foreach_set('co',base.ravel())
 if m.shape_keys:
  origbase=np.array([list(v.co) for v in sm.shape_keys.key_blocks[0].data]);m.shape_keys.key_blocks[0].data.foreach_set('co',base.ravel())
  for k in m.shape_keys.key_blocks[1:]:
   delta=np.array([list(v.co) for v in sm.shape_keys.key_blocks[k.name].data])-origbase;data=base+(delta[ii]*ww[:,:,None]).sum(axis=1);k.data.foreach_set('co',data.astype(np.float32).ravel())
 m.update();log[n]={'refined_faces':len(ids),'vertices_before':before,'vertices_after':len(m.vertices)}
(P/'Adaptive_Refinement.json').write_text(json.dumps(log,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(log)

