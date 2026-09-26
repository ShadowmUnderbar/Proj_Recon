import bpy,json
r=bpy.data.objects['ElseIf_MobileVR_Humanoid'];print(json.dumps({b.name:[list(b.head_local),list(b.tail_local)] for b in r.data.bones if any(w in b.name for w in ['UpperArm','LowerArm','Hand','Shoulder'])}))
