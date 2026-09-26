import bpy,json,numpy as np
from pathlib import Path
from mathutils.kdtree import KDTree
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1)
o=bpy.data.objects['Mobile__Jacket'];src=bpy.data.objects['LOD0__Jacket'];m=o.data;sm=src.data;ca=sm.attributes['DLHN_Component'];trees={};coords=np.array([list(v.co) for v in sm.vertices]);report={'exact_vertices':0,'ambiguous_vertices':0}
for cid in set(a.value for a in ca.data):
 ids=[v.index for v in sm.vertices if ca.data[v.index].value==cid];tree=KDTree(len(ids))
 for i in ids:tree.insert(sm.vertices[i].co,i)
 tree.balance();trees[cid]=tree
keys={k.name:np.array([list(v.co) for v in k.data]) for k in sm.shape_keys.key_blocks};base=keys['Basis'];newkeys={k.name:np.array([list(v.co) for v in k.data]) for k in m.shape_keys.key_blocks}
for v in m.vertices:
 cid=m.attributes['DLHN_Component'].data[v.index].value;hits=trees[cid].find_range(v.co,.0000005)
 if not hits:continue
 newweights={o.vertex_groups[g.group].name:g.weight for g in v.groups}
 def err(i):
  ow={src.vertex_groups[g.group].name:g.weight for g in sm.vertices[i].groups};return sum((ow.get(k,0)-newweights.get(k,0))**2 for k in set(ow)|set(newweights))
 hit=min(hits,key=lambda h:err(h[1]));idx=hit[1];report['exact_vertices']+=1;report['ambiguous_vertices']+=len(hits)>1
 for name,a in newkeys.items():a[v.index]=np.array(v.co)+(keys[name][idx]-base[idx])
for k in m.shape_keys.key_blocks:k.data.foreach_set('co',newkeys[k.name].astype(np.float32).ravel())
m.update();(P/'Exact_Protected_Key_Transfer.json').write_text(json.dumps(report,indent=2));print(report)
