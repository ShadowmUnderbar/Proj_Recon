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

- **実際に使われるのは `Value1` のみ**（`Value2`〜`Value5` は現状どの計算も参照していない。将来用の予約列）
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
| `GrantBuff` | `ShopUseCase.OnUpgradeSelected`（Calculator非経由。下記パターンC） |

> ⚠️ **未接続タイプ問題**: `HitRange` / `DodgeDistance` / `DodgeCount` / `DodgeCooldown` / `Health` は enum・CSVには存在するが**どこからも Calc されておらず、効果が出ない**。特に `Health` はCSVに `$Health` 行があってもHP最大値に反映されない（`PlayerStateDataStore.Initialize` が `BasePlayerParameter.Health` を直接使うだけ）。**新タイプ追加＝消費側コードもセットで書く**ことを絶対に忘れない。

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

## このスキルを拡張するタイミング

- 新しい `UpgradeType` を接続したら「効果適用の仕組み」の接続済みテーブルに追記する
- `Value2`〜`Value5` を使う計算を実装したら「Value1のみ使用」の記述を更新する
- CSVスキーマ（列追加）やインポーターのメニュー名が変わったら該当節を直す
- 新しい獲得フロー（ショップ以外の入手経路など）を作ったら「パイプライン全体像」を更新する
