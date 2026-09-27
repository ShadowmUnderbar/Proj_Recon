import bpy,bmesh,json,math
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=bpy.data.scenes['ElseIf_MobileVR_Review'];bpy.context.window.scene=s;s.frame_set(1);o=bpy.data.objects['Mobile__Jacket'];src=bpy.data.objects['LOD0__Jacket'];col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];cm={int(k):v for k,v in json.loads((P.parent/'LOD0Candidate_20260925/Component_Map.json').read_text()).items()};cids={cid for cid,v in cm.items() if v['source'].startswith('ElseIf_Jacket_') and any(t in v['source'] for t in ['Collar','Cuff','Hem','Front_Facing'])}
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Before_Edge_Repair.blend'))
part=src.copy();part.data=src.data.copy();part.name='Mobile_EdgeRepair';col.objects.link(part)
for mod in list(part.modifiers):part.modifiers.remove(mod)
for ob,keep in [(o,False),(part,True)]:
 m=ob.data;bm=bmesh.new();bm.from_mesh(m);layer=bm.verts.layers.int['DLHN_Component'];bad=[v for v in bm.verts if (v[layer] in cids)!=keep];bmesh.ops.delete(bm,geom=bad,context='VERTS');bm.to_mesh(m);bm.free();m.update()
bm=bmesh.new();bm.from_mesh(part.data);before=sum(len(f.verts)-2 for f in bm.faces);bmesh.ops.dissolve_limit(bm,angle_limit=.10,use_dissolve_boundaries=False,verts=list(bm.verts),edges=list(bm.edges),delimit={'MATERIAL','UV'});bm.to_mesh(part.data);bm.free();part.data.update();after=sum(len(f.vertices)-2 for f in part.data.polygons)
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);part.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
for v,k in zip(o.data.vertices,o.data.shape_keys.key_blocks[0].data):v.co=k.co
o.data.update();r={'components':[cm[c]['source'] for c in sorted(cids)],'original_edge_triangles':before,'edge_triangles_after_planar_dissolve':after,'jacket_triangles':sum(len(f.vertices)-2 for f in o.data.polygons)};(P/'Edge_Repair.json').write_text(json.dumps(r,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'ElseIf_Game_MobileVR_Test.blend'));print(r)

