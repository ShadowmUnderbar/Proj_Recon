#
# DOS（DOSDataStore / BattleHitUseCase.OnHit）を確かめるプローブ。
#
# 射撃（プレイヤー・味方の弾。HitData.ShotType がある命中）が敵に当たるたびに、確率 Value1 でウイルス（威力 Value2・効果時間 Value3 秒）を付与する。
# 最高レベルのみ採用。射撃以外（感電の伝播・回避の跳ね返し・ウイルス）では付与しない。
# 敵の当たり判定（HitBoxView.OnHit）に命中を直接流し、Presenter → BattleHitUseCase の実経路を通す。
# 1回ごとにウイルスを取り除いてから当て、付与された割合を数える（流れ弾で付いたウイルスと混ざらないように）。
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

$Global:DOSPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var virus = scope.Container.Resolve<IVirusDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData DOS(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.DOS && u.Level == level);

// 生きている敵の当たり判定に、ウイルスを取り除いてから小さな命中を tries 回流し、ウイルスが付いた回数を返す（敵がいなければ -1）
int CountInfections(int tries, ShotType? shotType, out int enemyId)
{
    enemyId = -1;
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f && !e.IsDead);
    if (target == null) return -1;
    var hitBox = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None)
        .FirstOrDefault(h => h.Id == target.Id);
    if (hitBox == null) return -1;
    enemyId = target.Id;
    var count = 0;
    for (var i = 0; i < tries; i++)
    {
        target.Hp = 100000f;
        virus.Remove(target.Id);
        hitBox.OnHit(0.1f, -1, hitBox.transform.position + Vector3.forward, out _, 1, shotType);
        if (virus.TryGetVirus(target.Id, out _)) count++;
    }
    return count;
}
'@

function Invoke-DOSSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-BossSnippet -Body ($Global:DOSPrelude + "`n" + $Body)
        if (-not $r.error) { return $r }
        Start-Sleep -Milliseconds 1500
    }
    throw $r.error
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected, [double]$Tolerance = 0.001)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt $Tolerance) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: 付かない ---
    $r = Invoke-DOSSnippet -Body @'
var n = CountInfections(200, ShotType.Normal, out _);
if (n < 0) return "{\"error\":\"敵がいません\"}";
return $"{{\"n\":{n}}}";
'@
    Assert-Near -Name '未所持では射撃を200回当てても付かない' -Actual $r.n -Expected 0

    # --- L1: 確率 Value1 で、威力 Value2・効果時間 Value3 のウイルス ---
    $r = Invoke-DOSSnippet -Body @'
session.AddUpgrade(DOS(1));
var u = DOS(1);
var n = CountInfections(2000, ShotType.Normal, out var id);
if (n < 0) return "{\"error\":\"敵がいません\"}";
// 付いたときの中身を見る（当たるまで流す）
var target = enemies.Enemies.First(e => e.Id == id);
var hitBox = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None).First(h => h.Id == id);
VirusStatus status = default;
for (var i = 0; i < 1000; i++)
{
    target.Hp = 100000f;
    virus.Remove(id);
    hitBox.OnHit(0.1f, -1, hitBox.transform.position + Vector3.forward, out _, 1, ShotType.Merge);
    if (virus.TryGetVirus(id, out status)) break;
}
virus.Remove(id);
return $"{{\"rate\":{F(n / 2000f)},\"power\":{F(status.Power)},\"duration\":{F(status.MaxDuration)},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)},\"v3\":{F(u.Value3.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1) Value2=$($r.v2) Value3=$($r.v3) 付いた割合=$($r.rate)"
    Assert-Near -Name 'L1: 付く割合が Value1 に近い（±3%）' -Actual $r.rate -Expected $r.v1 -Tolerance 0.03
    Assert-Near -Name 'L1: ウイルスの威力は Value2' -Actual $r.power -Expected $r.v2
    Assert-Near -Name 'L1: ウイルスの効果時間は Value3' -Actual $r.duration -Expected $r.v3

    # --- L3（L1 と同時所持）: 最高レベルの値だけ。射撃以外では付かない ---
    $r = Invoke-DOSSnippet -Body @'
session.AddUpgrade(DOS(3));
var u = DOS(3);
var n = CountInfections(2000, ShotType.Waltz, out _);
var other = CountInfections(300, null, out _);
if (n < 0 || other < 0) return "{\"error\":\"敵がいません\"}";
return $"{{\"rate\":{F(n / 2000f)},\"other\":{other},\"v1\":{F(u.Value1.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.v1) 付いた割合=$($r.rate)"
    Assert-Near -Name 'L1とL3: 付く割合が L3 の Value1 に近い（±3%。重ねない）' -Actual $r.rate -Expected $r.v1 -Tolerance 0.03
    Assert-Near -Name 'L1とL3: 射撃以外（ShotType なし）では300回当てても付かない' -Actual $r.other -Expected 0
}
