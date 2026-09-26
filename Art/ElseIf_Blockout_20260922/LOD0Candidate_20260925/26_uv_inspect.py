import bpy,json
for n in ['LOD0__Jacket','Backup__EL_Back_Reference_Artwork','Backup__ElseIf_Jacket_Sleeve_L']:
 o=bpy.data.objects[n];print(n,[(u.name,u.active_render) for u in o.data.uv_layers],o.data.uv_layers.active.name if o.data.uv_layers.active else None)
