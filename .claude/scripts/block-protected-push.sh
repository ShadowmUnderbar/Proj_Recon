#!/usr/bin/env bash
# PreToolUse(Bash) フックの入口。
#
# 設定側の "if" で Bash(git push:*) に絞ると、
# 「git add -A && git push origin develop」のような複合コマンドが
# 前方一致せずフックを素通りしてしまう。
# そのため全Bashコマンドを対象にし、ここで安く振るい落とす。
# push という文字列を含まない大半のコマンドは、python を起動せずに抜ける。

set -uo pipefail

payload="$(cat)"

case "$payload" in
    *push*) ;;
    *) exit 0 ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
printf '%s' "$payload" | python "$script_dir/block_protected_push.py"
