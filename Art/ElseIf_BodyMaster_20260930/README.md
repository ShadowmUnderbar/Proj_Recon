# ElseIf 身体・衣装 Master（首から下）

キャラクターシート（`Reference/ElseIf_CharacterSheet.webp`）を元に、Blender MCP で新規制作した Master モデル。
ゲーム用リトポ・ポリゴン削減の前段階。原本の最新は `Art/Latest/ElseIf_Body_Master.blend` にコピーしている。

## 世代

| 版 | 内容 |
|---|---|
| v01 | 初版（人体・ジャケット・萌え袖・ハーフパンツ・サイハイ・靴・手指・HeadSocket・主要ディテール） |
| v02 | ジャケットの着衣感調整（肩の布落ち、前腕〜袖口の布量、腹部〜裾の離れと左右差、裾の不揃い）。承認済み |
| v03 | 絶対領域: ソックス上端を 0.772→0.656m に下げ、パンツ裾との間に脚長の約5%（3.9cm）の肌を出した |
| v04 | 右太腿ベルトを 0.628〜0.650m に下げ、ソックス上端より下に収めた（肌に重ねない）。Master として完成 |

## 構成

- `ElseIf_Master_BASE`（A-pose 42°）と `ElseIf_Master_NATURAL`（自然立ち 14°）の2コレクション。同じスクリプトから腕角度だけ変えて生成
- ジャケット `Jacket` は身頃・襟・袖（`JacketSrc_*` に非表示で保持）を SDF で一体化したもの。ループ整列の元パーツは `JacketSrc_*`
- `HeadSocket`（Empty）に Head Part の首下端を合わせる

## 再生成

Blender（MCP）内で:

```python
R = r"D:/UnityProj/Proj_Recon/Art/ElseIf_BodyMaster_20260930"
exec(open(R + "/wip.py", encoding="utf-8").read(),
     {"R": R, "POSE": "BASE", "PARTS": ["ALL"], "VIEWS_TO": [], "TAG": "x"})
```

- 寸法は `body.py`（人体）/ `garments.py`（衣装）/ `details.py`（ディテール）の定数で調整する
- 検査: `check_poke.py`（人体がジャケットから出ていないか）/ `check_hands.py`（手が袖内か）/ `check_leak.py`
- `Renders/` は git 管理外（スクリプトから作り直せる）
