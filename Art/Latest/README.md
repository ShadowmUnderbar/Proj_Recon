# Art/Latest

Blenderの**最新の原本**を置くディレクトリ。`.blend` はGitで追跡しない（LFSも使わない）ので、原本はGit外でバックアップすること。

## 運用

- 工程フォルダ（`ElseIf_Blockout_20260922/<工程名>_<日付>/` など）は作業場所
  - スクリプトが絶対パスで参照しているため、工程フォルダのファイルは移動・改名しないこと
- 工程が一区切りついたら、その成果物を `Art/Latest/` に**コピー**して同名で上書きする
- `.blend` / `.blend1` は `.gitignore` でリポジトリ全体から除外している
- Gitに入れるのは生成スクリプト・README など、テキストで再現に必要なもの。Unity用のFBX・テクスチャは `Assets/` 側で管理する

## 追跡しないもの

- すべての `.blend` / `.blend1`（Art/Latest を含む）
- 参考画像の `.jpg`、`__pycache__/`
- `Renders/` フォルダ（確認用のプレビュー画像）と `*.log`。生成スクリプトから作り直せるため
- `Reference/` フォルダ（参考資料）
- `Export/` フォルダ（FBXやテクスチャの書き出し結果）。Unityへ取り込んだ後は `Assets/` 側で管理するため

## 履歴について

2026-10-01 以前に LFS でコミットした `.blend`（`ElseIf_Game_LOD0_Candidate.blend`、`ElseIf_MobileVR_HeadPart01.blend`）は過去のコミットに残っており、
GitHub の LFS ストレージからも自動では消えない。容量を完全に空けるには、リポジトリの LFS オブジェクト削除（GitHub のサポート依頼か、リポジトリの作り直し）が必要。
