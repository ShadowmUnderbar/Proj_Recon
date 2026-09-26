# ElseIf Game Optimized LOD0候補

## 対象

承認済みMasterの肩と靴の局所改善を、検証済みGame版へ移植した。
ウェイトと姿勢駆動補正は既存Game版を引き継ぎ、移植後の12姿勢検証に通った状態を比較元に固定した。

- `ElseIf_Master` と `ElseIf_Master_Quality`：変更なし。
- `ElseIf_Game_Backup`：局所改善を反映した比較元。
- `ElseIf_Game_Optimized`：今回の編集対象。
- 比較シーン：`ElseIf_Game_Backup_Review` と `ElseIf_LOD0_Review`。

工程原本：`D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/LOD0Candidate_20260925/ElseIf_Game_LOD0_Candidate.blend`
比較元の保存ファイル：同フォルダの `ElseIf_Game_Backup.blend`。

## 数値

床、ライト、カメラ、非表示の別世代を除いた1体分。人体を含む。
Bone Influenceは骨名に対応する頂点グループだけを数え、`Visible_Anatomy_Only` などの非ボーングループを除外した。

| 項目 | 最適化前 | 最適化後 |
|---|---:|---:|
| Vertices | 148,131 | 140,603 |
| Triangles | 288,752 | 272,628 |
| Mesh Object数 | 124 | 8 |
| Skinned Mesh Object数 | 124 | 8 |
| Material Slot数の合計 | 128 | 29 |
| 使用Material数 | 10 | 10 |
| 使用画像Texture数 | 1 | 1 |
| Bone数 | 74 | 74 |
| 最大Bone Influence | 4 | 4 |
| Shape Key配置数（Basis除外） | 190 | 27 |
| Shape Key名の種類数 | 27 | 27 |
| Shape Key配置数（Basis込み） | 250 | 28 |

Trianglesは16,124削減（5.58%）、Verticesは5.08%削減。
Skinned Mesh Object数は93.55%、Material Slot数は77.34%削減した。
同名補正を1つの衣服メッシュへまとめたためShape Key配置数が減っているが、27種類の補正は維持している。
Boneは既存Humanoidと既存の萌え袖補助Bone 2本を維持し、新規追加も削除もしていない。

画像Texture数は使用マテリアルから参照される画像の種類数で、プロシージャルな白灰色パターンは含まない。
これらはBlender内の構造の数値であり、Unityの実測Draw Call数やフレーム時間ではない。

## 人体の削減

| 人体Mesh | 削除前Triangles | 削除後Triangles | 削除数 |
|---|---:|---:|---:|
| Mannequin | 7,520 | 5,147 | 2,373 |
| Anatomy Restoration | 36,024 | 31,358 | 4,666 |
| 合計 | 43,544 | 36,505 | 7,039 |

人体の削除率は16.17%。
手指、首と襟周辺、衣服開口部の安全マージンは保持した。
骨格は人体Meshの削除と切り離して保護している。

削除面の部位分類は、胴体4,593、骨盤周辺2,040、肩から上腕寄り98、上腿4、下腿208、足96 triangles。
これは元の頂点位置と主な骨ウェイトから求めた分類である。

**人体削除は部分的であり、指定部位を一括して消してはいない。**
前腕など、全姿勢で遮蔽を確認しきれない面は残した。
保護した手指と開口部マージンを遮蔽物として考慮し、面の頂点と中心から26方向の遮蔽を12姿勢で調べた。
不確実な領域から2面リングの余裕を取り、追加の境界検証で遮蔽を確認できなかった箇所は面を戻した。
最終的な新規削除境界1,503辺は、端点と中点の遮蔽検証で全12姿勢とも未遮蔽0だった。
有限の姿勢と方向サンプルに対する確認であり、任意の連続動作すべてに対する保証ではない。

## Geometryと描画メッシュ

全体Decimateは使用していない。
身頃の平坦部、ソックスの関節から離れた部分、ソールの平坦部、薄いデザイン支持面を領域別に簡略化した。
材質境界、UV境界、シャープ辺、保護領域は維持した。

肩、脇、袖付け、肘、前腕、萌え袖、袖口、裾の支持領域と、既存の姿勢補正が作用する領域を保護した。
削減で姿勢差が大きくなった箇所は削減対象から外し、最終的な12姿勢の低優先領域の表面差は最大約1.27mm、平均約0.04mmだった。
この距離は削減前の頂点から削減後の表面までのサンプル距離であり、全表面の厳密なHausdorff距離ではない。

描画メッシュはジャケット、パンツ、左右のソックス、左右の靴、人体2つの計8個に統合した。
統合前後の全頂点を12姿勢で比較し、最大位置差は約0.00017mmだった。
統合後も既存ウェイトと全補正の変形を維持している。

## ディテールとマテリアル

| 対象 | 今回の扱い |
|---|---|
| 背面グラフィック | 既存画像Textureを維持。支持面を必要な範囲で簡略化し、統合後もDesignUVを有効な描画UVとして保持。 |
| 白灰色の大きな配色 | 既存Materialと座標属性によるパターンを維持。 |
| センターライン、袖マーキング、靴の配色 | 見た目を維持し、対応する衣服メッシュへ統合。個別Renderer相当の分割を解消。 |
| 薄い支持面、小さな段差 | 低優先領域の平坦な辺を簡略化。姿勢補正が作用する部分は保護。 |
| 襟、袖口、裾の折り返し | 開口部とシルエットに関わるため保持。 |

新規のNormal Mapベイクや最終Texture Atlasは作成していない。
細かな印字は将来のベイク候補だが、今回は別メッシュをなくすことで描画コストを下げ、画像と材質の再設計を避けた。
Materialは無理に1つへ統合せず、10種類を維持した。

## 再ストレステスト

Base、Natural、Arms 90、Forward、Asymmetric Aim、Elbow 90、One Arm Back、Arms Wide、Front Near、Front Back、Wrist Twist、Arms 45の12姿勢を検証した。

- ジャケットと人体の新しい交差を検出しなかった。
- 全衣服の比較でも、新たに交差する人体面は0だった。
- 左右それぞれ764点の手指頂点は、全姿勢で袖内に収まった。
- 人体削除境界の未遮蔽辺は全姿勢で0だった。
- 肩、脇、肘、袖口の変形と主要マーキングは比較画像で確認した。

パンツとソックスの内部には、比較元から人体との重なりが存在する。
交差対象の元人体Face数は360から144へ減り、新規の対象Faceは0だった。
これらは衣服内部の重なりとして残っており、「内部を含めた全交差が0」とはしていない。

## 比較レンダー

同じカメラ、照明、姿勢、880×1120px、48 samplesで16枚を出力した。
7姿勢のHighAngleと基準姿勢のBackQuarterについて、Current / Optimizedを比較している。

- Base HighAngle
- Natural HighAngle
- Arms Wide
- Asymmetric Aim
- Elbow 90
- One Arm Back
- Wrist Twist
- Base BackQuarter

`SmallView_256_Comparison.jpg` はキャラクターの投影範囲を同じ領域で切り出し、長辺を256pxへ揃えた比較一覧。
最終版では、いずれかのRGBチャンネルに10/255を超える差がある画素は各比較画像の約0.02〜0.11%だった。
目視でも大きなシルエットと主要デザインの差は認識しにくい状態だった。
画素差には照明とアンチエイリアスの影響が含まれ、知覚上の完全一致を意味しない。

## 保存と停止範囲

元Masterと以前の世代は保全比較で変更0件。
工程フォルダの原本とバックアップを保持し、完成原本を `Art/Latest/ElseIf_Game_LOD0_Candidate.blend` へコピー済み。SHA256一致とLatest内の.blendが1本であることを確認した。以前のLatest原本2本も工程フォルダへ退避して保持した。
LOD1/LOD2、最終Unity Export、Humanoid Avatar設定、Unity側性能測定、最終Atlas確定には進んでいない。
今回の成果はLOD0候補であり、最終的なゲーム負荷の判定は未実施。
比較レンダーと数値報告をもって停止する。

## 検証記録

- `Statistics.json` / `Reduction_Summary.json`
- `Body_Deletion_Counts.json` / `Body_Deletion_Record.json` / `Boundary_Audit.json`
- `Baseline_Stress_Audit.json` / `Optimized_Final_Stress_Audit.json`
- `Penetration_Comparison.json` / `Surface_Comparison.json`
- `Merge_Equivalence_Final.json` / `UV_Preservation.json`
- `SmallView_Metrics.json` / `Render_Manifest.json`

