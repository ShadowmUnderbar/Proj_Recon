# ElseIf_Body_GameOpt01（スタンドアロンVR向け・トップダウン視点）

`Art/ElseIf_BodyMaster_20260930/`（Master v04、承認済み・変更禁止）を参照して作ったゲーム用の最適化モデル。
Master のファイル・オブジェクトは読み取りのみで、書き換えていない。

| ファイル | 内容 |
|---|---|
| `ElseIf_Body_GameOpt01.blend` | Game モデル単体（メッシュ1・アーマチュア・テクスチャ同梱）。`Art/Latest/` にもコピー |
| `ElseIf_Body_GameOpt01_work.blend` | Master 参照込みの作業ファイル（再ベイク用。git 管理外） |
| `Textures/` | ベイク結果 BaseColor / Normal（2048） |

## Unity への書き出し（2026-10-01）

- `Assets/App/Models/Elseif/Body/ElseIf_Body_GameOpt01.fbx`（バトル中モデルの確定版）＋ `T_ElseIf_Body_GameOpt01_BaseColor/Normal.png`、`M_ElseIf_Body_GameOpt01.mat`（URP/Lit）
- FBX設定: -Z前・Y上・FBX_SCALE_ALL・リーフボーンなし・Triangulate モディファイア適用（Nゴンのタンジェント対策）
- `HeadSocket`（Empty）は `J_Bip_C_Head` の子、高さ 1.40m。Head Part のルートをこの子にして localTransform=0 で装着
- 取り込み: Humanoid（Create From This Model）、マテリアルは外部 .mat へリマップ

## 作り方

- `game_build.py`: Master のビルダー関数を捕捉し、同じ形状関数を低解像度で評価（セル平均で細かいシワを落とす）
  - ジャケット: 身頃＋襟を1本のロフト、袖ぐりは身頃グリッド上の 4×4 面の長方形（周囲16頂点）を袖の根元リング16頂点と1対1でつなぐ。接合部だけ Master 外側面へ吸着
  - 単面。厚みは裾・袖口・襟・履き口・パンツ裾の折り返しだけ
  - 人体は絶対領域の太腿（パンツ・ソックスの内側へ数cmの余裕）と手だけ
- `game_bake.py`: UV（Smart UV＋部位優先度で島の大きさを調整）→ Master からベイク（衣服下の人体・平面デカールの扱いに注意）
- `game_rig.py`: J_Bip 命名 52 ボーン、部位ごとの許可ボーン＋骨距離で手続きウェイト（最大4、袖ぐり周辺は追加平滑化）
- `game_render.py` / `make_sheets.py`: Master 比較と変形テスト

## 再生成（Master の .blend を開いた Blender 上で）

```python
M = r"D:/UnityProj/Proj_Recon/Art/ElseIf_BodyMaster_20260930"
G = r"D:/UnityProj/Proj_Recon/Art/ElseIf_BodyGameOpt01_20260930"
exec(open(G + "/game_pipeline.py", encoding="utf-8").read(),
     {"MROOT": M, "GROOT": G, "STEPS": ["geo", "uv", "bake", "rig"]})
exec(open(G + "/game_render.py", encoding="utf-8").read(),
     {"MROOT": M, "GROOT": G, "WHAT": ["compare", "poses"], "PCT": 70})
```

保存時は Master の .blend に上書きしないこと（別名保存する）。
