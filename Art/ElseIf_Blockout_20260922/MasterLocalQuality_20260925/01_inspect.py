import bpy,json
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925');r={'file':bpy.data.filepath,'scenes':{}}
for sn in ['ElseIf_Basis_Construction','ElseIf_NaturalPose_Validation']:
 s=bpy.data.scenes.get(sn)
 if not s:continue
 bpy.context.window.scene=s;bpy.context.view_layer.update()
 r['scenes'][sn]={'frame':s.frame_current,'camera':s.camera.name if s.camera else None,'resolution':[s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage],'samples':s.cycles.samples,'collections':[(c.name,c.hide_render) for c in s.collection.children],'objects':[{'name':o.name,'type':o.type,'hidden':o.hide_render,'visible':o.visible_get(),'collections':[c.name for c in o.users_collection],'modifiers':[(m.name,m.type,getattr(getattr(m,'object',None),'name',None)) for m in o.modifiers],'keys':[(k.name,k.value) for k in o.data.shape_keys.key_blocks] if o.type=='MESH' and o.data.shape_keys else []} for o in s.objects if o.visible_get() and not o.hide_render]}
(P/'01_inspection.json').write_text(json.dumps(r,indent=2));print(json.dumps({k:{n:v for n,v in a.items() if n!='objects'} for k,a in r['scenes'].items()}));print({k:len(v['objects']) for k,v in r['scenes'].items()})
