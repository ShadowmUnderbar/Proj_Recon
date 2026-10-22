#
# Unity エディタを開いたまま git のブランチを切り替える。
#
# 開いているシーン（Battle.unity など）がブランチの切り替えでディスク上で書き換わると、
# Unity は「シーンが外部で変更されました。再読み込みしますか」というモーダルダイアログを出す。
# ダイアログの間はメインスレッドが止まり、uloop の要求がすべてタイムアウトする。
#
# そこで、切り替える前に開いているシーンを控えて Boot シーンへ移り、切り替えてから元のシーンを開き直す。
# 書き換わるファイルを Unity が開いていないので、ダイアログの条件そのものが成立しない。
# Boot.unity 自体が切り替え元と切り替え先で違うときは、大きな変更の可能性があるので切り替えずに止める（終了コード 2）。
# ユーザーに確認したうえで進めるときだけ -AllowBootSceneChange を付け、ディスクに無い空のシーンへ移って切り替える。
#
# 使い方（プロジェクトのルートで）:
#   & "Tools/Git/Switch-Branch.ps1" -Branch feature/foo                              # 既存のブランチへ
#   & "Tools/Git/Switch-Branch.ps1" -Branch feature/foo -Create -StartPoint origin/develop  # 作って切り替える
#   & "Tools/Git/Switch-Branch.ps1" -Branch feature/foo -AllowBootSceneChange       # Boot.unity が違うと確認を取ったうえで切り替える
#
# Unity が起動していなければ、ふつうに git switch するだけ。
# 次の場合は切り替えずに止める（終了コード 1）:
#   - Play モード中
#   - 開いているシーンに未保存の変更がある（Boot へ移ると変更が消えるため）
#   - git switch が失敗した（元のシーンへ戻してから止める）
# Boot.unity がブランチ間で違うときは、何もせずに止める（終了コード 2）
#
# 注意: このファイルは BOM 付き UTF-8 で保存すること（Windows PowerShell 5.1 が日本語を誤読するため。Tools/Playtest と同じ）
#

param(
    [Parameter(Mandatory)] [string]$Branch,
    [switch]$Create,
    [string]$StartPoint = '',
    # Boot.unity がブランチ間で違っても切り替える（ユーザーに確認を取ったときだけ付ける）
    [switch]$AllowBootSceneChange
)

# git は進捗を標準エラーへ出すので、Stop にすると Windows PowerShell 5.1 ではそれだけで例外になる
$ErrorActionPreference = 'Continue'

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
. (Join-Path $ProjectRoot 'Tools/Playtest/PlaytestCommon.ps1')

$BootScenePath = 'Assets/Scenes/Boot.unity'

function Test-UnityEditorRunning {
    # 起動中のエディタは Temp/UnityLockfile を開いたまま持っている。
    # クラッシュで残ったロックファイルは開けるので、開けるかどうかで見分ける
    $lockPath = Join-Path $ProjectRoot 'Temp/UnityLockfile'
    if (-not (Test-Path $lockPath)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None')
        $stream.Close()
        return $false
    } catch {
        return $true
    }
}

function Invoke-GitSwitch {
    $gitArgs = @('switch')
    if ($Create) { $gitArgs += '-c' }
    $gitArgs += $Branch
    if ($Create -and $StartPoint) { $gitArgs += $StartPoint }
    # 出力を関数の戻り値に混ぜないよう、画面へ流してから終了コードだけ返す
    & git -C $ProjectRoot @gitArgs 2>&1 | Out-Host
    return $LASTEXITCODE
}

function Get-SwitchTarget {
    # 切り替え先のコミット。作るときは起点（省略時は今の HEAD）、既存なら そのブランチ
    if ($Create) {
        if ($StartPoint) { return $StartPoint }
        return 'HEAD'
    }
    return $Branch
}

function Test-BootSceneUnchanged {
    # 切り替え元と切り替え先で Boot.unity が同じなら、Boot を開いたまま切り替えてもダイアログは出ない
    & git -C $ProjectRoot diff --quiet HEAD (Get-SwitchTarget) -- $BootScenePath 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Restore-Scenes {
    param([string[]]$ScenePaths, [string]$ActiveScenePath)
    if (-not $ScenePaths -or $ScenePaths.Count -eq 0) { return }

    $joined = ($ScenePaths -join '|')
    $result = Invoke-UnityCode -Snippet (@'
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var paths = "__PATHS__".Split('|');
var active = "__ACTIVE__";
var skipped = new StringBuilder();
var opened = 0;
foreach (var path in paths)
{
    // 切り替え先のブランチに無いシーンは開けないので飛ばす
    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
    {
        skipped.Append(path).Append(' ');
        continue;
    }

    EditorSceneManager.OpenScene(path, opened == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
    opened++;
}

var activeScene = SceneManager.GetSceneByPath(active);
if (activeScene.IsValid() && activeScene.isLoaded)
{
    SceneManager.SetActiveScene(activeScene);
}

return opened + " 個のシーンを開き直しました" + (skipped.Length > 0 ? "（切り替え先に無いため開けなかったもの: " + skipped + "）" : "");
'@).Replace('__PATHS__', $joined).Replace('__ACTIVE__', $ActiveScenePath)
    Write-Host $result
}

# 切り替え先が無ければ、Boot.unity の比較もできないので先に止める（打ち間違いを Boot の差分と取り違えないため）
& git -C $ProjectRoot rev-parse --verify --quiet "$(Get-SwitchTarget)^{commit}" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "切り替えを中止しました: 切り替え先 $(Get-SwitchTarget) が見つかりません"
    exit 1
}

if (-not (Test-UnityEditorRunning)) {
    Write-Host 'Unity エディタが起動していないので、そのまま git switch します'
    exit (Invoke-GitSwitch)
}

$useBoot = Test-BootSceneUnchanged
if (-not $useBoot -and -not $AllowBootSceneChange) {
    Write-Host "切り替えを中止しました: Boot.unity が今のブランチと $(Get-SwitchTarget) で違います。"
    Write-Host '起動まわりの大きな変更の可能性があるので、ユーザーに確認してください。進めてよければ -AllowBootSceneChange を付けて実行し直します'
    exit 2
}
$parking = if ($useBoot) { 'Boot' } else { '空のシーン' }

# 開いているシーンを控え、退避先のシーンへ移る。
# すでにダイアログなどで uloop が止まっていると、リトライのたびに長く待つので1回で諦める
$prepared = Invoke-UnityCode -MaxAttempts 1 -Snippet (@'
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

if (EditorApplication.isPlaying)
{
    return "error:Play モード中です。止めてから切り替えてください";
}

var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
var dirty = scenes.Where(s => s.isDirty).Select(s => string.IsNullOrEmpty(s.path) ? "(無題のシーン)" : s.path).ToList();
if (dirty.Count > 0)
{
    return "error:未保存の変更があるシーンがあります。保存してから切り替えてください: " + string.Join(", ", dirty);
}

var paths = scenes.Where(s => !string.IsNullOrEmpty(s.path)).Select(s => s.path).ToList();
var active = SceneManager.GetActiveScene().path;

if (__USE_BOOT__)
{
    EditorSceneManager.OpenScene("__BOOT__", OpenSceneMode.Single);
}
else
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
}

return "ok:" + active + ":" + string.Join("|", paths);
'@).Replace('__USE_BOOT__', $useBoot.ToString().ToLower()).Replace('__BOOT__', $BootScenePath)

if ($prepared.StartsWith('error:')) {
    Write-Host "切り替えを中止しました: $($prepared.Substring(6))"
    exit 1
}

# "ok:<アクティブなシーン>:<開いていたシーン|...>"（パスに ':' は入らない前提）
$parts = $prepared.Substring(3).Split(@(':'), 2)
$activeScenePath = $parts[0]
$scenePaths = @($parts[1].Split('|') | Where-Object { $_ })
Write-Host "開いていたシーンを控えて $parking へ移りました: $($scenePaths -join ', ')"

$exitCode = Invoke-GitSwitch
if ($exitCode -ne 0) {
    Write-Host "git switch に失敗したので、元のシーンへ戻します（終了コード $exitCode）"
    Restore-Scenes -ScenePaths $scenePaths -ActiveScenePath $activeScenePath
    exit 1
}

# 切り替え後のファイルを取り込み、スクリプトの変更があればコンパイルを待ってから開き直す。
# コンパイルの待ちが失敗しても（タイムアウトなど）、シーンを開き直すところまでは進める
try {
    $compile = Invoke-Uloop -Command 'compile' -Params @{ 'wait-for-domain-reload' = 'true' }
    if (-not $compile.Success) {
        Write-Host "コンパイルエラーがあります（$($compile.ErrorCount) 件）。シーンは開き直します"
    }
} catch {
    Write-Host "コンパイルの完了を確かめられませんでした: $($_.Exception.Message)"
}

try {
    Restore-Scenes -ScenePaths $scenePaths -ActiveScenePath $activeScenePath
} catch {
    Write-Host "ブランチは切り替えましたが、シーンを開き直せませんでした（$($_.Exception.Message)）。開いていたシーン: $($scenePaths -join ', ')"
    exit 1
}
Write-Host "ブランチを切り替えました: $Branch"
exit 0
