---
name: upgrade-add
description: RECONの「アップグレード」（1プレイサイクル中にウェーブ間ショップでランダム獲得する強化要素。例: ダメージアップ、連射速度、コンボ攻撃UP、低HP攻撃UP）を新規実装・追加する。アップグレードの追加・レベル追加・新パラメータ種別の追加・条件付きバフ型の追加を頼まれたら必ずこのスキルを使う。「強化を足して」「ショップに出る強化」「UpgradeType」「GrantBuff」「バフ付与型」などの依頼が該当する。
---

# upgrade-add: アップグレード実装ガイド

RECONの**アップグレード**は、1回のゲームプレイサイクル中にウェーブ突破ごとのショップで最大5件の候補から1件をランダム獲得していく強化要素。獲得はそのプレイサイクル限り（`UpgradeSessionDataStore.Reset()` でクリア）。

追加作業は必ず次の3パターンのどれかに当てはまる。**まずどのパターンかを判断**してから着手すること。判断を誤ると「データは足したのに効果が出ない」事故になる（後述の未接続タイプ問題）。

| パターン | 例 | 新規コードの要否 |
|---|---|---|
| **A. 既存タイプにレベル/行を追加** | ダメージアップ Lv4 を足す | 不要（データのみ） |
| **B. 新しい `UpgradeType`（新パラメータ）** | 「移動速度アップ」を新設 | **必須**（消費側コードを書く） |
| **C. GrantBuff型（条件付き発動バフ）** | コンボ攻撃UP、低HP攻撃UP | 場合による（新条件/新効果なら必要） |

## パイプライン全体像

```
Googleスプレッドシート（正本）
  └─ GAS UpgradeDataExporter.gs でエクスポート
      └─ UpgradeData.csv / UpgradeType.cs（自動生成物）
          └─ Unity: Tools/マスターデータ/UpgradeData CSVインポート
              └─ Upgrade/*.asset 生成 ＋ UpgradeDatabase.asset 配列更新
                  └─ 実行時: ショップ抽選 → 獲得 → 効果計算 → プレイヤーパラメータへ反映
```

上流（スプレッドシート）の更新は [`sheets-write`](../sheets-write/SKILL.md) スキル、動作検証は [`playtest-run`](../playtest-run/SKILL.md) スキルを併用する。

## 主要ファイル早見表

| 役割 | パス |
|---|---|
| 種別enum（**GAS自動生成・手動編集禁止**） | `Assets/App/Script/Common/Data/UpgradeType.cs` |
| マスターデータ型（`UpgradeMasterData`, SO） | `Assets/App/Script/Common/Data/MasterData/UpgradeData.cs` |
| Database（配列＋検索 `TryGetUpgradeMasterData`） | `Assets/App/Script/Common/Data/Database/UpgradeDatabase.cs` |
| CSVインポーター（Editor拡張） | `Assets/App/Script/Editor/UpgradeDataImporter.cs` |
| **効果計算**（乗算/加算合成） | `Assets/App/Script/Battle/DataStore/UpgradeEffectSimpleCalculator.cs`（クラス名は `UpgradeEffectSimpleCalculatorDataStore`） |
| 取得済みセッション管理 | `Assets/App/Script/Battle/DataStore/UpgradeSessionDataStore.cs` |
| 抽選（Fisher-Yates） | `Assets/App/Script/Battle/DataStore/UpgradeLotteryDataStore.cs` |
| ショップ制御（獲得＋GrantBuff処理） | `Assets/App/Script/Battle/UseCase/ShopUseCase.cs` |
| マスターデータCSV | `Assets/App/MasterData/Origin/UpgradeData.csv` |
| 生成SOアセット | `Assets/App/MasterData/Upgrade/*.asset` |
| Database アセット | `Assets/App/MasterData/Database/UpgradeDatabase.asset` |
| GrantBuff用: バフ型/DB/CSV/インポーター | 「パターンC」節を参照 |

## CSVスキーマ（UpgradeData.csv）

**データ開始は Row2**（Row1=ヘッダー、インポーターが1行スキップ）。列順は `UpgradeDataImporter.cs` の `Col*` 定数が正（ファイル冒頭コメントの列挙は古いので信用しないこと）。

```
0:id  1:NameKey  2:UpgradeType  3:PlayerUnlockType  4:Level
5:Value1  6:Value1ParameterType  7:Value2  8:Value2ParameterType
9:Value3 10:Value3ParameterType 11:Value4 12:Value4ParameterType
13:Value5 14:Value5ParameterType 15:BuffId（任意列。省略可 / GrantBuff時のみ使用）
```

- **`Value1`〜`Value3` が使用中**（`HealOnKill` は Value2、`PeaceMaker` は Value2/Value3、`Avalanche` は Value2、注視系の `SnakeEyes`/`Medusa`/`MeanMug` は Value2=注視半径・`SnakeEyes` のみ Value3=解除猶予秒 を参照する。`Value4`/`Value5` は未使用の予約列）。汎用の `CalcMultiply/CalcAdd/CalcMax` は `Value1` しか見ないため、複数Valueを使う効果は専用DataStoreで `TryGetHighestLevelUpgrade` から読む
- `id` は連番の整数（文字列扱い）。`NameKey` は `$` + PascalCase のローカライズキー（例 `$BaseDamageUp`）
- 生成アセット名は `NameKey` から `$` を除き `_L{Level}` を付与（例 `BaseDamageUp_L1.asset`）
- `MinColumnCount=15`。BuffId列が無い行も許容される

## 効果適用の仕組み（ここが要）

獲得済みアップグレードの効果は `UpgradeEffectSimpleCalculatorDataStore` が合成する:
- `CalcMultiply(UpgradeType)`: 該当タイプ全件の `Value1` を**乗算**（初期値 1.0）
- `CalcAdd(UpgradeType)`: 該当タイプ全件の `Value1` を**加算**（初期値 0）

**消費側**が明示的にこれを呼んで初めて効果が出る。現状の接続済みは以下だけ:

| UpgradeType | 消費箇所 |
|---|---|
| `BulletDamage` | `PlayerBulletParameterDataStore.cs` `damage *= CalcMultiply(BulletDamage)` |
| `FireRate` | `PlayerBulletParameterDataStore.cs` `coolDown *= CalcMultiply(FireRate)` |
| `BombRange` | `PlayerBulletParameterDataStore.cs` `explosive *= CalcMultiply(BombRange)` |
| `HitRange` | `PlayerBulletParameterDataStore.GetBulletParameter` `bullet.Size *= CalcMultiply(HitRange)`（**非フォーカス弾のみ**。`BulletData.Size` は見た目スケールと SphereCast 判定を兼ねる） |
| `NormalDamage` / `WaltzDamage` / `MergeDamage` | `PlayerBulletParameterDataStore.GetBulletDamage`（`ShotType` に応じて該当フォームの弾にのみ乗算） |
| `Barrier` | `UpgradeSideEffectApplier.Apply` → `PlayerBarrierDataStore.GrantFull`（獲得時／セット読込時に最大HP×Value1 のバリアを満タン付与。Calculator非経由） |
| `GrantBuff` | `UpgradeSideEffectApplier.Apply` → `BuffStateDataStore.AddBuff`（Calculator非経由。下記パターンC） |
| `ChokePoint` / `BigMouse` / `Diversion` | `EnemyRandomSpawnUseCase`（スポーン挙動を変える系。抽選位置・湧き方に介入する） |
| `HealOnKill` | `HealOnKillDataStore`（撃破時回復。Value2 併用） |
| `PeaceMaker` | `PeaceMakerDataStore` → `PlayerBulletParameterDataStore.SetCoolDownTime`（`TryGetHighestLevelUpgrade` で最高レベルのみ採用。Value1=連続ノーマルショット数 / Value2=強化CD倍率 / Value3=ペナルティCD倍率） |
| `Avalanche` | `AvalancheDataStore` → `PlayerBulletParameterDataStore.SetCoolDownTime`（Value1=通常CD倍率 / Value2=直前マージ命中時のCD倍率。命中通知は `BattleHitUseCase.NotifyMergeHit`） |
| `LuckyChance` / `KillingCall` / `TurnTable` | `CriticalHitDataStore` → `BattleHitUseCase.OnHit`（3種の**クリティカル確率を合算**し、100%ごとに1段確定＋端数を抽選。1段=ダメージ+100%。いずれも `CalcMax` で最高レベルのみ採用。キリングコールは `HitData.FocusType`/`IsFocusTarget`/`PenetrationIndex==1` で条件判定、ターンテーブルは `IPlayerStateDataStore` のHP減少割合を参照） |
| `SnakeEyes` | `SnakeEyesDataStore` → `PlayerGazeUseCase` → `IEnemyPresenter.SetSpeedMultiplier` → `EnemyAIBase`（Value1=速度倍率 / Value2=注視半径 / Value3=解除猶予秒） |
| `Medusa` | `MedusaDataStore` → `PlayerGazeUseCase` → `IEnemyPresenter.SetStun` → `EnemyAIBase`（Value1=スタン秒 / Value2=注視半径。Major/Boss/Irregular のみ・敵ごとに1度） |
| `MeanMug` | `MeanMugDataStore` → `BattleHitUseCase.OnHit`（Value1=被ダメージ倍率 / Value2=注視半径。注視状態の更新は `PlayerGazeUseCase`） |
| `DependencyNode` | `DependencyNodeDataStore`（効果なし。α・β・γの3行を別NameKeyで持ち、**有効な種類数**を他ノード系へ提供する。無効化は実行時状態のみでセーブ内容に影響しない） |
| `DamageNode` | `DamageNodeDataStore` → `PlayerBulletParameterDataStore.GetBulletDamage`（Value1=依存ノード1種あたりの加算率 / Value2=依存ノード0種時のデメリット倍率） |
| `CareNode` | `CareNodeDataStore` → `CareNodeUseCase`（1秒ごと。Value1=依存ノード1種あたりの毎秒回復割合 / Value2=依存ノード0種時の毎秒ダメージ割合 / Value3=その最低ダメージ量。HPは1未満にならない） |
| `EmergencyNode` | `EmergencyNodeDataStore` → `PlayerStateDataStore.TakeDamage`（致死ダメージを無効化し、依存ノードを1つ（α→β→γ）無効化して最大HP×Value1 まで回復） |
| `Appraisal` | `ShopUseCase.GetUpgradeChoiceCount`（ショップの抽選数に加算。`CalcMax` で最高レベルのみ採用。上限 `MaxUpgradeChoiceCount`=12 は `ShopView.prefab` のボタン数と一致させること） |
| `ExtraConflict` | `ExtraConflictDataStore` → `PlayerBulletParameterDataStore`（Value1=ダメージ倍率 / Value2=クールダウン倍率）＋ `PlayerShotTypeDataStore`・`PlayerAimUseCase`・`PlayerFocusDataStore`（ワルツ・マージ・フォーカスを封印）。**フォーカス封印は書き込み元の `PlayerAimUseCase.UpdateOnFocus` で止める**こと（`PlayerFocusDataStore.Tick` での解除は同フレーム内に `OnFocus` 通知で上書きされる） |
| `ElectricShock` | `ElectricShockDataStore` → `BattleHitUseCase.OnHit`（ワルツ命中時に命中先の周囲へダメージを伝播。Value1=レベルごとの半径倍率 / Value2=伝播ダメージ割合 / Value3=基礎半径m。半径は Value3 × `CalcMultiply(HitRange)` × Value1。伝播は `ShotType=null` で与えるためフォーム条件のバフを二重に駆動しない） |
| `ParryingDagger` | `ParryingDaggerDataStore` → `ParryingDaggerUseCase`（回避中に無効化した敵弾を撃ってきた相手へ撃ち返す。Value1=引き継ぐフォーム数 1=ノーマル/2=+ワルツ/3=+マージ。`TryGetHighestLevelUpgrade` で最高レベルのみ採用。パリィ弾は `PlayerBulletParameterDataStore.GetParryBulletData` が生成する即着弾のフォーカスショット） |
| `Fixation` | `UpgradeLotteryDataStore.DrawUpgrades`（取得済みと同じ `NameKey` の候補の抽選重みを `1 + Value1` 倍にする。`CalcMax` で最高レベルのみ採用） |

> 📌 **注視（視界中央）系の共通基盤**: `IBattlePlayerView.TryGetGazePose`（`Camera.main` をキャッシュ）→ `IEnemyStoreView.GetGazeEnemies`（`EnemyLayer` への SphereCast、バッファ使い回し）→ `PlayerGazeUseCase`（毎フレーム3種を更新。未所持ならレイキャストしない／敵消滅時に状態破棄）。
> **PCモード（見下ろしカメラ）では半径2〜3mの判定にほぼ敵が入らず発動しない**（カメラが上空約18mからほぼ真下を向いているため）。VR前提の仕様なので、PCで検証したい場合は半径を大きくして確認する。

> ⚠️ **未接続タイプ問題**: `DodgeDistance` / `DodgeCount` / `DodgeCooldown` / `Health` は enum・CSVには存在するが**どこからも Calc されておらず、効果が出ない**。特に `Health` はCSVに `$Health` 行があってもHP最大値に反映されない（`PlayerStateDataStore.Initialize` が `BasePlayerParameter.Health` を直接使うだけ）。**新タイプ追加＝消費側コードもセットで書く**ことを絶対に忘れない。

いずれも基礎値は静的クラス `BasePlayerParameter`（`Assets/App/Script/Battle/Data/BasePlayerParamater.cs`）。乗算補正として重なる。

---

## パターンA: 既存タイプにレベル/行を追加（データのみ）

1. スプレッドシート `UpgradeData` シートに行追加（[`sheets-write`](../sheets-write/SKILL.md)）→ GASでエクスポート
2. `Assets/App/MasterData/Origin/UpgradeData.csv` を追随（またはUnityメニュー `Tools/マスターデータ/UpgradeData ファイルコピー`）
3. Unityで `Tools/マスターデータ/UpgradeData CSVインポート` 実行 → アセット生成＋Database更新
4. 消費側は既存の `CalcMultiply/CalcAdd` が自動で拾うため**コード変更不要**
5. [`playtest-run`](../playtest-run/SKILL.md) で獲得→効果反映を確認

## パターンB: 新しい UpgradeType（新パラメータ系）

1. スプレッドシート `UpgradeType` enumシートに要素追加 → GAS `UpgradeType Enum C#エクスポート` で `UpgradeType.cs` を再生成（**手動編集不可**。手で足すと次回エクスポートで消える）
2. `UpgradeData.csv` に新タイプの行を追加 → パターンAの2〜3でインポート
3. **消費側コードを実装**（このパターンの本体）:
   - 効果を効かせたい `Player*ParameterDataStore` に `IUpgradeEffectSimpleCalculatorDataStore` をDIで注入（VContainer。直接 `new` しない）
   - 基礎値に `CalcMultiply(UpgradeType.新規)`（倍率系）か `CalcAdd(...)`（加算系）を掛ける/足す
   - HP・回避系のように「基礎値を `BasePlayerParameter` から直接代入している箇所」がある場合、そこに補正を差し込む改修が要る（未接続タイプ問題と同じ轍を踏まないこと）
4. コンパイル（`uloop-compile` スキル）→ `playtest-run` で検証

## パターンC: GrantBuff型（条件付き・時限バフ）

コンボ攻撃UP / 低HP攻撃UP がこの型。`UpgradeType.GrantBuff` は効果計算を経由せず、**選択時にバフを起動**する。バフ側の関連ファイル:

| 役割 | パス |
|---|---|
| バフ型 / Database / CSV | `Assets/App/MasterData/Origin/BuffData.csv` ほか `MasterData/Buff/`・`Database/BuffDatabase.asset` |
| バフCSVインポーター | `Assets/App/Script/Editor/BuffDataImporter.cs`（メニュー `Tools/マスターデータ/BuffData CSVインポート`） |
| 実行時バフ状態 | `Assets/App/Script/Battle/DataStore/BuffStateDataStore.cs`（`AddBuff` / `IsActive` / `CalcMultiply(BuffEffectType)`） |
| 発動条件の駆動 | `Assets/App/Script/Battle/UseCase/BuffConditionUseCase.cs`（`NotifyHit` / `SetHealthRatio` 等） |

BuffData.csv ヘッダー: `id,NameKey,ConditionType,ConditionValue,Duration,EffectType,EffectValue`
（`ConditionType`=`BuffConditionType`enum, `EffectType`=`BuffEffectType`enum。いずれもGAS自動生成）

> ⚠️ **`BuffConditionType` は enum に値があっても実装済みとは限らない**。`AfterDodge`（回避直後N秒）は enum 定義だけで `BuffStateDataStore` に分岐が無く、CSVに行を足しても発動しない状態だった（カウンターステップ実装時に `NotifyDodge` を追加して接続済み）。新しい条件を使う前に `BuffStateDataStore.IsActive` の switch と `BuffConditionUseCase` の購読を必ず確認すること。
>
> 回避の通知経路: `PlayerDodgeUseCase` → `PlayerDodgeParameterDataStore.SetCoolDownTime()`（ここで `OnDodge` を発火）→ `BuffConditionUseCase` → `BuffStateDataStore.NotifyDodge()`。

手順:
1. `BuffData` シート/CSVにバフ行を追加 → `Tools/マスターデータ/BuffData CSVインポート`
2. `UpgradeData.csv` に `UpgradeType=GrantBuff` かつ `BuffId=対象バフのid` の行を追加 → UpgradeDataインポート
3. `BuffDatabase.asset` と `UpgradeDatabase.asset` が更新されたことを確認。`CommonLifetimeScope`（`Assets/App/Script/Common/CommonLifetimeScope.cs`）に両Databaseが割当済みか確認
4. 選択時挙動: `ShopUseCase.OnUpgradeSelected` が GrantBuff を見て `BuffDatabase.TryGetBuffMasterData(BuffId)` → `BuffStateDataStore.AddBuff`。BuffIdがDBに無いと `Debug.LogWarning`（`playtest-run` で検出される）
5. **新しい発動条件や効果種別が必要な場合のみ**、`BuffStateDataStore`（効果合成・有効判定）と `BuffConditionUseCase`（入力購読）に分岐を追加。既存の条件/効果で足りるならコード変更不要

---

## 命名規則

- `UpgradeType` enum: PascalCase の機能名詞（`BulletDamage`, `FireRate`, `GrantBuff`）、0=None から連番
- `NameKey`: `$` + PascalCase（`$ComboAttackUp`）
- バフ名: アップグレード名 + `Buff` 接尾辞（`$ComboAttackUp` → `$ComboAttackUpBuff`）
- 生成アセット: `NameKey` の `$` 除去 + `_L{Level}`

## 完了チェックリスト

- [ ] enum変更はスプレッドシート側にも反映したか（`UpgradeType.cs` を手編集で終わらせていないか）
- [ ] パターンB/C で新タイプ・新条件を足したなら、**消費側コードを書いたか**（データだけで満足していないか）
- [ ] `UpgradeDatabase.asset`（GrantBuffなら `BuffDatabase.asset` も）が配列更新されたか
- [ ] `uloop-compile` でエラーなし
- [ ] `playtest-run` でショップに候補が出て、獲得後に効果が反映され、`Debug.LogWarning/Error` が出ていないか
- [ ] コード変更は日本語コミット＋PR（developベース）。Claudeの変更は必ずPR経由

## 既知の同期ギャップ（着手前に確認）

- `UpgradeType.GrantBuff=9` は現状 `UpgradeType.cs` に手動追加された状態で、スプレッドシート側 `UpgradeType` シートへの反映が未完了の可能性がある。GASエクスポートを走らせる前に、スプレッドシートに `GrantBuff` 行があるか確認すること（無いままエクスポートすると enum から消える）。

---

## issue駆動エントリポイント（スマホから仕様を送って自動実装するモード）

GitHub Issue（`.github/ISSUE_TEMPLATE/add-upgrade.yml` のフォーム）で送られた仕様を、開発マシン上のClaudeが受け取って本スキルの手順を最後まで実行し、developベースのPRを作るための入口と出口を定義する。対話でユーザーが直接依頼する通常モードと手順本体（パターンA/B/C）は共通で、以下は自律実行時の追加ルール。

### 前提
- **Unity Editorが起動していること**（CSVインポート・`uloop-compile`・`playtest-run` に必須）。
- ポーラー（後述）が `add-upgrade` ラベルの open issue を検知して本スキルを起動する。

### 入口: Issueフォーム → スキル語彙の対応

| フォーム項目 | スキル上の扱い |
|---|---|
| アップグレード名（NameKey） | CSV `NameKey` 列（`$`+PascalCase）。生成アセット名・ブランチ名の元 |
| 効果カテゴリ（UpgradeType） | 「既存」を選択 → **パターンA**（コード変更不要）。「GrantBuff」→ **パターンC**。「新規カテゴリを作る」→ **パターンB** |
| レベルごとの効果値（Value1） | `Lv{n}=値` を1行1レベルとしてパース。各行が CSV 1行（`Level`=n, `Value1`=値）になる |
| 解放条件（PlayerUnlockType） | CSV `PlayerUnlockType` 列（enum数値に変換） |
| GrantBuffの場合のバフ内容 | パターンC。既存BuffIdならその番号、新規記述なら [`buff-add`](../buff-add/SKILL.md) スキルでバフを先に用意してから `BuffId` を紐づける |
| 効果の意図・補足 | パターンBの「消費側コードの追加先」の手がかり。ここに指定があればその DataStore に `CalcMultiply/CalcAdd` を追加する |

判断に迷う点（新規カテゴリでどのパラメータに掛けるか不明、など）があり、フォームだけで確定できない場合は**推測で実装せず、issueにコメントで質問して一旦停止する**（PRは作らない）。

### 出口: PR作成とissueクローズの定型
本スキルの「完了チェックリスト」を満たしたうえで:

1. `git checkout develop && git pull` → `feature/add-upgrade-<name>`（`<name>`はNameKeyの`$`除去・kebab化）を作成
2. 変更をコミット（**日本語メッセージ + `Co-Authored-By`**）。差分には `UpgradeData.csv`・再生成された `.asset`（GrantBuffなら Buff 側も）・パターンBのコードを含む
3. `gh pr create --base develop`（本文に対象issue番号 `Closes #N`、実装したパターン、playtest結果を日本語で記載）
4. `gh issue comment <N>` でPRリンクを通知し、`gh issue close <N>`
5. **マージは絶対にしない**（人間レビュー必須）。`main`/`develop` へのforce push禁止

## ローカルポーラーの起動（Runbook）

`add-upgrade` issue を拾って本スキルを回す常駐ループ。開発マシンで Unity Editor を開いた状態で使う。

- **初回のみ**: リポジトリに `add-upgrade` ラベルを作成する（無いとテンプレートのラベルが付かない）。
  ```powershell
  gh label create add-upgrade --description "スマホ等から送るアップグレード追加依頼" --color 1D76DB
  ```
- **起動**: Claude Code で `/loop 5m` を使い、次の主旨のプロンプトを回す（自己ペースで回す場合は間隔省略）。
  > `gh issue list --label add-upgrade --state open` を確認し、未処理のissueがあれば `upgrade-add` スキルの「issue駆動エントリポイント」に従って1件実装し、developベースのPRを作ってissueをクローズする。open issueが無ければ何もしない。
- **停止**: `/loop` を停止する（ループのループ停止操作）。
- ポーラーは **PR作成まで**。マージ・force push はしない。`@claude` を含むissueには反応しない（そちらはクラウドの `.github/workflows/claude.yml` の担当）。

> 補足: クラウドの `claude.yml`（`runs-on: ubuntu-latest`）では Unity Editor が無く、CSVインポート/コンパイル/playtest ができないため、アップグレード追加はローカルポーラー方式を採る。将来 self-hosted runner を用意すれば `@claude` 起動に一本化する余地はある。

## 自動実行の落とし穴（issue #25 の実装で判明）

自動実行（uLoop CLI / execute-dynamic-code 経由）でインポートや検証を回すときの既知の罠。対話でメニューを手動実行する分にはどれも問題にならない。

- **インポートはメニュー実行(`ExecuteMenuItem`)ではなく `ImportData(false)` を呼ぶ**。インポーターは完了時に `EditorUtility.DisplayDialog`（モーダル）を出し、`ExecuteMenuItem` 経由だと Unity のメインスレッドがダイアログ待ちでブロックし、uLoop の全コマンドが 180 秒タイムアウトする（＝ハング）。自動実行では次を execute-dynamic-code で呼ぶ:
  ```csharp
  App.Editor.BuffDataImporter.ImportData(false);      // interactive=false でダイアログを出さずログ出力
  App.Editor.UpgradeDataImporter.ImportData(false);
  ```
  なお `ImportData(false)` でも AssetDatabase 更新で応答が返らずタイムアウト表示になることがあるが、**インポート自体は完了している**（`git status` で `Upgrade/*.asset`・`UpgradeDatabase.asset` の生成/更新を確認できる）。
- **スプレッドシートの列構成はCSVと違う**。`UpgradeData`/`BuffData` シートには CSV に無い人間用の列（`開発名称`、および UpgradeData では enum の表示名を入れる補助列）があり、GAS エクスポートで除外される。`sheets-write` の `append-rows` は**シートの全列順に値を並べる**必要があり、CSV の列順で渡すと列ずれする。既存行（例: 既存の GrantBuff 行）を `get` で読んでテンプレにし、補助列は空欄でよい。詳細は [`sheets-write`](../sheets-write/SKILL.md) を参照。
- **Play Mode 遷移直後はドメインリロードで uLoop が一時的に応答しない**。`control-play-mode --action Play` の直後に execute-dynamic-code を撃つと「Domain Reload in progress」で失敗する。数秒待ってリトライするか、シーン非依存のロジック検証は **Edit モードで完結**させる（例: `BuffStateDataStore` は素の C# クラスなので `new` して `NotifyHit`/`CalcMultiply` を直接叩けば、Play Mode 不要でスタック挙動を検証できる）。

## このスキルを拡張するタイミング

- 新しい `UpgradeType` を接続したら「効果適用の仕組み」の接続済みテーブルに追記する
- `Value2`〜`Value5` を使う計算を実装したら「Value1のみ使用」の記述を更新する
- CSVスキーマ（列追加）やインポーターのメニュー名が変わったら該当節を直す
- 新しい獲得フロー（ショップ以外の入手経路など）を作ったら「パイプライン全体像」を更新する
