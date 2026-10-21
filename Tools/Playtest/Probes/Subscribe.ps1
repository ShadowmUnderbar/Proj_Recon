#
# サブスク（SubscribeDataStore / SubscribeUseCase）を確かめるプローブ。
#
# ウェーブクリア時（次のウェーブへ進んでショップが開くとき）に、所持キャッシュを Value1 倍にする（増えた分の端数は切り捨て）。最高レベルのみ採用。
# デバッグ対戦はウェーブを進めないので使わず、通常のバトルで制限時間を満たして実際の進行経路（AdvanceWaveInternal → OnWaveAdvanced）を通す。
# ボスウェーブはボスを倒すまで進まないので、その手前のショップまでで確かめる。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:SubscribePrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var points = scope.Container.Resolve<IPointDataStore>();
var subscribe = scope.Container.Resolve<ISubscribeDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData Subscribe(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Subscribe && u.Level == level);

// 所持キャッシュをちょうど value にする
void SetPoint(int value)
{
    points.TrySpend(points.CurrentPoint.CurrentValue);
    points.Add(value);
}
'@

function Invoke-SubscribeSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    $r = Invoke-BossSnippet -Body ($Global:SubscribePrelude + "`n" + $Body)
    if ($r.error) { throw $r.error }
    return $r
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

# 所持キャッシュを point にしてから制限時間を満たし、ウェーブクリア後（ショップが開いた時点）の所持キャッシュを返す
function Invoke-WaveClear {
    param([int]$Point)
    $before = Get-WaveState
    if ($before.isWavePause) { Skip-BossProbeShop }
    $before = Get-WaveState

    Invoke-SubscribeSnippet -Body @"
player.Health.Value = player.MaxHealth.Value;
SetPoint($Point);
wave.AddElapsedTime(9999f);
return "{}";
"@ | Out-Null

    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 300
        $state = Get-WaveState
        if ($state.isWavePause -and $state.currentWave -gt $before.currentWave) { break }
    }
    Assert-ProbeTrue -Name "ウェーブ$($before.currentWave)をクリアしてショップが開いた" -Condition ($state.isWavePause -and $state.currentWave -gt $before.currentWave) -Detail "wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null

    $r = Invoke-SubscribeSnippet -Body @'
return $"{{\"point\":{points.CurrentPoint.CurrentValue}}}";
'@
    return [double]$r.point
}

function ProbeRun {
    Start-Sleep -Seconds 3
    # ビルド選択が出ていれば何も持たずに始める
    Resolve-RunStartIfSelecting -WaveState (Get-WaveState) | Out-Null
    Start-Sleep -Seconds 1

    $r = Invoke-SubscribeSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var boss = Enumerable.Range(1, 99).First(w => config.IsBossWave(w));
return $"{{\"boss\":{boss},\"wave\":{wave.CurrentWave.CurrentValue},\"v1\":{F(Subscribe(1).Value1.value)},\"v3\":{F(Subscribe(3).Value1.value)}}}";
'@
    Write-Host "  現在ウェーブ=$($r.wave) ボスウェーブ=$($r.boss) L1=$($r.v1)倍 L3=$($r.v3)倍"
    Assert-ProbeTrue -Name 'ボスウェーブまでに4回ウェーブをクリアできる' -Condition ([int]$r.boss - [int]$r.wave -ge 4) -Detail "現在=$($r.wave) ボス=$($r.boss)" | Out-Null
    $l1 = [double]$r.v1
    $l3 = [double]$r.v3

    # --- 未所持: 増えない ---
    $point = Invoke-WaveClear -Point 100
    Assert-Near -Name '未所持ではウェーブクリアで所持キャッシュが増えない' -Actual $point -Expected 100

    # --- L1: Value1 倍 ---
    Invoke-SubscribeSnippet -Body 'session.AddUpgrade(Subscribe(1)); return "{}";' | Out-Null
    $point = Invoke-WaveClear -Point 100
    Assert-Near -Name 'L1: ウェーブクリアで所持キャッシュが Value1 倍になる（100）' -Actual $point -Expected ([math]::Floor(100 * $l1 + 0.001))

    # --- L3（L1 と同時所持）: 最高レベルの値だけ ---
    Invoke-SubscribeSnippet -Body 'session.AddUpgrade(Subscribe(3)); return "{}";' | Out-Null
    $point = Invoke-WaveClear -Point 100
    Assert-Near -Name 'L1とL3: L3 の Value1 倍だけになる（重ねない。100）' -Actual $point -Expected ([math]::Floor(100 * $l3 + 0.001))

    # --- 端数は切り捨て ---
    $point = Invoke-WaveClear -Point 7
    Assert-Near -Name 'L3: 増えた分の端数は切り捨てる（7）' -Actual $point -Expected (7 + [math]::Floor(7 * ($l3 - 1) + 0.001))

    # --- 0 のときは増えない（ボーナスの計算だけ確かめる） ---
    $r = Invoke-SubscribeSnippet -Body 'return $"{{\"bonus\":{subscribe.GetWaveClearBonus(0)}}}";'
    Assert-Near -Name '所持キャッシュ0ではボーナス0' -Actual $r.bonus -Expected 0

    if ((Get-WaveState).isWavePause) { Skip-BossProbeShop }
}
