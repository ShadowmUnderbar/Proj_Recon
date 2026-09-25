import bpy,json
s=bpy.data.scenes['ElseIf_Game_Validation'];s.frame_set(101);bpy.context.view_layer.update()
o=bpy.data.objects['Game__ElseIf_Jacket_Sleeve_L']
print([(k.name,k.value) for k in o.data.shape_keys.key_blocks])
print([(d.driver.is_valid,len(d.driver.expression),d.driver.expression) for d in o.data.shape_keys.animation_data.drivers][-4:])
