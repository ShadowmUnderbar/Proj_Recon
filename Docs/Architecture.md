# RECON 実装構成ドキュメント（引き継ぎ用）

> **最終更新**: 2026-09-26（PR #109 全体リファクタリング / PR #110 UpgradeCardBoardView 分割）
> **目的**: 実コードと突き合わせながら、バトル／メインメニューの各シーンが「どのレイヤーに何があり、どう繋がっているか」を把握できるようにする。
> **読み方**: 1章で全体の約束事、2〜3章で各シーンをレイヤー別に、4章で両シーン共通の常駐部分、5章で開発ツール、6〜7章で今回の変更と残っている負債をまとめる。クラス名は原則ファイル名と一致し、`Assets/App/Script/` 配下にある。

---

## 目次

1. [全体構成](#1-全体構成)
2. [メインメニューシーン](#2-メインメニューシーン)
3. [バトルシーン](#3-バトルシーン)
4. [共通層（常駐スコープ）](#4-共通層常駐スコープ)
5. [開発ツール・デバッグ](#5-開発ツールデバッグ)
6. [今回のリファクタリング内容](#6-今回のリファクタリング内容)
7. [技術的負債と次の候補](#7-技術的負債と次の候補)

---

## 1. 全体構成

### 1.1 レイヤーと依存方向

```
Views  →  Presenters  →  UseCase  →  DataStore  →  Data
 (MonoBehaviour)   (Viewの薄い窓口)   (流れの制御)   (状態・計算)   (POCO / ScriptableObject / enum)
```

| レイヤー | 役割 | 実装上の約束 |
|---|---|---|
| **Data** | 値そのもの。enum・struct・ScriptableObject（Config/Database/MasterData） | ロジックは「計算だけ」のものに限る（例: `StreamerCameraFramingCalculator`） |
| **DataStore** | ランや設定の**状態**と、その状態から導く**計算**（倍率・判定） | `ReactiveProperty` / `Subject` で公開。ラン限りの状態は `IRunResettable` を実装 |
| **UseCase** | イベントを購読して**流れ**を作る。VContainer の EntryPoint（`IInitializable` / `ITickable`） | DataStore と Presenter を組み合わせる。View に直接触らない |
| **Presenters** | View の窓口。UseCase から View を隠す薄いラッパー | ほぼ委譲のみ。ロジックを持たない |
| **Views** | MonoBehaviour。表示・入力・物理 | DataStore/UseCase を参照しない（インターフェース越しに Presenter から呼ばれる） |
| **Interface** | 各レイヤーのインターフェース。`Interface/DataStore`・`Interface/Presenters`・`Interface/Views` に分ける | 名前空間は `App.<Area>.Interface`（DataStore だけ `.Interface.DataStore`） |

- 下位から上位への参照は禁止（DataStore → Presenter など）。現状、違反は無い（`grep` で確認済み）。
- 計算ロジックは UseCase/Service を増やさず、**DataStore 側にヘルパー／計算クラスとして置く**（`CLAUDE.md` の DataStore パターン）。
- リアクティブは R3、DI は VContainer、非同期は UniTask。

### 1.2 シーンと DI スコープ

```
VContainerSettings.RootLifetimeScope = CommonLifetimeScope.prefab  ← DontDestroyOnLoad で常駐
        │
        ├── MainMenu.unity : MainMenuLifetimeScope（親 = Common）
        └── Battle.unity   : BattleLifetimeScope （親 = Common）
```

| スコープ | ファイル | 持ち物 |
|---|---|---|
| `CommonLifetimeScope` | `Common/CommonLifetimeScope.cs`（プレハブ `Prefub/CommonLifetimeScope.prefab`） | セーブデータ・設定・入力・マスターデータ DB・シーン遷移・XR 初期化・配信表示切替 |
| `MainMenuLifetimeScope` | `MainMenu/MainMenuLifetimeScope.cs` | メニュー UI／部屋移動／オプションの UseCase・Presenter・View（シーン配置物を `RegisterComponentInHierarchy`） |
| `BattleLifetimeScope` | `Battle/BattleLifetimeScope.cs` | バトルの全 DataStore・UseCase・Presenter・View（プレハブ生成）・Config |

注意点（`project-scene-split-mainmenu` メモより）:
- 新しい `LifetimeScope` の .cs を作ると VContainer のスクリプト自動生成が雛形で上書きすることがある（`DisableScriptModifier` は有効にしてある）。
- `VContainerSettings.RemoveClonePostfix` は **false のまま**。Playtest ツールが `XxxView(Clone)` の名前で `GameObject.Find` している。

### 1.3 起動〜遷移フロー

```
起動（Build Settings 先頭 = MainMenu）
  → CommonLifetimeScope 生成: SaveDataStore.Load → 各 DataStore が値を反映、XRInitUseCase が XR 起動
  → MainMenuLifetimeScope: START ボタン → MainMenuUseCase がセット選択 UI（RunStartView）を出す
       → スロット選択／使わずに開始 → RunLoadoutDataStore（常駐）に選択を積む → ISceneTransitionUseCase.LoadBattle()
  → BattleLifetimeScope: RunStartUseCase が RunLoadoutDataStore の選択を装備してすぐウェーブ1 開始
       （選択が無い＝Battle シーンを直接再生したときだけ、バトル内でセット選択 UI を出す。IsWavePause=true）
  → … → HP0 → GameOverUseCase
       ├ リスタート: RunResetUseCase → 全 IRunResettable.ResetRun() → 同じ選択で再開（直接再生時はセット選択へ）
       └ メインメニューへ: ISceneTransitionUseCase.LoadMainMenu()
```

### 1.4 入力

- `Common/Inputs/GameMaininput.cs` は Input System の**自動生成**ファイル（手で編集しない）。
- `GameInputDataStore`（常駐）が Input Actions を有効化し、トリガー／グラブ／スティック／デバッグキーを `ReactiveProperty` に変換する。バトル・メニューの両方がこれを読む。
- VR／PC の分岐は `DebugConfig.IsVRMode`（エディタは EditorPrefs、ビルドは常に true）。

### 1.5 マスターデータのパイプライン

```
Google スプレッドシート（GAS で CSV/ScriptableObject 出力）
  → Tools/マスターデータ/* ファイルコピー（Editor/*Importer.cs）
  → Assets/App/MasterData/**（UpgradeMasterData / BuffMasterData / EnemyMasterData / WaveScalingMasterData）
  → *Database.asset にまとめて CommonLifetimeScope から RegisterInstance
```

- 上流（シート）の編集は `sheets-write` スキル、enum 追加も同スキルが担当。
- ゲームバランスの手調整値は `*Config.asset`（ScriptableObject）に外部化し、`BattleLifetimeScope` / `CommonLifetimeScope` の `[SerializeField]` から `RegisterInstance` する。

---

## 2. メインメニューシーン

### 2.1 シーン構成（`Assets/Scenes/MainMenu.unity`）

| オブジェクト | 内容 |
|---|---|
| `MainMenuLifetimeScope` | DI スコープ |
| `MenuRoom` / `Floor` / `Ceiling` / `Wall*` | `Tools/MainMenu/メインメニューの部屋を生成` で作った部屋（`Editor/MainMenuRoomBuilder.cs`） |
| `MenuPlayerRig` | XR リグの親。`MenuLocomotionView`（CharacterController）と `VrUiRayAlwaysOnView` が付く |
| `TeleportLine` / `TeleportMarker` | テレポート照準の見た目 |
| UI パネル（`UI/MainMenuView.prefab`, `UI/OptionPanelView.prefab`） | 部屋に固定設置。`MenuPanelProximityView` で近づいたときだけ操作可 |

### 2.2 レイヤー別一覧

**Views（`MainMenu/Views`）**

| クラス | 責務 |
|---|---|
| `MainMenuView` | タイトルと START ボタン。`OnStart` を流し、セット選択中は START を隠す |
| （共通）`RunStartView` | `MainMenuView.prefab` 内の BuildSelectPanel に付く。スロット3＋「使わずに開始」＋「戻る」。実装は `Common/Views`（4.3） |
| `OptionPanelView` | 利き手／移動方式ボタン、移動速度／スナップターン角スライダー。値の表示と操作通知のみ |
| `MenuLocomotionView` | XR リグを CharacterController で動かす。カプセルを毎フレーム HMD 真下へ合わせる。スナップターン・テレポート実行 |
| `MenuPanelProximityView` | CanvasGroup の interactable/alpha を距離で切り替える（ヒステリシスあり）。DI 対象外の純粋な見た目制御 |

**Presenters（`MainMenu/Presenters`）** — すべて委譲のみ

| クラス | 対応 View |
|---|---|
| `MainMenuPresenter` | `IMainMenuView` |
| （共通）`RunStartPresenter` | `IRunStartView`（`Common/Presenters`） |
| `OptionPanelPresenter` | `IOptionPanelView` |
| `MenuLocomotionPresenter` | `IMenuLocomotionView` |

**UseCase（`MainMenu/UseCase`）**

| クラス | 依存 | 何をするか |
|---|---|---|
| `MainMenuUseCase` | `IMainMenuPresenter`, `IRunStartPresenter`, `IRunLoadoutDataStore`, `IMetaProgressionDataStore`, `ISceneTransitionUseCase` | START → セット選択 UI を表示。スロット／使わずに開始 → `RunLoadoutDataStore` に積んで `LoadBattle()`。失敗時は選び直せる状態へ戻す。Initialize で前回の選択を `Clear()` |
| `OptionUseCase` | `IOptionPanelPresenter`, `IPlayerSettingDataStore` | 設定→パネル表示、パネル操作→保存。スライダーは 0.3 秒 Debounce、Dispose 時に保存漏れを回収 |
| `MenuLocomotionUseCase` | `IMenuLocomotionPresenter`, `IGameInputDataStore`, `IPlayerSettingDataStore` | 左スティック＝歩行／テレポート照準、右スティック左右＝スナップターン。移動方式は設定で切替 |

**DataStore / Data** — メニュー固有のものは無く、常駐の `RunLoadoutDataStore`（次ランのセット選択）、`MetaProgressionDataStore`（スロット内容）、`PlayerSettingDataStore`（`SaveData` の `DominantHand` / `Locomotion` / `MoveSpeed` / `SnapTurnAngle`）と `PlayerSettingRange`（範囲・刻み・既定値）を使う。

### 2.3 データフロー

```
GameInputDataStore.V2LeftAxis / V2RightAxis
   └→ MenuLocomotionUseCase.Tick → MenuLocomotionPresenter → MenuLocomotionView.Move / SnapTurn / Teleport*

OptionPanelView(操作) → OptionPanelPresenter → OptionUseCase → PlayerSettingDataStore.SetXxx → SaveDataStore.Save
PlayerSettingDataStore.Xxx(ReactiveProperty) → OptionUseCase → OptionPanelPresenter.SetXxx → OptionPanelView(表示)

MainMenuView.OnStart → MainMenuPresenter → MainMenuUseCase → RunStartPresenter.Show（START を隠す）
RunStartView.OnSlotSelected / OnStartWithoutLoad → RunStartPresenter → MainMenuUseCase
   → RunLoadoutDataStore.Select(スロットの UpgradeIds) / SelectNone → SceneTransitionUseCase.LoadBattle
RunStartView.OnBack → MainMenuUseCase → タイトル表示へ戻す
```

### 2.4 エディタ支援

- `Editor/MainMenuRoomBuilder.cs`: 部屋・リグ・パネル配置を生成（`Tools/MainMenu/メインメニューの部屋を生成`）
- `Editor/OptionPanelBuilder.cs`: `OptionPanelView.prefab` の UI 構築

---

## 3. バトルシーン

### 3.1 シーン構成（`Assets/Scenes/Battle.unity`）

| オブジェクト | 内容 |
|---|---|
| `BattleLifetimeScope` | DI スコープ。全 View プレハブと Config を `[SerializeField]` で持つ |
| `Stage` / `Plane` / `CurvedGround` | 地形。`CurvedGround` は `CurvedWorldGroundView` が生成する見た目用メッシュ（コライダーは平ら） |
| `EventSystem`, `Directional Light` | 標準 |

プレイヤー・敵・UI は**すべて `BattleLifetimeScope` がプレハブから生成**する（`RegisterComponentInNewPrefab` / `SimpleObjectFactory`）。シーンには置かない。

### 3.2 ランの進行

```
RunStartUseCase  : RunLoadoutDataStore（メインメニューの選択）の UpgradeIds を UpgradeSessionDataStore.Preload
                   → UpgradeSideEffectApplier で副作用適用 → IsWavePause=false でウェーブ1開始
                   選択が無い（Battle シーン直接再生）ときだけセット選択 UI（RunStartView）を出して待つ
WaveManagerUseCase: 経過時間 or キル数が WaveConfig に達したら AdvanceWave
                   （IsWavePause=true → スポーン周期リセット → 弾・粒子全消去 → OnWaveAdvanced）
ShopUseCase      : OnWaveAdvanced でショップを開く。UpgradeLotteryDataStore で抽選、ポイントで購入
                   → 「次のウェーブへ」で IsWavePause=false
GameOverUseCase  : PlayerState.Health<=0 → 死亡演出（PlayerDeathConfig）→ GameOverView
                   → スロット保存（MetaProgressionDataStore） / リスタート（RunResetUseCase） / メインメニューへ
RunResetUseCase  : IReadOnlyList<IRunResettable> を全部 ResetRun() → 敵・弾・粒子を消す → RunStartDataStore.IsSelecting=true（同じ選択で再開）
```

`IRunResettable` は VContainer の登録から自動で集めるため、**ラン限りの状態を持つ DataStore を足すときは実装するだけでよい**。

### 3.3 Views（`Battle/Views`）

**プレイヤー**

| クラス | 責務 |
|---|---|
| `BattlePlayerView` | プレイヤーの root。手のポーズ、フォーカス入力、視線（`TryGetGazePose`）、子 View への振り分け。`ISimpleObjectFactory` で照準／マズル／ショット／ブリッツ／カウンタートレーサーを生成 |
| `PlayerMoveView` / `PlayerAnimationView` / `PlayerAimIKView` | 移動・アニメ（死亡含む）・腕 IK |
| `PlayerTopDownAimView` / `PlayerTopDownAimListView` | 見下ろし視点の照準レイ（`LayerConstants.Default` / `Enemy`） |
| `PlayerAimMuzzleView` / `PlayerShotView` / `PlayerBulletView` | マズル・発射・プレイヤー弾（`StraightBullet` 派生、即着弾） |
| `PlayerDamageReceiverView` | プレイヤー側の `IHitBoxView`。被弾ダメージを `OnDamaged` で公開（`HitBoxStoreView` には登録しない） |
| `PlayerLifeGaugeView` | 足元の半円ゲージ（シェーダ描画） |

**敵**

| クラス | 責務 |
|---|---|
| `EnemyStoreView` | 敵の生成（Addressables）・破棄・検索（レイキャスト／注視／直線）。`IHitBoxStoreView` へヒットボックス登録 |
| `EnemyView` | 敵1体。Pose の公開・被弾フィードバック（`EnemyHitFeedbackView`）・ヒットボックス群 |
| `Interface/Views/EnemyAI/EnemyAIBase` | NavMeshAgent ベースの AI 基底。`Idle / Battle / Dead` ステート、ポーズ・スタン・速度倍率・吹き飛ばし |
| `Enemy/AI/Fire, Orbit, Rush, Satellite, Scout, Shield` | 6 種の AI。`Fire` 派生が弾を撃つ |
| `Enemy/Bullet/BaseBulletView, StraightBullet, HomingBullet` | 敵弾（`_shooterLayer` は `Framework.Layer`）。`PointParticle` レイヤーを除外して判定 |
| `HitBoxView` / `HitBoxStoreView` | ヒットボックス。`OnHit` で `HitData` を作って流す。耐性方向で貫通可否を返す |
| `BulletStoreView` / `BulletTracerView` / `CounterTracerView` / `BlitzEffectView` | 弾の一括管理・曳光弾・カウンター演出・ブリッツ演出。`TracerFreezeState` でフリーズ中に停止 |

**UI・演出**

| クラス | 責務 |
|---|---|
| （共通）`RunStartView` | 直接再生時のセット選択（スロット3＋「使わずに開始」）。実装は `Common/Views`、プレハブは `UI/RunStartView.prefab` |
| `ShopView` + `UpgradeCardBoardView` + `UpgradeCardView` + `CardHighlight` | ウェーブ間ショップ。VR は 3D カードを掴んでトリガー確定、PC はマウスクリック。Canvas ボタンはフォールバック |
| └ `UpgradeCardBoardLayout` / `UpgradeCardFinder` / `UpgradeCardHandInteraction` / `UpgradeCardPointerInteraction` | ボードの内部分担（plain C#、DI 対象外）。配置の純粋計算／近接・レイ・UI越しの検索／VR両手の掴み・ひねり・確定の状態機械／非VRのホバー・クリック確定。Inspector 値は `UpgradeCardHoldSettings` / `UpgradeCardGrabSettings` に毎フレーム束ねて渡す |
| `GameOverView` | スロット保存／リスタート／メインメニューへ |
| `PointParticleStoreView` / `PointParticleView` | ポイント粒子（一括更新、粒子ごとの Update 無し） |
| `StreamerCameraView` | 配信用カメラ（HMD 映像に干渉しない）。`StreamerModeConfig` で既定 OFF |

### 3.4 Presenters（`Battle/Presenters`）

| クラス | 対応 View | 備考 |
|---|---|---|
| `PlayerControlPresenter` | `IBattlePlayerView` | 移動・射撃・照準・レイ色・死亡アニメなど、UseCase からプレイヤーへの窓口を一手に持つ |
| `EnemyPresenter` | `IEnemyStoreView` | スポーン／消去／検索／ポーズ／スタン／速度倍率／吹き飛ばし |
| `BattleHitPresenter` | `IHitBoxStoreView` | `OnHit` の集約 |
| `ShopPresenter` / `GameOverPresenter` | 各 UI View | |
| （共通）`RunStartPresenter` | `IRunStartView`（`Common/Presenters`） | |
| `PlayerLifeGaugePresenter` / `PointParticlePresenter` / `StreamerCameraPresenter` | 各 View | |

### 3.5 UseCase（`Battle/UseCase`）

登録順が `Tick` / 購読順になるため、`BattleLifetimeScope` の並びを変えるときは注意（コメントで理由を残してある箇所あり）。

| クラス | 何をするか | 主な依存 |
|---|---|---|
| `PlayerMoveUseCase` | 左スティック → 移動・モデル向き・アニメ。フリーズ／回避中は止める | PlayerState, Freeze, DodgeParameter |
| `PlayerAimUseCase` | 手のレイ／マウスから照準位置を決めフォーカス対象を追う | PlayerAim, PlayerFocus, ShotConflict |
| `PlayerShotUseCase` | トリガー → `PlayerBulletParameterDataStore` で弾データを作り `Shot`。フォーム（Normal/Merge/Waltz）と両手の可否 | PlayerShotType, PlayerBulletParameter, CoreSkillUnlock |
| `PlayerDodgeUseCase` | 回避入力 → 直線移動。通過した敵を接触として記録 | PlayerDodgeParameter, DodgeCounterAttack |
| `DodgeCounterAttackUseCase` | 回避終了時の跳ね返し攻撃（扇形＋直線検索 → レイ演出 → フリーズ → ダメージ）。`DodgeCounterAttackConfig` | DodgeCounterAttack, Enemy, Freeze |
| `PlayerGazeUseCase` | 視界中央の敵を判定し注視系（スネークアイズ／メデューサ／ガン飛ばし）を反映 | SnakeEyes, Medusa, MeanMug |
| `PlayerHitUseCase` | 被弾 → `PlayerStateDataStore.TakeDamage`（回避中は無効） | PlayerState, DodgeParameter |
| `PlayerLifeGaugeUseCase` | HP・バリアをゲージへ、位置を追従 | PlayerState, PlayerBarrier |
| `EnemySpawnUseCase` / `EnemyRandomSpawnUseCase` | `EnemyDataStore` の生成要求を View へ／周期スポーン（`EnemyRandomSpawnCycleDataStore`）と出現位置 | Enemy, RandomSpawnCycle |
| `EnemyControlUseCase` | プレイヤーの照準方向を敵 AI へ渡す | PlayerAim, Enemy |
| `BattleHitUseCase` | `OnHit` → 倍率（貫通バフ／ガン飛ばし／クリティカル）→ 感電伝播 → `EnemyDataStore.Damage`。撃破時の回復（HealOnKill）等 | Enemy, BuffState, CriticalHit, ElectricShock |
| `PointDropUseCase` | 撃破 → 粒子ドロップ、回収 → ポイント加算。`BattleHitUseCase` より先に登録（撃破地点を読むため） | Point, PointDropCalculator |
| `BuffConditionUseCase` / `CareNodeUseCase` | バフ条件の入力（ヒット・HP割合・回避）／ケア・ノードの毎秒効果 | BuffState, CareNode |
| `FreezeUseCase` | フリーズの開始・解除で敵・弾・レイ演出を止める | Freeze, Enemy, BulletStore |
| `WaveManagerUseCase` / `ShopUseCase` / `RunStartUseCase` / `GameOverUseCase` / `RunResetUseCase` | 3.2 参照 | |
| `UpgradeSideEffectApplier` | アップグレード付与の副作用（バフ起動・バリア満タン）。Shop と RunStart の共通処理 | BuffState, PlayerBarrier |
| `StreamerCameraUseCase` | ウェーブ進行・ボス・マルチキルで配信カメラの演出をトリガー | StreamerCamera, Enemy |

### 3.6 DataStore（`Battle/DataStore`）

**プレイヤー状態・パラメータ**

| クラス | 内容 |
|---|---|
| `PlayerStateDataStore` | 位置・HP・最大HP・`TakeDamage`（バリア→エマージェンシー→軽減バフの順）。`PlayerBaseParameterConfig` から基礎値 |
| `PlayerBulletParameterDataStore` | 弾データ生成（ダメージ・貫通・爆風）とクールダウン。全倍率をここで乗算 |
| `PlayerDodgeParameterDataStore` | 回避回数・クールダウン・移動中フラグ・`OnDodge` 等 |
| `PlayerAimDataStore` / `PlayerFocusDataStore` / `PlayerShotTypeDataStore` | 照準位置・フォーカス対象・射撃フォーム |
| `PlayerBarrierDataStore` | バリア残量と自然回復 |
| `ShotConflictDataStore` | コンフリクト系（フォーム封印と引き換えの強化） |

**敵・ウェーブ**

| クラス | 内容 |
|---|---|
| `EnemyDataStore` | 敵の実体データ（`EnemyData`）、HP、`OnEnemyDead`。スポーン時 HP/攻撃力は `EnemyWaveScalingCalculatorDataStore` で決める |
| `EnemyRandomSpawnCycleDataStore` | ランク別スポーン周期・同時数の増加（`MinorSpawnCountGrowthRate`） |
| `EnemyWaveScalingCalculatorDataStore` | `WaveScalingDatabase` からウェーブ帯ごとの増加率（線形・切り上げ） |
| `WaveManagerDataStore` | 現在ウェーブ・経過時間・キル数・ポーズ・`OnWaveAdvanced` |
| `GameStateDataStore` / `RunStartDataStore` / `FreezeDataStore` | ゲームオーバー／セット選択中／フリーズ残時間 |

**アップグレード・バフ**

| クラス | 内容 |
|---|---|
| `UpgradeSessionDataStore` | このランで所持しているアップグレード ID |
| `UpgradeEffectSimpleCalculatorDataStore` | `UpgradeType` ごとの単純倍率（`CalcMultiply`） |
| `UpgradeLotteryDataStore` / `UpgradeLocalizationDataStore` / `UpgradeDescriptionFormatter` / `EffectTextStyler` / `UpgradeLocalizationKey` | 抽選・ローカライズ・説明文の整形 |
| `BuffStateDataStore` | 取得済みバフの発動条件進行と残り時間 |
| 個別効果: `Avalanche`, `PeaceMaker`, `CriticalHit`, `ElectricShock`, `HealOnKill`, `SnakeEyes`, `Medusa`, `MeanMug`, `DependencyNode`, `DamageNode`, `CareNode`, `EmergencyNode`, `DodgeCounterAttack` | 各アップグレードの実行時状態・倍率計算。**新しいアップグレードはこの粒度で DataStore を足す**（`upgrade-add` スキル参照） |

**ポイント・配信**

| クラス | 内容 |
|---|---|
| `PointDataStore` / `PointDropCalculatorDataStore` | 所持ポイント／ドロップ量と粒子分割 |
| `StreamerCameraDataStore` / `StreamerCameraMultiTargetTracker` | 演出の状態・マルチキル計数 |

### 3.7 Data / Config（`Battle/Data`）

| 種別 | クラス |
|---|---|
| ScriptableObject（`Assets/App/MasterData/**` に実体） | `PlayerBaseParameterConfig`（**今回新設**）, `DodgeCounterAttackConfig`, `PlayerDeathConfig`, `PointDropConfig`, `PointParticleConfig`, `StreamerCameraTriggerConfig`, `StreamerCameraShotData`, `UpgradeDescriptionStyle` |
| 定数 | `PlayerConstants.PlayerId`（**今回新設**）, `ThemeColors` |
| POCO / struct | `EnemyData`, `HitData`, `BulletData`(Common), `DodgeEndData`, `PlayerDamagedData`, `ElectricShockChain`, `ShopHandInput`, `ShopPointerInput`, `StreamerCameraShotRequest`, `UpgradeLocalizedText` |
| enum | `EnemyAIState`, `HitBoxType`, `StreamerCameraShotType` |

### 3.8 主要データフロー

```
【射撃】
GameInputDataStore.IsRightTrigger
 → PlayerShotUseCase.Tick → PlayerBulletParameterDataStore.CanShot / GetBulletData
 → PlayerControlPresenter.Shot → BattlePlayerView → PlayerShotView → PlayerBulletView(即着弾 SphereCast)
 → HitBoxView.OnHit → HitBoxStoreView → BattleHitPresenter.OnHit
 → BattleHitUseCase(倍率・感電) → EnemyDataStore.Damage → OnEnemyDead
     ├→ WaveManagerUseCase(キル数) / PointDropUseCase(粒子) / BattleHitUseCase(撃破処理)
     └→ EnemyPresenter.UnSpawn

【被弾】
敵AI(Rush) or 敵弾 → PlayerDamageReceiverView.OnHit → BattlePlayerView.OnDamaged
 → PlayerHitUseCase → PlayerStateDataStore.TakeDamage → Health → GameOverUseCase

【敵スポーン】
EnemyRandomSpawnCycleDataStore.Tick → OnSpawnXxxEnemy → EnemyRandomSpawnUseCase(位置決め)
 → EnemyDataStore.Spawn(HP/攻撃力はウェーブ倍率適用) → OnSpawn → EnemySpawnUseCase → EnemyPresenter.Spawn → EnemyStoreView(Addressables)

【回避 → 跳ね返し】
IsDodge → PlayerDodgeUseCase(直線移動, 接触記録) → OnDodgeEnd
 → DodgeCounterAttackUseCase → DodgeCounterAttackDataStore(対象・ダメージ算出) → FreezeDataStore → ダメージ適用
```

---

## 4. 共通層（常駐スコープ）

### 4.1 DataStore（`Common/DataStore`）

| クラス | 内容 |
|---|---|
| `SaveDataStore` | `SaveData` の JSON 保存・読込。**パスは `Application.dataPath/DLHN/SaveData.json`**（7章参照） |
| `PlayerSettingDataStore` | 利き手・移動方式・移動速度・スナップターン角。非 VR では利き手を左に固定 |
| `CoreSkillUnlockDataStore` | コアスキル解放状態（`SaveData.UnlockType`、`DebugConfig.IsAllUnLock` で全解放） |
| `MetaProgressionDataStore` | アップグレードセットのスロット保存（3 スロット） |
| `RunLoadoutDataStore` | メインメニューで選んだ次ランのセット（選択時点のアップグレード ID 一覧のコピー／使わない／未選択）。スロット番号ではなく ID を持つので、ゲームオーバーでスロットを上書きした後のリスタートでも開始時のセットで再開する。永続化なし。メニューに入るたび `Clear()` |
| `GameInputDataStore` | 1.4 参照 |

### 4.2 UseCase（`Common/UseCase`）

| クラス | 内容 |
|---|---|
| `SceneTransitionUseCase` | `LoadMainMenu` / `LoadBattle`。二重遷移防止、失敗を戻り値で返す |
| `XRInitUseCase` | XR Loader 起動／停止 |
| `StreamerDisplayUseCase` | ストリーマーモード時のミラー表示抑制 |

### 4.3 Views / Presenters（`Common/Views`, `Common/Presenters`）

| クラス | 内容 |
|---|---|
| `RunStartView` / `RunStartPresenter` | アップグレードセット選択 UI（スロット3＋「使わずに開始」＋任意の「戻る」）。メインメニューとバトル（直接再生時）で共用 |
| `VrUiFollowCanvasView` / `VrUiRayView` / `VrUiRayAlwaysOnView` | VR 向け UI 基盤（遅延追従キャンバス・ハンドレイ） |
| `ForwardRayView` / `HandForwardRayView` / `PlatformHandRotation` | 手・照準のレイ表示、プラットフォーム別の手の回転補正 |
| `PlayerCameraTrackingView` | エディタ非 VR 時に `TrackedPoseDriver` を切る（旧 `PlayerCameraData`、`Camera.prefab` に付く） |
| `CurvedWorldView` / `CurvedWorldGroundView` / `CurvedWorldBoundsView` / `CurvedWorldCameraRigView` / `CurvedWorldLine` | 水平線カーブ（頂点シェーダ）。詳細は `curved-world` スキル |
| `CurvedWorldPrototypeMoveView` / `CurvedWorldTunerView` | `CurvedWorldPrototype.unity` 専用のプロトタイプ用（本編未使用） |

### 4.4 Data（`Common/Data`）

| 種別 | クラス |
|---|---|
| Database（ScriptableObject） | `EnemyDatabase`, `EnemySpawnDatabase`, `UpgradeDatabase`, `BuffDatabase`, `WaveScalingDatabase` |
| MasterData（ScriptableObject、シート由来） | `EnemyMasterData`, `UpgradeMasterData`, `BuffMasterData`, `WaveScalingMasterData` |
| Config（ScriptableObject） | `WaveConfig`, `StreamerModeConfig`, `CurvedWorldConfig`, `EnemyHitFeedbackConfig` |
| 定数・設定 | `DebugConfig`（EditorPrefs）, `SceneNames`, `LayerConstants`（`Default` / `Enemy` / `PointParticle`、名前から引く）, `TagConstants`, `GameParamData`, `PlayerSettingRange`, `VectorConstants`（`DirectionEpsilon`、各 View の方向判定で共用） |
| セーブ | `SaveData`, `UpgradeSetSlot` |
| enum | `AimFocusType`, `ShotType`, `HandType`, `LocomotionType`, `PlatformType`, `EnemyRankType`, `HitDirectionType`, `UpgradeType`, `BuffConditionType`, `BuffEffectType`, `ParameterType`, `PlayerUnlockType`, `UnlockCoreSkillType` |

### 4.5 Framework（`Framework`）

`SimpleObjectFactory<TInterface, TView>`（プレハブからの生成）、`Layer` 構造体（レイヤー選択用、Editor 依存は `#if UNITY_EDITOR` で分離済み）、拡張メソッド（`TransformExtensions.ToPose`, `RelativeYawExtension`, `TopdownVector2Extensions`）、`AssetPathAttribute` / `ReadOnlyAttribute`。

---

## 5. 開発ツール・デバッグ

| 入口 | 内容 |
|---|---|
| `Tools/Playtest Runner`（`Editor/TestRun/PlaytestRunnerWindow.cs`）／`Tools/Playtest/run-playtest.ps1` | バトルの自動プレイ・エラー検出。`playtest-run` スキル |
| `Tools/マスターデータ/*` | シート出力ファイルの取り込み（1.5） |
| `Tools/MainMenu/…` / `Tools/Upgrade/…` | 部屋・カードプレースホルダの生成 |
| `Editor/AppVRModeMenu.cs` / `StartUpgradeDebugWindow.cs` | `DebugConfig` の EditorPrefs（VR モード／全解放／開始時アップグレード）を切り替える |
| `GameInputDataStore.Debug*` | `Shift+U` でショップを開く等のデバッグ入力（エディタのみ） |
| uLoop MCP | Claude からのコンパイル・PlayMode・ログ取得 |

---

## 6. 今回のリファクタリング内容

| # | 種別 | 変更 |
|---|---|---|
| 1 | バグ修正 | `Framework/Layer.cs` と旧 `PlayerCameraData.cs` の `using UnityEditor;` が `#if UNITY_EDITOR` の外にあり、**プレイヤービルドが通らない**状態だったのをガード |
| 2 | 静的クラス廃止 | `BasePlayerParameter`（静的）→ `PlayerBaseParameterConfig`（ScriptableObject、`Assets/App/MasterData/Player/`）。`PlayerState` / `PlayerBulletParameter` / `PlayerDodgeParameter` の各 DataStore にコンストラクタ注入。プレイヤー ID は `PlayerConstants.PlayerId` へ分離。未使用だった 7 プロパティ（`BaseShotRate`, `FocusDamageMagnification`(旧1.1), `FocusPenetration`, `FocusExplosiveMagnification`, `LongFocus*` の未参照分）は削除。旧 `LongFocusDamageMagnification`(3.0) が実際にはフォーカス時の倍率として使われていたため `FocusDamageMagnification` = 3.0 として引き継いだ（**挙動は変えていない**） |
| 3 | 重複統一 | `Battle/Data/LayerMasks`（`LayerNumber.cs`）と `Common/Data/LayerConstants` を後者に統一。レイヤー番号の直書き（`1 << 6` 等）をやめ `LayerMask.NameToLayer` で引く。`Hitbox`（実体は Enemy レイヤー）を `Enemy` に改名 |
| 4 | デッドコード削除 | `IPlayerAimView`, `PlayerData`, `SaveDataManager.cs`（中身は空の `PlayerUnlockDataStore`）, `DisplayUseCase`（`XRInitUseCase` と重複、未登録） |
| 5 | 配置整理 | `PlayerCameraData` → `Common/Views/PlayerCameraTrackingView`（MonoBehaviour が Data にあった。GUID 維持のため git mv）、`UpgradeEffectSimpleCalculator.cs` → クラス名と一致、`IStreamerCameraPresenter/View` と `IPlayerSettingDataStore` を Interface のサブフォルダへ |
| 6 | マジックナンバー | `EnemyRandomSpawnCycleDataStore` の `1.5f` → `MinorSpawnCountGrowthRate`、`EnemyStoreView` の回避判定半径 `0.5f` → `DodgeHitRadius`、`PlayerStateDataStore` の `BaseSpeed(0.05)` を Config の `MoveSpeed` に集約 |
| 6' | 安全策 | `LayerConstants` は未定義レイヤー名で例外を投げる（`1 << -1` で黙って壊れない）。`PlayerBaseParameterConfig` の各値に `[Min]` を付け、インスペクタから 0 や負値を入れられないようにした |
| 7 | 文書 | 本ドキュメント新設、`CLAUDE.md` の古い参照（`PlayerDataStore.cs` / `BasePlayerParameter.cs`）を更新 |
| 8 | God Class 分割（PR #110） | `UpgradeCardBoardView`（789 行）を `UpgradeCardBoardLayout` / `UpgradeCardFinder` / `UpgradeCardHandInteraction` / `UpgradeCardPointerInteraction` に分割（本体は約 230 行）。`SerializeField` 名と公開 API は据え置きでプレハブ変更なし。移動の過程で、両手が同じカードを狙って片方が外れると強調表示が戻らない旧バグを修正。`DirectionEpsilon` の 4 重定義を `VectorConstants` に集約 |

---

## 7. 技術的負債と次の候補

優先度は `CLAUDE.md` の基準（バグ > God Class・マジックナンバー > 重複・性能 > 命名・デッドコード）。

| 優先 | 項目 | 現状 | 提案 |
|---|---|---|---|
| 高 | **`DebugConfig` の直接参照**（22 ファイル） | `IsVRMode` を DataStore / UseCase / View が静的に読む。View の一部は DI 対象外のシーン配置物 | `IPlatformModeProvider`（仮）を Common スコープに登録し、DataStore/UseCase から順に注入へ置き換える。DI 対象外の View（`PlatformHandRotation`, `VrUiRayView` 等）は `RegisterComponentInHierarchy` か `[Inject]` メソッド化が要る。**一括でやると差分が大きいので、UseCase → DataStore → View の順に 2〜3 PR に分ける** |
| 高 | `UpgradeCardBoardView` 分割後の VR 実機確認 | PR #110 で分割済み。式・閾値は変えていないが、掴み・手首ひねり・両手の取り合いは PC のプレイテストでは通らない | Quest 実機でショップを開き、近接掴み／レイ掴み／ひねりで裏面確認／両手で同じカードを狙う、を一通り確認する |
| 中 | セーブデータのパス | `Application.dataPath/DLHN/SaveData.json`（Assets 配下。ビルドでは書き込み不可） | `Application.persistentDataPath` へ。既存メモ `project-meta-upgrade-set-save` の残タスク |
| 中 | `PlayerBulletParameterDataStore` の倍率乗算 | 全倍率を 1 メソッドに順に掛けている。加算・減算が混ざる変更をするときは事前相談（`feedback_damage_calc_additive`） | 倍率の「出典」ごとに計算クラスへ分けると読みやすいが、現状の順序に依存した仕様があるので急がない |
| 中 | `CurvedWorldPrototype.unity` が Build Settings に入っている | プロトタイプ用シーン・View（`CurvedWorldPrototypeMoveView` / `CurvedWorldTunerView`）が本編ビルドに含まれる | 実機調整が終わったら Build Settings から外す。View は `Editor` か `Prototype` フォルダへ隔離 |
| 低 | `GameParamData`（`RayMaxDistance` のみ） | 静的クラスに定数 1 つ | 使う側（`PlayerTopDownAimView` / `ForwardRayView`）の `[SerializeField]` にするか、`WaveConfig` 等の既存 Config へ吸収 |
| 低 | `Battle/Interface` 直下の `EnemyAI/EnemyAIBase` | 抽象クラス（MonoBehaviour）が Interface フォルダにある | `Battle/Views/Enemy/AI/` へ移動（名前空間 `App.Battle.Interface.EnemyAI` の変更を伴うためプレハブ参照は GUID で保たれる） |
| 低 | ドキュメントコメントの無い UseCase | `PlayerMoveUseCase` 等いくつかは `<summary>` が無い | 触ったタイミングで足す |
