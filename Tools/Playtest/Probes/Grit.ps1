#
# 食いしばり（GritDataStore / PlayerStateDataStore.TakeDamage）を確かめるプローブ。
#
# 致死ダメージを受けたとき、ダメージを無効化して体力1で耐える。1ウェーブに Value1 回まで。
# レイジ発動中なら、さらにレイジの残り秒数 × Value2 だけ体力を回復する（最大HPで頭打ち）。
# デバッグ対戦（無敵）で始め、被弾を確かめるスニペットの中だけ無敵を外す（スニペットの間に敵の攻撃で死なないように）。
# ウェーブの切り替えは WaveManagerDataStore の現在ウェーブを直接書き換えて再現する（AdvanceWave の購読先を動かさないため）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-001' -EnemyCount 1 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:GritPrelude = @'
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var grit = scope.Container.Resolve<IGritDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var arena = scope.Container.Resolve<DebugArenaSettings>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

var rageField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_rageRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
void SetRage(float seconds) => rageField.SetValue(buffs, seconds);

// 無敵（デバッグ対戦の設定）を被弾の確認中だけ外す
var invincibleField = typeof(DebugArenaSettings).GetField("<Invincible>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
void SetInvincible(bool value) => invincibleField.SetValue(arena, value);

// ウェーブの切り替えを再現する
var waveField = wave.GetType().GetField("_currentWave", BindingFlags.NonPublic | BindingFlags.Instance);
void SetWave(int value) => ((ReactiveProperty<int>)waveField.GetValue(wave)).Value = value;

App.Common.Data.MasterData.UpgradeMasterData Grit(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Grit && u.Level == level);

// 体力を hp にしてから致死ダメージを受け、その後の体力を返す
float LethalHit(float hp)
{
    player.Health.Value = hp;
    SetInvincible(false);
    try { player.TakeDamage(100000f); }
    finally { SetInvincible(true); }
    return player.Health.Value;
}
'@

function Invoke-GritSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:GritPrelude + "`n" + $Body)
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: 発動しない（実際に死なないよう TryActivate だけを見る） ---
    $r = Invoke-GritSnippet -Body @'
var ok = grit.TryActivate(out var hp);
return $"{{\"ok\":{ok.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '未所持では発動しない' -Actual $r.ok -Expected $false | Out-Null

    # --- L1: 1ウェーブに1回、レイジ非発動なら体力1 ---
    $r = Invoke-GritSnippet -Body @'
session.AddUpgrade(Grit(1));
SetWave(10);
SetRage(0f);
var first = LethalHit(5f);
var again = grit.TryActivate(out _);
SetWave(11);
SetRage(3f);
var nextWave = LethalHit(5f);
var l1 = Grit(1);
return $"{{\"first\":{F(first)},\"again\":{again.ToString().ToLower()},\"nextWave\":{F(nextWave)},\"value1\":{F(l1.Value1.value)},\"value2\":{F(l1.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1) 回 / Value2=$($r.value2) 倍"
    Assert-Near -Name 'L1: 致死ダメージを無効化し体力1で耐える' -Actual $r.first -Expected 1
    Assert-ProbeValue -Name 'L1: 同じウェーブの2回目は発動しない' -Actual $r.again -Expected $false | Out-Null
    Assert-Near -Name 'L1: 次のウェーブでは再び発動し、レイジ3秒 × Value2 を回復する' -Actual $r.nextWave -Expected (1 + 3 * $r.value2)

    # --- L1 と L3 を所持: 最高レベル（L3）だけを使う ---
    $r = Invoke-GritSnippet -Body @'
session.AddUpgrade(Grit(3));
SetWave(12);
SetRage(2f);
var first = LethalHit(5f);
SetRage(1f);
var second = LethalHit(5f);
var third = grit.TryActivate(out _);
var l3 = Grit(3);
return $"{{\"first\":{F(first)},\"second\":{F(second)},\"third\":{third.ToString().ToLower()},\"value1\":{F(l3.Value1.value)},\"value2\":{F(l3.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.value1) 回 / Value2=$($r.value2) 倍"
    Assert-Near -Name 'L3: 1回目はレイジ2秒 × Value2 を回復する' -Actual $r.first -Expected (1 + 2 * $r.value2)
    Assert-Near -Name 'L3: 同じウェーブの2回目も発動する' -Actual $r.second -Expected (1 + 1 * $r.value2)
    Assert-ProbeValue -Name 'L3: 同じウェーブの3回目は発動しない' -Actual $r.third -Expected $false | Out-Null

    # --- 回復は最大HPで頭打ち・致死でないダメージでは発動しない ---
    $r = Invoke-GritSnippet -Body @'
SetWave(13);
SetRage(100000f);
var capped = LethalHit(5f);
player.Health.Value = 50f;
SetInvincible(false);
try { player.TakeDamage(10f); }
finally { SetInvincible(true); }
var normal = player.Health.Value;
var remaining = grit.TryActivate(out _);
SetRage(0f);
player.Health.Value = player.MaxHealth.Value;
return $"{{\"capped\":{F(capped)},\"max\":{F(player.MaxHealth.Value)},\"normal\":{F(normal)},\"remaining\":{remaining.ToString().ToLower()}}}";
'@
    Assert-Near -Name '回復量は最大HPで頭打ち' -Actual $r.capped -Expected $r.max
    Assert-Near -Name '致死でないダメージは通常どおり受ける' -Actual $r.normal -Expected 40
    Assert-ProbeValue -Name '致死でないダメージでは発動回数を消費しない（2回目が残る）' -Actual $r.remaining -Expected $true | Out-Null
}
