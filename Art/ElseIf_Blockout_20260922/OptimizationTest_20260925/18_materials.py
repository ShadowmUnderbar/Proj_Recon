import bpy
from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/OptimizationTest_20260925')
for o in bpy.data.collections['ElseIf_Game_Optimized_Test'].objects:
 if o.type!='MESH':continue
 src=bpy.data.objects['Game__'+o['master_source']]
 for i,m in enumerate(src.data.materials):o.data.materials[i]=m
exec((p/'12_stats.py').read_text(encoding='utf-8-sig'))
