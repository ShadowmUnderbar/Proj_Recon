# RECON プロジェクト - Claude向け指示書

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
- **Co-Authored-By**: Claudeとの共同作業時は必ず追加

#### PR必須（Claudeによるコード変更すべてが対象）
- Claudeによるコード変更は**必ず**featureブランチを作成しPRを出す。`develop`への直接pushは禁止
- 現在のブランチが`develop`なら、確認を取らずにブランチ作成→コミット→push→`gh pr create --base develop`まで一連で実行する
- **コミットメッセージの先頭に「Claude: 」を付ける**（例: `Claude: 経験値システム雛形を追加`）
- 既存の変更とスコープが大きく異なる内容は、別のfeatureブランチ・別PRに分離する。判断が微妙ならユーザーに確認する
- このルールは `.claude/settings.json` の PreToolUse フックで強制している。`main` / `master` / `develop` へのpushはハーネス側で拒否される（実体は `.claude/scripts/block-protected-push.sh`）。解除が必要な場合はユーザー自身がターミナルで実行する

#### PR前のセルフレビュー必須
- コード実装を伴う作業では**常に**、PRを出す前に`/code-review`を回し、妥当な指摘を反映してからPRを作成する
- 反映内容はPR本文にも記載する。機能追加・リファクタリング・バグ修正すべてが対象
- 進め方: 実装 → コンパイル → 実測検証 → `/code-review` → 指摘反映 → 再コンパイル/再検証 → コミット・PR
- データ値の妥当性（issue記載どおりだが不自然な値など）は勝手に変えず、PR本文やissueコメントで確認を促す


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
- **静的クラスの乱用**: グローバルステートは避ける（バランス値は`PlayerBaseParameterConfig`のようにScriptableObject化して注入する）
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

## 参考になる既存コード

### 良い例
- `CoreSkillUnlockDataStore.cs`: DIの良い例
- `ISaveDataStore.cs`: インターフェース設計の参考
- `UpgradeCardBoardView.cs` と `UpgradeCard*` 群: MonoBehaviour は生成・破棄と Inspector 値だけを持ち、計算・検索・状態機械を plain C# に分けた例

### 要リファクタリング（参考にしない）
- `DebugConfig` の直接参照: DI経由に置き換え中。新規コードでは静的参照を増やさない

### 全体像
- `Docs/Architecture.md`: シーン×レイヤー別の構成・データフロー・残課題。構成を変えたら更新する


## その他

- **質問歓迎**: 不明点があれば実装前に質問してください
- **提案歓迎**: より良い設計があれば提案してください
- **段階的リファクタリング**: 一度にすべて変えず、段階的に改善
- **テスト**: 重要な処理には手動テストシナリオを提示してください