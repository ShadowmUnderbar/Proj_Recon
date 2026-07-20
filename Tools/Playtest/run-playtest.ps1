param(
    [string]$Scenario = 'FixedFlow',
    [int]$Waves = 3,
    [int]$PollIntervalSeconds = 3,
    [int]$MaxIterations = 300
)

$root = $PSScriptRoot
. (Join-Path $root 'PlaytestCommon.ps1')

$scenarioFile = Join-Path (Join-Path $root 'Scenarios') "$Scenario.ps1"
if (-not (Test-Path $scenarioFile)) {
    Write-Error "シナリオ '$Scenario' が見つかりません ($scenarioFile)。Tools/Playtest/Scenarios/ にファイルを追加してください。"
    exit 2
}
. (Resolve-Path $scenarioFile).Path

if (-not (Get-Command PlaytestScenarioStep -ErrorAction SilentlyContinue)) {
    Write-Error "シナリオファイル '$scenarioFile' が PlaytestScenarioStep を定義していません"
    exit 2
}

Write-Host "=== Playtest run: scenario=$Scenario, targetWaves=$Waves ==="

Invoke-Uloop -Command 'compile' -Params @{ 'wait-for-domain-reload' = 'true' } | Out-Null
Invoke-Uloop -Command 'clear-console' | Out-Null
Invoke-Uloop -Command 'control-play-mode' -Params @{ action = 'Play' } | Out-Null
Start-Sleep -Seconds 5

$errorsFound = @()
$reachedWave = 1
$success = $true
$gameOver = $false

try {
    for ($i = 0; $i -lt $MaxIterations; $i++) {
        $wave = Get-WaveState
        $reachedWave = $wave.currentWave

        # ゲームオーバー（HP0）はランの正常な終端。スロット保存フローを疎通させて終了する
        if ($wave.isGameOver) {
            $gameOver = $true
            Write-Host "ゲームオーバーを検出（Wave $reachedWave, HP $($wave.playerHealth)）。スロット保存を実行して終了します"
            Invoke-GameOverSlotSave
            $newErrors = Get-NewErrors
            if ($newErrors.Count -gt 0) {
                $errorsFound = $newErrors
                $success = $false
            }
            break
        }

        if ($reachedWave -gt $Waves -and $wave.isWavePause) {
            Write-Host "目標ウェーブ数($Waves)をクリア、終了します"
            break
        }

        if ($wave.isWavePause) {
            Resolve-ShopIfOpen -WaveState $wave | Out-Null
        } else {
            PlaytestScenarioStep
        }

        $newErrors = Get-NewErrors
        if ($newErrors.Count -gt 0) {
            $errorsFound = $newErrors
            $success = $false
            Write-Host "エラーを検出、テストランを中断します"
            break
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    }
} finally {
    Invoke-Uloop -Command 'control-play-mode' -Params @{ action = 'Stop' } | Out-Null
}

$reportPath = Write-PlaytestReport -Scenario $Scenario -TargetWaves $Waves -ReachedWave $reachedWave -Success $success -GameOver $gameOver -Errors $errorsFound
Write-Host "レポート: $reportPath"

if ($success) {
    $terminal = if ($gameOver) { "ゲームオーバーで終了" } else { "ウェーブ $reachedWave まで到達" }
    Write-Host "完了: $terminal、エラーなし"
    exit 0
} else {
    Write-Host "失敗: ウェーブ $reachedWave で $($errorsFound.Count) 件のエラーを検出"
    exit 1
}
