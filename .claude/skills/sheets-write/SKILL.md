---
name: sheets-write
description: マスターデータのGoogleスプレッドシートをClaudeから読み書きする（列追加・enum追加・行追加・シート新設）。GAS→CSV→Unityインポートのパイプラインの上流を更新する際に使う。ローカルCSV/enumを先行変更した後のスプレッドシート追随もこのスキルで行う。
---

# sheets-write: スプレッドシート読み書きCLI

マスターデータの正であるGoogleスプレッドシートを、`Tools/Sheets/sheets-cli.mjs`（Node + Sheets API + サービスアカウント認証）で読み書きする。

## 前提ファイル

| ファイル | 役割 | コミット |
|---|---|---|
| `Tools/Sheets/sheets-cli.mjs` | CLI本体 | する |
| `Tools/Sheets/config.json` | `spreadsheetId`（スプレッドシートURLの `/d/` と `/edit` の間の文字列） | する |
| `Tools/Sheets/service-account.json` | サービスアカウントのJSONキー | **禁止**（gitignore済み） |

初回のみ `Tools/Sheets` で `npm install` が必要（`node_modules`はgitignore済み）。

## コマンド一覧

```powershell
cd Tools/Sheets
node sheets-cli.mjs list-sheets                                  # シート一覧
node sheets-cli.mjs get UpgradeData                              # 全体をTSV表示
node sheets-cli.mjs get UpgradeData A1:C5 --json                 # 範囲指定+JSON
node sheets-cli.mjs get UpgradeData E140:E149 --formula          # 計算結果ではなく数式そのものを表示（数式の無いセルは値のまま）
node sheets-cli.mjs set "UpgradeData!P2" '[[1]]'                 # セル更新（2次元配列。入力したままの値で書き込む）
node sheets-cli.mjs set "UpgradeData!E150" '[["=vlookup(D150,UpgradeType!$A:$C,2,false)"]]' --formula  # 数式として書き込む
node sheets-cli.mjs append-rows BuffData '[[50,"開発名称","$FooBuff",1,0,5,1,1.2]]'  # 行追加（シートの全列順。下の注意を参照）
node sheets-cli.mjs add-column UpgradeData "BuffId,string"       # Row1右端に列スキーマ追記
node sheets-cli.mjs create-sheet BuffData --schema '["id,int","NameKey,string"]'  # シート新設
node sheets-cli.mjs add-enum UpgradeType 9 "バフ付与" GrantBuff  # enumシートへ行追加
node sheets-cli.mjs rename-sheet ConditionType BuffConditionType # シート名変更
node sheets-cli.mjs delete-columns UpgradeData X:Z               # 列削除（単一なら "X"）
node sheets-cli.mjs delete-rows UpgradeData 104:106              # 行削除（単一なら "104"。行番号はシート表示と同じ1始まり）
node sheets-cli.mjs set-dropdown UpgradeTagData B "UpgradeTag!C5:C"  # 列（Row2以降）に参照範囲から選ぶプルダウンを設定（範囲外の手入力は拒否）
node sheets-cli.mjs get Upgrade --spreadsheet <ID>               # 全コマンド共通: config.json 以外のスプレッドシートを対象にする
```

## 対象スプレッドシート

| 用途 | ID | 指定方法 |
|---|---|---|
| マスターデータ（UpgradeData / BuffData / enum 等） | `config.json` の `spreadsheetId` | 指定不要（既定） |
| ローカライズ表（`Upgrade` シート = Localization の Upgrade テーブル） | `17Lvu9ivs8Ybta8rvmfJ86YtqnEoRLdBkhbkGu-7b7VE` | `--spreadsheet <ID>` |

- ローカライズ表の `Upgrade` シートは A=Key / B=ja-JP / C=en の3列。アップグレード1件につき `$Name` / `$Name_SimpleDesc` / `$Name_Desc` の3行、レベル表記は `$Level{n}_Upgrade`
- 説明文の `{value1}`〜`{value3}` はアップグレード自身の Value を埋め込むプレースホルダ（`UpgradeDescriptionFormatter`）。GrantBuff型は数値がバフ側にあるため使えない
- サービスアカウントを**編集者で共有**していないと 403 になる（マスターデータ側と同じ）

## シート構成の約束事（GASエクスポータと対応）

- **データシート**（UpgradeData等）: Row1=スキーマ定義行。各セルは「変数名,型」形式（例: `id,int` / `UpgradeType,ref@UpgradeType`）。`ref@シート名` はエクスポート時にそのenumシートの値でバリデーションされる。Row2以降がデータで、**A列が空の行で終端**（途中に空行を作らないこと）
- **プルダウン**: `UpgradeTagData` の Tag 列は `UpgradeTag!C5:C`（要素名。4行目の None は除く）のプルダウン。enum の要素名で書く列を新設したら `set-dropdown` で同じように設定する。入力規則が拒否するのは手入力だけで、API（`set` / `append-rows`）からの書き込みは素通りする点に注意
- **enumシート**（UpgradeType等）: **4行目から** A列=数値、B列=日本語コメント、C列=要素名。こちらも**A列が空で終端**
- エクスポート処理の実体はリポジトリ内 `Tools/GAS/UpgradeDataExporter.gs`（スプレッドシートへは手動コピペで反映する運用）

> ⚠️ **書き込みは既定で「入力したままの値」（RAW）**。`set` / `append-rows` は次のように振る舞う。
> - **数値は JSON の数値で渡す**（`48`）。`"48"` と文字列で渡すと文字列のまま入り、それを `vlookup` で参照する補助列が `#N/A` になる。
> - **`=` で始まる文字列も文字列として入る**。数式を入れたいときは `set ... --formula`（シートに手入力したのと同じ扱い＝USER_ENTERED）を使う。`--formula` では数値に見える文字列は数値に、日付に見える文字列は日付に変わるので、数式を入れる範囲だけに使う。`append-rows` に `--formula` は無い。
> - 数式を確かめるときは `get ... --formula`。既定の `get` は計算結果を表示する。

> ⚠️ **スプレッドシートの列数はCSVより多い**（GASエクスポート時に一部列が除外される）。`append-rows` はシートの全列順に値を並べる必要があり、**CSVの列順で渡すと列ずれする**。
> - `BuffData`: CSVの7列に対し、`id` と `NameKey` の間に人間用の `開発名称` 列がある（計8列）。
> - `UpgradeData`: CSVの17列に対し、`開発名称` 列と、enum/ref 列の**直後に表示名の補助列（数式）**がある。実測で計25列（A〜Y）。列構成は下の表のとおり。

### UpgradeData の列構成（実測: 2026-10-22）

| 列 | 中身 | 列 | 中身 |
|---|---|---|---|
| A | `id,int` | N | （補助）`=vlookup(M{行},ParameterType!$A:$C,2,false)` |
| B | `開発名称`（人間用・エクスポート対象外） | O | `Value3,float` |
| C | `NameKey,string` | P | `Value3ParameterType` |
| D | `UpgradeType,ref@UpgradeType` | Q | （補助）`=vlookup(P{行},ParameterType!$A:$C,2,false)` |
| E | （補助）`=vlookup(D{行},UpgradeType!$A:$C,2,false)` | R | `Value4,float` |
| F | `PlayerUnlockType,ref@PlayerUnlockType` | S | `Value4ParameterType` |
| G | （補助）`=vlookup(F{行},PlayerUnlockType!$A:$C,2,false)` | T | （補助）`=vlookup(S{行},ParameterType!$A:$C,2,false)` |
| H | `Level,int` | U | `Value5,float` |
| I | `Value1,float` | V | `Value5ParameterType` |
| J | `Value1ParameterType,ref@ParameterType` | W | （補助）`=vlookup(V{行},ParameterType!$A:$C,2,false)` |
| K | （補助）`=vlookup(J{行},ParameterType!$A:$C,2,false)` | X | `BuffId,string` |
| L | `Value2,float` | Y | `Cost,int` |
| M | `Value2ParameterType,ref@ParameterType` | | |

### UpgradeData に行を足す手順

補助列（数式）を上書きしないよう、`append-rows` ではなく**列のまとまりごとに `set` で書く**。

1. `get UpgradeData A{行}:D{行}` で最終行を探し、新しい行番号を決める（**行番号は `id + 1` ではない**。欠番があり、2026-10 時点では `行番号 = id - 2`。必ず読んで確かめる）
2. 値の列を `set` で書く（数値は JSON の数値で）: `A:D` / `F` / `H:J` / `L:M` / `O:P` / `R:S` / `U:V` / `X:Y`
   ```powershell
   node sheets-cli.mjs set "UpgradeData!A150:D150" '[[152,"開発名称","$Foo",52]]'
   node sheets-cli.mjs set "UpgradeData!F150" '[[0]]'
   node sheets-cli.mjs set "UpgradeData!H150:J150" '[[1,1.1,1]]'
   # …L:M / O:P / R:S / U:V / X:Y も同様
   ```
3. **補助列の数式を確かめて、空なら入れる**。G・K・N・Q・T・W は先の行まで数式が入っていることが多いが、**E列は行を足しても伸びず空になりやすい**。`get UpgradeData E{行}:W{行} --formula` で見て、空の列だけ上の表の数式を `set ... --formula` で入れる
   ```powershell
   node sheets-cli.mjs set "UpgradeData!E150:E152" '[["=vlookup(D150,UpgradeType!$A:$C,2,false)"],["=vlookup(D151,UpgradeType!$A:$C,2,false)"],["=vlookup(D152,UpgradeType!$A:$C,2,false)"]]' --formula
   ```
4. `get UpgradeData A{行}:Y{行}` で読み戻し、補助列に表示名（「ダメージアップ」「強化効果」など）が出ていること・`#N/A` が無いことを確かめる

## カラム追加時の定型フロー

ローカル（CSV/enum/インポータ）を先行変更した場合のスプレッドシート追随手順:

1. `add-column <シート> "<変数名,型>"` でRow1にスキーマ追記
2. 既存行に値が必要なら `set` で埋める（省略時は空=既定値扱いになるか、インポータ側の任意列対応を確認）
3. 新しい行は `append-rows`、enum値は `add-enum` で追加
4. `get` で読み戻し、リポジトリの `Assets/App/MasterData/Origin/*.csv` と一致することを確認
5. **ユーザー作業**: スプレッドシートのメニュー「マスターデータ」からCSV/enumをエクスポート → Unityの `Tools/マスターデータ/インポート` でエクスポートした `UpgradeData.csv` を選ぶ（同じフォルダの enum ファイルも一緒にコピーされる）。git diffが出なければ完全同期

## 初回セットアップ（ユーザー作業）

1. [GCPコンソール](https://console.cloud.google.com/)で新規プロジェクト作成（無料枠で十分）
2. 「APIとサービス」→「ライブラリ」→ **Google Sheets API** を有効化
3. 「APIとサービス」→「認証情報」→「認証情報を作成」→「サービスアカウント」（ロール付与は不要）
4. 作成したサービスアカウント → 「キー」タブ → 「鍵を追加」→ JSON → ダウンロードしたファイルを `Tools/Sheets/service-account.json` に配置
5. 対象スプレッドシートの「共有」に、サービスアカウントのメールアドレス（JSONの `client_email`）を**編集者**で追加
6. スプレッドシートURLのIDを `Tools/Sheets/config.json` の `spreadsheetId` に設定
7. `cd Tools/Sheets && npm install`

※ 会社Workspaceで「組織外への共有」が制限されている場合、サービスアカウントは組織外扱いになるため共有できないことがある。その場合は管理者に例外設定を依頼するか、Workspace内でサービスアカウントを作成する（それも不可ならOAuthクライアント方式への切替を検討。このスキルの改修が必要）。

## トラブルシュート

- **403 権限がありません** → スプレッドシートが `client_email` に編集者で共有されているか確認
- **404 見つかりません** → `config.json` の `spreadsheetId` を確認（URLの `/d/xxx/edit` の xxx 部分）
- **認証キーが見つかりません** → `service-account.json` の配置場所を確認（`Tools/Sheets/` 直下）
- **`Cannot find package 'googleapis'`** → `cd Tools/Sheets && npm install`

## このスキルを拡張するタイミング

- 新しい定型操作（行削除・列並べ替え等）が必要になったら `sheets-cli.mjs` にサブコマンドを追加し、本ファイルのコマンド一覧も更新する
- スプレッドシートの構成ルール（スキーマ形式・enumシート形式）が変わったら「シート構成の約束事」を更新する
- OAuth方式へ切り替えた場合はセットアップ手順を書き換える
