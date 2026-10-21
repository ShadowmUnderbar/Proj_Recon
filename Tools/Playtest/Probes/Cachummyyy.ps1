#
# CACHUMMYYY!（CachummyyyDataStore / PointDropUseCase）を確かめるプローブ。
#
# キャッシュ（ポイント粒子）を1個回収するたびに、粒子の値が Value1 以上なら Value2 ＋ 粒子の値 × Value3 だけ体力を回復する。
# 最高レベルのみ採用・最大HPで頭打ち。
# 回収は PointParticleStoreView の回収通知（_onCollected）を直接流し、Presenter → PointDropUseCase の実際の購読経路を通す。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-002' -EnemyCount 1 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:CachummyyyPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var points = scope.Container.Resolve<IPointDataStore>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData Cachummyyy(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Cachummyyy && u.Level == level);

// 粒子1個の回収を、ストアビューの回収通知から流す（Presenter → PointDropUseCase の購読を通る）
var storeView = UnityEngine.Object.FindFirstObjectByType<App.Battle.Views.PointParticleStoreView>();
var collected = (Subject<int>)typeof(App.Battle.Views.PointParticleStoreView)
    .GetField("_onCollected", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(storeView);
void Collect(int point) => collected.OnNext(point);
'@

function Invoke-CachummyyySnippet {
    param([Parameter(Mandatory)] [string]$Body)
    $r = Invoke-BossSnippet -Body ($Global:CachummyyyPrelude + "`n" + $Body)
    if ($r.error) { throw $r.error }
    return $r
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: ポイントだけ増えて回復しない ---
    $r = Invoke-CachummyyySnippet -Body @'
player.Health.Value = 50f;
var before = points.CurrentPoint.CurrentValue;
Collect(5);
return $"{{\"hp\":{F(player.Health.Value)},\"gained\":{points.CurrentPoint.CurrentValue - before}}}";
'@
    Assert-Near -Name '未所持では回収しても回復しない' -Actual $r.hp -Expected 50
    Assert-Near -Name '回収した粒子の値がポイントに加算される' -Actual $r.gained -Expected 5

    # --- L1: Value1 以上の粒子だけ Value2 回復 ---
    $r = Invoke-CachummyyySnippet -Body @'
session.AddUpgrade(Cachummyyy(1));
var u = Cachummyyy(1);
player.Health.Value = 50f;
Collect((int)u.Value1.value - 1);
var below = player.Health.Value;
Collect((int)u.Value1.value);
var atMin = player.Health.Value;
Collect(100);
var big = player.Health.Value;
return $"{{\"below\":{F(below)},\"atMin\":{F(atMin)},\"big\":{F(big)},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)},\"v3\":{F(u.Value3.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1) Value2=$($r.v2) Value3=$($r.v3)"
    Assert-Near -Name 'L1: Value1 未満の粒子では回復しない' -Actual $r.below -Expected 50
    Assert-Near -Name 'L1: Value1 ちょうどの粒子で Value2 回復する' -Actual $r.atMin -Expected (50 + [double]$r.v2)
    Assert-Near -Name 'L1: 値の大きい粒子でも回復量は Value2（値に比例しない）' -Actual $r.big -Expected (50 + 2 * [double]$r.v2)

    # --- L2（L1 と同時所持）: 最高レベルの値で、値1の粒子でも回復 ---
    $r = Invoke-CachummyyySnippet -Body @'
session.AddUpgrade(Cachummyyy(2));
var u = Cachummyyy(2);
player.Health.Value = 50f;
Collect(1);
return $"{{\"hp\":{F(player.Health.Value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Assert-Near -Name 'L1とL2: L2 の値で、値1の粒子でも回復する' -Actual $r.hp -Expected (50 + [double]$r.v2)

    # --- L3: 粒子の値と同じだけ回復 ---
    $r = Invoke-CachummyyySnippet -Body @'
session.AddUpgrade(Cachummyyy(3));
player.Health.Value = 20f;
Collect(1);
var one = player.Health.Value;
Collect(10);
var ten = player.Health.Value;
return $"{{\"one\":{F(one)},\"ten\":{F(ten)}}}";
'@
    Assert-Near -Name 'L3: 値1の粒子で1回復する' -Actual $r.one -Expected 21
    Assert-Near -Name 'L3: 値10の粒子で10回復する' -Actual $r.ten -Expected 31

    # --- 最大HPで頭打ち ---
    $r = Invoke-CachummyyySnippet -Body @'
var max = player.MaxHealth.Value;
player.Health.Value = max - 0.5f;
Collect(100);
var capped = player.Health.Value;
return $"{{\"capped\":{F(capped)},\"max\":{F(max)}}}";
'@
    Assert-Near -Name '回復は最大HPで頭打ち' -Actual $r.capped -Expected $r.max
}
