from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926');s=(p/'03_hidden_faces.py').read_text(encoding='utf-8-sig')
# 復元人体が元人体の内側に重なる部分も、残す人体を遮蔽物として評価する。
s=s.replace("bodies=[bpy.data.objects['Mobile__'+n] for n in ['ElseIf_Mannequin','ElseIf_Anatomy_Restoration']]","bodies=[bpy.data.objects['Mobile__ElseIf_Anatomy_Restoration']]")
s=s.replace("o.name not in [b.name for b in bodies]","'Mannequin' not in o.name and 'Anatomy' not in o.name")
s=s.replace(" tree=BVHTree.FromPolygons(cv,cf)"," vs0,fs0=snap(bpy.data.objects['Mobile__ElseIf_Mannequin']);off=len(cv);cv+=vs0;cf.extend(tuple(i+off for i in f) for f in fs0)\n tree=BVHTree.FromPolygons(cv,cf)")
s=s.replace('Body_Deletion_Record.json','Body_Overlap_Deletion_Record.json').replace('Body_Deletion_Counts.json','Body_Overlap_Deletion_Counts.json')
(p/'05_overlap_hidden.py').write_text(s,encoding='utf-8')
