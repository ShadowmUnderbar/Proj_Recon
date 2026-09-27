# エルスイフの自室 アセットキット（2026-09-27）

低所得層向けSF居住区のワンルームを、Unity上で組み立てるためのキットです。床・壁・天井・窓・ドアなどの建築モジュールと、家具・設備・小物がすべて独立しています。完成した部屋やレイアウトは含みません。FPS視点で使う前提です。

## 制作方針
- **メッシュは形・厚み・機能構造だけを持つ。** 輪郭、大きな段差、パネル形状、フレーム、取っ手、端子、大きな継ぎ目、可動部を作っている
- **傷・擦れ・汚れ・塗装剥がれ・補修跡・細かいパネルラインは、メッシュに作り込まない。** テクスチャ（BaseColor / Normal / Roughness）で表現する
  - 以前の版にあったテープ補修、擦れパッチ、天板の傷線・コップ跡・角欠け、貼り紙、ステッカー、ハザード帯、ラベルは削除した
  - カップ麺・缶の帯やブランケットの縞も、重ねた板ではなく面のマテリアル分けにした
- 家具・小物の「使い込まれている」感じは、焼き込んだテクスチャで出している（下の「家具・小物のテクスチャ」）。ソファーのへたり（クッションの傾き）のように、形そのものの変化はメッシュに残した

## ファイル
Gitで追跡しているのは、スクリプト・README・統計JSONだけ。`Export/`・`Renders/`・`.blend`・`build.log` はGitに入らないので、手元にない場合は `build_room_assets.py` を実行して作り直す（RTX 3060で約7分）。

- `Export/Architecture/`：床・壁・天井・窓・ドア（14）
- `Export/ArchitectureProps/`：換気口・コンセント・スイッチ・ケーブルモール（4）
- `Export/Furniture/`：家具・設備（8）
- `Export/Props/`：生活小物（15）
- `Export/Textures/`：建築用テクスチャとカップ麺の中身
- `Export/Textures/Assets/`：家具・小物・窓枠・ドア・壁付けPropの劣化テクスチャ（アセットごとに3枚、31セット）
- FBXからはテクスチャを相対パスで参照しているので、`Export` のフォルダ構成のままUnityへ入れる
- `ElseIf_Room_Assets.blend`：元データ。確認用に仮配置した状態で保存している（FBXには影響しない）
- `build_room_assets.py`：生成スクリプト。`blender.exe -b --factory-startup --python build_room_assets.py` で全部作り直す
- `verify_fbx.py`：FBXを読み戻して寸法・UV・テクスチャ参照を確認する（Blender MCP経由）
- `Renders/`：プレビュー。`FPS_*.png` は目の高さ1.55m、焦点距離24mmで撮っている。`Arch_Assembly_Outside.png` と `FPS_*` に写っている4m四方の一角は接続確認用で、レイアウトの提案ではない
- `Asset_Stats.json`：アセットごとのTris数・寸法・オブジェクト名・マテリアル、パレットの色値

## 共通ルール
- 単位はm。Unityへはスケール1、回転(0,0,0)で入る（FBXのApply Transformを使用）
- 正面はBlenderの-Yで、Unityでは+Z
- 左右は、そのアセットを使う人から見た向き（`Chair_Arm_L` は座った本人の左）
- 階層はルート直下の1段だけ（FBXのApply Transformが孫階層の位置を崩すため）
- トゥーン向けの法線：35°以上の角をハードエッジにしている。面取りは1〜3段

### UV
| 対象 | UV |
|---|---|
| 家具・小物・窓枠・ドア・壁付けProp | UV0は重ならない展開（Smart UV、0〜1）。アセット内の全パーツで1枚のUVを共有する |
| 床・壁・天井・ハッチ | ワールド座標からの投影で、1UV=2m。隣に並べるとテクスチャが途切れずつながる |
| 画面（Monitor / MobileDevice）・窓ガラス | 画面または開口全体が0〜1。RenderTextureや外景を1枚で貼れる |
| カップ麺の中身 | 円がテクスチャ全体に対応する |

ライトマップ用のUV1は、Unity側の「Generate Lightmap UVs」で作る。

### マテリアル
- 家具・小物・窓枠・ドア・壁付けPropは、アセットごとに1つのマテリアル `M_<アセット名>`（例：`M_Chair`）にまとめた。全パーツが同じテクスチャセットを使う
- 床・壁・天井は共通の `M_Room_Floor` / `M_Room_FloorDamaged` / `M_Room_Wall` / `M_Room_Ceiling`
- 発光：`M_Room_CyanGlow`（機械のシアン）、`M_Room_LampWarm`（デスクライト）、`M_Room_CeilingLight`（天井灯）
- 画面：`M_Room_Screen`。ガラス：`M_Room_Glass`（独立マテリアル。透明度・反射・外景はUnity側で設定する）
- 発光・画面・ガラス・カップ麺の中身は、アセットのマテリアルに統合せず、別スロットのまま残している

## 建築モジュールの規格
- **グリッド：2m。** 床・天井は2m×2m、壁は幅2m×高さ2.5m×厚さ0.15m
- **床**：原点は上面の中心。厚さ0.1mは下へ伸びる。グリッドの升目の中心（X,Zが奇数m）に置く
- **天井**：原点は下面（室内から見える面）の中心。床と同じX,Zで、Y=2.5に置く
- **壁**：原点は室内側の面の下端中央。厚みは原点から後ろ（室外側）へ伸びる。グリッド線の中点に置き、正面（Unityの+Z）を部屋の内側へ向ける。室内側の面同士が角でぴったり接する（外側の角には隙間ができるが、室内からは見えない）
- **窓・ドア・ドア枠**：原点が壁と同じ。対応する壁と同じ位置・回転に置けば、開口に収まる
- 巾木（高さ8cm）は壁に含む。ドア開口の部分だけ切れている

| 開口 | 幅 | 高さ | 下端の高さ | 対応する壁 |
|---|---:|---:|---:|---|
| Window_Normal | 1.0m | 0.9m | 0.95m | Wall_Window |
| Window_Large | 1.6m | 1.4m | 0.6m | Wall_Window_Large（依頼の一覧にはない追加分） |
| Door | 1.0m | 2.15m | 0 | Wall_Door |

## アセット一覧

### 建築（Architecture）
| アセット | Tris | 構成 | 補足 |
|---|---:|---|---|
| Floor_Normal | 44 | 1 Mesh | 樹脂系の集合住宅用床材。1m区切りの目地はテクスチャ |
| Floor_Damaged | 44 | 1 Mesh | Floor_Normalと同じ形。擦り傷、汚れ、補修プレート跡、ひびはテクスチャだけで表現 |
| Floor_Service | 292 | Floor / Hatch | 0.6m角の点検ハッチと、深さ0.25mの配線ピット。Hatchは奥の辺が原点で、X回転で開く |
| Wall_Normal | 24 | 1 Mesh | |
| Wall_Panel | 224 | 1 Mesh | 設備パネル（扉と取っ手）と、床近くの配線口 |
| Wall_Window / Wall_Window_Large | 60 / 60 | 1 Mesh | 窓用の開口 |
| Wall_Door | 60 | 1 Mesh | ドア用の開口 |
| Ceiling_Normal | 44 | 1 Mesh | |
| Ceiling_Light | 100 | 1 Mesh | 1.2m×0.32mの直付け灯。発光面は別マテリアル |
| Window_Normal | 420 | Frame / Glass | 引き違いの2枚窓。室内側に窓台と額縁がある |
| Window_Large | 416 | Frame / Glass | 上ははめ殺し、下は換気用の小窓 |
| Door | 308 | 1 Mesh | 引き戸。ローカル+X方向へ約1m動かすと壁の中へ収まる（ドア枠の戸袋側には通り道の隙間がある） |
| DoorFrame | 176 | 1 Mesh | 開口の内張り、室内側・室外側の額縁、シアンの開閉パネル |

### 壁付けProp（ArchitectureProps）
原点は壁に接する背面の中心で、正面は-Y（Unityでは+Z）。壁の室内側の面に置く。

| アセット | Tris | 補足 |
|---|---:|---|
| WallVent | 248 | 0.4m×0.25m。羽根板5枚 |
| WallOutlet | 144 | 2口。小さなシアン表示灯 |
| LightSwitch | 100 | 大きめのロッカースイッチ |
| CableCover | 108 | 1mの直線モール。原点は片端で、+X方向へ伸びる。1mずつずらしてつなぐ。床にも置ける |

### 家具・設備（Furniture）
| アセット | Tris | 構成 | 動かし方 |
|---|---:|---|---|
| Chair | 2,812 | Base / Seat / Back / Cable / Arm_L / Arm_R / Legrest | Backはヒンジが原点で、X回転でリクライニング。CableもBackと同じ原点。Legrestは座面の前縁が原点 |
| HeadSwapMachine | 2,488 | Base / Pillar / Boom / Ring | BoomはZ回転で退かせる。Ringは下へ約0.2mまで下げられる。Boomと一緒に回したい場合は、Unity上でRingをBoomの子にする |
| HeadRack | 2,296 | Frame / HeadSocket_01〜04 | ヘッドパーツは含まない。実物ヘッドの原点（首の接続位置）をHeadSocketに合わせると、首プラグが台座の差込口に入る |
| Sofa | 3,824 | Frame / SeatCushion_L,R / BackCushion_L,R | 右の座面クッションだけ少し傾けてある |
| LowTable | 204 | 1 Mesh | |
| Desk | 492 | 1 Mesh | 引き出し3段、配線穴、前縁の下にシアンのLED |
| Monitor_A / Monitor_B | 190 / 242 | Body / Screen | Aは薄型、Bは古い小型で少し上向き |

### 生活小物（Props）
| アセット | Tris | 構成 | 補足 |
|---|---:|---|---|
| DeskLight | 728 | 1 Mesh | 暖色の光 |
| CupNoodle | 928 | Cup / Lid_Sealed / Lid_Open / Contents_Full / Contents_Half | 子を表示・非表示して使う。未開封はCup＋Lid_Sealed、食べかけはCup＋Lid_Open＋Contents_Half。中身はテクスチャ1枚を平らな円盤に貼ったもの |
| Utensil_Chopsticks / Utensil_Fork | 56 / 124 | 1 Mesh | |
| DrinkCan / DrinkCan_Crushed | 544 / 544 | 1 Mesh | 空き缶。潰れ版は形の違い |
| Mug | 392 | 1 Mesh | |
| GameController | 768 | 1 Mesh | |
| MobileDevice | 330 | Body / Screen | |
| TrashBin | 872 | Body / Contents | 中身の紙くずは別Mesh |
| StorageBox | 184 | Body / Lid | |
| Blanket_Folded / Blanket_Crumpled | 448 / 3,776 | 1 Mesh | Crumpledは床に広がった裾を含めて約1.06×1.11m |
| Cushion | 768 | 1 Mesh | |
| PowerStrip | 820 | 1 Mesh | 充電器が1つ挿さっている |

合計41アセット、26,702 Tris。

## テクスチャ（`Export/Textures/`、1024px＝2m四方でタイル）
| マテリアル | 内容 |
|---|---|
| Floor | 青みのダークグレーの樹脂床。わずかなムラと1m目地 |
| FloorDamaged | Floorに擦り傷、汚れ、補修プレート跡、ひびを加えたもの |
| Wall | 青みグレーの塗装。塗装ムラ、モジュール間の継ぎ目、1mごとのパネルライン |
| WallRepaired | Wallにパテ補修跡とビス穴跡を加えたもの。メッシュでは使っていないので、Unity上で好きな壁のマテリアルと差し替えて使う |
| Ceiling | 明るいグレー。外周の継ぎ目だけ |

- 建築用もアセット用も共通：Normal MapはOpenGL形式（緑が上）で、Unityの形式と同じ
- Roughnessは粗さ（1で粗い）で入れてある。UnityのSmoothnessとして使うときは反転が必要
- どれも手続き的に作った素材。雰囲気が固まったら、描き直しや差し替えを前提にしている

## 家具・小物のテクスチャ（`Export/Textures/Assets/`）
- アセットごとに `T_<アセット名>_BaseColor / _Roughness / _Normal` の3枚。解像度は表面積で決めていて、2048（椅子・ソファー・交換装置・ラック・デスク・ドア）、1024（中くらいの物）、512（小物）
- Cycles（GPU）で、手続き的な劣化を焼き込んだ。メッシュには手を加えていない
  - 塗装した金属：角の塗装剥がれ（下地の明るい金属色）、くぼみの汚れ、擦り傷
  - 金属：擦り傷と、擦れて艶が出た部分
  - 樹脂：軽い汚れと角の擦れ
  - 合皮（椅子のパッド）：上向きの面の擦れ、角のすり減り、しわ風の凹凸
  - 布（ソファー・クッション・ブランケット）：上向きの面の擦れ、くぼみの汚れ、織り目のNormal
- 描き込み：椅子の座面とソファーの肘掛け、サブモニター、交換装置の支柱にガムテープ補修。交換装置の床置き部とアームには注意表示の黄帯、収納箱には紙ラベル（文字なし）
- Normalは面の細かい凹凸だけで、角は半径4mmで丸めてある。トゥーン調で角の丸みが邪魔なら、Normal Mapの強さを下げるか外す
- 劣化の強さは `build_room_assets.py` の `WEAR` 表（素材の種類ごとの係数）で変えられる。描き込みの位置は `DECALS`。どちらも変えたらスクリプトを実行し直す（RTX 3060で約7分）

## 組み合わせの目安
- 椅子とヘッド交換装置：装置の原点を、椅子の原点から背もたれ側へ0.87m離して置くと、リングが座った頭の真上に来る
- 天板の高さ：デスク0.74m、ローテーブル0.38m
- 実物ヘッドは首の接続位置から上に0.21m、下に0.03mある。ラックの棚間隔は0.4mなので、どの段にも収まる
