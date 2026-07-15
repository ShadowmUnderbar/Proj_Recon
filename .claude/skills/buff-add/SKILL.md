---
name: buff-add
description: RECONの「バフ」（発動条件付き・時限の効果。例: 10ヒットで5秒間攻撃力1.1倍＝コンボ攻撃UP、HP30%以下で攻撃力1.5倍＝低HP攻撃UP）を新規実装・追加する。バフの追加、新しい発動条件（BuffConditionType）や新しい効果種別（BuffEffectType）の追加を頼まれたら必ずこのスキルを使う。「バフを足して」「条件付きの強化」「◯◯したら△△になる効果」「BuffConditionType」「BuffEffectType」などの依頼が該当する。現状は機能が限定的（実装済みは HitCount/HpBelow 条件 × AttackPower 効果のみ）で今後拡張していく前提。
---

# buff-add: バフ実装ガイド

RECONの**バフ**は「発動条件を満たしている間だけ効くパラメータ補正」。アップグレードが**恒久的な単純補正**なのに対し、バフは**条件付き・時限**なのが違い。現状バフは [`upgrade-add`](../upgrade-add/SKILL.md) の `GrantBuff` 型アップグレードを獲得したときにだけ起動する（＝バフ単体の入手経路はまだ無い）。

実装済みの現状（**ここが狭い。拡張時の起点**）:
- 発動条件 `BuffConditionType`: **`HitCount`** と **`HpBelow`** のみ稼働。`HpAbove` / `AfterDodge` はenumにあるが未接続
- 効果種別 `BuffEffectType`: **`AttackPower`（攻撃力倍率）** のみ
- 効果の合成は**乗算のみ**（`CalcMultiply`）。加算はまだ無い

追加作業は次の3パターン。**着手前にどれかを判定**する（アップグレードと同じく「データを足したのに効かない」事故を防ぐため）。

| パターン | 例 | 新規コードの要否 |
|---|---|---|
| **A. 既存の条件×効果で新バフ** | 「5ヒットで攻撃力1.2倍」を追加 | 不要（データのみ） |
| **B. 新しい発動条件（ConditionType）** | 「回避直後3秒」を実装 | **必須**（状態管理＋駆動入力） |
| **C. 新しい効果種別（EffectType）** | 「防御力アップ」を実装 | **必須**（消費側コード） |

## アーキテクチャ

```
BuffData.csv（発動条件・効果・時間の定義）
  └─ Tools/マスターデータ/BuffData CSVインポート
      └─ Buff/*.asset 生成 ＋ BuffDatabase.asset 配列更新
          └─ 実行時:
             GrantBuffアップグレード獲得 → ShopUseCase → BuffStateDataStore.AddBuff（監視開始）
             BuffConditionUseCase が入力（ヒット/HP）を購読 → BuffStateDataStore へ通知
             BuffStateDataStore が発動判定・残時間管理 → CalcMultiply()
             消費側（PlayerBulletParameterDataStore 等）が CalcMultiply を掛けて反映
```

3つのDataStore/UseCaseの役割分担が肝:
- **`BuffStateDataStore`**（実行時状態の中枢）: 取得済みバフの発動条件の進行・残り効果時間を保持。`ITickable` で毎フレーム残時間を減算。`AddBuff` / `NotifyHit` / `SetHealthRatio` / `CalcMultiply` / `Reset`
- **`BuffConditionUseCase`**（入力の配線）: R3で `OnHit`（`IBattleHitPresenter`）と `Health`/`MaxHealth` を購読し、`BuffStateDataStore` の通知メソッドを叩く。**新しい条件の入力源はここで購読を足す**
- **消費側**（`PlayerBulletParameterDataStore` 等）: `CalcMultiply(BuffEffectType.X)` を基礎値に掛ける。**新しい効果はここに配線しないと効かない**

## 主要ファイル早見表

| 役割 | パス |
|---|---|
| 発動条件enum（**GAS自動生成・手動編集禁止**） | `Assets/App/Script/Common/Data/BuffConditionType.cs` |
| 効果種別enum（**GAS自動生成・手動編集禁止**） | `Assets/App/Script/Common/Data/BuffEffectType.cs` |
| マスターデータ型（`BuffMasterData`, SO） | `Assets/App/Script/Common/Data/MasterData/BuffData.cs` |
| Database（`TryGetBuffMasterData`） | `Assets/App/Script/Common/Data/Database/BuffDatabase.cs` |
| CSVインポーター（Editor拡張） | `Assets/App/Script/Editor/BuffDataImporter.cs` |
| 実行時状態の中枢 | `Assets/App/Script/Battle/DataStore/BuffStateDataStore.cs`（`IBuffStateDataStore`） |
| 入力配線 | `Assets/App/Script/Battle/UseCase/BuffConditionUseCase.cs` |
| 効果の消費（攻撃力） | `Assets/App/Script/Battle/DataStore/PlayerBulletParameterDataStore.cs:145` |
| 起動（GrantBuff獲得時） | `Assets/App/Script/Battle/UseCase/ShopUseCase.cs`（`OnUpgradeSelected`） |
| DI登録 | `Assets/App/Script/Battle/BattleLifetimeScope.cs`（52行 `BuffStateDataStore` / 75行 `BuffConditionUseCase`） |
| マスターデータCSV | `Assets/App/MasterData/Origin/BuffData.csv` |
| 生成SOアセット / Databaseアセット | `Assets/App/MasterData/Buff/*.asset` / `Assets/App/MasterData/Database/BuffDatabase.asset` |

## CSVスキーマ（BuffData.csv）

**データ開始は Row2**（Row1=ヘッダー、インポーターが1行スキップ）。列順は `BuffDataImporter.cs` の `Col*` 定数が正。

```
0:id  1:NameKey  2:ConditionType  3:ConditionValue  4:Duration  5:EffectType  6:EffectValue
```
`MinColumnCount=7`。

- `ConditionValue`: 条件の閾値。`HitCount` → 必要ヒット数 / `HpBelow` → HP割合（0〜1）
- `Duration`: 効果時間（秒）。**0以下なら「条件成立中のみ有効」**（時限ではなく持続判定型）
- `EffectValue`: 倍率
- 現在のデータ: `$ComboAttackUpBuff`（HitCount=10, Duration=5, AttackPower×1.1）/ `$LowHealthAttackUpBuff`（HpBelow=0.3, Duration=0, AttackPower×1.5）
- `NameKey` は `$` + PascalCase。生成アセット名は `$` を除いたもの（例 `ComboAttackUpBuff.asset`）

## 発動条件（ConditionType）の実装状況

| 値 | 意味 | 実装 | 駆動入力 |
|---|---|---|---|
| `HitCount` | ヒット数N到達で Duration 秒発動 | ✅ | `NotifyHit()` ← `OnHit` |
| `HpBelow` | HP割合がN以下の間だけ有効 | ✅ | `SetHealthRatio()` ← Health/MaxHealth |
| `HpAbove` | HP割合がN以上 | ❌ enum定義のみ | 未接続 |
| `AfterDodge` | 回避直後N秒 | ❌ enum定義のみ | 未接続（回避イベント源が未購読） |

> ⚠️ enumにあっても `BuffState.IsActive` の switch と駆動入力が無ければ**永遠に非アクティブ**。アップグレードの「未接続タイプ問題」と同型の罠。新条件は必ず実装まで通すこと。

発動の挙動（`BuffStateDataStore`）: `HitCount` は到達で `RemainingTime = Duration` にリフレッシュ（**スタックしない**）。同一idの重複 `AddBuff` は無視。

## パターンA: 既存の条件×効果で新バフ（データのみ）

1. スプレッドシート `BuffData` シートに行追加（[`sheets-write`](../sheets-write/SKILL.md)）→ GASエクスポート
2. `Assets/App/MasterData/Origin/BuffData.csv` を追随（またはUnityメニュー `Tools/マスターデータ/BuffData ファイルコピー`）
3. Unityで `Tools/マスターデータ/BuffData CSVインポート` → `Buff/*.asset` 生成＋`BuffDatabase.asset` 更新
4. **バフをプレイヤーに届ける経路を用意**: 現状は `GrantBuff` アップグレード経由のみ。[`upgrade-add`](../upgrade-add/SKILL.md) のパターンCで `UpgradeData.csv` に `UpgradeType=GrantBuff, BuffId=このバフのid` の行を追加する
5. [`playtest-run`](../playtest-run/SKILL.md) で「獲得→条件成立→効果反映」を確認

## パターンB: 新しい発動条件（ConditionType）を追加

1. スプレッドシート `BuffConditionType` enumシートに要素追加 → GASで `BuffConditionType.cs` 再生成（手動編集禁止）
2. `BuffStateDataStore` を改修:
   - `BuffState.IsActive` の switch に新条件の case を追加
   - 必要なら `BuffState` に進行用フィールド（`HitCount` / `RemainingTime` / `IsConditionActive` 相当）を追加
   - 新条件を進行させる通知メソッドを追加（`NotifyHit` / `SetHealthRatio` に倣う）。`HpAbove` は `SetHealthRatio` に条件を足すだけで足りる場合もある
3. `BuffConditionUseCase.Initialize` で入力源をR3購読し、上記通知メソッドを呼ぶ（例: `AfterDodge` なら回避イベントのObservableを購読）。`IBuffStateDataStore` に新メソッドを足したらインターフェースも更新
4. `BuffData.csv` に新条件のバフ行を追加 → パターンAの手順でインポート＆配布
5. コンパイル（`uloop-compile`）→ `playtest-run` で検証

## パターンC: 新しい効果種別（EffectType）を追加

1. スプレッドシート `BuffEffectType` enumシートに要素追加 → GASで `BuffEffectType.cs` 再生成（手動編集禁止）
2. **消費側コードを実装**（このパターンの本体）: 効かせたい `Player*ParameterDataStore` に `IBuffStateDataStore` をDI注入し、基礎値に `CalcMultiply(BuffEffectType.新規)` を掛ける（`PlayerBulletParameterDataStore.cs:145` の攻撃力が手本）
   - **加算効果が必要な場合**は `BuffStateDataStore` に `CalcAdd` を新設し `IBuffStateDataStore` にも追加（現状は乗算のみ。`UpgradeEffectSimpleCalculatorDataStore.CalcAdd` が実装の参考）
3. `BuffData.csv` に新効果のバフ行を追加 → インポート＆配布
4. コンパイル → `playtest-run` で検証

## 既知の制約・ギャップ（着手前に確認）

- **入手経路が `GrantBuff` アップグレード経由のみ**。ドロップやウェーブ報酬など別経路が要るなら `BuffStateDataStore.AddBuff` を呼ぶ新しい起動点を作る
- **効果は乗算のみ・スタックしない**。同一バフ複数回や加算合成が要件なら `BuffStateDataStore` の合成ロジックを拡張する
- **`BuffStateDataStore.Reset()` は定義のみで呼び出し元が無い**。プレイサイクル境界での明示リセットは未実装（バトルシーン再ロードでSingletonが作り直されるため現状は実害が出にくい）。1プレイ中に明示リセットが要る仕様になったら呼び出しを足す
- ヒット判定はウェーブ間ポーズ中は除外される（`BuffConditionUseCase.OnHit`）
- enum系（`BuffConditionType.cs` / `BuffEffectType.cs`）は**GAS自動生成物**。手編集は次回エクスポートで消えるため必ずスプレッドシートへ反映
- コード変更は日本語コミット。Claudeの変更はPR運用が原則（developベース）
- **自動実行時のインポート**は `Tools/マスターデータ/BuffData CSVインポート` メニュー（`ExecuteMenuItem`）だと完了ダイアログでメインスレッドがブロックしuLoopがハングする。execute-dynamic-code から `App.Editor.BuffDataImporter.ImportData(false)` を呼ぶ（`interactive=false` でダイアログを出さずログ出力）。詳細は [`upgrade-add`](../upgrade-add/SKILL.md) の「自動実行の落とし穴」を参照

## 完了チェックリスト

- [ ] enum変更はスプレッドシート側にも反映したか（`*.cs` の手編集で終えていないか）
- [ ] 新条件を足したなら `BuffState.IsActive` の case ＋ `BuffConditionUseCase` の購読まで通したか
- [ ] 新効果を足したなら消費側で `CalcMultiply/CalcAdd` を呼んだか
- [ ] バフをプレイヤーに届ける経路（GrantBuffアップグレード等）を用意したか
- [ ] `BuffDatabase.asset` が配列更新されたか
- [ ] `uloop-compile` でエラーなし → `playtest-run` で条件成立時に効果が乗り、警告/例外が出ないか

## このスキルを拡張するタイミング

- 新しい `BuffConditionType` / `BuffEffectType` を接続したら「実装状況」テーブルと「実装済みの現状」を更新する
- 加算合成・スタック・別入手経路を実装したら「既知の制約・ギャップ」を更新する
- CSVスキーマやインポーターのメニュー名が変わったら該当節を直す
