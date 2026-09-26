import bpy,bmesh,json,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925');s=bpy.data.scenes['ElseIf_LOD0_Review'];bpy.context.window.scene=s;s.frame_set(1);col=bpy.data.collections['ElseIf_Game_Optimized'];report=[]
def priority(n,p):
 z=p.z;x=abs(p.x)
 if n.startswith('ElseIf_Jacket_'):
  if any(k in n for k in ['Sleeve','Cuff']):return 3
  if z>1.17 or (x>.13 and z>1.10):return 3
  if z<.86 or 'Collar' in n or 'Hem' in n:return 2
  return 1
 if 'Sock' in n:return 2 if .44<z<.66 or z<.3 else 1
 if 'Shoe' in n:return 1 if 'Sole' in n and z<.075 else 2
 if n.startswith('EL_'):
  if x>.12 and z>1.10:return 3
  return 1 if .86<z<1.15 else 2
 return 2
for o in col.objects:
 if o.type!='MESH':continue
 m=o.data;attr=m.attributes.get('DLHN_DeformPriority') or m.attributes.new('DLHN_DeformPriority','INT','POINT')
 for v in m.vertices:attr.data[v.index].value=priority(o.get('master_source',''),v.co)
 tag=m.attributes.get('DLHN_PreReduceVertex') or m.attributes.new('DLHN_PreReduceVertex','INT','POINT')
 for v in m.vertices:tag.data[v.index].value=v.index
 oldv=len(m.vertices);m.calc_loop_triangles();oldt=len(m.loop_triangles);counts={str(k):sum(d.value==k for d in attr.data) for k in [1,2,3]}
 if counts['1']<100:report.append({'object':o.name,'before_vertices':oldv,'after_vertices':oldv,'before_triangles':oldt,'after_triangles':oldt,'priority_vertices':counts});continue
 bm=bmesh.new();bm.from_mesh(m);layer=bm.verts.layers.int.get('DLHN_DeformPriority');low=[v for v in bm.verts if v[layer]==1 and all(e.other_vert(v)[layer]==1 for e in v.link_edges)];ls=set(low);edges=[e for e in bm.edges if all(v in ls for v in e.verts) and e.calc_length()<.018 and len(e.link_faces)==2]
 # 頂点を移動せず、保護領域から離れたほぼ平坦な辺のみを溶解。
 bmesh.ops.dissolve_limit(bm,angle_limit=math.radians(3.5),use_dissolve_boundaries=False,verts=low,edges=edges,delimit={'MATERIAL','SEAM','SHARP','UV'})
 # ゲーム側の三角形分割を固定し、ポーズによるN-gon再分割の変化を避ける。
 bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4],quad_method='BEAUTY',ngon_method='BEAUTY');bm.to_mesh(m);bm.free();m.update();m.calc_loop_triangles()
 assert not m.shape_keys or all(len(k.data)==len(m.vertices) for k in m.shape_keys.key_blocks)
 report.append({'object':o.name,'before_vertices':oldv,'after_vertices':len(m.vertices),'before_triangles':oldt,'after_triangles':len(m.loop_triangles),'priority_vertices':counts})
(P/'Regional_Reduction.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_LOD0_Candidate.blend'));print('Total triangles',sum(r['before_triangles'] for r in report),sum(r['after_triangles'] for r in report));print([r for r in report if r['before_triangles']!=r['after_triangles']])
