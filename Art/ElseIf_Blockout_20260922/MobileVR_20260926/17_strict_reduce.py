import bpy,bmesh,json,ast,math,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');OLD=P.parent/'LOD0Candidate_20260925'
s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_MobileVR_Test']
code=(OLD/'16_merge_renderers.py').read_text(encoding='utf-8-sig')
for n in ast.parse(code).body:
 if isinstance(n,ast.FunctionDef) and n.name in ['driver_info','restore_driver']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<driver>','exec'))
for ob in list(bpy.data.objects):
 if ob.name.startswith('MobilePart_'):bpy.data.objects.remove(ob,do_unlink=True)
components={int(k):v for k,v in json.loads((OLD/'Component_Map.json').read_text()).items()};report=[]
def ratio(n):
 if n.startswith('ElseIf_Jacket_'):
  if 'Sleeve' in n:return .40
  if 'Cuff' in n:return .48
  if 'Collar' in n or 'Hem' in n:return .40
  return .23
 if 'Sock' in n:return .22
 if 'Shoe' in n:return .18 if 'Sole' in n else .25
 if 'Shorts' in n:return .22
 if 'Artwork' in n or 'Print_Base' in n:return .5
 return .45
for original in list(col.objects):
 if original.type!='MESH' or 'Mannequin' in original.name or 'Anatomy' in original.name:continue
 m=original.data;m.calc_loop_triangles();ca=m.attributes['DLHN_Component'];cids=sorted(set(a.value for a in ca.data));parts=[];keys=list(m.shape_keys.key_blocks) if m.shape_keys else [];specs={k.name:driver_info(original,k) for k in keys[1:]}
 keyarrays={k.name:np.array([list(v.co) for v in k.data],dtype=np.float32) for k in keys};allcoords=np.array([list(v.co) for v in m.vertices],dtype=np.float32)
 for cid in cids:
  name=components[cid]['source'];ids=[v.index for v in m.vertices if ca.data[v.index].value==cid];remap={i:j for j,i in enumerate(ids)};polys=[f for f in m.polygons if ca.data[f.vertices[0]].value==cid];faces=[tuple(remap[i] for i in f.vertices) for f in polys]
  mesh=bpy.data.meshes.new('MobilePart_'+name);mesh.from_pydata(allcoords[ids].tolist(),[],faces);mesh.update();part=bpy.data.objects.new('MobilePart_'+name,mesh);col.objects.link(part);parts.append(part)
  for mat in m.materials:mesh.materials.append(mat)
  for f,src in zip(mesh.polygons,polys):f.material_index=src.material_index;f.use_smooth=src.use_smooth
  for uv in m.uv_layers:
   layer=mesh.uv_layers.new(name=uv.name)
   for f,src in zip(mesh.polygons,polys):
    for li,oldli in zip(f.loop_indices,src.loop_indices):layer.data[li].uv=uv.data[oldli].uv
   layer.active_render=uv.active_render
  for vg in original.vertex_groups:part.vertex_groups.new(name=vg.name)
  for j,i in enumerate(ids):
   for g in m.vertices[i].groups:part.vertex_groups[g.group].add([j],g.weight,'REPLACE')
  attr=mesh.attributes.new('DLHN_Component','INT','POINT');attr.data.foreach_set('value',[cid]*len(ids))
  mesh.calc_loop_triangles();before=len(mesh.loop_triangles);rate=ratio(name)
  # 各部品内で開口境界・関節付近を相対的に保護する。
  protect=part.vertex_groups.new(name='_MobileReduction')
  edgecount={}
  for f in mesh.polygons:
   for a,b in f.edge_keys:edgecount[tuple(sorted((a,b)))]=edgecount.get(tuple(sorted((a,b))),0)+1
  boundary={i for e,c in edgecount.items() if c==1 for i in e}
  for v in mesh.vertices:
   w=1.0;p=v.co
   if v.index in boundary:w=0.0
   if 'Sleeve' in name:
    if p.z>1.22 or p.z<1.00:w=min(w,0.0)
   if 'Sock' in name and (.46<p.z<.60 or p.z<.27):w=min(w,0.0)
   if name.startswith('ElseIf_Jacket_') and (p.z>1.19 or p.z<.865):w=min(w,0.0)
   protect.add([v.index],w,'REPLACE')
  if before>100:
   # Shape Keyのない作業部品にだけ局所Decimateを適用。後で元部品から補正量を補間する。
   dec=part.modifiers.new('Regional_Mobile_Reduction','DECIMATE');dec.ratio=rate;dec.vertex_group=protect.name;dec.vertex_group_factor=8.0;dec.use_collapse_triangulate=True
   bpy.context.view_layer.objects.active=part;bpy.ops.object.modifier_apply(modifier=dec.name)
  if part.vertex_groups.get('_MobileReduction'):part.vertex_groups.remove(part.vertex_groups['_MobileReduction'])
  mesh=part.data
  # 三角形上の重心座標から全Correctiveを転写し、同じ領域の補正だけを参照する。
  if keys:
   sourceverts=[Vector(allcoords[i]) for i in ids];sourcefaces=[]
   for t in m.loop_triangles:
    if ca.data[t.vertices[0]].value==cid:sourcefaces.append(tuple(remap[i] for i in t.vertices))
   tree=BVHTree.FromPolygons(sourceverts,sourcefaces,all_triangles=True);ii=[];ww=[]
   for v in mesh.vertices:
    hit=tree.find_nearest(v.co);tri=sourcefaces[hit[2]];a,b,c=[sourceverts[j] for j in tri];ab=b-a;ac=c-a;ap=hit[0]-a;d00=ab.dot(ab);d01=ab.dot(ac);d11=ac.dot(ac);d20=ap.dot(ab);d21=ap.dot(ac);den=d00*d11-d01*d01
    if abs(den)<1e-20:u=0;vv=0
    else:u=(d11*d20-d01*d21)/den;vv=(d00*d21-d01*d20)/den
    ii.append([ids[j] for j in tri]);ww.append([1-u-vv,u,vv])
   ii=np.array(ii);ww=np.array(ww);base=np.array([list(v.co) for v in mesh.vertices]);part.shape_key_add(name='Basis',from_mix=False)
   for key in keys[1:]:
    delta=keyarrays[key.name]-keyarrays[keys[0].name];newco=base+(delta[ii]*ww[:,:,None]).sum(axis=1);k=part.shape_key_add(name=key.name,from_mix=False);k.data.foreach_set('co',newco.astype(np.float32).ravel());k.slider_min=key.slider_min;k.slider_max=key.slider_max;k.value=key.value
  # 補間後も最大4影響へ正規化。
  for v in mesh.vertices:
   gs=sorted([(g.group,g.weight) for g in v.groups if g.weight>1e-8],key=lambda x:-x[1]);top=gs[:4];total=sum(w for _,w in top)
   for g,w in gs:part.vertex_groups[g].remove([v.index])
   for g,w in top:part.vertex_groups[g].add([v.index],w/total,'REPLACE')
  mesh.calc_loop_triangles();report.append({'mesh':original.name,'component':name,'before':before,'after':len(mesh.loop_triangles),'requested_ratio':rate})
  (P/'Regional_Reduction_Strict.json').write_text(json.dumps(report,indent=2))
 # 全部品が同じキー一覧を持つ状態で統合。
 bpy.ops.object.select_all(action='DESELECT')
 for part in parts:part.select_set(True)
 active=max(parts,key=lambda o:len(o.data.vertices));bpy.context.view_layer.objects.active=active;bpy.ops.object.join();oldname=original.name
 for mod in original.modifiers:
  if mod.type=='ARMATURE':new=active.modifiers.new(mod.name,'ARMATURE');new.object=mod.object;new.use_deform_preserve_volume=mod.use_deform_preserve_volume
 active.parent=original.parent;active.matrix_world=original.matrix_world.copy()
 for key,val in original.items():active[key]=val
 bpy.data.objects.remove(original,do_unlink=True);active.name=oldname
 if keys:
  for name,spec in specs.items():restore_driver(active.data.shape_keys.key_blocks[name],spec)
 oldmats=list(active.data.materials);unique=[];matmap={}
 for i,mat in enumerate(oldmats):
  if mat not in unique:unique.append(mat)
  matmap[i]=unique.index(mat)
 idx=[matmap[f.material_index] for f in active.data.polygons];active.data.materials.clear()
 for mat in unique:active.data.materials.append(mat)
 for f,i in zip(active.data.polygons,idx):f.material_index=i
 if active.data.uv_layers.get('DesignUV'):active.data.uv_layers.active=active.data.uv_layers['DesignUV'];active.data.uv_layers['DesignUV'].active_render=True
 print(oldname,sum(r['before'] for r in report if r['mesh']==oldname),sum(r['after'] for r in report if r['mesh']==oldname),flush=True)
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print('Regional reduction complete')


