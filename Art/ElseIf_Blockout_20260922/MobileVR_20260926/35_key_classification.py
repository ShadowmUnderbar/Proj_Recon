import bpy,json,numpy as np,hashlib
from pathlib import Path
P=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');o=bpy.data.objects['Mobile__Jacket'];sk=o.data.shape_keys;act=json.loads((P/'Shape_Activation.json').read_text());basis=np.array([list(v.co) for v in sk.key_blocks[0].data]);rows=[]
for k in sk.key_blocks[1:]:
 delta=np.array([list(v.co) for v in k.data])-basis;length=np.linalg.norm(delta,axis=1);dr=sk.animation_data.drivers.find(k.path_from_id('value')) if sk.animation_data else None;role='elbow_volume' if 'Elbow' in k.name else 'wrist_sleeve_clearance' if 'Wrist' in k.name else 'shoulder_underarm_drape' if any(x in k.name for x in ['Wide','Underarm']) else 'pose_contact_correction';rows.append({'name':k.name,'classification':'required_deformation_corrective','role':role,'nonzero_vertices':int(np.sum(length>1e-7)),'max_delta_mm':float(length.max())*1000,'max_test_activation':max(act.get(k.name,{}).values(),default=0),'driver':dr.driver.expression if dr else None,'decision':'retain'})
assert all(r['nonzero_vertices'] and r['driver'] for r in rows)
(P/'Shape_Key_Classification.json').write_text(json.dumps(rows,indent=2));col=bpy.data.collections['ElseIf_Game_MobileVR_Test'];bones={b.name for o in col.objects if o.type=='ARMATURE' for b in o.data.bones};weights={}
for ob in col.objects:
 if ob.type!='MESH':continue
 sums=[sum(g.weight for g in v.groups if ob.vertex_groups[g.group].name in bones) for v in ob.data.vertices];weights[ob.name]={'zero_weight_vertices':sum(w<1e-7 for w in sums),'max_sum_error':max(abs(w-1) for w in sums)}
(P/'Weight_Normalization.json').write_text(json.dumps(weights,indent=2));print({'keys_retained':len(rows),'weights':weights})
