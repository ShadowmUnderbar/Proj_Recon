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
var gameState = scope.Container.Resolve<IGameStateDataStore>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();
return $"{{\"currentWave\":{wave.CurrentWave.CurrentValue},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"kill\":{wave.KillCount.CurrentValue},\"isGameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"playerHealth\":{player.Health.Value},\"isSelectingRunStart\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()}}}";
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

# VrUiFollowCanvasViewはカメラへ再ペアレントせず、ワールド座標で遅延追従する（VR向けの酔い対策）。
# そのため各UIプレハブのインスタンス配下のパスを指定する
$Global:PlaytestShopUpgradeButtonPath = 'BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/UpgradeButtons/UpgradeButton0'
$Global:PlaytestShopNextWaveButtonPath = 'BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/NextWaveButton'

# ゲームオーバー画面のスロット0保存ボタン
$Global:PlaytestGameOverSlotButtonPath = 'BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/SlotButtons/SlotButton0'

function Invoke-GameOverSlotSave {
    # ゲームオーバー画面のスロット0保存ボタンを押し、アップグレードセット保存フローを疎通させる（ベストエフォート）
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestGameOverSlotButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 500
}

# ラン開始時のセット選択UIの「使わずに開始」ボタン
$Global:PlaytestRunStartButtonPath = 'BattleLifetimeScope/RunStartView(Clone)/RunStartCanvas/Panel/SlotButtons/StartWithoutLoadButton'

function Resolve-RunStartIfSelecting {
    # ラン開始のセット選択中なら「使わずに開始」を押してランを始める（プレイテストはセット読込せず開始）
    param([Parameter(Mandatory)] $WaveState)
    if (-not $WaveState.isSelectingRunStart) { return $false }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
            action = 'Click'; 'target-path' = $Global:PlaytestRunStartButtonPath; 'bypass-raycast' = 'true'
        } | Out-Null
        Start-Sleep -Milliseconds 500

        $state = Get-WaveState
        if (-not $state.isSelectingRunStart) { return $true }
    }
    throw "ラン開始のセット選択を解除できませんでした（使わずに開始ボタン押下後もisSelectingRunStart=trueのまま）"
}

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
        # 次ウェーブ再開直後に低HPで即死しゲームオーバーになると、ポーズが解けないまま残る。
        # これはショップ遷移の失敗ではないので、ゲームオーバーなら成功扱いで抜ける（呼び出し元がゲームオーバー終端を処理する）
        if ($state.isGameOver) { return $true }
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

# ===== 演出の数値検証（probe-effect.ps1 が使う） =====

function Invoke-UnityCode {
    # 任意のC#スニペットをUnity上で実行し、Result文字列を返す。
    # BOM付きで渡すと先頭のusingがCS1001等で壊れるため、必ずBOM無しUTF-8で書き出す
    param(
        [Parameter(Mandatory)] [string]$Snippet,
        [int]$MaxAttempts = 5
    )
    $snippetPath = Join-Path ([System.IO.Path]::GetTempPath()) 'playtest-probe-snippet.csx'
    [System.IO.File]::WriteAllText($snippetPath, $Snippet, (New-Object System.Text.UTF8Encoding($false)))

    $result = $null
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $result = Invoke-Uloop -Command 'execute-dynamic-code' -Params @{ 'code-file' = $snippetPath }
        if ($result.Success) { return $result.Result }
        Start-Sleep -Seconds 3
    }
    throw "スニペットの実行に失敗しました: $($result.ErrorMessage) $($result.DiagnosticsSummary)"
}

function Invoke-UnityJson {
    # スニペットがJSON文字列を返す前提でオブジェクト化する
    param([Parameter(Mandatory)] [string]$Snippet)
    return (Invoke-UnityCode -Snippet $Snippet) | ConvertFrom-Json
}

$Global:ProbeChecks = @()

function Reset-ProbeChecks {
    $Global:ProbeChecks = @()
}

function Assert-ProbeValue {
    # 数値を許容誤差付きで比較する。演出は補間が絡むため完全一致ではなく許容誤差で見る
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [double]$Actual,
        [Parameter(Mandatory)] [double]$Expected,
        [double]$Tolerance = 0.001
    )
    $diff = [Math]::Abs($Actual - $Expected)
    $passed = $diff -le $Tolerance
    $Global:ProbeChecks += [ordered]@{
        name = $Name; actual = $Actual; expected = $Expected; tolerance = $Tolerance
        diff = [Math]::Round($diff, 5); passed = $passed
    }
    $mark = if ($passed) { '  OK' } else { '  NG' }
    Write-Host "$mark $Name : actual=$Actual expected=$Expected (許容 $Tolerance / 差 $([Math]::Round($diff, 5)))"
    return $passed
}

function Assert-ProbeTrue {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [bool]$Condition,
        [string]$Detail = ''
    )
    $Global:ProbeChecks += [ordered]@{
        name = $Name; actual = $Condition; expected = $true; passed = $Condition; detail = $Detail
    }
    $mark = if ($Condition) { '  OK' } else { '  NG' }
    Write-Host "$mark $Name $Detail"
    return $Condition
}

function Get-ProbeFailures {
    return @($Global:ProbeChecks | Where-Object { -not $_.passed })
}

function Write-ProbeReport {
    param(
        [Parameter(Mandatory)] [string]$Probe,
        [Parameter(Mandatory)] [bool]$Success,
        [array]$Checks = @(),
        [array]$Errors = @()
    )
    $reportsDir = Join-Path $PSScriptRoot 'Reports'
    if (-not (Test-Path $reportsDir)) { New-Item -ItemType Directory -Path $reportsDir | Out-Null }

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $report = [ordered]@{
        timestamp = $timestamp
        probe     = $Probe
        success   = $Success
        checks    = $Checks
        errors    = $Errors
    }
    $reportPath = Join-Path $reportsDir "probe_$timestamp.json"
    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $reportPath -Encoding utf8

    $existing = Get-ChildItem -Path $reportsDir -Filter 'probe_*.json' | Sort-Object LastWriteTime
    $excess = $existing.Count - 10
    if ($excess -gt 0) {
        $existing | Select-Object -First $excess | Remove-Item -Force
    }
    return $reportPath
}

function Write-PlaytestReport {
    param(
        [Parameter(Mandatory)] [string]$Scenario,
        [Parameter(Mandatory)] [int]$TargetWaves,
        [Parameter(Mandatory)] [int]$ReachedWave,
        [Parameter(Mandatory)] [bool]$Success,
        [bool]$GameOver = $false,
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
        gameOver    = $GameOver
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
