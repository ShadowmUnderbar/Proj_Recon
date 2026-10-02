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
  → BattleLifetimeScope: RunStartUseCase が RunLoadoutDataStore の選択を装備（1回で消費）してすぐウェーブ1 開始
       （選択が無い＝Battle シーンを直接再生したときは、バトル内でセット選択 UI を出す。IsWavePause=true）
       （メニュー側・バトル側とも、保存済みスロットが 1 つも無ければ選択 UI を出さず「使わずに開始」扱いで即開始）
  → … → HP0 → GameOverUseCase
       ├ リスタート: RunResetUseCase → 全 IRunResettable.ResetRun() → バトル内のセット選択へ
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
                   → UpgradeSideEffectApplier で副作用適用 → 選択を Clear（消費）→ IsWavePause=false でウェーブ1開始
                   選択が無い（Battle シーン直接再生・リスタート）ときはセット選択 UI（RunStartView）を出して待つ
WaveManagerUseCase: 経過時間 or キル数が WaveConfig に達したら AdvanceWave
                   （IsWavePause=true → スポーン周期リセット → 弾・粒子全消去 → OnWaveAdvanced）
                   ボスウェーブ（BossWaveConfig、既定5）だけは時間・キル数を見ず、ボスを全員倒したときに進める
BossWaveUseCase  : ボスウェーブの開始（IsWavePause=false）で残った敵を撃破扱いせずに消し、
                   プレイヤーを BossWaveConfig.PlayerPosition へ移して BossGroupConfig のボスを出す。
                   ボスウェーブ中は EnemyRandomSpawnCycleDataStore が周期スポーンを止める
ShopUseCase      : OnWaveAdvanced でショップを開く。UpgradeLotteryDataStore で抽選、ポイントで購入
                   → 「次のウェーブへ」で IsWavePause=false
GameOverUseCase  : PlayerState.Health<=0 → 死亡演出（PlayerDeathConfig）→ GameOverView
                   → スロット保存（MetaProgressionDataStore） / リスタート（RunResetUseCase） / メインメニューへ
RunResetUseCase  : IReadOnlyList<IRunResettable> を全部 ResetRun() → 敵・弾・粒子を消す → RunStartDataStore.IsSelecting=true（セット選択へ）
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
| `EnemyStoreView` | 敵の生成（Addressables）・破棄・検索（レイキャスト／直線）。`IHitBoxStoreView` へヒットボックス登録。注視判定は物理を使わず、敵プレハブの `EnemyGazeBoundsView`（判定球の高さ・半径、ギズモ表示）と視線の距離を `EnemyGazeTracker`（plain C#）が LateUpdate で1フレームに決まった数ずつ順番に更新する。`BattleLifetimeScope` でプレハブから生成（`RegisterComponentInNewPrefab`） |
| `EnemyView` | 敵1体。Pose の公開・被弾フィードバック（`EnemyHitFeedbackView`）・ヒットボックス群 |
| `Interface/Views/EnemyAI/EnemyAIBase` | NavMeshAgent ベースの AI 基底。`Idle / Battle / Dead` ステート、ポーズ・スタン・速度倍率・吹き飛ばし |
| `Enemy/AI/Fire, Orbit, Rush, Satellite, Scout, Shield` | 6 種の AI。`Fire` 派生が弾を撃つ |
| `Enemy/AI/Boss/BossAIBase`（`IBossMemberView`） | ボスグループの台本から命令を受けて動くボス AI の基底。自分では攻撃を抽選せず、命令された行動を `BossActionPhaseMachine`（plain C#）で 予備動作 → 攻撃 → 硬直 と進める（秒数は `EnemyMasterData` の `WindupTime / ActiveTime / RecoveryTime`）。スタンで行動を打ち切る。待機（Hold）中は移動も止める |
| `Enemy/AI/Boss/BossShooter` | 弾を撃つボス AI。行動番号ごとに弾数・広がり（0: 単発、1: 扇状 など） |
| `BossLifeGaugeStoreView` | ボスごとの足元の体力ゲージを生成・破棄する。ゲージ本体はプレイヤーの `PlayerLifeGaugeView`（半円・CurvedWorld 対応のシェーダ）をそのまま複製し、バリアの弧は隠す。色（体力・低HP・空き部分）と大きさは `BossLifeGaugeConfig` で決め、ゲージごとに MaterialPropertyBlock で上書きする（マテリアルはプレイヤーと共有のまま） |
| `Enemy/AI/Boss/BossLineStrikeView` | ボスの帯状の攻撃の予兆・攻撃の表示。長さ方向に刻んだ帯のメッシュを生成し（`HideFlags.DontSave`、バウンズを下へ広げる）、`CurvedWorldUnlit` の頂点ごとモードで地面に沿わせる。ボスの拡大率・回転の影響を受けないよう単独のオブジェクトとして置く |
| `Enemy/AI/Boss/BossAxisBarrage` | 体力を共有する二人組ボス用。台本の配置（`CrossFormation`）でプレイヤーの上下左右（ワールド軸）の一定距離へ瞬間移動する。行動していない間はその位置で距離を保って追い、行動（弾幕）中は自分の軸の線上だけを動いてプレイヤーと並び、移動方向と直交する向き（プレイヤーの側）へ一定間隔で撃つ。弾幕の長さは `ActiveTime`。行動1（帯の攻撃、発狂フェイズ）はその場に留まり、プレイヤーの側へ伸びる帯を予兆として出し、予兆が明けた瞬間に `OverlapBox` で帯の中のプレイヤー（`HitBoxType.Player`）へ1回当てる。行動2（帯の連続攻撃）は同じ帯を予兆1回のあと同じ向き・位置のまま予兆なしで `RepeatCount` 回、`RepeatInterval` 間隔で当てる（×字の配置で使う）。範囲・秒数・回数・ダメージ・色は `BossLineStrikeConfig`。行動ごとの秒数は `BossAIBase.GetActionDurations` で上書きする |
| `Enemy/Bullet/BaseBulletView, StraightBullet, HomingBullet` | 敵弾（`_shooterLayer` は `Framework.Layer`）。`PointParticle` レイヤーを除外して判定 |
| `HitBoxView` / `HitBoxStoreView` | ヒットボックス。`OnHit` で `HitData` を作って流す。耐性方向で貫通可否を返す |
| `BulletStoreView` / `BulletTracerView` / `CounterTracerView` / `BlitzEffectView` | 弾の一括管理・曳光弾・カウンター演出・ブリッツ演出。`TracerFreezeState` でフリーズ中に停止 |

**UI・演出**

| クラス | 責務 |
|---|---|
| （共通）`RunStartView` | 直接再生時・リスタート時のセット選択（スロット3＋「使わずに開始」）。実装は `Common/Views`、プレハブは `UI/RunStartView.prefab` |
| `ShopView` + `UpgradeCardBoardView` + `UpgradeCardView` + `CardHighlight` | ウェーブ間ショップ。VR は 3D カードを掴んでトリガー確定、PC はマウスクリック。Canvas ボタンはフォールバック |
| └ `UpgradeCardBoardLayout` / `UpgradeCardFinder` / `UpgradeCardHandInteraction` / `UpgradeCardPointerInteraction` | ボードの内部分担（plain C#、DI 対象外）。配置の純粋計算／近接・レイ・UI越しの検索／VR両手の掴み・ひねり・確定の状態機械／非VRのホバー・クリック確定。Inspector 値は `UpgradeCardHoldSettings` / `UpgradeCardGrabSettings` に毎フレーム束ねて渡す |
| `GameOverView` | スロット保存／リスタート／メインメニューへ |
| `PointParticleStoreView` / `PointParticleView` | ポイント粒子（一括更新、粒子ごとの Update 無し） |
| `StreamerCameraView` | 配信用カメラ（HMD 映像に干渉しない）。`StreamerModeConfig` で既定 OFF |
| `TutorialMessageView` | バトル中のチュートリアルメッセージ（WorldSpace Canvas、プレハブは `UI/TutorialMessageView.prefab`）。表示直後は視点の正面に追従し、規定時間後に非利き手の脇へ移って常に頭の方を向く。追従先の姿勢は UseCase から毎フレーム受け取る。オフセット・時間・追従速度は Inspector |
| └ `TutorialMessagePlacement` / `TutorialMessagePlacementSettings` | 配置の状態機械と純粋計算（plain C#、DI 対象外）。左手向けオフセットを右手では x 反転、真下では頭の向きへフォールバック |

### 3.4 Presenters（`Battle/Presenters`）

| クラス | 対応 View | 備考 |
|---|---|---|
| `PlayerControlPresenter` | `IBattlePlayerView` | 移動・射撃・照準・レイ色・死亡アニメなど、UseCase からプレイヤーへの窓口を一手に持つ |
| `EnemyPresenter` | `IEnemyStoreView` | スポーン／消去／検索／ポーズ／スタン／速度倍率／吹き飛ばし |
| `BattleHitPresenter` | `IHitBoxStoreView` | `OnHit` の集約 |
| `ShopPresenter` / `GameOverPresenter` | 各 UI View | |
| （共通）`RunStartPresenter` | `IRunStartView`（`Common/Presenters`） | |
| `PlayerLifeGaugePresenter` / `PointParticlePresenter` / `StreamerCameraPresenter` / `TutorialMessagePresenter` | 各 View | |

### 3.5 UseCase（`Battle/UseCase`）

登録順が `Tick` / 購読順になるため、`BattleLifetimeScope` の並びを変えるときは注意（コメントで理由を残してある箇所あり）。

| クラス | 何をするか | 主な依存 |
|---|---|---|
| `PlayerMoveUseCase` | 左スティック → 移動・モデル向き・アニメ。フリーズ／回避中は止める | PlayerState, Freeze, DodgeParameter |
| `PlayerAimUseCase` | 手のレイ／マウスから照準位置を決めフォーカス対象を追う | PlayerAim, PlayerFocus, ShotConflict |
| `PlayerShotUseCase` | トリガー → `PlayerBulletParameterDataStore` で弾データを作り `Shot`。フォーム（Normal/Merge/Waltz）と両手の可否 | PlayerShotType, PlayerBulletParameter, CoreSkillUnlock |
| `PlayerDodgeUseCase` | 回避入力 → 直線移動。通過した敵を接触として記録 | PlayerDodgeParameter, DodgeCounterAttack |
| `DodgeCounterAttackUseCase` | 回避終了時の跳ね返し攻撃（扇形＋直線検索 → レイ演出 → フリーズ → ダメージ）。`DodgeCounterAttackConfig` | DodgeCounterAttack, Enemy, Freeze |
| `PlayerGazeUseCase` | `EnemyStoreView` が更新した「視線から判定球までの距離」をアップグレードごとの半径で絞り込み、注視系（スネークアイズ／メデューサ／ガン飛ばし）を反映 | SnakeEyes, Medusa, MeanMug |
| `PlayerHitUseCase` | 被弾 → `PlayerStateDataStore.TakeDamage`（回避中は無効） | PlayerState, DodgeParameter |
| `PlayerLifeGaugeUseCase` | HP・バリアをゲージへ、位置を追従 | PlayerState, PlayerBarrier |
| `EnemySpawnUseCase` / `EnemyRandomSpawnUseCase` | `EnemyDataStore` の生成要求を View へ／周期スポーン（`EnemyRandomSpawnCycleDataStore`）と出現位置 | Enemy, RandomSpawnCycle |
| `EnemyControlUseCase` | プレイヤーの照準方向を敵 AI へ渡す | PlayerAim, Enemy |
| `BattleHitUseCase` | `OnHit` → 倍率（貫通バフ／ガン飛ばし／クリティカル）→ 感電伝播 → `EnemyDataStore.Damage`。撃破時の回復（HealOnKill）等 | Enemy, BuffState, CriticalHit, ElectricShock |
| `PointDropUseCase` | 撃破 → 粒子ドロップ、回収 → ポイント加算。`BattleHitUseCase` より先に登録（撃破地点を読むため） | Point, PointDropCalculator |
| `BuffConditionUseCase` / `CareNodeUseCase` | バフ条件の入力（ヒット・HP割合・回避）／ケア・ノードの毎秒効果 | BuffState, CareNode |
| `FreezeUseCase` | フリーズの開始・解除で敵・弾・レイ演出を止める | Freeze, Enemy, BulletStore |
| `BossGroupUseCase` | ボスの個体の状態（`OnBossMemberStatusChanged`）を `BossGroupDataStore` へ渡し、台本が出した行動・待機の命令を `EnemyPresenter` 経由で個体へ届ける。フリーズ・ウェーブ間ポーズ中は台本を止める | BossGroup, Enemy, Freeze, WaveManager |
| `BossLifeGaugeUseCase` | Boss ランクの敵が出たら足元に体力ゲージを出し、`OnEnemyPoseUpdate` で位置に追従、命中のたびに `Hp / MaxHp` を反映（体力共有の仲間も同時に更新）、`OnEnemyRemoved` で消す | Enemy, BossLifeGauge |
| `BossWaveUseCase` | ボスウェーブ開始時に残った敵の消去・プレイヤーの移動・ボスの出現（3.2 参照） | BossWave, Enemy, PlayerState |
| `WaveManagerUseCase` / `ShopUseCase` / `RunStartUseCase` / `GameOverUseCase` / `RunResetUseCase` | 3.2 参照 | |
| `UpgradeSideEffectApplier` | アップグレード付与の副作用（バフ起動・バリア満タン）。Shop と RunStart の共通処理 | BuffState, PlayerBarrier |
| `StreamerCameraUseCase` | ウェーブ進行・ボス・マルチキルで配信カメラの演出をトリガー | StreamerCamera, Enemy |
| `TutorialMessageUseCase` | チュートリアルメッセージを出す手段。`ITutorialMessageUseCase`（`Show(TutorialType)` / `Hide()`）として登録し、呼び出し側が注入する。リスタートでセット選択へ戻ると自動で消す。文言は `TutorialLocalizationDataStore`、頭は `TryGetGazePose`、手は `NonDominantHand` 側の Pose を毎フレーム View へ渡す。表示のきっかけと `MarkViewed` は呼び出し側の責務 | TutorialLocalization, PlayerSetting, PlayerControl |
| `TutorialWaveUseCase` | ウェーブ開始（`IsWavePause` が false になった瞬間）に `TutorialWaveConfig` の割り当てを引き、`ShouldShow` なら `ITutorialMessageUseCase.Show` して `MarkViewed`。出すものが無いウェーブでは前のメッセージを消す | WaveManager, TutorialProgress, TutorialMessage |

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
| `EnemyDataStore` | 敵の実体データ（`EnemyData`）、HP、`OnEnemyDead`。スポーン時 HP/攻撃力は `EnemyWaveScalingCalculatorDataStore` で決める。`LinkSharedHealth` で複数の敵に体力を共有させる（合計値。`EnemyData.MaxHp` も合計にそろえる。誰に当てても減り、0で当てた敵だけ撃破扱い、残りは `RemoveEnemyData` で消す） |
| `EnemyRandomSpawnCycleDataStore` | ランク別スポーン周期・同時数の増加（`MinorSpawnCountGrowthRate`） |
| `EnemyWaveScalingCalculatorDataStore` | `WaveScalingDatabase` からウェーブ帯ごとの増加率（線形・切り上げ） |
| `WaveManagerDataStore` | 現在ウェーブ・経過時間・キル数・ポーズ・`OnWaveAdvanced` |
| `BossGroupDataStore` / `BossPatternRunner` | 複数個体のボスの出現（メンバーを `EnemyDataStore` へ登録。`SharedHealth` なら体力を共有させる）と行動台本の進行。`BossPatternRunner`（plain C#）が個体の状態から `Act`（全員が行動可能になったら同じフレームで一斉に行動、`HoldOthers` で対象の行動・硬直が終わるまで他を待機）・`WaitActionable`（硬直・スタン明けを待つ）・`Wait`（秒数）・`CrossFormation`（対象をプレイヤーの縦方向・横方向へ交互に振り分けて配置し直す。どちらが縦か・正負の側はランダム。横へのずれの候補と「少なくとも1体は0」を指定できる）・`RandomLoop`（直前のいくつかのステップを合計 Min〜Max 回ランダムに繰り返す。入れ子不可）・`DiagonalFormation`（2体を隣り合う斜めの角へ。向きが直交して帯が×字になる）を進め、命令（`BossDirectorCommand`）を出す。撃破されたメンバーは対象から外す。`BossGroupConfig.RageHealthRatio` 以下まで体力が減ったら台本を `RagePattern`（発狂フェイズ）へ差し替え、行動中の個体は `Cancel` で打ち切る |
| `BossWaveDataStore` | 現在ウェーブがボスウェーブか・出現済みか・全員倒したか |
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
| ScriptableObject（`Assets/App/MasterData/**` に実体） | `PlayerBaseParameterConfig`（**今回新設**）, `DodgeCounterAttackConfig`, `PlayerDeathConfig`, `PointDropConfig`, `PointParticleConfig`, `StreamerCameraTriggerConfig`, `StreamerCameraShotData`, `TutorialWaveConfig`（ウェーブ番号 → `TutorialType`、`MasterData/Tutorial`）, `UpgradeDescriptionStyle`, `BossWaveConfig`（ボスウェーブの番号・出すボスグループ・プレイヤー/ボスの位置、`MasterData/Boss`）, `BossLifeGaugeConfig`（ボスの体力ゲージの色・大きさ、`MasterData/Boss`。既定は紫）, `BossLineStrikeConfig`（ボスの帯の攻撃の幅・長さ・予兆/攻撃/硬直の秒数・連続攻撃の回数と間隔・ダメージ倍率・色、`MasterData/Boss`）, `BossGroupConfig`（ボスのメンバー・行動台本 `BossPatternStep[]`・体力共有、`MasterData/Boss`。`BossGroup_TwinShooter`＝交代と同時行動の確認用、`BossGroup_AxisPair`＝体力共有の二人組） |
| 定数 | `PlayerConstants.PlayerId`（**今回新設**）, `ThemeColors` |
| POCO / struct | `EnemyData`, `HitData`, `BossMemberStatus`, `BossDirectorCommand`, `BulletData`(Common), `DodgeEndData`, `PlayerDamagedData`, `ElectricShockChain`, `ShopHandInput`, `ShopPointerInput`, `StreamerCameraShotRequest`, `TutorialMessageAnchor`, `UpgradeLocalizedText` |
| enum | `EnemyAIState`, `BossActionPhase`, `BossPatternStepType`, `BossFormationSlot`（プレイヤーの上下左右、ワールド軸）, `HitBoxType`, `StreamerCameraShotType`, `TutorialMessagePhase` |

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

【ボスウェーブ・ボスの台本】
ShopUseCase「次のウェーブへ」→ IsWavePause=false → BossWaveUseCase(ボスウェーブなら敵消去・プレイヤー移動)
 → BossWaveDataStore.TrySpawnBoss → BossGroupDataStore.SpawnGroup → EnemyDataStore.AddEnemyData（以降は【敵スポーン】と同じ）
BossAIBase.Status → EnemyStoreView.OnBossMemberStatusChanged → BossGroupUseCase → BossGroupDataStore(BossPatternRunner)
BossGroupUseCase.Tick → BossGroupDataStore.Tick → BossDirectorCommand → EnemyPresenter.CommandBossAction / SetBossHold → BossAIBase
全員撃破 → BossWaveDataStore.IsBossCleared → WaveManagerUseCase が AdvanceWave

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
| `RunLoadoutDataStore` | メインメニューで選んだ次ランのセット（選択時点のアップグレード ID 一覧のコピー／使わない／未選択）。バトル開始時に装備したら `Clear()` で消費する（リスタートはバトル内で選び直す）。永続化なし。メニューに入るたびにも `Clear()` |
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
| `RunStartView` / `RunStartPresenter` | アップグレードセット選択 UI（スロット3＋「使わずに開始」＋任意の「戻る」）。メインメニュー（START 後）とバトル（直接再生時・リスタート時）で共用 |
| `VrUiFollowCanvasView` / `VrUiRayView` / `VrUiRayAlwaysOnView` | VR 向け UI 基盤（遅延追従キャンバス・ハンドレイ） |
| `ForwardRayView` / `HandForwardRayView` / `PlatformHandRotation` | 手・照準のレイ表示、プラットフォーム別の手の回転補正 |
| `GazeTargetView` / `GazeTargetStoreView` / `GazeDetector` / `GazeHitTest` | 視線が対象に当たっているかの汎用判定。対象は球で近似し、余白は角度で持つ。`GazeTargetView`（対象に付ける）が有効な間だけ `GazeTargetStoreView`（`CommonLifetimeScope.prefab` 上に常駐、`IGazeTargetStoreView` で注入）に登録され、Store が LateUpdate で1フレームに決まった数ずつ順番に判定する。結果は `IsGazed`（R3）。判定本体の `GazeDetector`／`GazeHitTest` は plain C# で、入り／外れの余白差（ヒステリシス）と遅延でちらつきを抑える |
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
| 中 | 敵の停止判定が3か所に分散 | `FreezeUseCase`・`WaveManagerUseCase`・`BossGroupUseCase` がそれぞれ「フリーズ OR ウェーブ間ポーズ」を自前で合成している | 停止理由を1か所で合成するプロパティ（例: 敵の停止状態を持つ DataStore）にまとめる。オーバークロックの時間停止を足す前に行うと漏れが出ない |
| 中 | ボスの出現演出・ボスウェーブの仕様 | ボスウェーブは開始と同時にその場へ出すだけ。番号は `BossWaveConfig` の固定値（5）。出すボスは二人組（`BossGroup_AxisPair` / `B-002`）。ボスのマスターデータ・台本・`BossAxisBarrage` の距離や弾の間隔は仮の値 | 演出（予兆・登場カメラ）、ボスの数値と台本の作り込み、勝利条件（GameSpec 1.2）との関係を決める |
| 低 | ドキュメントコメントの無い UseCase | `PlayerMoveUseCase` 等いくつかは `<summary>` が無い | 触ったタイミングで足す |
