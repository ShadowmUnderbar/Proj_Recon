param(
    [string]$Probe = 'StreamerCameraShot',
    [int]$RunStartMaxIterations = 20
)

$root = $PSScriptRoot
. (Join-Path $root 'PlaytestCommon.ps1')

$probeFile = Join-Path (Join-Path $root 'Probes') "$Probe.ps1"
if (-not (Test-Path $probeFile)) {
    Write-Error "プローブ '$Probe' が見つかりません ($probeFile)。Tools/Playtest/Probes/ にファイルを追加してください。"
    exit 2
}
. (Resolve-Path $probeFile).Path

if (-not (Get-Command ProbeRun -ErrorAction SilentlyContinue)) {
    Write-Error "プローブファイル '$probeFile' が ProbeRun を定義していません"
    exit 2
}

$hasPrepare = [bool](Get-Command ProbePrepare -ErrorAction SilentlyContinue)
$hasCleanup = [bool](Get-Command ProbeCleanup -ErrorAction SilentlyContinue)

Write-Host "=== Effect probe: $Probe ==="

Invoke-Uloop -Command 'compile' -Params @{ 'wait-for-domain-reload' = 'true' } | Out-Null
Invoke-Uloop -Command 'clear-console' | Out-Null

$success = $true
$errorsFound = @()
$prepared = $false

try {
    # Play前の仕込み（設定フラグの有効化など）。Play中に変えても間に合わない設定はここで行う
    if ($hasPrepare) {
        Write-Host "--- ProbePrepare ---"
        ProbePrepare
        $prepared = $true
    }

    Invoke-Uloop -Command 'control-play-mode' -Params @{ action = 'Play' } | Out-Null
    Start-Sleep -Seconds 5

    # ラン開始のセット選択ゲートを解除し、通常のバトル状態にしてから観測する
    for ($i = 0; $i -lt $RunStartMaxIterations; $i++) {
        $wave = Get-WaveState
        if (-not $wave.isSelectingRunStart) { break }
        Resolve-RunStartIfSelecting -WaveState $wave | Out-Null
        Start-Sleep -Seconds 2
    }

    Write-Host "--- ProbeRun ---"
    Reset-ProbeChecks
    ProbeRun

    # 関数の戻り値が1要素の場合、PowerShellは配列をアンロールして中身（=ハッシュテーブル）を返す。
    # そのまま .Count を見るとハッシュテーブルのキー数になるため、呼び出し側で必ず @() で配列化する
    $failures = @(Get-ProbeFailures)
    if ($failures.Count -gt 0) {
        $success = $false
        Write-Host "検証NG: $($failures.Count) 件"
    }

    $newErrors = @(Get-NewErrors)
    if ($newErrors.Count -gt 0) {
        $errorsFound = $newErrors
        $success = $false
        Write-Host "エラーログを検出: $($newErrors.Count) 件"
    }
} catch {
    Write-Host "プローブ実行中に例外が発生しました: $_"
    $success = $false
    $errorsFound += [ordered]@{ Type = 'ProbeException'; Message = "$_" }
} finally {
    Invoke-Uloop -Command 'control-play-mode' -Params @{ action = 'Stop' } | Out-Null

    # 仕込んだ設定は失敗時も必ず戻す。戻し漏れは以降のテストランを汚染する
    if ($hasCleanup -and $prepared) {
        Write-Host "--- ProbeCleanup ---"
        try {
            ProbeCleanup
        } catch {
            Write-Host "ProbeCleanupに失敗しました（手動で設定を確認してください）: $_"
            $success = $false
        }
    }
}

$checks = @($Global:ProbeChecks)
$reportPath = Write-ProbeReport -Probe $Probe -Success $success -Checks $checks -Errors @($errorsFound)
Write-Host "レポート: $reportPath"

if ($success) {
    Write-Host "完了: 検証 $($checks.Count) 件すべてOK、エラーなし"
    exit 0
} else {
    Write-Host "失敗: 検証NG $(@(Get-ProbeFailures).Count) 件 / エラーログ $(@($errorsFound).Count) 件"
    exit 1
}
