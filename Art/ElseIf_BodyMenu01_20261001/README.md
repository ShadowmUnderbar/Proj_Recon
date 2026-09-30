# ElseIf_Body_Menu01（メインメニュー用・近距離で見る高品質版）

`Art/ElseIf_BodyMaster_20260930/`（Master v04）から直接作ったメニュー専用の身体モデル。
Battle 用の GameOpt01 を高密度化したものではない（GameOpt01 のパイプラインを複製し、Master の形状関数を高い解像度で評価し直している）。

| ファイル | 内容 |
|---|---|
| `ElseIf_Body_Menu01.blend` | Menu モデル単体（メッシュ1・アーマチュア・HeadSocket・テクスチャ同梱）。`Art/Latest/` にもコピー |
| `ElseIf_Body_Menu01_work.blend` | Master のコレクションを読み込んだ作業ファイル（再生成・再ベイク用。git 管理外） |
| `Textures/` | BaseColor / Normal（4096。スマホ向けには Unity 側で 2048 に落とせる） |

## GameOpt01 との違い

- 解像度: 身頃 周72×縦38段、袖 周28×28段（袖ぐりは 8×6 面＝周28頂点と1対1＋中間2リング）、脚・パンツ 周28、手のひら12・指8角、靴アッパー28
- 裏地: 裾5段・袖口7段・襟8段を内側へ複製（GameOpt01 は1段の折り返しだけ）。パンツ裾・靴の履き口も深く
- 形状で作り直したもの: 靴のストラップ2本とバックル、襟元のドローコード、ソール側面の段
- 肩の接合: 頂点法線方向のレイで Master 外表面へ吸着（最近傍点だと段差が出た）
- ベイク: 全体→裏地（小さいケージ）→手（手だけから）の3パス。袖の中の手が袖の色を拾わないように
- ウェイト: 背面の裾は太腿の影響を弱めた（座りポーズで裾が太腿に引かれないように）

## 再生成（work.blend を開いた Blender 上で）

```python
M = r"D:/UnityProj/Proj_Recon/Art/ElseIf_BodyMaster_20260930"
D = r"D:/UnityProj/Proj_Recon/Art/ElseIf_BodyMenu01_20261001"
exec(open(D + "/menu_pipeline.py", encoding="utf-8").read(),
     {"MROOT": M, "GROOT": D, "STEPS": ["geo", "uv", "bake", "rig"]})
exec(open(D + "/menu_render.py", encoding="utf-8").read(),
     {"MROOT": M, "GROOT": D, "WHAT": ["compare", "close", "poses"], "PCT": 70})
```
