#
# 毒舌（InvectiveDataStore / BattleHitUseCase.OnHit）を確かめるプローブ。
#
# 射撃（プレイヤー・味方の弾。HitData.ShotType がある命中）で敵にダメージを与えるたびに、
# 実際に与えたダメージ × Value1 を威力・効果時間（秒）とするウイルスを付与する。レイジ発動中は威力だけ Value2 倍。最高レベルのみ採用。
# 敵の当たり判定（HitBoxView.OnHit）に命中を直接流し、Presenter → BattleHitUseCase の実経路を通す。
# 「実際に与えたダメージ」は弱点・耐性の倍率と最低保証の後の値なので、命中前後の体力の差から期待値を出す。
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

$Global:InvectivePrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var virus = scope.Container.Resolve<IVirusDataStore>();
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

var rageField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_rageRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
void SetRage(float seconds) => rageField.SetValue(buffs, seconds);

App.Common.Data.MasterData.UpgradeMasterData Invective(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Invective && u.Level == level);

// 生きている敵の当たり判定に、ウイルスを取り除いてから damage の命中を流し、
// 実際に減った体力と、付いたウイルスの状態を JSON の断片で返す（敵がいなければ null）
string HitAndRead(float damage, ShotType? shotType, float hp = 100000f)
{
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f && !e.IsDead);
    if (target == null) return null;
    var hitBox = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None)
        .FirstOrDefault(h => h.Id == target.Id);
    if (hitBox == null) return null;
    target.Hp = hp;
    virus.Remove(target.Id);
    hitBox.OnHit(damage, -1, hitBox.transform.position + Vector3.forward, out _, 1, shotType);
    var dealt = hp - target.Hp;
    var has = virus.TryGetVirus(target.Id, out var status);
    virus.Remove(target.Id);
    return $"\"dealt\":{F(dealt)},\"has\":{has.ToString().ToLower()},\"power\":{F(status.Power)},\"duration\":{F(status.MaxDuration)}";
}
'@

function Invoke-InvectiveSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-BossSnippet -Body ($Global:InvectivePrelude + "`n" + $Body)
        if (-not $r.error) { return $r }
        Start-Sleep -Milliseconds 1500
    }
    throw $r.error
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.01) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: 付かない ---
    $r = Invoke-InvectiveSnippet -Body @'
SetRage(0f);
var hit = HitAndRead(50f, ShotType.Normal);
if (hit == null) return "{\"error\":\"敵がいません\"}";
return "{" + hit + "}";
'@
    Assert-ProbeValue -Name '未所持では付かない' -Actual $r.has -Expected $false | Out-Null

    # --- L1: 与えたダメージ × Value1 を威力・効果時間に ---
    $r = Invoke-InvectiveSnippet -Body @'
session.AddUpgrade(Invective(1));
var hit = HitAndRead(50f, ShotType.Normal);
if (hit == null) return "{\"error\":\"敵がいません\"}";
return "{" + hit + $",\"v1\":{F(Invective(1).Value1.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1) 与えたダメージ=$($r.dealt)"
    $expected = [double]$r.dealt * [double]$r.v1
    Assert-ProbeValue -Name 'L1: 射撃の命中で付く' -Actual $r.has -Expected $true | Out-Null
    Assert-Near -Name 'L1: 威力は与えたダメージ × Value1' -Actual $r.power -Expected $expected
    Assert-Near -Name 'L1: 効果時間も与えたダメージ × Value1（秒）' -Actual $r.duration -Expected $expected

    # --- L3（L1 と同時所持）: 最高レベルの値だけ。レイジ中は威力だけ Value2 倍 ---
    $r = Invoke-InvectiveSnippet -Body @'
session.AddUpgrade(Invective(3));
var u = Invective(3);
var normal = HitAndRead(50f, ShotType.Waltz);
SetRage(30f);
var rage = HitAndRead(50f, ShotType.Waltz);
SetRage(0f);
var other = HitAndRead(50f, null);
if (normal == null || rage == null || other == null) return "{\"error\":\"敵がいません\"}";
return "{\"normal\":{" + normal + "},\"rage\":{" + rage + "},\"other\":{" + other + $"}},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.v1) Value2=$($r.v2) 与えたダメージ=$($r.normal.dealt)"
    $l3 = [double]$r.normal.dealt * [double]$r.v1
    Assert-Near -Name 'L1とL3: 威力は L3 の Value1（重ねない）' -Actual $r.normal.power -Expected $l3
    Assert-Near -Name 'L1とL3: 効果時間も L3 の Value1' -Actual $r.normal.duration -Expected $l3
    $rageAmount = [double]$r.rage.dealt * [double]$r.v1
    Assert-Near -Name 'レイジ発動中は威力が Value2 倍' -Actual $r.rage.power -Expected ($rageAmount * [double]$r.v2)
    Assert-Near -Name 'レイジ発動中も効果時間はそのまま' -Actual $r.rage.duration -Expected $rageAmount
    Assert-ProbeValue -Name '射撃以外（ShotType なし）では付かない' -Actual $r.other.has -Expected $false | Out-Null

    # --- この命中で撃破した敵には付けない ---
    $r = Invoke-InvectiveSnippet -Body @'
var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f && !e.IsDead);
if (target == null) return "{\"error\":\"敵がいません\"}";
var id = target.Id;
var hitBox = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None).First(h => h.Id == id);
virus.Remove(id);
hitBox.OnHit(1e9f, -1, hitBox.transform.position + Vector3.forward, out _, 1, ShotType.Normal);
var has = virus.TryGetVirus(id, out _);
return $"{{\"dead\":{target.IsDead.ToString().ToLower()},\"has\":{has.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '撃破した命中: 敵が撃破されている' -Actual $r.dead -Expected $true | Out-Null
    Assert-ProbeValue -Name '撃破した命中: ウイルスは付かない' -Actual $r.has -Expected $false | Out-Null
}
