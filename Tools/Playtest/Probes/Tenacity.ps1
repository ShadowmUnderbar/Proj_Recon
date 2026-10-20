#
# 執念（TenacityDataStore / PlayerStateDataStore.TakeDamage）を確かめるプローブ。
#
# 被弾すると、受けたダメージ × Value1 を即座に回復する。レイジ発動中なら × Value2。受けるダメージ自体は減らさない。
# レイジ発動中かは被弾した時点で判定し、この被弾で得るレイジ（逆ギレ）は含めない。
# 致死ダメージを食いしばりで耐えたときは回復しない（体力が0になる被弾は回復せず敗北する扱い）。
# デバッグ対戦（無敵）で始め、被弾を確かめるスニペットの中だけ無敵を外す。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-001' -EnemyCount 1 -AutoRespawn $false -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:TenacityPrelude = @'
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var buffDb = scope.Container.Resolve<App.Common.Data.Database.BuffDatabase>();
var arena = scope.Container.Resolve<DebugArenaSettings>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

var rageField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_rageRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
float Rage() => (float)rageField.GetValue(buffs);
void SetRage(float seconds) => rageField.SetValue(buffs, seconds);

var invincibleField = typeof(DebugArenaSettings).GetField("<Invincible>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
void SetInvincible(bool value) => invincibleField.SetValue(arena, value);

App.Common.Data.MasterData.UpgradeMasterData Tenacity(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Tenacity && u.Level == level);

// 体力を hp にしてから damage を受け、その後の体力を返す
float Hit(float hp, float damage)
{
    player.Health.Value = hp;
    SetInvincible(false);
    try { player.TakeDamage(damage); }
    finally { SetInvincible(true); }
    return player.Health.Value;
}
'@

function Invoke-TenacitySnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:TenacityPrelude + "`n" + $Body)
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # 敵を倒しておく（敵の攻撃で被弾が混ざらないように）
    Invoke-TenacitySnippet -Body @'
foreach (var e in enemies.Enemies.ToList()) enemies.Damage(new HitData(e.Id, 1e9f, App.Common.Data.HitDirectionType.None));
return "{}";
'@ | Out-Null
    Start-Sleep -Seconds 1

    # --- 未所持: 回復しない ---
    $r = Invoke-TenacitySnippet -Body @'
SetRage(0f);
var hp = Hit(50f, 10f);
return $"{{\"hp\":{F(hp)}}}";
'@
    Assert-Near -Name '未所持ではダメージをそのまま受ける' -Actual $r.hp -Expected 40

    # --- L1: レイジ非発動なら Value1、発動中なら Value2 ---
    $r = Invoke-TenacitySnippet -Body @'
session.AddUpgrade(Tenacity(1));
SetRage(0f);
var normal = Hit(50f, 10f);
SetRage(5f);
var rage = Hit(50f, 10f);
SetRage(0f);
var l1 = Tenacity(1);
return $"{{\"normal\":{F(normal)},\"rage\":{F(rage)},\"value1\":{F(l1.Value1.value)},\"value2\":{F(l1.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1) / Value2=$($r.value2)"
    Assert-Near -Name 'L1: レイジ非発動ならダメージ × Value1 を回復する' -Actual $r.normal -Expected (40 + 10 * [double]$r.value1)
    Assert-Near -Name 'L1: レイジ発動中ならダメージ × Value2 を回復する' -Actual $r.rage -Expected (40 + 10 * [double]$r.value2)
    $l1Value1 = [double]$r.value1

    # --- この被弾で得るレイジは含めない（逆ギレ所持・レイジ非発動からの被弾） ---
    $r = Invoke-TenacitySnippet -Body @'
var snap = db.UpgradeMasterData.First(u => u.NameKey == "$SnapBack" && u.Level == 1);
session.AddUpgrade(snap);
buffDb.TryGetBuffMasterData(snap.BuffId, out var snapBuff);
buffs.AddBuff(snapBuff);
SetRage(0f);
var hp = Hit(50f, 10f);
var activeAfter = buffs.IsRageActive;
SetRage(0f);
return $"{{\"hp\":{F(hp)},\"activeAfter\":{activeAfter.ToString().ToLower()}}}";
'@
    Assert-Near -Name 'この被弾で得るレイジは含めず Value1 で回復する' -Actual $r.hp -Expected (40 + 10 * $l1Value1)
    Assert-ProbeValue -Name '被弾のあとはレイジが発動している（逆ギレ）' -Actual $r.activeAfter -Expected $true | Out-Null

    # --- L1 と L3 を所持: 最高レベルの値だけを使う ---
    $r = Invoke-TenacitySnippet -Body @'
session.AddUpgrade(Tenacity(3));
SetRage(0f);
var normal = Hit(50f, 10f);
SetRage(5f);
var rage = Hit(50f, 10f);
SetRage(0f);
var l3 = Tenacity(3);
return $"{{\"normal\":{F(normal)},\"rage\":{F(rage)},\"value1\":{F(l3.Value1.value)},\"value2\":{F(l3.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.value1) / Value2=$($r.value2)"
    Assert-Near -Name 'L1とL3: レイジ非発動なら L3 の Value1 だけで回復する' -Actual $r.normal -Expected (40 + 10 * [double]$r.value1)
    Assert-Near -Name 'L1とL3: レイジ発動中なら L3 の Value2 だけで回復する' -Actual $r.rage -Expected (40 + 10 * [double]$r.value2)

    # --- 食いしばりで耐えたときは回復しない ---
    $r = Invoke-TenacitySnippet -Body @'
session.AddUpgrade(db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Grit && u.Level == 1));
SetRage(0f);
var grit = Hit(5f, 100f);
player.Health.Value = player.MaxHealth.Value;
return $"{{\"grit\":{F(grit)}}}";
'@
    Assert-Near -Name '食いしばりで耐えたときは回復しない（体力1）' -Actual $r.grit -Expected 1
}
