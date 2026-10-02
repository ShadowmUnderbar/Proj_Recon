---
name: playtest-run
description: "RECONのバトルシーンを自動プレイし、ウェーブ進行・ショップ操作を通してDebug.LogError/例外を検出する（テストラン）。あわせて、カメラ演出・エフェクトが仕様どおりに動いたかをTransformや設定値の実測で検証する（プローブ）。Tools/Playtest/run-playtest.ps1 / probe-effect.ps1（またはUnity Editorの Tools > Playtest Runner ウィンドウ）で実行する。実行そのものだけでなく、フロー拡張（シナリオ追加、対象ウェーブ数変更、新規チェック項目追加、新規DataStoreの観測追加）や、新しい演出を追加してその挙動を数値で検証したいときもこのスキルを更新して使うこと。"
---

# 自動プレイテストラン（playtest-run）

用途は2つある。

1. **テストラン**: バトルフロー（ウェーブ進行→ショップ→次ウェーブ…）を自動プレイし、その間に発生した `Debug.LogError` / 例外を検出する
2. **プローブ**: カメラ演出・エフェクトを実際に発火させ、Transformやカメラ設定の**実測値**が仕様どおりかを検証する（後述の「演出の数値検証」）

- **実行本体**: `Tools/Playtest/run-playtest.ps1`（テストラン）/ `Tools/Playtest/probe-effect.ps1`（プローブ）。どちらもPowerShellスクリプトで、uLoopMCPのCLI(`uloop`)を直接叩く。Claudeの対話操作なしで単体実行できる
- **手軽な起動**: Unity Editorの `Tools > Playtest Runner` メニューからEditorWindowを開き、シナリオ＋ウェーブ数を選んで「テストラン実行」、またはプローブを選んで「プローブ実行」を押すだけでよい（`Assets/App/Script/Editor/TestRun/PlaytestRunnerWindow.cs`）
- **テストシチュエーションの追加**: `Tools/Playtest/Scenarios/*.ps1` に新しいファイルを1つ追加するだけでよい（後述）。Editor拡張のドロップダウンにも自動で反映される
- **演出の検証項目の追加**: `Tools/Playtest/Probes/*.ps1` に新しいファイルを1つ追加するだけでよい（後述）。こちらもドロップダウンに自動反映される
- 新規のUnity C#コードは自動化基盤としては追加していない（Editor拡張はツール専用で、DataStore/UseCaseなどゲーム本体のアーキテクチャには一切関与しない）。ウェーブ状態の読み取りは既存のDataStoreを`execute-dynamic-code`から読むだけ

CIの恒久テストではなく、**開発中の調査ループ**（自動プレイ→エラー確認→修正→再実行）として使う。

**重要: `Tools/Playtest/`配下の`.ps1`ファイルは必ずBOM付きUTF-8で保存すること。** BOM無しUTF-8で保存すると、Windows PowerShell 5.1がファイル内の日本語文字列をシステムのコードページ（Shift-JIS系）で誤読し、それ以降のスクリプト解釈が壊れて自作関数（`PlaytestScenarioStep`等）が`CommandNotFoundException`になる（検証済みの実際の不具合、原因特定に長時間かかった既知の落とし穴）。編集後は以下でBOMを確認・復元する。
```powershell
$f = "Tools/Playtest/対象ファイル.ps1"
[System.IO.File]::WriteAllText($f, (Get-Content -Raw $f -Encoding UTF8), (New-Object System.Text.UTF8Encoding($true)))
```
ファイルツール（Write/Edit）で保存すると多くの場合BOMが失われるため、保存後は`xxd <file> | head -1`で先頭が`efbb bf`になっているか確認する。

## 前提・制約（2026-07時点、実装が進んだら要更新）

- シーンは`Assets/Scenes/MainMenu.unity`（タイトル）と`Assets/Scenes/Battle.unity`（バトル）の2本。
  **プレイテストは`Assets/Scenes/Battle.unity`を開いた状態で実行する**（ランナーは現在開いているシーンでPlayに入るため）
- プレイヤーのHP減少は実装済み（PR #34）。敵の攻撃がプレイヤーの被弾受け（`PlayerDamageReceiverView`）に当たると`PlayerStateDataStore.Health`が減る
- **ゲームオーバー判定は実装済み**（メタ進行Phase1）。HPが0になると`GameStateDataStore.IsGameOver`がtrueになり`GameOverUseCase`がウェーブをポーズ＋ゲームオーバー画面（`GameOverView`）を表示する。ランナーは`Get-WaveState`の`isGameOver`を監視し、**ゲームオーバーを検出したらスロット0保存ボタン（`Invoke-GameOverSlotSave`）を押してから正常終端**する（エラー扱いにはしない）。**スロット保存を押しても画面は閉じない**（保存とリスタートを分けたため）。画面を閉じるのは`RestartButton`で、押すとラン状態が初期化されビルド選択（`RunStartView`）へ戻る。この経路は`GameOverRestart`プローブで検証している。つまり終端は「目標ウェーブ到達」か「ゲームオーバー」のどちらか。ランダムドリルは被弾を避けないため、目標ウェーブ到達前にゲームオーバーで終わることがある（正常）。HP0まで到達させたくない検証（全ウェーブクリアの確認等）をしたい場合は将来的に無敵/回復手段の注入が要る
- **HP0の直後にはゲームオーバー画面は出ない**。死亡演出（ヒットストップ→死亡アニメ→余韻、`PlayerDeathConfig`で調整）を挟むため、既定で約1.4秒遅れて表示される。ボタンを押す処理は固定待ちにせず`Wait-GameOverPanel`で表示を待つこと（`Invoke-GameOverSlotSave`は内部で待つ）。演出そのものは`PlayerDeath`プローブで検証している
- **ラン開始時にセット選択ゲートがある**（メタ進行Phase2）。Play開始直後は`RunStartDataStore.IsSelecting=true`＋`IsWavePause=true`でゲームが停止し、セット選択UI（`RunStartView`）が出る。ランナーは`Get-WaveState`の`isSelectingRunStart`を見て、`Resolve-RunStartIfSelecting`で「使わずに開始」ボタンを押しランを始める（プレイテストはセットを読み込まずに開始する）。この解除を最優先で処理するため、`isSelectingRunStart`の間はショップ/ゲームオーバー処理より先に返す
- ウェーブ数を表示するUIは存在しない → 状態はUIではなく`execute-dynamic-code`経由でDataStoreから読む
- ショップの開閉状態を公開するプロパティはない → `IsWavePause`とショップUIプレハブ（`ShopView`）の出現で判断する
- 検知基準は`Debug.LogError`/例外に加え、`PlaytestCommon.ps1`の`$Global:PlaytestKnownIssuePatterns`に登録した既知の問題メッセージ（Log/Warningレベルでも検知対象になる）。登録されていないWarning/Logは対象外

## 実行方法

### 通常の実行（推奨）
Unity Editorで `Tools > Playtest Runner` を開き、シナリオ（`FixedFlow`/`RandomDrill`）と対象ウェーブ数を選んで「テストラン実行」を押す。PowerShellウィンドウが開いて進行状況が流れ、終了するとウィンドウ内に直近の結果（到達ウェーブ・成否・エラー内容）が表示される。

ターミナルから直接実行することもできる（Unity Editorは起動している必要がある）：
```powershell
& "Tools/Playtest/run-playtest.ps1" -Scenario RandomDrill -Waves 3
```
終了コード0=エラーなし、1=エラー検出、2=シナリオ指定ミス。結果は`Tools/Playtest/Reports/`にJSONで保存される（最大10件、古いものから自動削除、`.gitignore`済みでコミット対象外）。

### スクリプト構成
- `Tools/Playtest/PlaytestCommon.ps1`: 共通ヘルパー（`Invoke-Uloop`＝uloop CLIラッパー、`Get-WaveState`＝ウェーブ状態取得、`Resolve-ShopIfOpen`＝ショップ自動選択、`Get-ShopCards`／`Invoke-ShopCardClick`＝アップグレードカードの取得とクリック、`Get-NewErrors`＝エラー取得、`Write-PlaytestReport`＝レポート出力）
- `Tools/Playtest/Scenarios/*.ps1`: シナリオ本体。各ファイルは`PlaytestScenarioStep`関数を1つ定義するだけでよい（詳細は後述）
- `Tools/Playtest/run-playtest.ps1`: テストランのランナー本体。compile→clear-console→Play→（状態観測→シナリオ実行 or ショップ処理→エラー確認）の繰り返し→Stop→レポート出力
- `Tools/Playtest/probe-effect.ps1`: プローブのランナー本体。compile→clear-console→ProbePrepare→Play→ラン開始ゲート解除→ProbeRun→エラー確認→Stop→ProbeCleanup→レポート出力
- `Tools/Playtest/Probes/*.ps1`: プローブ本体。`ProbeRun`（必須）/`ProbePrepare`/`ProbeCleanup`（任意）を定義する（詳細は後述）
- `Assets/App/Script/Editor/TestRun/PlaytestRunnerWindow.cs`: 上記スクリプトをUnity Editorから起動するEditorWindow。`Tools/Playtest/Scenarios/`をスキャンしてシナリオ一覧を動的生成する

### 新しいテストシチュエーションを追加する
`Tools/Playtest/Scenarios/`に新規`.ps1`ファイルを1つ追加し、`PlaytestScenarioStep`関数を定義するだけでよい。ランナー側の変更は不要、Editor拡張のドロップダウンにも自動で反映される。

```powershell
function PlaytestScenarioStep {
    # ウェーブ進行中（IsWavePause=falseの間）に毎サイクル呼ばれる。
    # ここでInvoke-Uloopを使い、移動・発射・フォーム切替などを組み立てる。
}
```
既存の`FixedFlow.ps1`（決め打ち移動+発射）・`RandomDrill.ps1`（移動/発射/フォーム/フォーカス/回避をサイクルごとに変える）を参考にする。ショップでのアップグレード選択・次ウェーブ操作は`PlaytestCommon.ps1`の`Resolve-ShopIfOpen`が共通処理として自動で行う（買える3Dカードを1枚クリック→`NextWaveButton`押下。所持ポイントが足りず買えるカードが無ければ購入せずに次ウェーブへ進む（異常ではない）→ポーズ解除を確認できるまで最大3回リトライ、解除されなければthrow）。

## 演出の数値検証（プローブ）

カメラ演出やエフェクトは、スクリーンショットの目視では「何か動いた」ことしか分からない。プローブは**演出を実際に発火させ、TransformやCameraの値を読んで期待値と突き合わせる**。カメラ・エフェクト系の演出を追加したら、まずこれで検証する。

```powershell
& "Tools/Playtest/probe-effect.ps1" -Probe StreamerCameraShot
```
終了コード0=全項目OK、1=検証NGまたはエラー検出、2=プローブ指定ミス。結果は`Tools/Playtest/Reports/probe_*.json`に保存される（最大10件、`.gitignore`済み）。

### 表示判定とデバッグ設定の落とし穴（実証済み）

- **パス形式の`GameObject.Find("A/B/C")`は非アクティブなオブジェクトも返す**。UIの表示/非表示を確かめるときは`activeInHierarchy`まで見ること（見ないと常にtrueになり、検証が素通りする）
- **デバッグウィンドウの「開始時アップグレード」（EditorPrefs）が入ったままだと実測値がぶれる**。実際にバリアが致死ダメージを丸ごと吸収してHPを0にできなかった。期待値を固定したいプローブは`ProbePrepare`でEditorPrefsを退避・クリアし、`ProbeCleanup`で戻す（`Probes/GameOverRestart.ps1`が実例）
- **短い演出は別々の`Invoke-UnityJson`で観測できない**。uloopの往復は数百msかかるため、0.2秒のヒットストップは次の呼び出しでは既に明けている。同期的に起きる状態は「起点の処理と同じスニペット内」で読むこと（`Probes/PlayerDeath.ps1`が実例）
- **`UseCase`を`IRunResettable`にするときは`RunResetUseCase`を注入していないか確認する**。両方満たすとVContainerが循環参照になり、`InvalidOperationException: ValueFactory attempted to access the Value property`でコンテナ構築ごと失敗する（`GameOverUseCase`で実際に踏んだ）

### 検証の組み立て方（実証済みのパターン）

演出は数フレームで終わってしまうので、**中間状態を観測できる長尺のパラメータで発火させる**のが要点。ScriptableObjectの演出データを`ScriptableObject.CreateInstance`＋`SerializedObject`でメモリ上に組み立て、`_holdDuration`だけ極端に長くして発火すれば、好きなタイミングで測れる（アセットは汚さない）。

1. **発火前のベースラインを測る**（例: 配信カメラがプレイヤーカメラと完全一致しているか）
2. **長尺パラメータで発火**し、ブレンドイン完了を`Start-Sleep`で待つ
3. **中間状態を測る**（位置・回転・視野角・注視方向の内積など）
4. **期待値はPowerShell側で独立に計算**して突き合わせる。ゲーム側の計算クラスをそのまま呼ぶと同じ実装を比較するだけになり検証にならない
5. **時間経過で変化する演出は2回測って差分を見る**（回り込み・スクロール等）
6. **キャンセル/完走させて元の状態へ戻り切ったか**を測る（位置差0・回転差0・再生中フラグが下りる）

### 新しいプローブを追加する

`Tools/Playtest/Probes/`に新規`.ps1`を1つ追加する。ランナー側とEditor拡張の変更は不要。

```powershell
function ProbePrepare {   # 任意。Play前に走る。設定フラグの有効化など
    # Play中に変えても間に合わない設定はここで仕込み、元の値を $Global:... に退避する
}

function ProbeRun {       # 必須。Play中に走る。ここで発火・測定・検証する
}

function ProbeCleanup {   # 任意。Stop後に必ず走る（失敗時も）。ProbePrepareで変えた設定を戻す
}
```

利用できる共通ヘルパー（`PlaytestCommon.ps1`）:

| ヘルパー | 用途 |
|---|---|
| `Invoke-UnityCode -Snippet <C#>` | C#スニペットをUnityで実行し`Result`文字列を返す（BOM無しUTF-8で渡す処理・リトライ込み） |
| `Invoke-UnityJson -Snippet <C#>` | 同じくJSON文字列を返す前提でオブジェクト化する |
| `Assert-ProbeValue -Name -Actual -Expected [-Tolerance]` | 数値を許容誤差付きで比較して記録する（既定 0.001） |
| `Assert-ProbeTrue -Name -Condition [-Detail]` | 真偽を検証して記録する |

C#スニペットはPowerShellの**単一引用符ヒアストリング**（`@'` … `'@`）に書くこと。二重引用符だとC#の`$"..."`補間がPowerShellに食われる。値を差し込みたい場合は`__PLACEHOLDER__`を置いて`.Replace()`する（`Probes/StreamerCameraShot.ps1`の`ProbeCleanup`が実例）。

**`$foo.Count` はハッシュテーブルを返す関数の戻り値に直接使わないこと。** PowerShellは1要素の配列を戻り値でアンロールするため、検証NGが1件のとき`(Get-ProbeFailures).Count`がハッシュテーブルのキー数（6）を返す。呼び出し側で必ず`@(...)`で配列化する（実際に件数が6件と誤表示された既知の落とし穴）。

### 既存のプローブ

- `ShopPurchase` — ショップの通貨制を、3Dカードを実際にマウスでクリックして19項目で検証する。ポイント0では買えない／買えないカードはクリックしても何も起きない／表示中にポイントが入るとその場で買えるようになる／購入でコストが引かれる／購入したカードだけが消える／同じショップで続けて買える／買えるだけ買うと残りは買えないものだけになる、を見る。コストは固定値を仮定せずカード上の`CostText`から読むこと（デバッグ用の開始アップグレードが載っているとLv2以上＝別コストの候補が並ぶ）。`AppliedUpgrades` の件数も必ず差分で見ること。**カードのクリック判定は非VRのポインタ操作なので、`ProbePrepare`でVRモード（EditorPrefsの`VRMode`）を一時的に切り、`ProbeCleanup`で元へ戻している**（VRモードのままだとカードは掴み操作でしか選べず、クリックが空振りする）
- `PointParticleDrop` — 敵撃破時のポイント粒子ドロップ。ドロップ量の分割内訳・撃破時の生成数・接触回収・弾の通過回収（弾が消えないこと）・即着弾での回収・ウェーブ切り替わり時の一括消去・プレイヤーの高さへの追従・取得判定の最小サイズ・弾を当てた粒子の吸い込み（距離に依らず0.5秒で完了）を29項目で検証する。弾の検証は粒子と同じ高さの水平弾道で撃つこと（斜め撃ちは銃口が地面に埋まって弾が即消える）。粒子は専用レイヤー `PointParticle`（8）にあり、レイキャスト側で除外しているため通常のクエリでは掛からない。生成直後の粒子は同じフレームの物理クエリに反映されないので、粒子を出してから撃つまでに必ず1フレーム待つこと。ウェーブ進行の検証は `AddElapsedTime` で実際のTick経路を通すこと（DataStoreの `AdvanceWave()` 直叩きでは一括消去を含む `AdvanceWaveInternal` を通らない）。弾道の検証では `IEnemyDataStore.RemoveAllEnemyData` だけでなく `IEnemyPresenter.RemoveAllEnemies` も呼ぶこと（データだけ消しても敵のビューが残って弾道を塞ぐ）。粒子はプレイヤーの高さへ移動し続けるため、撃つ前に高さが落ち着くまで待つこと。弾を当てた粒子は即座には回収されず吸い込み（既定0.5秒）を挟むので、回収の確認は弾の到達時間＋0.5秒ぶん待つこと。吸い込みの「最中」を狙って観測するとuloop呼び出しの往復時間で窓を外してフレークするため、状態は同期的に読める場所で確認し、完了は時間の下限だけ待って確かめること
- `TutorialMessage` — バトル中のチュートリアルメッセージ（`TutorialMessageUseCase` / `TutorialMessageView`）。配置計算 `TutorialMessagePlacement` の純粋ロジック（フェーズ切替・右手での x 反転・非VRでは視点正面に留まる・真下での向きフォールバック）と、実表示（Show→視点正面に一致→規定時間後に非利き手＋オフセットへ収束し頭を向く→再Showで視点正面へ戻る→Hide）に加え、追従の遅延（頭の移動は同フレームで付いてくる／首を回した直後は位置・向きが遅れ、時間が経てば追いつく）と、縮小表示（`TutorialMessageFold` の順序＝縮小は本文が先・展開は背景が広がりきってから本文、非利き手追従中に見ていなければ本文の折り返し幅が割合（既定0.5）まで狭まり、高さが1行目＋余白・横幅が本文幅＋余白へ縮み表示1行＋「…」・1行目の文字数もおおむね割合ぶんに減る、見れば元の大きさ・全文に戻る、文字サイズが変わらない、再Showで展開から始まる）を61項目で検証する。エディタでは手元のダイアログが頭のすぐ近くに来て常に「見ている」判定になるため、見ていない／見た状態は `GazeTargetView` の半径・余白角度を一時的に書き換えて作り、最後に元の値へ戻している。HMDが無いエディタではカメラも手も動かないため実表示の「追従」は「目標姿勢への収束」で見る。ウェーブ1開始時に `TutorialWaveConfig` の割り当てで自動表示されるため、実表示の検証はまず `Hide` してから始める（このぶん Wave1 の閲覧回数が1回分記録される）。**非利き手追従はVRモードでしか動かないので `ProbePrepare` で `VRMode` を一時的に ON にし、`ProbeCleanup` で戻す**。また **開いているシーンを切り替えず `EditorSceneManager.playModeStartScene` に Battle を設定して再生**し、終了後に戻している（MainMenu に未保存の変更があっても失わない。他のプローブでも使える手）
- `TutorialWave` — ウェーブ開始時のチュートリアル表示（`TutorialWaveUseCase` / `TutorialWaveConfig`）。ウェーブ1の割り当て・未閲覧なら表示され閲覧回数が1になる・規定回数閲覧済みなら出ない・再表示設定ONなら出る・割り当てのないウェーブでは前のメッセージが消える、既に始まっているウェーブ1に後から購読しても即座に出る（メインメニュー経由の即開始と同じ状況）、を9項目で検証する。ウェーブ開始は `IsWavePause` を true→false と切り替えて起こす（同期的に購読が走るので同じスニペット内で読める）。**閲覧回数と再表示設定はセーブデータに永続化されるため、ProbeRun の最初に退避し `finally` で復元して保存する**。ただしランナーのゲート解除で既に1回分記録された後の退避になるため、退避値には最初の表示ぶんが含まれる
- `BossWave` — ボスウェーブ（`BossWaveUseCase` / `BossWaveDataStore`）と複数個体のボスの台本制御（`BossGroupDataStore` / `BossPatternRunner` / `BossAIBase`）を31項目で検証する。`BossWaveConfig` のボスウェーブまで `AddElapsedTime(9999)` で通常の進行経路を通して進め、開始時の敵消去・プレイヤー移動・ボス出現、ボスウェーブ中に湧かない／制限時間で進まない、台本（交代・同時行動・休み）の順序と秒数、スタン中は相手が待機し続ける、フリーズ中は行動段階が進まない、1体撃破では進まず全員撃破で次ウェーブ、次ウェーブで通常の敵が湧く、を見る。**行動段階の切り替わりはuloopの往復より短いので、`Observable.EveryUpdate` の購読で毎フレーム記録し（購読は `AppDomain` のデータに置く）、記録をまとめて読んで判定する**。記録中は毎フレームHPを全快させて倒れないようにし、`finally` で購読を破棄する。台本の判定は `BossGroup_TwinShooter`（A→B→同時→休み1秒）と `B-001` の段階秒数（0.8/0.2/1.5）を前提にしているため、どちらかを変えたらプローブ冒頭の定数と判定も直すこと。ボスウェーブに出すボスは別の構成に変わることがあるので、**実行中だけ `BossWaveConfig` のボスグループを `BossGroup_TwinShooter` に差し替え、`ProbeCleanup` で戻す**
- `BossTickTock` — 体力を共有する二人組ボス「TickTock」（`BossGroup_TickTock` / `BossTickTock`）と足元の体力ゲージ（`BossLifeGaugeUseCase` / `BossLifeGaugeStoreView`）を47項目で検証する。ゲージは数・色（ボスは `BossLifeGaugeConfig` の色をプロパティブロックで上書き、プレイヤーのゲージは上書きしない）・足元への追従（XZのずれ）・割合（共有体力÷最大体力、2体とも同じ）・撃破での消去を見る（割合は表示中の補間値ではなく目標値 `_targetHealth` で判定する）。配置（必ず縦と横の組・プレイヤーから一定距離へ瞬間移動）、弾幕役（規定秒・弾数＝秒数÷間隔・弾の向きが移動方向と直交してプレイヤー側・自分の軸の線上だけを動いてプレイヤーと並ぶ）、追跡役（撃たない・距離を保って追う）、交代、体力の共有（どちらに当てても同じだけ減る）、同時撃破（撃破扱いは当てた1体だけ、もう1体は `OnEnemyRemoved` で消える）、撃破でウェーブが進む、を見る。記録中はプレイヤーを `WarpTo` で一定速度に動かして追跡を確かめる。距離・間隔・秒数はプレハブとマスターデータから読むので、値を変えても判定はそのまま使える
- `BossRagePhase` — 二人組ボス「TickTock」の発狂フェイズ（`BossGroupConfig.RagePattern` / `BossTickTock` の帯の攻撃 / `BossLineStrikeView`）を約100項目で検証する（帯の攻撃の回数がランダムなので項目数は回ごとに変わる）。共有体力を `RageHealthRatio` の少し下まで削り、切り替え（行動中の個体の打ち切り）、帯の攻撃ごとの配置（縦横の組・横のずれは -5/0/+5・少なくとも1体は0・位置）、2体同時の予兆と攻撃、予兆の秒数、予兆中の帯の表示と攻撃後の消去、当たり（立ち止まれば当たる・2回目は予兆の途中で `WarpTo` で帯の外へ出して当たらない）、×字の前の通常の帯が3〜6回、×字（隣り合う斜めの角・位置・予兆は1回目の前だけ・連続攻撃の長さ・その間ボスは動かず帯も出たまま・1回目は当たり、直後に `WarpTo` で帯の外へ出すと向き直さない2・3回目は当たらない）、×字のあと弾幕1回を挟んで帯に戻る、撃破でウェーブが進む・帯が残らない、を見る。被弾は `IPlayerStateDataStore.OnDamaged` で記録する（2本の帯が両方重なると2回当たる）。記録が予兆の途中で終わった回は判定しない
- `DebugArena` — デバッグ対戦の入口（`App/デバッグ: 敵と対戦` / `DebugArenaLauncher` / `DebugArenaUseCase`）を22項目で検証する。`Request-DebugArena` で `BossGroup_TickTock` をウェーブ5（ボスウェーブの番号）で予約して再生し、予約が1回で消費される・セット選択を出さずに始まる・指定したウェーブから始まりボスウェーブとしては扱わない（ボスが重ねて出ない）・ボスの体力がそのウェーブの倍率・ボスグループが出てプレイヤーがボスウェーブの位置・無敵（致死量でも HP が減らず `OnDamaged` は流れる）・制限時間を過ぎてもウェーブが進まない・雑魚が湧かない・全員倒すと秒数のあとで新しい個体が出る・撃破してもウェーブが進まない・リスタート（`RunResetUseCase`）後も同じウェーブ・同じ相手で始まる、を見る
- **ボスの挙動を確かめる新しいプローブは、`Move-ToBossWaveShop` でボスウェーブまで進める代わりに `ProbePrepare` で `Request-DebugArena -BossGroupPath <BossGroupConfig のパス>` を呼んで始める**（`Enter-BossProbeScene` のあとに呼ぶ）。ウェーブ1〜4とショップを通らないので数秒で対象のボスと戦え、`BossWaveConfig` のボスグループを差し替える必要も無い。配置はボスウェーブと同じ。既定では倒すと3秒後に出し直し・プレイヤー無敵（`-AutoRespawn $false` / `-Invincible $false` で切る）。ウェーブ進行・ショップ・ボスウェーブの終了条件そのものを見るプローブ（`BossWave` / `BossSpawnFailure`）は従来どおり通常の進行で始める。雑魚は `-EnemyCode <EnemyMasterDataId> -EnemyCount <数>` で同じように出せる。`-Wave <番号>` で敵の強さをそのウェーブ相当にできる（既定1。ウェーブは進まない）
- `BossTimeStop` — 二人組ボス「TickTock」の時止めの記憶攻撃（台本の `TimeStopMemory` / `BossTickTock` の行動3・4 / `TimeStopDataStore`）を34項目で検証する。デバッグ対戦（`Request-DebugArena`）で始め、通常の台本の後半の時止めまで待って、時止めの前は W で動けること、全員の行動が明けてから止めて2体とも待機させること、予兆が1体ずつ3回・上下左右・設定の長さで出て当たらないこと、時止め中は W を押しても動かない・弾が止まる・被弾しないこと、解いてから一息おいて同じ個体・配置・順で短い予兆のあと当たること、終われば待機が解けて台本の先頭へ戻ること、2回目の時止めの途中で全員倒すと時止めが解けて動けることを見る。移動の入力は `simulate-keyboard` の W（押している間の位置の変化を毎フレームの記録で見る）
- `BossSpawnFailure` — ボスのプレハブの読み込みに失敗してもボスウェーブから抜けられることを検証する。`B-002` の PrefabPath を実行中だけ存在しないパスへ差し替え、敵データが撃破扱いにならずに取り除かれる・ボスグループ・ビュー・体力ゲージが残らない・ウェーブが進む、を見る。**Addressables は無効なキーだと同期的に失敗する**ため、出現の失敗はボスグループの登録より先に起きる（`BossGroupDataStore.SpawnGroup` が登録時に取り除かれた個体を除外している）。読み込み失敗のエラーログは想定どおりなので、プローブ内で想定外のエラーが無いことを確かめてからコンソールを消す（`PlaytestKnownIssuePatterns` は「追加でエラーとして数える」パターンなので、除外には使えない）
- 台本ランナーのロジック（同時行動・相互排他・配置・ずれ・繰り返し・差し替え・行動段階）は、プローブではなく EditMode テスト `Assets/App/Tests/EditMode/BossPatternRunnerTests.cs` で確かめる（`uloop run-tests --test-mode EditMode --filter-type regex --filter-value BossPatternRunnerTests`）。台本のステップを足したらここにもテストを足すこと
- ボス系プローブの共通処理は `Probes/Common/BossProbeCommon.ps1`（再生シーン・ボスグループの差し替え、C#の前置き、ボスウェーブ直前まで進める処理）。**アップグレードでボスの行動を変えないよう、ショップは `Skip-BossProbeShop` で何も買わずに進め、開始時アップグレード（EditorPrefs `StartUpgradeIds`）も実行中だけ空にする**（メデューサ＝注視した Boss ランクのスタン、スネークアイズ＝減速。持っていると注視の向きしだいで弾幕が打ち切られ、たまにだけ判定が落ちる。実際に開始時アップグレードのメデューサで弾幕が5.8秒・7.2秒で切れたことがある）。ボスの段階表示の末尾の「*」はスタン中
- `StreamerCameraShot` — ストリーマーモードの配信用カメラ。追従一致・自動フレーミング距離・注視方向・Orbitの回り込み・プレイヤー視点への非干渉・復帰を20項目で検証する。`ProbePrepare`で`StreamerModeConfig`を一時的に有効化（PCモードでも動くよう`_vrOnly`を外す）し、`ProbeCleanup`で元の値へ必ず戻す

### 状態観測の仕組み（`Get-WaveState`の内部）
`execute-dynamic-code`で以下のC#スニペットを実行し、`currentWave`/`isWavePause`/`elapsed`/`kill`/`isGameOver`/`playerHealth`をJSON文字列で取得している。

```csharp
using VContainer;
using VContainer.Unity;
var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var gameState = scope.Container.Resolve<IGameStateDataStore>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();
return $"{{\"currentWave\":{wave.CurrentWave.CurrentValue},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"kill\":{wave.KillCount.CurrentValue},\"isGameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"playerHealth\":{player.Health.Value},\"isSelectingRunStart\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()}}}";
```
- `using VContainer;` が無いと`Container.Resolve<T>()`（拡張メソッド）がコンパイルエラーになる（`CS0308`）。必ず両方の`using`を入れること
- スニペットを一時ファイル経由で渡す際は**BOM無しUTF-8**で書き込むこと（`[System.IO.File]::WriteAllText(path, text, (New-Object System.Text.UTF8Encoding($false)))`）。PowerShell 5.1の`Set-Content -Encoding utf8`はBOM付きになり、先頭の`using`が`CS1001`等で壊れる（検証済みの既知の落とし穴）
- `BattleLifetimeScope`はシーン上に固定配置されたGameObjectなので`LifetimeScope.Find<T>()`で解決できる（Awakeでの動的生成ではない）
- `IWaveManagerDataStore`（`Assets/App/Script/Battle/Interface/DataStore/IWaveManagerDataStore.cs`）: `CurrentWave`, `IsWavePause`, `ElapsedTime`, `KillCount`, `OnWaveAdvanced`
- `IGameStateDataStore`（`Assets/App/Script/Battle/Interface/DataStore/IGameStateDataStore.cs`）: `IsGameOver`（HP0でtrue）。`IPlayerStateDataStore`: `Health`（現在HP）。`IRunStartDataStore`（`.../IRunStartDataStore.cs`）: `IsSelecting`（ラン開始のセット選択中でtrue）。追加interfaceも`using`なしで自動解決される
- Unityがcompile直後やPlay Mode遷移直後は一時的に応答できない（ドメインリロード中）ことがあるため、`Get-WaveState`は失敗時に2秒間隔で最大5回リトライする

### 手動調査（uLoopMCPツールを直接使う場合）
スクリプト化する前の探索的な調査や、スクリプトが拾わない異常の確認をしたい場合は、Claudeが`uloop-control-play-mode`/`uloop-execute-dynamic-code`/`uloop-screenshot`/`uloop-simulate-*`/`uloop-get-logs`を対話的に呼び出しながら進めることもできる。手順はスクリプトの内部ロジック（上記）と同じなので、そちらを参照すること。

### 入力操作（ウェーブ中）
`Assets/App/Script/Common/Inputs/GameMaininput.inputactions`（"Main"アクションマップ）より：

| 操作 | キー/ボタン | 使用ツール |
|---|---|---|
| 移動 | `WASD` | `simulate-keyboard` |
| 回避 | `Space`（`Dodge`） | `simulate-keyboard` |
| 発射/フォーカス | マウス左/右ボタン | `simulate-mouse-input` |
| 射撃フォーム切替（Debugマップ） | `Digit1`/`Digit2`/`Digit3`（Normal/Waltz/Merge） | `simulate-keyboard` |

キル数を進めたいだけなら`simulate-mouse-input`の`LongPress`（Left, 2〜3秒）を画面中央付近に対して撃つだけで十分（敵が寄ってくるため照準操作は必須ではない）。左右ボタンの厳密な用途（発射/フォーカスどちらか）は毎回決め打ちせず、`get-logs`とスクリーンショットで実際の効きを確認すること。

**`simulate-keyboard`の`Key`は数字キーでも`"1"`のような素の数字文字列ではなく、Input SystemのKey enum名（`Digit1`/`Digit2`/`Digit3`）を渡すこと。** `"2"`を渡すとエラーにならず`Enter`キーが押される（無関係な入力が誤って発生するため要注意、検証済みの既知の落とし穴）。

### 3.1 ランダム操作ドリル（ランダム移動+発射+フォーム/フォーカス切替）

「適当な方向への移動・発射を繰り返しつつフォーム/フォーカスを定期的に切り替える」ような、決め打ちでない負荷テスト的な入力ドリルも実行可能。検証済みの組み立て方：

1. 移動キー（`W`/`A`/`S`/`D`、または未検証だが組合せ可）を`simulate-keyboard`(`Press`, `Duration=1〜2秒`)で1回ずつランダムに変えて呼ぶ
2. 同時に`simulate-mouse-input`(`LongPress`, `Button=Left`, `Duration`は移動と揃える)を画面内のランダムな座標（`Width`/`Height`の範囲内、例: 1080x1080なら200〜880）に対して呼び、発射を継続させる
3. 数サイクルごとに`simulate-keyboard`(`Press`, `Key=Digit1/Digit2/Digit3`)で射撃フォームを切替
4. 数サイクルごとに`simulate-mouse-input`(`LongPress`, `Button=Right`, `Duration=1〜2秒`)でフォーカスを切替
5. 適宜`simulate-keyboard`(`Press`, `Key=Space`)で回避も混ぜる
6. 1〜2サイクルごとに手順5（エラー確認）と手順2（ウェーブ状態確認）を必ず挟む。`IsWavePause=true`になったら手順4のショップ操作に切り替える

移動・発射の座標や継続時間に厳密な乱数生成は不要（Claude自身が呼び出しごとに値を変えれば十分）。1回のドリルで移動キー・発射座標・フォーム・フォーカスの組み合わせを変え続けることが目的。ウェーブ1〜3クリア+ショップ2回+フォーム全種切替+フォーカス切替+回避を含む形で検証済み、エラーなしで完走した。

### アップグレード候補の3Dカード（`Resolve-ShopIfOpen`が使用）
アップグレード候補は`ShopView`のCanvasボタンではなく、**3Dカード**（`UpgradeCardBoardView`が生成する`UpgradeCardView`）で並ぶ。VRはカードを掴んでトリガーで確定、非VRはマウスでカードをクリックして確定する。カードを並べられなかったとき（カメラ未取得・プレハブ未設定）だけ従来のCanvasボタンへフォールバックする。

- カードの一覧（インデックス・画面座標・コスト・購入可否）は`Get-ShopCards`で取れる。コストはカード上の`CostText`から読んでいるため、本番モデルへ差し替えてオブジェクト名が変わったらここも直すこと
- カードのクリックは`Invoke-ShopCardClick`（`simulate-mouse-input`でカードの画面座標をクリックする）。`simulate-mouse-ui`ではなく`simulate-mouse-input`を使うこと（カードはUIではなく3Dコライダーのため）
- **カードのクリックが効くのは非VRのときだけ**。VRモードのエディタではカードは掴み操作でしか選べず、クリックは空振りする（購入せずに次ウェーブへ進む）
- 非VRではショップの背景パネル（`ShopCanvas/Panel`のImage）をカード表示中だけ隠している。ScreenSpaceOverlayのCanvasが手前に描かれ、カードが一切見えなくなるため

### ショップUIの階層パス（`Resolve-ShopIfOpen`のフォールバックが使用）
`ShopView`プレハブは`BattleLifetimeScope`が実行時にインスタンス化する。**`ShopCanvas`は`VrUiFollowCanvasView`によってワールド座標でカメラ正面へ遅延追従する（再ペアレントはしない）**ため、クリック対象のパスはプレハブインスタンス配下を指定すること：
```
BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/UpgradeButtons/UpgradeButton0～11
BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/NextWaveButton
```
以前（`WorldSpaceUICanvasView`時代）は`MainCamera`配下へ再ペアレントされていたため、`BattleLifetimeScope/Player(Clone)/Camera/MainCamera/ShopCanvas/...`を指定していた。UIの追従方式を変えた場合はこのパスも要更新（過去に空振りでショップから遷移できない不具合が発生している）。
候補ボタンは`UpgradeButtons`の`GridLayoutGroup`（横4列×縦3行=最大12件）に並ぶ。通常は5件だが、アップグレード「目利き」を取得していると最大7件まで増える（`ShopUseCase.GetUpgradeChoiceCount`）。表示数に関わらず`UpgradeButton0`は常に存在するため、`Resolve-ShopIfOpen`の変更は不要。
`simulate-mouse-ui`の`--target-path`+`--bypass-raycast true`でスクリーンショット無しにクリックできる。アップグレード選択後は**選択したボタンだけ**が非表示になり、他の候補と`NextWaveButton`は表示されたまま残る（`ShopView.HideUpgradeButton(index)`の動作。残った候補ボタンは押しても反応しない＝1ウェーブ1回制限はUseCase側で担保）。

## このスキルを拡張するタイミング

以下のいずれかに該当したら、このファイル（`SKILL.md`）と関連スクリプトを更新すること。個別のClaude会話の中だけで済ませず、次回以降も再利用できるように反映する。

- **新しいテストシチュエーションを増やす** → `Tools/Playtest/Scenarios/`に新規`.ps1`を追加（このファイルの「新しいテストシチュエーションを追加する」節を参照）。SKILL.md側の変更は基本不要
- **新しいカメラ演出・エフェクトを追加した** → `Tools/Playtest/Probes/`に新規`.ps1`を追加し、「既存のプローブ」の一覧に1行足す（「演出の数値検証」節を参照）。演出は目視では検証にならないので、必ず数値で押さえる
- **プローブ共通の道具が足りない** → `PlaytestCommon.ps1`の「演出の数値検証」セクションにヘルパーを追加し、上の表を更新
- **ショップの選択ロジックを増やす**（例: 常に同じ候補ではなく、状況に応じて選ぶ）→ `PlaytestCommon.ps1`の`Resolve-ShopIfOpen`を拡張。カードの見た目・構成を変えた場合は`Get-ShopCards`（`CostText`の参照）も合わせて直す
- **新しい状態観測が必要になる**（例: プレイヤーのHP減少/ゲームオーバー判定が実装された、ウェーブ数UIが追加された）→ `Get-WaveState`のC#スニペットに新しいDataStore/プロパティを追加し、「前提・制約」セクションの記述を更新
- **検知基準を広げる**（例: ソフトロック検知、見た目異常チェックを追加する）→ `run-playtest.ps1`のエラー確認部分に新しいチェックを追記し、「前提・制約」の検知基準の記述も更新
- **`Debug.LogError`ではないが検知したい既知の問題が見つかった** → `PlaytestCommon.ps1`の`$Global:PlaytestKnownIssuePatterns`にメッセージの一部（検索文字列）を1行追加するだけでよい。`Get-NewErrors`が`log-type=All`＋`search-text`でLog/Warningレベルのメッセージも横断検索し、エラーとして報告する
- **入力バインドが変わる**（`GameMaininput.inputactions`の変更）→ 「入力操作」の表を更新

## 検知対象に加えた既知の問題

`$Global:PlaytestKnownIssuePatterns`（`PlaytestCommon.ps1`）に登録済み。ここに載っていないWarning/Logは検知対象外（気になる場合は「既知の非エラー事象」を参照、またはパターンを追加する）。

- `'can only be called on an active agent that has been placed on a NavMesh'` — `Assets/App/Script/Battle/Views/Enemy/AI/Rush.cs:22`で`NavMeshAgent.SetDestination`をエージェントがNavMeshに配置される前に呼んでいる（`Log`レベルだが実害があるため検知対象に追加）

## 既知の非エラー事象（参考、Errorではないため検知対象外）

過去の実行で見つかった、`Debug.LogError`ではないが気になる挙動。再度Claudeに調査を依頼する際の手がかりとして残す。新たに見つけたものもここに追記する。検知したいと判断したら上の「検知対象に加えた既知の問題」に移すこと。

- `Assets/App/Script/Battle/Views/HitBoxStoreView.cs:18`, `Assets/App/Script/Battle/Views/EnemyStoreView.cs:25` — VContainerのDI経由で`MonoBehaviour`を`new`で生成しようとする`Warning`が出る
