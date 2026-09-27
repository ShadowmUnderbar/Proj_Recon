#!/bin/bash
# 使い方: bash run.sh NN_name.py  → Blender MCPで実行し、結果の本文だけを表示する
D=D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/Claude_HeadPart01_20260926
python D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/mcp_client.py "$D/$1" >/dev/null
python -c "
import json,sys;r=json.load(open(sys.argv[1],encoding='utf-8'))
if r.get('status')!='success':print(json.dumps(r,ensure_ascii=False)[:4000]);sys.exit(1)
t=r['result'].get('result','');print(t[-6000:])
" "$D/${1%.py}.response.json"
