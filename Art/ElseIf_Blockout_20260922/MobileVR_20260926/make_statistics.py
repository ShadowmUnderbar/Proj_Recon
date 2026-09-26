from pathlib import Path
p=Path('D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922');s=(p/'LOD0Candidate_20260925/19_statistics.py').read_text(encoding='utf-8-sig').replace('LOD0Candidate_20260925','MobileVR_20260926').replace("['ElseIf_Game_Backup','ElseIf_Game_Optimized']","['ElseIf_Game_Backup','ElseIf_Game_Optimized','ElseIf_Game_MobileVR_Test']").replace("bpy.data.scenes['ElseIf_LOD0_Review']","bpy.data.scenes['ElseIf_MobileVR_Review']")
(p/'MobileVR_20260926/07_statistics.py').write_text(s,encoding='utf-8')
