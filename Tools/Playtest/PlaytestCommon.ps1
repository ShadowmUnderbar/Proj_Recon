function Invoke-Uloop {
    param(
        [Parameter(Mandatory)] [string]$Command,
        [hashtable]$Params = @{}
    )
    $cliArgs = @($Command)
    foreach ($key in $Params.Keys) {
        $cliArgs += "--$key"
        $cliArgs += "$($Params[$key])"
    }
    $raw = & uloop @cliArgs
    $json = $raw -join "`n"
    try {
        return $json | ConvertFrom-Json
    } catch {
        throw "uloop $Command の出力をJSONとして解釈できませんでした: $json"
    }
}

function Get-WaveState {
    $snippetPath = Join-Path ([System.IO.Path]::GetTempPath()) 'playtest-wave-state.csx'
    $snippet = @'
using VContainer;
using VContainer.Unity;
var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
return $"{{\"currentWave\":{wave.CurrentWave.CurrentValue},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"kill\":{wave.KillCount.CurrentValue}}}";
'@
    [System.IO.File]::WriteAllText($snippetPath, $snippet, (New-Object System.Text.UTF8Encoding($false)))

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $result = Invoke-Uloop -Command 'execute-dynamic-code' -Params @{ 'code-file' = $snippetPath }
        if ($result.Success) {
            return $result.Result | ConvertFrom-Json
        }
        Start-Sleep -Seconds 2
    }
    throw "ウェーブ状態の取得に失敗しました: $($result.ErrorMessage)"
}

$Global:PlaytestShopUpgradeButtonPath = 'BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/UpgradeButtons/UpgradeButton0'
$Global:PlaytestShopNextWaveButtonPath = 'BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/NextWaveButton'

function Resolve-ShopIfOpen {
    param([Parameter(Mandatory)] $WaveState)
    if (-not $WaveState.isWavePause) { return $false }

    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestShopUpgradeButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 300
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestShopNextWaveButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    return $true
}

function Get-NewErrors {
    $result = Invoke-Uloop -Command 'get-logs' -Params @{ 'log-type' = 'Error'; 'include-stack-trace' = 'true'; 'max-count' = '50' }
    return @($result.Logs)
}

function Write-PlaytestReport {
    param(
        [Parameter(Mandatory)] [string]$Scenario,
        [Parameter(Mandatory)] [int]$TargetWaves,
        [Parameter(Mandatory)] [int]$ReachedWave,
        [Parameter(Mandatory)] [bool]$Success,
        [array]$Errors = @()
    )
    $reportsDir = Join-Path $PSScriptRoot 'Reports'
    if (-not (Test-Path $reportsDir)) { New-Item -ItemType Directory -Path $reportsDir | Out-Null }

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $report = [ordered]@{
        timestamp   = $timestamp
        scenario    = $Scenario
        targetWaves = $TargetWaves
        reachedWave = $ReachedWave
        success     = $Success
        errors      = $Errors
    }
    $reportPath = Join-Path $reportsDir "playtest_$timestamp.json"
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportPath -Encoding utf8

    $existing = Get-ChildItem -Path $reportsDir -Filter 'playtest_*.json' | Sort-Object LastWriteTime
    $excess = $existing.Count - 10
    if ($excess -gt 0) {
        $existing | Select-Object -First $excess | Remove-Item -Force
    }
    return $reportPath
}
