import bpy,json
r=bpy.data.objects['ElseIf_Game_Humanoid'];print('rig matrix',list(map(list,r.matrix_world)));print('upper local',list(r.data.bones['J_Bip_L_UpperArm'].head_local));print('body matrix',list(map(list,bpy.data.objects['Game__ElseIf_Mannequin'].matrix_world)))
