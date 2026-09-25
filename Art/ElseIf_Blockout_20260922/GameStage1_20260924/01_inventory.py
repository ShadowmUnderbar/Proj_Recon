import bpy,json,hashlib,struct
from pathlib import Path
out=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/GameStage1_20260924');s=bpy.data.scenes['ElseIf_Basis_Construction'];bpy.context.window.scene=s;bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();r={'file':bpy.data.filepath,'objects':[],'bones':[],'collections':[]}
for c in s.collection.children:r['collections'].append({'name':c.name,'hide':c.hide_render,'objects':len(c.all_objects)})
for o in s.objects:
 if o.type=='MESH' and not o.hide_render and o.visible_get():
  e=o.evaluated_get(dg);m=e.to_mesh();m.calc_loop_triangles();r['objects'].append({'name':o.name,'verts':len(m.vertices),'tris':len(m.loop_triangles),'raw':len(o.data.vertices),'materials':[a.name if a else None for a in o.data.materials],'mods':[(x.name,x.type) for x in o.modifiers],'groups':len(o.vertex_groups),'collections':[c.name for c in o.users_collection]});e.to_mesh_clear()
rig=bpy.data.objects['ElseIf_Humanoid']
for b in rig.data.bones:
 p=rig.pose.bones[b.name];r['bones'].append({'name':b.name,'parent':b.parent.name if b.parent else None,'deform':b.use_deform,'head':list(rig.matrix_world@p.head),'tail':list(rig.matrix_world@p.tail),'matrix': [list(x) for x in p.matrix],'constraints':[(c.name,c.type) for c in p.constraints]})
(out/'01_inventory.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'collections':r['collections'],'objects':r['objects'],'bones':len(r['bones'])}))
