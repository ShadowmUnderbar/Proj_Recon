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

    # Play Mode突入直後はドメインリロード＋uloopサーバー再起動で30秒以上応答しないことがある。
    # リトライ枠を計60秒確保する（5回×2秒では開始直後に使い切って失敗する実績あり）
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        $result = Invoke-Uloop -Command 'execute-dynamic-code' -Params @{ 'code-file' = $snippetPath }
        if ($result.Success) {
            return $result.Result | ConvertFrom-Json
        }
        Start-Sleep -Seconds 3
    }
    throw "ウェーブ状態の取得に失敗しました: $($result.ErrorMessage)"
}

# ShopCanvasはWorldSpaceUICanvasViewによって実行時にMainCamera配下へ再ペアレントされる（VR/PC両対応のWorld Space化）。
# そのためShopView(Clone)配下ではなくカメラ配下のパスを指定する必要がある
$Global:PlaytestShopUpgradeButtonPath = 'BattleLifetimeScope/Player(Clone)/Camera/MainCamera/ShopCanvas/Panel/UpgradeButtons/UpgradeButton0'
$Global:PlaytestShopNextWaveButtonPath = 'BattleLifetimeScope/Player(Clone)/Camera/MainCamera/ShopCanvas/Panel/NextWaveButton'

function Resolve-ShopIfOpen {
    param([Parameter(Mandatory)] $WaveState)
    if (-not $WaveState.isWavePause) { return $false }

    # フロー: アップグレードを1つ選択 → 「次のウェーブへ」を押す
    # 選択後は選んだボタンだけが非表示になり、他の候補は表示されたまま残る（ShopView.HideUpgradeButton(index)の動作）
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestShopUpgradeButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 300

    # 「次のウェーブへ」押下後、実際にポーズ解除されたことを確認できるまでリトライする。
    # 確認なしに進むと、遷移失敗時にランナーが同じショップ処理を繰り返して進行不能になる
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
            action = 'Click'; 'target-path' = $Global:PlaytestShopNextWaveButtonPath; 'bypass-raycast' = 'true'
        } | Out-Null
        Start-Sleep -Milliseconds 500

        $state = Get-WaveState
        if (-not $state.isWavePause) { return $true }
    }
    throw "ショップから次ウェーブへ遷移できませんでした（NextWaveButton押下後もisWavePause=trueのまま）"
}

$Global:PlaytestKnownIssuePatterns = @(
    'can only be called on an active agent that has been placed on a NavMesh'
)

function Get-NewErrors {
    # uloopがタイムアウト等で応答しなかった場合、$result.Logsは$nullになる。
    # $nullをエラー件数に数えるとゲーム側のエラーが無いのにランが失敗扱いになるため必ず除外する
    $found = @()

    $result = Invoke-Uloop -Command 'get-logs' -Params @{ 'log-type' = 'Error'; 'include-stack-trace' = 'true'; 'max-count' = '50' }
    $found += @($result.Logs | Where-Object { $null -ne $_ })

    foreach ($pattern in $Global:PlaytestKnownIssuePatterns) {
        $patternResult = Invoke-Uloop -Command 'get-logs' -Params @{ 'log-type' = 'All'; 'search-text' = $pattern; 'include-stack-trace' = 'true'; 'max-count' = '50' }
        $found += @($patternResult.Logs | Where-Object { $null -ne $_ })
    }

    return $found
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
