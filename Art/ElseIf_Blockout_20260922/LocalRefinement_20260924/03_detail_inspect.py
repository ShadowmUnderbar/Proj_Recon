import bpy,json
from pathlib import Path
r={}
for n in ['ElseIf_Shoe_L_Upper','ElseIf_Shoe_L_Sole','ElseIf_Shoe_L_AnkleCollar','ElseIf_Shoe_L_Tongue','NaturalPose__ElseIf_Jacket_Sleeve_L']:
 o=bpy.data.objects[n];r[n]={'mods':[{'name':m.name,'type':m.type,'levels':getattr(m,'levels',None),'thickness':getattr(m,'thickness',None),'offset':getattr(m,'offset',None)} for m in o.modifiers], 'collections':[c.name for c in o.users_collection]}
print(json.dumps(r))
