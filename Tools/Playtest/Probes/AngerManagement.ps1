#
# アンガーマネジメント（AngerManagementDataStore / AngerManagementUseCase）を確かめるプローブ。
#
# 被弾すると、Value2 秒後に受けたダメージ × Value1 秒のレイジを得る（発動していなければ発動する）。
# 逆ギレ未所持でも BuffId のレイジで発動する。秒数・待ち時間は被弾した時点の最高レベルの値で決める。
# デバッグ対戦（無敵）で始め、敵の攻撃で予約が増えないよう最初に敵を倒しておく（出し直しはしない）。
# 被弾は IPlayerStateDataStore.TakeDamage で起こす（無敵でも OnDamaged は発火する）。
# オーバークロック中は待ち時間が止まり、終わると進み出すことも見る。
# スニペットの間は時間が進むので、待ち時間の前後は余裕を持って比べる。
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

$Global:AngerManagementPrelude = @'
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var anger = scope.Container.Resolve<IAngerManagementDataStore>();
var overclock = scope.Container.Resolve<IOverclockDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var buffDb = scope.Container.Resolve<App.Common.Data.Database.BuffDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

var rageField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_rageRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
float Rage() => (float)rageField.GetValue(buffs);
void SetRage(float seconds) => rageField.SetValue(buffs, seconds);
float Attack() => buffs.CalcMultiply(BuffEffectType.AttackPower);

// 予約中のレイジの件数
var pendingField = typeof(App.Battle.DataStore.AngerManagementDataStore).GetField("_pendingRages", BindingFlags.NonPublic | BindingFlags.Instance);
int Pending() => ((System.Collections.ICollection)pendingField.GetValue(anger)).Count;

App.Common.Data.MasterData.UpgradeMasterData Anger(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.AngerManagement && u.Level == level);

App.Common.Data.MasterData.BuffMasterData SnapBackBuff(int level)
{
    var upgrade = db.UpgradeMasterData.First(u => u.NameKey == "$SnapBack" && u.Level == level);
    buffDb.TryGetBuffMasterData(upgrade.BuffId, out var buff);
    return buff;
}
'@

function Invoke-AngerSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:AngerManagementPrelude + "`n" + $Body)
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected, [double]$Tolerance = 0.001)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt $Tolerance) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # 敵を倒しておく（敵の攻撃で被弾が混ざらないように）
    Invoke-AngerSnippet -Body @'
foreach (var e in enemies.Enemies.ToList()) enemies.Damage(new HitData(e.Id, 1e9f, App.Common.Data.HitDirectionType.None));
return "{}";
'@ | Out-Null
    Start-Sleep -Seconds 1

    # --- 未所持: 被弾しても予約されない ---
    $r = Invoke-AngerSnippet -Body @'
SetRage(0f);
player.TakeDamage(4f);
return $"{{\"pending\":{Pending()},\"rage\":{F(Rage())}}}";
'@
    Assert-ProbeValue -Name '未所持では被弾しても予約しない' -Actual $r.pending -Expected 0 | Out-Null
    Assert-Near -Name '未所持では被弾してもレイジは発生しない' -Actual $r.rage -Expected 0

    # --- 逆ギレ未所持・L1 所持: すぐには得ず、Value2 秒後にダメージ × Value1 秒得る ---
    $r = Invoke-AngerSnippet -Body @'
session.AddUpgrade(Anger(1));
SetRage(0f);
player.TakeDamage(4f);
var l1 = Anger(1);
return $"{{\"pending\":{Pending()},\"rage\":{F(Rage())},\"value1\":{F(l1.Value1.value)},\"value2\":{F(l1.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1) / Value2=$($r.value2) 秒"
    $delay = [double]$r.value2
    $expected = 4 * [double]$r.value1
    Assert-ProbeValue -Name 'L1: 被弾で1件予約する' -Actual $r.pending -Expected 1 | Out-Null
    Assert-Near -Name 'L1: 被弾した直後はレイジを得ない' -Actual $r.rage -Expected 0

    Start-Sleep -Milliseconds ([int](($delay - 1.5) * 1000))
    $r = Invoke-AngerSnippet -Body @'
return $"{{\"pending\":{Pending()},\"rage\":{F(Rage())}}}";
'@
    Assert-ProbeValue -Name 'L1: Value2 秒より前は予約のまま' -Actual $r.pending -Expected 1 | Out-Null
    Assert-Near -Name 'L1: Value2 秒より前はレイジを得ない' -Actual $r.rage -Expected 0

    Start-Sleep -Milliseconds 3000
    $r = Invoke-AngerSnippet -Body @'
var rageUpgrade = db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.AngerManagement && u.Level == 1);
buffDb.TryGetBuffMasterData(rageUpgrade.BuffId, out var rageBuff);
return $"{{\"pending\":{Pending()},\"rage\":{F(Rage())},\"active\":{buffs.IsRageActive.ToString().ToLower()},\"attack\":{F(Attack())},\"effect\":{F(rageBuff.EffectValue)}}}";
'@
    Assert-ProbeValue -Name 'L1: Value2 秒後に予約が消化される' -Actual $r.pending -Expected 0 | Out-Null
    # 消化から読むまでの間に最大で約1.5秒＋スニペットの時間だけ減っている
    Assert-ProbeTrue -Name 'L1: ダメージ × Value1 秒のレイジを得る' -Condition (([double]$r.rage -le $expected) -and ([double]$r.rage -gt $expected - 3)) -Detail "実測=$($r.rage) 期待=$expected（経過分だけ少ない）" | Out-Null
    Assert-ProbeValue -Name 'L1: 逆ギレ未所持でもレイジが発動する' -Actual $r.active -Expected $true | Out-Null
    Assert-Near -Name 'L1: 攻撃倍率は BuffId のバフの効果値' -Actual $r.attack -Expected $r.effect

    # --- L1 と L3 を所持: 最高レベルの値だけを使う ---
    $r = Invoke-AngerSnippet -Body @'
session.AddUpgrade(Anger(3));
var seconds = anger.NotifyDamaged(2f);
var l3 = Anger(3);
pendingField.FieldType.GetMethod("Clear").Invoke(pendingField.GetValue(anger), null);
return $"{{\"seconds\":{F(seconds)},\"value1\":{F(l3.Value1.value)}}}";
'@
    Assert-Near -Name 'L1とL3: 最高レベルの Value1 で秒数を決める' -Actual $r.seconds -Expected (2 * [double]$r.value1)

    # --- 逆ギレ所持: 被弾で即座に得るレイジに、Value2 秒後の分が足される ---
    $r = Invoke-AngerSnippet -Body @'
var snapUpgrade = db.UpgradeMasterData.First(u => u.NameKey == "$SnapBack" && u.Level == 1);
session.AddUpgrade(snapUpgrade);
buffs.AddBuff(SnapBackBuff(1));
// 待ち時間のあいだに切れないよう、先に10秒持たせておく
SetRage(10f);
player.TakeDamage(2f);
return $"{{\"rage\":{F(Rage())},\"rate\":{F(SnapBackBuff(1).ConditionValue)},\"pending\":{Pending()},\"value1\":{F(Anger(3).Value1.value)}}}";
'@
    $immediate = 10 + 2 * [double]$r.rate
    $later = 2 * [double]$r.value1
    Assert-Near -Name '逆ギレ所持: 被弾直後は逆ギレの分だけ得る' -Actual $r.rage -Expected $immediate -Tolerance 0.2
    Assert-ProbeValue -Name '逆ギレ所持: アンガーマネジメントの分は予約される' -Actual $r.pending -Expected 1 | Out-Null

    Start-Sleep -Milliseconds ([int](($delay + 1.5) * 1000))
    $r = Invoke-AngerSnippet -Body @'
return $"{{\"rage\":{F(Rage())},\"pending\":{Pending()}}}";
'@
    $total = $immediate + $later
    Assert-ProbeValue -Name '逆ギレ所持: Value2 秒後に予約が消化される' -Actual $r.pending -Expected 0 | Out-Null
    # 同じ残り時間に足される（逆ギレの分は待ち時間のあいだも減り続ける）
    Assert-ProbeTrue -Name '逆ギレ所持: 同じレイジの残り時間に足される' -Condition (([double]$r.rage -le $total - $delay + 0.5) -and ([double]$r.rage -gt $total - $delay - 3)) -Detail "実測=$($r.rage) 期待≒$($total - $delay)（経過分だけ少ない）" | Out-Null

    # --- オーバークロック中は待ち時間が進まない ---
    $r = Invoke-AngerSnippet -Body @'
// しきい値を超えるストックを足して発動させる（待ち時間より長く続くように）
overclock.AddStock(60f);
player.TakeDamage(2f);
return $"{{\"active\":{overclock.IsActive.CurrentValue.ToString().ToLower()},\"pending\":{Pending()}}}";
'@
    Assert-ProbeValue -Name 'オーバークロック: 発動している' -Actual $r.active -Expected $true | Out-Null
    Assert-ProbeValue -Name 'オーバークロック: 被弾で予約する' -Actual $r.pending -Expected 1 | Out-Null

    Start-Sleep -Milliseconds ([int](($delay + 1.5) * 1000))
    $r = Invoke-AngerSnippet -Body @'
var pending = Pending();
overclock.ForceEnd();
return $"{{\"pending\":{pending}}}";
'@
    Assert-ProbeValue -Name 'オーバークロック: 発動中は Value2 秒経っても予約のまま' -Actual $r.pending -Expected 1 | Out-Null

    Start-Sleep -Milliseconds ([int](($delay + 1.5) * 1000))
    $r = Invoke-AngerSnippet -Body @'
return $"{{\"pending\":{Pending()}}}";
'@
    Assert-ProbeValue -Name 'オーバークロック: 終わると待ち時間が進み消化される' -Actual $r.pending -Expected 0 | Out-Null
}
