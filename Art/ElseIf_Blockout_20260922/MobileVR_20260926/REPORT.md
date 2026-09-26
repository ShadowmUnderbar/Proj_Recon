# ElseIf MobileVR Test — LOD0候補

2026-09-26。Masterと承認済みGame/Optimizedを保持し、ElseIf_Game_MobileVR_Testのみを編集した。

最終候補は **118,656 Triangles**。Current Gameから **58.91%**、272,628 TrianglesのOptimizedから **56.48%** 削減した。60k〜100kの目安は超えている。約104kの候補では袖口・裾の縁に256pxでも分かる乱れが発生したため、その部分を保護して118,656で停止した。

256px HighAngleでは全身、左右の腕、萌え袖、脚、靴の主要な外周と配色を維持している。布の陰影や縁の細部は完全一致ではない。Standalone実機での性能は未測定。

## 数値比較

| 項目 | Current Game | Optimized | MobileVR Test |
|---|---:|---:|---:|
| Vertices | 148,131 | 140,603 | 63,620 |
| Triangles | 288,752 | 272,628 | 118,656 |
| Mesh | 124 | 8 | 5 |
| Skinned Mesh | 124 | 8 | 5 |
| Material Slots | 128 | 29 | 20 |
| Materials | 10 | 10 | 10 |
| Textures | 1 | 1 | 1 |
| Bones | 74 | 74 | 74 |
| 最大Bone Influence | 4 | 4 | 4 |
| Shape Keys（Basis除外・配置総数） | 190 | 27 | 27 |

CurrentのShape Keyは複数Objectへの重複配置を含む190個、名称種類は27個。MobileVRはJacket上の27個のみで、Basisを含めると28個。追加Boneは0。Humanoidの74 Bone名は元モデルと一致し、手指を含めSkeletonを保持した。

## 削減前：272,628 Trianglesの内訳

| Object | Vertices | Triangles | 割合 |
|---|---:|---:|---:|
| LOD0__Jacket | 84,616 | 164,117 | 60.20% |
| LOD0__ElseIf_Anatomy_Restoration | 16,277 | 31,358 | 11.50% |
| LOD0__Socks_L | 9,484 | 18,770 | 6.88% |
| LOD0__Socks_R | 9,474 | 18,748 | 6.88% |
| LOD0__Shorts | 6,606 | 13,128 | 4.82% |
| LOD0__Shoes_L | 5,414 | 10,680 | 3.92% |
| LOD0__Shoes_R | 5,414 | 10,680 | 3.92% |
| LOD0__ElseIf_Mannequin | 3,318 | 5,147 | 1.89% |

ジャケットには縫製・マーキング・背面グラフィックなどの小部品も統合されている。人体はMannequinとAnatomy_Restorationの2Object。Restorationの大部分は上半身・首周辺の高密度な補完面で、脚全体ではない。

## 最終：Mesh別の内訳

| Object | Vertices | Triangles | 割合 | Material Slots |
|---|---:|---:|---:|---:|
| Mobile__Jacket | 35,671 | 66,223 | 55.81% | 7 |
| Mobile__Body | 19,111 | 35,345 | 29.79% | 1 |
| Mobile__Socks | 4,298 | 8,368 | 7.05% | 2 |
| Mobile__Shoes | 2,820 | 5,364 | 4.52% | 6 |
| Mobile__Shorts | 1,720 | 3,356 | 2.83% | 4 |

実際に評価された描画MeshのTriangle数も上表と一致。未適用Subdivisionによる隠れた増加はない。人体2個、左右ソックス、左右靴をそれぞれ統合し、全12ポーズで統合前後の頂点位置差は0 mmだった。

## 削減方法と保持箇所

- Jacket：部品ごとの局所削減。肩・脇・肘・補正量の変化が大きい頂点と開口境界を優先保護。接触した16面には95頂点を局所的に追加し、補正とWeightを元モデルから転写した。
- 袖口・襟・裾・前開きの縁：位置を動かす削減を取り消し、元の部品から再構成。平坦に近い面の分割だけを整理した。対象48,406→23,766 Triangles。
- ソックス：左右37,518→8,368。靴：左右21,360→5,364。パンツ：13,128→3,356。関節・外周を優先した部位別削減。
- マーキング・背面画像・配色を保持。既存の画像Textureは1枚を埋め込み。小さな平面グラフィックは既に安価な形状のため、必要な面を残した。新しいNormal MapのベイクやAtlas制作は行っていない。
- Materialは10種類を保持。統合可能な同一Material Slotを整理して29→20。無理な1 Material化はしていない。

## 人体隠面と安全マージン

| 人体 | Triangles |
|---|---:|
| Current Game | 43,544 |
| 今回開始時Optimized | 36,505 |
| MobileVR Test | 35,345 |

今回追加削除：1,160 Triangles（開始時人体から3.18%）。Currentから累計8,199 Triangles、18.83%削減。

胸・腹側の衣服内部、および残存人体に重なる補完面のうち、テストポーズで隠れると判定できた面を削除した。境界に露出候補が出た箇所には4 Edge Loop分の余裕を戻した。手・指・首周辺と開口部の安全域は保持。腕・脚・足などは前工程の削除状態を引き継いだが、今回すべての残存面を一括削除したわけではない。確実性を確認できない上半身の補完面は保守的に残している。

全12ポーズで新しい削除境界の端点・中点を26方向から検査し、露出候補0。これは有限のポーズ・方向に対する検査結果であり、あらゆる将来ポーズを保証するものではない。

## Shape Key / Weight

27個すべてに非ゼロの変形、ドライバー、テスト中の作動を確認した。肘の体積補正2、手首・萌え袖補正4、肩・脇8、その他のポーズ接触補正13。今回不要と確定できたキーはなく、全27個を保持した。キー別の分類は Shape_Key_Classification.json に記録。

最大Bone Influenceは4。無ウェイト頂点0、ウェイト合計誤差は最大0.000092未満。全補正ドライバーの対象はElseIf_MobileVR_Humanoid。最終Unity Export時のドライバー移行は未実施。

## ストレステスト

| Pose | Jacket / 人体交差ペア | 状態 |
|---|---:|---|
| Base | 0 | 検出0 |
| A_Natural | 0 | 検出0 |
| B_Side90 | 0 | 検出0 |
| C_Forward | 0 | 検出0 |
| D_AsymmetricAim | 0 | 検出0 |
| E_Elbow90 | 0 | 検出0 |
| F_OneArmBack | 0 | 検出0 |
| G_WideOpen | 0 | 検出0 |
| H_FrontNear | 0 | 検出0 |
| I_FrontBack | 12 | 補完人体との内部接触が残る（元モデルも6ペア） |
| J_WristTwist | 0 | 検出0 |
| K_Arms45 | 0 | 検出0 |

指定7ポーズ（Base、Natural、ArmsWide、AsymmetricAim、Elbow90、OneArmBack、WristTwist）はJacket/人体交差0。追加のI_FrontBackだけ12交差ペアが残る。これを全ポーズ完全無交差とは扱わない。ソックス・パンツと内部人体の重なりも元モデルから存在し、MobileVRでも検出されるため、モデル全体の交差数0を主張しない。

手指は1,528頂点をHighAngle/BackQuarterから全12ポーズで確認し、見えるサンプル0。人工的に袖口を閉じた判定用Meshでは不安定な検出があったため、その値を露出判定の結論には用いていない。比較レンダーでも手指の新しい露出は確認できなかった。

元Jacketに対する三角形中心の距離は95パーセンタイル0.45〜0.53 mm。最大値はポーズによって4.75〜31.76 mmで、一部の大きな面には差が残る。数値は元部品表面への片方向距離であり、完全一致の証明ではない。

## 比較画像

同一カメラ・照明・ポーズ、880×1120、Cycles 48 samples。Current/Optimizedの前工程画像を再利用し、MobileVRを同条件で描画。Small Viewは各ポーズ共通の切り出し範囲でキャラクター長辺256px。

![256px比較](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/SmallView_256_Comparison.jpg)

通常サイズ比較：
- [Base_HighAngle](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_Base_HighAngle.jpg)
- [Natural_HighAngle](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_Natural_HighAngle.jpg)
- [ArmsWide](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_ArmsWide.jpg)
- [AsymmetricAim](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_AsymmetricAim.jpg)
- [Elbow90](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_Elbow90.jpg)
- [OneArmBack](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_OneArmBack.jpg)
- [WristTwist](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_WristTwist.jpg)
- [Base_BackQuarter](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/MobileVR_20260926/Compare_Base_BackQuarter.jpg)

256px比較のOptimized→MobileVR平均RGB絶対差は1.03〜2.41/255。陰影・アンチエイリアスを含むため知覚上の一致を意味しない。主要外周・配色は維持される一方、近距離の布面の陰影差、背面裾の細部差は残る。

## 保護・保存・停止範囲

Protected_Backup_Evaluated.jsonとの照合で既存原本の変更0件。Master、承認済みGame、Optimized、既存Shape Keyを保持。工程フォルダの原本とバックアップは移動せず保存した。Art/Latestへ完成原本をコピー済み。SHA256一致とLatest内の.blendが1本であることを確認し、Latest_Copy_Verification.jsonへ記録した。旧Latest原本は工程フォルダのPrevious_Latest_LOD0.blendに保持した。

LOD1/LOD2、Unity Export、Humanoid Avatar設定、実機性能測定、Texture Atlas最終化には進んでいない。118,656 TrianglesのMobileVR LOD0候補と比較結果をもって停止する。

主な検証記録：Statistics.json、Final_Validation.json、Final_Merge_Equivalence.json、Stress_Audit.json、Boundary_Visibility_Final.json、Hand_Camera_Visibility.json、Shape_Key_Classification.json、SmallView_Metrics.json。

