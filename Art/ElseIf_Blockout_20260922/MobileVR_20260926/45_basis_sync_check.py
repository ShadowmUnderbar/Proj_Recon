import bpy,numpy as np,json
m=bpy.data.objects['Mobile__Jacket'].data;a=np.array([list(v.co) for v in m.vertices]);b=np.array([list(v.co) for v in m.shape_keys.key_blocks[0].data]);d=np.linalg.norm(a-b,axis=1);print({'mesh_basis_different_vertices':int(np.sum(d>1e-7)),'max_mm':float(d.max())*1000})
