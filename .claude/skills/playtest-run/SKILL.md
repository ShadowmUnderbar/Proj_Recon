---
name: playtest-run
description: "RECONのバトルシーンを自動プレイし、ウェーブ進行・ショップ操作を通してDebug.LogError/例外を検出する。Tools/Playtest/run-playtest.ps1（またはUnity Editorの Tools > Playtest Runner ウィンドウ）で実行する。テストラン実行そのものだけでなく、フロー拡張（シナリオ追加、対象ウェーブ数変更、新規チェック項目追加、新規DataStoreの観測追加など）を行う際もこのスキルを更新して使うこと。"
---

# 自動プレイテストラン（playtest-run）

RECONのバトルフロー（ウェーブ進行→ショップ→次ウェーブ…）を自動プレイし、その間に発生した `Debug.LogError` / 例外を検出する。

- **実行本体**: `Tools/Playtest/run-playtest.ps1`（PowerShellスクリプト、uLoopMCPのCLI(`uloop`)を直接叩く。Claudeの対話操作なしで単体実行できる）
- **手軽な起動**: Unity Editorの `Tools > Playtest Runner` メニューからEditorWindowを開き、シナリオとウェーブ数を選んで「テストラン実行」ボタンを押すだけでよい（`Assets/App/Script/Editor/TestRun/PlaytestRunnerWindow.cs`）
- **テストシチュエーションの追加**: `Tools/Playtest/Scenarios/*.ps1` に新しいファイルを1つ追加するだけでよい（後述）。Editor拡張のドロップダウンにも自動で反映される
- 新規のUnity C#コードは自動化基盤としては追加していない（Editor拡張はツール専用で、DataStore/UseCaseなどゲーム本体のアーキテクチャには一切関与しない）。ウェーブ状態の読み取りは既存のDataStoreを`execute-dynamic-code`から読むだけ

CIの恒久テストではなく、**開発中の調査ループ**（自動プレイ→エラー確認→修正→再実行）として使う。

**重要: `Tools/Playtest/`配下の`.ps1`ファイルは必ずBOM付きUTF-8で保存すること。** BOM無しUTF-8で保存すると、Windows PowerShell 5.1がファイル内の日本語文字列をシステムのコードページ（Shift-JIS系）で誤読し、それ以降のスクリプト解釈が壊れて自作関数（`PlaytestScenarioStep`等）が`CommandNotFoundException`になる（検証済みの実際の不具合、原因特定に長時間かかった既知の落とし穴）。編集後は以下でBOMを確認・復元する。
```powershell
$f = "Tools/Playtest/対象ファイル.ps1"
[System.IO.File]::WriteAllText($f, (Get-Content -Raw $f -Encoding UTF8), (New-Object System.Text.UTF8Encoding($true)))
```
ファイルツール（Write/Edit）で保存すると多くの場合BOMが失われるため、保存後は`xxd <file> | head -1`で先頭が`efbb bf`になっているか確認する。

## 前提・制約（2026-07時点、実装が進んだら要更新）

- ゲームシーンは`Assets/Scenes/SampleScene.unity`の1本のみ
- プレイヤーのHP減少・ゲームオーバー判定は未実装 → 「ゲームオーバーまで」のフローは組めない。**ウェーブクリアの繰り返し**が自然な終端
- ウェーブ数を表示するUIは存在しない → 状態はUIではなく`execute-dynamic-code`経由でDataStoreから読む
- ショップの開閉状態を公開するプロパティはない → `IsWavePause`とショップUIプレハブ（`ShopView`）の出現で判断する
- 検知基準は`Debug.LogError`と例外のみ（Warning/Logは対象外、ただし調査の参考として記録はする）

## 実行方法

### 通常の実行（推奨）
Unity Editorで `Tools > Playtest Runner` を開き、シナリオ（`FixedFlow`/`RandomDrill`）と対象ウェーブ数を選んで「テストラン実行」を押す。PowerShellウィンドウが開いて進行状況が流れ、終了するとウィンドウ内に直近の結果（到達ウェーブ・成否・エラー内容）が表示される。

ターミナルから直接実行することもできる（Unity Editorは起動している必要がある）：
```powershell
& "Tools/Playtest/run-playtest.ps1" -Scenario RandomDrill -Waves 3
```
終了コード0=エラーなし、1=エラー検出、2=シナリオ指定ミス。結果は`Tools/Playtest/Reports/`にJSONで保存される（最大10件、古いものから自動削除、`.gitignore`済みでコミット対象外）。

### スクリプト構成
- `Tools/Playtest/PlaytestCommon.ps1`: 共通ヘルパー（`Invoke-Uloop`＝uloop CLIラッパー、`Get-WaveState`＝ウェーブ状態取得、`Resolve-ShopIfOpen`＝ショップ自動選択、`Get-NewErrors`＝エラー取得、`Write-PlaytestReport`＝レポート出力）
- `Tools/Playtest/Scenarios/*.ps1`: シナリオ本体。各ファイルは`PlaytestScenarioStep`関数を1つ定義するだけでよい（詳細は後述）
- `Tools/Playtest/run-playtest.ps1`: ランナー本体。compile→clear-console→Play→（状態観測→シナリオ実行 or ショップ処理→エラー確認）の繰り返し→Stop→レポート出力
- `Assets/App/Script/Editor/TestRun/PlaytestRunnerWindow.cs`: 上記スクリプトをUnity Editorから起動するEditorWindow。`Tools/Playtest/Scenarios/`をスキャンしてシナリオ一覧を動的生成する

### 新しいテストシチュエーションを追加する
`Tools/Playtest/Scenarios/`に新規`.ps1`ファイルを1つ追加し、`PlaytestScenarioStep`関数を定義するだけでよい。ランナー側の変更は不要、Editor拡張のドロップダウンにも自動で反映される。

```powershell
function PlaytestScenarioStep {
    # ウェーブ進行中（IsWavePause=falseの間）に毎サイクル呼ばれる。
    # ここでInvoke-Uloopを使い、移動・発射・フォーム切替などを組み立てる。
}
```
既存の`FixedFlow.ps1`（決め打ち移動+発射）・`RandomDrill.ps1`（移動/発射/フォーム/フォーカス/回避をサイクルごとに変える）を参考にする。ショップでのアップグレード選択・次ウェーブ操作は`PlaytestCommon.ps1`の`Resolve-ShopIfOpen`が共通処理として自動で行う（現在は常に最初の候補=`UpgradeButton0`を選ぶ）。

### 状態観測の仕組み（`Get-WaveState`の内部）
`execute-dynamic-code`で以下のC#スニペットを実行し、`currentWave`/`isWavePause`/`elapsed`/`kill`をJSON文字列で取得している。

```csharp
using VContainer;
using VContainer.Unity;
var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
return $"{{\"currentWave\":{wave.CurrentWave.CurrentValue},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"kill\":{wave.KillCount.CurrentValue}}}";
```
- `using VContainer;` が無いと`Container.Resolve<T>()`（拡張メソッド）がコンパイルエラーになる（`CS0308`）。必ず両方の`using`を入れること
- スニペットを一時ファイル経由で渡す際は**BOM無しUTF-8**で書き込むこと（`[System.IO.File]::WriteAllText(path, text, (New-Object System.Text.UTF8Encoding($false)))`）。PowerShell 5.1の`Set-Content -Encoding utf8`はBOM付きになり、先頭の`using`が`CS1001`等で壊れる（検証済みの既知の落とし穴）
- `BattleLifetimeScope`はシーン上に固定配置されたGameObjectなので`LifetimeScope.Find<T>()`で解決できる（Awakeでの動的生成ではない）
- `IWaveManagerDataStore`（`Assets/App/Script/Battle/Interface/DataStore/IWaveManagerDataStore.cs`）: `CurrentWave`, `IsWavePause`, `ElapsedTime`, `KillCount`, `OnWaveAdvanced`
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

### ショップUIの階層パス（`Resolve-ShopIfOpen`が使用）
`BattleLifetimeScope`が実行時にインスタンス化する`ShopView`プレハブの構造：
```
BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/UpgradeButtons/UpgradeButton0～4
BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/NextWaveButton
```
`simulate-mouse-ui`の`--target-path`+`--bypass-raycast true`でスクリーンショット無しにクリックできる。アップグレード選択後は他のUpgradeButtonが非表示になり`NextWaveButton`のみ残る（`ShopUseCase.HideUpgradeButtons()`の動作）。

## このスキルを拡張するタイミング

以下のいずれかに該当したら、このファイル（`SKILL.md`）と関連スクリプトを更新すること。個別のClaude会話の中だけで済ませず、次回以降も再利用できるように反映する。

- **新しいテストシチュエーションを増やす** → `Tools/Playtest/Scenarios/`に新規`.ps1`を追加（このファイルの「新しいテストシチュエーションを追加する」節を参照）。SKILL.md側の変更は基本不要
- **ショップの選択ロジックを増やす**（例: 常に同じ候補ではなく、状況に応じて選ぶ）→ `PlaytestCommon.ps1`の`Resolve-ShopIfOpen`を拡張
- **新しい状態観測が必要になる**（例: プレイヤーのHP減少/ゲームオーバー判定が実装された、ウェーブ数UIが追加された）→ `Get-WaveState`のC#スニペットに新しいDataStore/プロパティを追加し、「前提・制約」セクションの記述を更新
- **検知基準を広げる**（例: ソフトロック検知、見た目異常チェックを追加する）→ `run-playtest.ps1`のエラー確認部分に新しいチェックを追記し、「前提・制約」の検知基準の記述も更新
- **入力バインドが変わる**（`GameMaininput.inputactions`の変更）→ 「入力操作」の表を更新

## 既知の非エラー事象（参考、Errorではないため検知対象外）

過去の実行で見つかった、`Debug.LogError`ではないが気になる挙動。再度Claudeに調査を依頼する際の手がかりとして残す。新たに見つけたものもここに追記する。

- `Assets/App/Script/Battle/Views/Enemy/AI/Rush.cs:22` — `NavMeshAgent.SetDestination`をエージェントがNavMeshに配置される前に呼んでおり、`Log`レベルの警告が多発する
- `Assets/App/Script/Battle/Views/HitBoxStoreView.cs:18`, `Assets/App/Script/Battle/Views/EnemyStoreView.cs:25` — VContainerのDI経由で`MonoBehaviour`を`new`で生成しようとする`Warning`が出る
