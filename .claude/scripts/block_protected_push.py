"""保護ブランチ（main / master / develop）への git push を止める PreToolUse フック。

CLAUDE.md の「PR必須」ルールをハーネス側で強制するためのもの。
Claudeが読み落としても、このフックが実際のpushを止める。

標準入力でフックのペイロードJSONを受け取り、ブロックする場合だけ
permissionDecision=deny のJSONを標準出力へ返す。許可する場合は何も出さない。

判定できない場合は通す（fail open）。フックの不具合で正当なpushまで
止まると作業が止まってしまうため。ただし理由をstderrへ出す。
"""

import json
import os
import re
import shlex
import subprocess
import sys

PROTECTED_BRANCHES = {"main", "master", "develop"}

# git 本体のオプションのうち、直後に値を取るもの
GIT_GLOBAL_OPTIONS_WITH_VALUE = {
    "-C", "-c", "--git-dir", "--work-tree", "--namespace", "--exec-path",
}

# git push のオプションのうち、直後に値を取るもの
PUSH_OPTIONS_WITH_VALUE = {
    "-o", "--push-option", "--repo", "--receive-pack", "--exec",
}

# シェルの区切り。1コマンド内に複数のgitが並ぶ場合に分割する
SHELL_SEPARATORS = re.compile(r"&&|\|\||[;|\n]")


def find_git_invocations(command):
    """コマンド文字列から git のサブコマンドと引数の組を取り出す"""
    invocations = []

    for segment in SHELL_SEPARATORS.split(command):
        segment = segment.strip()
        if not segment:
            continue

        try:
            tokens = shlex.split(segment)
        except ValueError:
            # 閉じていないクォートなど。判定できないので飛ばす
            continue

        if not tokens or os.path.basename(tokens[0]) not in ("git", "git.exe"):
            continue

        index = 1
        work_tree = None
        while index < len(tokens):
            token = tokens[index]
            if token in GIT_GLOBAL_OPTIONS_WITH_VALUE:
                if token == "-C" and index + 1 < len(tokens):
                    work_tree = tokens[index + 1]
                index += 2
                continue
            if token.startswith("-"):
                index += 1
                continue
            invocations.append((token, tokens[index + 1:], work_tree))
            break

    return invocations


def push_destinations(args):
    """git push の引数から、宛先ブランチ名の一覧を返す。

    明示されていない場合は None を返す（呼び出し側で現在のブランチを見る）。
    """
    positionals = []
    index = 0
    while index < len(args):
        arg = args[index]
        if arg in PUSH_OPTIONS_WITH_VALUE:
            index += 2
            continue
        if arg.startswith("-"):
            index += 1
            continue
        positionals.append(arg)
        index += 1

    # 先頭はリモート名。残りがrefspec
    refspecs = positionals[1:]
    if not refspecs:
        return None

    destinations = []
    for refspec in refspecs:
        # "HEAD:develop" や ":develop"（削除）は : の後ろが宛先
        destination = refspec.split(":")[-1]
        destination = re.sub(r"^refs/heads/", "", destination)
        if destination:
            destinations.append(destination)
    return destinations or None


def current_branch(work_dir):
    try:
        result = subprocess.run(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            cwd=work_dir,
            capture_output=True,
            text=True,
            timeout=10,
        )
    except (OSError, subprocess.SubprocessError):
        return None

    if result.returncode != 0:
        return None
    return result.stdout.strip() or None


def write_utf8(stream, text):
    """UTF-8で書き出す。

    Windowsの既定ではstdoutがcp932になり、日本語の理由文が文字化けして
    ユーザーに届く。バイト列として直接書くことで環境に依存させない。
    """
    stream.buffer.write(text.encode("utf-8") + b"\n")
    stream.buffer.flush()


def deny(reason):
    write_utf8(sys.stdout, json.dumps({
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": reason,
        }
    }, ensure_ascii=False))


def main():
    try:
        payload = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        write_utf8(sys.stderr, "[block_protected_push] ペイロードを読めなかったため素通しした")
        return 0

    command = (payload.get("tool_input") or {}).get("command") or ""
    if "push" not in command:
        return 0

    base_dir = payload.get("cwd") or os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()

    for subcommand, args, work_tree in find_git_invocations(command):
        if subcommand != "push":
            continue

        destinations = push_destinations(args)
        if destinations is None:
            branch = current_branch(work_tree or base_dir)
            if branch is None:
                write_utf8(sys.stderr, "[block_protected_push] 現在のブランチを取得できず素通しした")
                continue
            destinations = [branch]
            source = "現在のブランチ"
        else:
            source = "コマンドで指定された宛先"

        blocked = [d for d in destinations if d in PROTECTED_BRANCHES]
        if blocked:
            deny(
                f"{'/ '.join(blocked)} への push は禁止されています（{source}）。"
                "CLAUDE.mdの「Git運用 > PR必須」により、変更はfeatureブランチへpushして "
                "gh pr create --base develop でPRを出してください。"
                "どうしても直接pushが必要な場合は、ユーザー自身がターミナルで実行してください。"
            )
            return 0

    return 0


if __name__ == "__main__":
    sys.exit(main())
