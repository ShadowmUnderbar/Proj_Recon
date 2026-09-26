# ElseIf Master複製の局所品質改善

## 対象と保存先

承認済みMasterを独立したメッシュとArmatureへ複製し、`ElseIf_Master_Quality` および `ElseIf_Master_Quality_NaturalPose` で作業した。
基準姿勢のシーンは `ElseIf_Master_Quality_Review`、自然姿勢は `ElseIf_Master_Quality_NaturalPose`。
工程原本は `D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MasterLocalQuality_20260925/ElseIf_Master_LocalQuality.blend`。
検証後、同じファイルを `D:/UnityProj/Proj_Recon/Art/Latest/ElseIf_Master_LocalQuality.blend` へコピーした。SHA-256の一致とLatest内の.blendが1本であることを確認した。

直前のGame版に対する人体削除と局所トポロジー作業は中断したまま保持し、今回の複製には使用していない。
元Master、以前のCollection、復元用Shape Key、Game側の途中状態は保持した。

## 調整内容

- 肩：人体表面と肩関節を実測し、肩先から上腕へ落ちる布の変化を弱く整理した。身体に近い頂点は内向きの変位を抑え、肩の人体との接触を回避した。
- 袖付け：既存の接続境界に沿う弱い谷と、脇の少量の布の余りを調整した。新しい縫い目パーツは追加していない。
- 靴：前端の両隅を後退させ、つま先の平面形を丸めた。甲の勾配、履き口の前側、タンの形を整え、既存の締結部と配色パーツを追従させた。ソールを厚くする変更はない。

調整前の形を復元できるよう、複製に `Quality_Shoulder_Armhole_20260925` と `Quality_StreetShoe_Form_20260925` のShape Keyを追加した。
これは局所造形の切り替え用で、ゲーム用の変形補正や最終ウェイトではない。
初期調整量の最大値は袖で約2.7mm、靴で約4.3mm。肩の人体に近い箇所では、その後に内向きの成分を抑えた。
全体プロポーションを再設計する変更は行っていない。

## 実測

座標はBlenderワールド座標、単位m。左右は鏡像。

| 部位 | 左側の位置 |
|---|---|
| 鎖骨相当のShoulder Bone | 始点 (0.02111, 0.02525, 1.36668)、終点 (0.08922, 0.02525, 1.35699) |
| 肩関節 | (0.08915, 0.02525, 1.35649) |
| 肩先の人体表面 | (0.12626, 0.02525, 1.35500) |
| 上腕前面の人体表面 | (0.12500, -0.01130, 1.28500) |
| 脇側の人体表面 | (0.10500, 0.02525, 1.24356) |

表面位置は指定した断面へのレイ交差点であり、解剖学上の部位全体を代表する一点ではない。
骨のRest状態と基準姿勢での位置は `Anatomy_Measurements.json` に分けて保存した。

## 検証

基準姿勢と既存の自然立ち姿で確認した。

| 確認対象 | 基準姿勢 | 自然姿勢 |
|---|---:|---:|
| ジャケットと人体の三角形交差 | 0 | 0 |
| 既存デザイン部品と人体の三角形交差 | 0 | 0 |
| 袖外の手指頂点（左右各764点） | 0 | 0 |
| パンツとソックスで覆う太腿の未被覆点（204点） | 0 | 0 |
| 靴外の足頂点（左右各279点） | 0 | 0 |

元データの保全比較では変更0件。
複製の人体、骨格、HeadSocket、ハーフパンツ、ソックスも元データと一致した。
メッシュの頂点数と面数、マテリアル割当は変更していない。
全可動域のゲーム用ストレステストは今回の対象外であり、この2姿勢の結果を全ポーズの保証とは扱わない。
詳細は `Validation.json` に保存した。

## 確認画像

従来のカメラ、照明、880×1120px、48 samplesを使用。

- Front / Side / Back / ThreeQuarter / BackQuarter / HighAngle
- NaturalPose_Front / NaturalPose_ThreeQuarter / NaturalPose_HighAngle

一覧画像は `Review_Grid.jpg`。
人体削除、最適化、リトポロジー、Decimate、LOD、最終スキニング、頭部、武器には進んでいない。
レンダー確認と保存をもって停止する。

