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
node sheets-cli.mjs set "UpgradeData!P2" '[["1"]]'               # セル更新（2次元配列）
node sheets-cli.mjs append-rows UpgradeData '[[13,"$Foo",9,0,1,0,0,0,0,0,0,0,0,0,0,"1"]]'  # 行追加
node sheets-cli.mjs add-column UpgradeData "BuffId,string"       # Row1右端に列スキーマ追記
node sheets-cli.mjs create-sheet BuffData --schema '["id,int","NameKey,string"]'  # シート新設
node sheets-cli.mjs add-enum UpgradeType 9 "バフ付与" GrantBuff  # enumシートへ行追加
node sheets-cli.mjs rename-sheet ConditionType BuffConditionType # シート名変更
node sheets-cli.mjs delete-columns UpgradeData X:Z               # 列削除（単一なら "X"）
node sheets-cli.mjs delete-rows UpgradeData 104:106              # 行削除（単一なら "104"。行番号はシート表示と同じ1始まり）
```

## シート構成の約束事（GASエクスポータと対応）

- **データシート**（UpgradeData等）: Row1=スキーマ定義行。各セルは「変数名,型」形式（例: `id,int` / `UpgradeType,ref@UpgradeType`）。`ref@シート名` はエクスポート時にそのenumシートの値でバリデーションされる。Row2以降がデータで、**A列が空の行で終端**（途中に空行を作らないこと）
- **enumシート**（UpgradeType等）: **4行目から** A列=数値、B列=日本語コメント、C列=要素名。こちらも**A列が空で終端**
- エクスポート処理の実体はリポジトリ内 `Tools/GAS/UpgradeDataExporter.gs`（スプレッドシートへは手動コピペで反映する運用）

> ⚠️ **スプレッドシートの列数はCSVより多い**（GASエクスポート時に一部列が除外される）。`append-rows` はシートの全列順に値を並べる必要があり、**CSVの列順で渡すと列ずれする**。
> - `BuffData`: CSVの7列に対し、`id` と `NameKey` の間に人間用の `開発名称` 列がある（計8列）。
> - `UpgradeData`: CSVの16列に対し、`開発名称` 列に加え、各 enum/ref 列の**直後に表示名の補助列**が挿入されている（例: `UpgradeType` の右に「ダメージアップ」）。実測で計24列（A〜X、末尾Xが `BuffId`）。補助列は手動運用で、既存のGrantBuff行では空欄。
> - 安全な手順: **既存の同種行を `get <シート> A{行}:X{行} --json` で読み、それをテンプレに全列並べて `append-rows`**（補助列は空文字でよい）。追記後は `get` で読み戻して列ずれが無いか必ず確認する。

## カラム追加時の定型フロー

ローカル（CSV/enum/インポータ）を先行変更した場合のスプレッドシート追随手順:

1. `add-column <シート> "<変数名,型>"` でRow1にスキーマ追記
2. 既存行に値が必要なら `set` で埋める（省略時は空=既定値扱いになるか、インポータ側の任意列対応を確認）
3. 新しい行は `append-rows`、enum値は `add-enum` で追加
4. `get` で読み戻し、リポジトリの `Assets/App/MasterData/Origin/*.csv` と一致することを確認
5. **ユーザー作業**: スプレッドシートのメニュー「マスターデータ」からCSV/enumをエクスポート → Unityの `Tools/マスターデータ/UpgradeData ファイルコピー` → `CSVインポート`。git diffが出なければ完全同期

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
