import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
OUT=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');scene=bpy.data.scenes['ElseIf_Game_Validation'];bpy.context.window.scene=scene;scene.frame_set(1);col=bpy.data.collections['ElseIf_Game'];report=[]
targets=['ElseIf_Anatomy_Restoration','ElseIf_Shorts','ElseIf_Sock_L','ElseIf_Sock_R']+['ElseIf_Shoe_'+s+'_'+p for s in ['L','R'] for p in ['Sole','Upper','AnkleCollar','Tongue']]
for name in targets:
 game=bpy.data.objects['Game__'+name];old=game.data;old.calc_loop_triangles();ov=[v.co.copy() for v in old.vertices];of=[tuple(t.vertices) for t in old.loop_triangles];og=[[(game.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in old.vertices];tree=BVHTree.FromPolygons(ov,of,all_triangles=True)
 source=bpy.data.objects[name];temp=source.copy();temp.data=source.data.copy();temp.name='_GAME_REDUCTION_TEMP';col.objects.link(temp)
 levels=[]
 for mod in temp.modifiers:
  if mod.type=='SUBSURF':
   before=mod.levels;mod.levels=0 if name=='ElseIf_Anatomy_Restoration' else max(0,before-1);mod.render_levels=mod.levels;levels.append([before,mod.levels])
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();mesh=bpy.data.meshes.new_from_object(temp.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);mesh.name=game.name+'_Stage1Mesh';bpy.data.objects.remove(temp,do_unlink=True);game.data=mesh;game.vertex_groups.clear();groups={};distances=[]
 for v in mesh.vertices:
  p=game.matrix_world@v.co;hit=tree.find_nearest(p);distances.append(hit[3]);a,b,c=of[hit[2]];bc=barycentric_transform(hit[0],ov[a],ov[b],ov[c],Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)));w={}
  for idx,t in zip([a,b,c],bc):
   for n,value in og[idx]:w[n]=w.get(n,0)+value*max(0,t)
  items=sorted(w.items(),key=lambda a:-a[1])[:4];total=sum(v for n,v in items)
  for n,value in items:
   if n not in groups:groups[n]=game.vertex_groups.new(name=n)
   groups[n].add([v.index],value/total,'REPLACE')
 mesh.calc_loop_triangles();report.append({'object':name,'old_triangles':len(of),'new_triangles':len(mesh.loop_triangles),'subdivision_levels':levels,'max_surface_distance_mm':max(distances)*1000,'mean_surface_distance_mm':sum(distances)/len(distances)*1000});game['reduction_note']='局所的なSubdivision段階削減。評価済み形状からウェイト補間。全体Decimateなし。'
(OUT/'15_reduction_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ElseIf_Game_Stage1.blend'));print(json.dumps(report))
