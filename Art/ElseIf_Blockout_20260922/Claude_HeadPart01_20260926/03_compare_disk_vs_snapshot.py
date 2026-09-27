import bpy,json
D='D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926'
def summary():
    head=[o.name for o in bpy.data.objects if 'Head' in o.name and o.type in('MESH','EMPTY')]
    info={n:{'type':bpy.data.objects[n].type,'parent':bpy.data.objects[n].parent.name if bpy.data.objects[n].parent else None,
             'tris':sum(len(p.vertices)-2 for p in bpy.data.objects[n].data.polygons) if bpy.data.objects[n].type=='MESH' else 0} for n in head}
    return {'scenes':sorted(s.name for s in bpy.data.scenes),'n_obj':len(bpy.data.objects),'head_objs':info,
            'colls':[c.name for c in bpy.data.collections if 'Head' in c.name]}
r={'disk':summary()}
bpy.ops.wm.open_mainfile(filepath=D+'/Blender_UnsavedState_Snapshot.blend')
r['snapshot']=summary()
bpy.ops.wm.open_mainfile(filepath=D+'/Claude_MobileVR_HeadPart01_Work.blend')
print(json.dumps(r,ensure_ascii=False))
