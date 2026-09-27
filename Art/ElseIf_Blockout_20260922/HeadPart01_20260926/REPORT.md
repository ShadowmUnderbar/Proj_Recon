# ElseIf 初期交換式Head Part

2026-09-26。ElseIf MobileVR Testの身体を保持し、機械頭「ElseIf_Monitor01」を独立したHead Partとして制作した。
提供されたキャラクターデザイン画像の白灰色の角丸筐体、黒いモニター、淡い桃紫色の顔文字表示を基準にした。
髪を除いたコンパクトな筐体と短い回転接続部とし、背面は簡素なカバーに留めた。

## 頭部の数値

| 項目 | 数値 |
|---|---:|
| Triangles | 1,992 |
| Vertices | 1,201 |
| Mesh数 | 2 |
| Material数 | 3 |
| Material Slot数 | 3 |
| 頭部のBone / Shape Key | 0 / 0 |
| 仮表情Texture | 512×512、3枚 |

| Mesh | Vertices | Triangles | Material Slots |
|---|---:|---:|---:|
| ElseIf_HeadShell | 1,008 | 1,656 | 2 |
| ElseIf_FaceMonitor | 193 | 336 | 1 |

筐体は幅188mm、高さ176mm、奥行き約169mm。
接続部を含む頭部全体の高さは約261mmで、装着時の頭頂は約1.676m。
身体118,656 Trianglesに頭部1,992を加えた装着状態は120,648 Triangles。
身体の削減や再スキニングは行っていない。

## HeadSocketとの接続

既存Socket名：`Mobile__LOD0__Backup__Fix__HeadSocket`。
Blender MCPで測定したWorld Positionは約 `(0, 0.023843661, 1.468153119)` m、Rotationは実質0度、Scaleは実質 `(1, 1, 1)`。
数値には評価行列の微小な浮動小数点誤差が含まれる。

新しいCollectionは `ElseIf_HeadPart_Monitor01`、取り付け基準はEmptyの `HeadPart_ElseIf_Monitor01`。
その子に `ElseIf_HeadShell` と `ElseIf_FaceMonitor` を置いた。
取り付け基準を既存HeadSocketの子にし、ローカルPositionとRotationを0、Scaleを1にすると装着位置になる。
BlenderのParent Inverseも単位行列。
頭部側の前方はBlender -Y、上方は+Z。
身体側のSocketやBone構造は変更していない。

頭部単体ファイルのRootは原点、親なし、Scale 1で保存した。
Collectionを追加してRootを対象HeadSocketへ親子付けし、ローカルTransformを初期化すれば再装着できる。
これは取り付け構造の準備までであり、視線追従やUnityでのアニメーションは実装していない。

## MaterialとモニターUV

- `ElseIf_HeadShell`：白灰色の筐体。
- `ElseIf_HeadJoint`：黒い接続部と薄い画面周囲のパッキン。
- **`ElseIf_FaceMonitor`**：独立制御用の画面Material。

Monitorは不透明、最大約3.5mmの浅い曲面。
目と口はGeometryとして作らず、すべて仮Textureに含めた。
`FaceUV` はUV0の単一アイランドで、正面のX/Z平面投影を0〜1へ正規化している。
角丸部分だけが四隅を切り欠き、反転三角形とUV面積0の三角形はともに0。
物理的な表示面は166×146mmで、表情の配置はこの比率に合わせて調整できる。

Blender Materialには `ExpressionTexture`、`MonitorTint`、`MonitorBrightness` を用意した。
Texture、色、発光の明るさを筐体とは独立に変更できることを確認した。
これらはBlender上の確認用構成で、Unity ShaderそのものやUnityのプロパティ設定を作成したものではない。

## 仮表情

通常、喜び、泣きの3枚を用意し、すべて.blend内へ埋め込んだ。
通常は `(＞∀＜)`、喜びは `(^ω^)`、泣きは `(；ω；)` を意識したドット表示。
最終表情セットではなく、Material分離とUVの確認用。
同じ頭部Geometryのまま画像参照だけを変更してレンダーし、全11枚でGeometryのSHA256が一致した。
保存時の表示は通常顔。

![表情比較](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Expressions.jpg)

## 回転と保全の確認

身体のBasePoseで、水平Yawを15度刻みの24方向、Pitch ±15/±30度とYaw四方向の16姿勢、Roll ±15度とYaw四方向の8姿勢を確認した。
合計48姿勢で頭部とジャケットの交差検出は0。
初期形状では上下30度で筐体下端が襟に接触したため、頭部筐体を12mm上げて接続部を延長した。
これは頭部側だけの変更で、HeadSocketと襟は変更していない。
検証は有限の姿勢に対する静的確認であり、任意の全回転や動的アニメーションの保証ではない。

既存1176 Objectについて、形状、Weight、Shape Key、骨、Transform、既存カメラと照明の保全記録を照合し、変更0件。
身体の元原本とバックアップを工程フォルダに保持した。
頭部単体ファイルの内容も確認し、ObjectはRootと2 Meshのみ、Armatureとカメラは0。

## 確認レンダー

頭部単体：720×720、Front / Side / Back / ThreeQuarter / HighAngle。
身体装着：880×1120、Front / ThreeQuarter / BackQuarter / HighAngle。
HighAngleには既存 `Game_Cam_Stress_HighAngle` をそのまま使用した。
その他の装着ビューは、新規の確認カメラだけを頭頂から靴まで入る構図へ調整した。
身体モデル、既存カメラ、既存照明は変更していない。

![頭部5方向](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_Views.jpg)

![装着4方向](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_Views.jpg)

![ゲーム距離256px](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_HighAngle_256.jpg)

個別PNG：

- [Head_Front](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_Front.png)
- [Head_Side](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_Side.png)
- [Head_Back](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_Back.png)
- [Head_ThreeQuarter](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_ThreeQuarter.png)
- [Head_HighAngle](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Head_HighAngle.png)
- [Assembly_Front](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_Front.png)
- [Assembly_ThreeQuarter](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_ThreeQuarter.png)
- [Assembly_BackQuarter](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_BackQuarter.png)
- [Assembly_HighAngle](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Assembly_HighAngle.png)
- [Expression_Joy](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Expression_Joy.png)
- [Expression_Cry](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/Expression_Cry.png)

## 原本と停止範囲

- [身体装着済み原本](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/ElseIf_MobileVR_HeadPart01.blend)
- [独立Head Part](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/ElseIf_HeadPart_Monitor01.blend)
- [制作前の身体バックアップ](D:/UnityProj/Proj_Recon/Art/ElseIf_Blockout_20260922/HeadPart01_20260926/ElseIf_Body_Before_Head.blend)

最新原本をArt/Latest/ElseIf_MobileVR_HeadPart01.blendへコピー済み。工程フォルダの原本は移動していない。
SHA256一致とLatest内の.blendが1本であることを確認し、Latest_Copy_Verification.jsonに記録した。以前の身体原本もPrevious_Latest_MobileVR.blendとして保持した。
今回は初期機械Head Partの制作と確認まで。
人間頭、髪、別Head Part、武器、Unity Shader、最終表情Texture一式、LOD1/LOD2、身体の再最適化、Unity Exportには進んでいない。

