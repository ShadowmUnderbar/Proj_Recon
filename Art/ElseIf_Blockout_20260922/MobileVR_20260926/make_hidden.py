from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922');s=(p/'LOD0Candidate_20260925/06_hidden_faces.py').read_text(encoding='utf-8-sig')
s=s.replace('LOD0Candidate_20260925','MobileVR_20260926').replace('ElseIf_LOD0_Review','ElseIf_MobileVR_Review').replace('ElseIf_Game_Optimized','ElseIf_Game_MobileVR_Test').replace('ElseIf_LOD0_Humanoid','ElseIf_MobileVR_Humanoid').replace('LOD0__','Mobile__').replace('ElseIf_Game_LOD0_Candidate.blend','ElseIf_Game_MobileVR_Test.blend')
s=s.replace("o['master_source'].startswith(('ElseIf_Jacket_','ElseIf_Shorts','ElseIf_Sock_','ElseIf_Shoe_'))","o.name not in [b.name for b in bodies]")
# 初回削除の保護範囲より縮小するが、手指・首と開口部は残す。
s=s.replace('(p-sh).length<.045 or ','').replace('(p-wrist).length<.052','(p-wrist).length<.035')
s=s.replace(" pass\nscene.frame_set", " print(label,{n:len(v) for n,v in candidate.items()},flush=True)\nscene.frame_set")
(p/'MobileVR_20260926/03_hidden_faces.py').write_text(s,encoding='utf-8')
