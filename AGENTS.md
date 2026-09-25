# RECON プロジェクト - Codex向け指示書

## Project Overview
This project involves a Unity C# codebase with Google Apps Script (GAS) for spreadsheet export and Unity Editor extensions for CSV import. When asked about CSV/data pipeline work, assume this full GAS → CSV → Unity Editor importer workflow.

## Architecture Conventions
This project uses a DataStore pattern for data and calculations. Do NOT create UseCase or Service classes for logic that belongs in DataStore helpers. When adding new calculation logic, first check existing DataStore classes and add helper/calculator classes within that pattern.

## Data Import/Export
When parsing CSV or spreadsheet data, always confirm the header/data start row with the user before implementing. Default assumption: data starts at row 4 unless specified otherwise.

## プロジェクト概要
- Unity製VR/XRゲーム（Meta Quest向け）
- クリーンアーキテクチャ採用
- VContainer（DI）、R3（Reactive Extensions）を使用

## やってほしいこと ✅

### コーディング規約
- **日本語優先**: コミットメッセージ、コメント、PR説明は日本語で記述
- **クリーンアーキテクチャ遵守**: レイヤー間の依存関係を正しく保つ
  - Views → Presenters → UseCase → Data → DataStore
  - インターフェースを介した疎結合を維持
- **using文の使用**: IDisposableなリソース（StreamWriter等）は必ずusing文で管理
- **例外処理**: ファイルI/O、ネットワーク処理には必ずtry-catch
- **null安全性**: nullable参照型を意識し、適切なnullチェック

### Git運用
- **ブランチ戦略**: `develop`から`feature/*`ブランチを作成
- **PRのベース**: developブランチ
- **コミット単位**: 機能単位で分割、わかりやすいメッセージ
- **Co-Authored-By**: Codexとの共同作業時は必ず追加


### リファクタリング優先度
1. **最優先**: バグ修正（左右反転、無限ループ、ファイルI/O等）
2. **高優先**: God Classの分割、マジックナンバーの外部化
3. **中優先**: 重複コード削減、パフォーマンス改善
4. **低優先**: 命名規則統一、デッドコード削除

### 設計方針
- **ScriptableObject活用**: ゲームバランス調整用パラメータは外部化
- **DI優先**: VContainerを活用し、直接インスタンス化を避ける
- **Reactive優先**: R3のObservableを活用したイベント駆動設計
- **テストしやすさ**: インターフェースを介し、モック可能な設計

## やってほしくないこと ❌

### 絶対にやらないこと
- **静的クラスの乱用**: グローバルステートは避ける（既存の`BasePlayerParameter`等は要リファクタリング）
- **God Classの作成**: 1クラス1責務を守る
- **レイヤー違反**: 下位レイヤーから上位レイヤーへの参照禁止
  - 例: DataStoreからPresenterへの参照は禁止
- **マジックナンバー**: 定数は必ず名前付き定数またはScriptableObjectに
- **using文なしのIDisposable**: StreamWriter、StreamReaderは必ずusingで囲む

### 避けてほしいこと
- **過度な最適化**: 計測なしのパフォーマンス改善
- **過度な抽象化**: YAGNI原則を守る（必要になるまで実装しない）
- **破壊的変更**: 既存のインターフェースは可能な限り維持
- **英語コメント**: コメントは日本語で（コードは英語）
- **DebugConfigの直接参照**: 依存性注入で設定を渡す（既存コードは要リファクタリング）

### コミット・PRに関して
- **説明なしの大規模変更**: 変更理由を必ず記載
- **複数機能の混在コミット**: 1コミット1機能
- **force push**: main/developへのforce pushは厳禁
- **自動フォーマット**: Unity標準のコードスタイルを維持

## Unity固有の注意事項

### Updateループ
- **重い処理をUpdateに書かない**: 毎フレーム実行される処理は最小限に
- **Camera.main**: 毎フレーム取得せずキャッシュ
- **FindObjectOfType**: Start時のみ、Updateでは絶対に使わない
- **アロケーション削減**: GetComponent結果はキャッシュ

### VR/XR開発
- **左右の明確化**: Left/Rightの処理は慎重に（反転バグに注意）
- **パフォーマンス**: VRは90fps必須、Update内の処理は特に注意
- **入力処理**: VR/PC両対応を意識（DebugConfigで分岐）

## プロジェクト構造

```
Assets/App/
├── Battle/          # バトルシステム
├── Common/          # 共通機能
│   ├── Data/        # データモデル
│   ├── DataStore/   # データ永続化・管理
│   ├── Interface/   # インターフェース定義
│   ├── Presenters/  # プレゼンテーション層
│   ├── UseCase/     # ビジネスロジック
│   └── Views/       # UI/表示層
└── Framework/       # フレームワーク・ユーティリティ
```

## 3Dモデル作業（Art/）

Blenderでの作業ファイルはGit LFSの容量制約があるため、追跡対象を絞っている。

### 置き場所のルール

- **工程フォルダ**（`Art/ElseIf_Blockout_20260922/<工程名>_<日付>/`）は作業場所であり、**Gitには入らない**
  - `.gitignore` で `.blend` / `.blend1` / `.jpg` / `__pycache__` を除外済み。今までどおり自由に使ってよい
- **`Art/Latest/`** に置いた `.blend` だけがGit（LFS）で追跡される
- 工程が一区切りついて「これが現時点の最新の原本」と言える `.blend` ができたら、`Art/Latest/` に**コピー**する

```powershell
Copy-Item "Art/ElseIf_Blockout_20260922/<工程>/<成果物>.blend" "Art/Latest/"
```

### 守ること

- **移動ではなくコピー**にする。工程フォルダの `.blend` はPythonスクリプトが絶対パスで参照しているため、移動すると作業スクリプトが壊れる
- **`Art/Latest/` には原則1本だけ**置く。新しい世代ができたら古いものは削除して置き換える
- ファイル名は変えてよい。`.gitignore` は `Art/Latest/` 配下の `.blend` を名前によらず追跡するので、ignoreの書き換えは不要
- レンダ画像（`.png`）・作業ログ（`.json`）・スクリプト（`.py` / `.ps1`）・レポート（`.md`）は工程フォルダのままGitに入る。特別な操作は要らない

### LFS容量に関する注意

GitHubのLFS無料枠は**ストレージ1GB・帯域1GB/月**で、現在570MB程度まで使っている。帯域はclone/fetchのたびに消費される。

- `.blend` の世代を `Art/Latest/` に溜めると枠を使い切る
- レビュー用のPNGもLFS対象（`.gitattributes` で `*.png` を追跡）。中間レンダを大量に残さないよう意識すること
- 過去世代を残したい場合はGit外のバックアップか、データパック購入をユーザーに相談する

詳細は `Art/Latest/README.md` を参照。

## 参考になる既存コード

### 良い例
- `CoreSkillUnlockDataStore.cs`: DIの良い例
- `ISaveDataStore.cs`: インターフェース設計の参考

### 要リファクタリング（参考にしない）
- `PlayerDataStore.cs`: God Class、責務分割が必要
- `BasePlayerParameter.cs`: 静的クラス、ScriptableObject化が必要


## その他

- **質問歓迎**: 不明点があれば実装前に質問してください
- **提案歓迎**: より良い設計があれば提案してください
- **段階的リファクタリング**: 一度にすべて変えず、段階的に改善
- **テスト**: 重要な処理には手動テストシナリオを提示してください