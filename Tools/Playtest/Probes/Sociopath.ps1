#
# ソシオパス（SociopathDataStore / SociopathUseCase）と、レイジの判定・獲得（IBuffStateDataStore.IsRageActive / GainRage）を確かめるプローブ。
#
# デバッグ対戦（Request-DebugArena）で周回型の雑魚（O-001）を出し、次を見る。
#   - レイジもソシオパスも未所持なら IsRageActive は false、撃破してもレイジは発生しない
#   - レイジ未所持・ソシオパス L1 所持: 撃破で BuffId のレイジが Value1 秒発動する。このレイジは被弾では延びない。
#     発動中の撃破では Value1 × Value2 秒を得る
#   - その後レイジを取得すると、アップグレード無しのレイジは残り時間を引き継いで通常のレイジに置き換わり、被弾で延びる
#   - レイジ所持・非発動中の撃破で Value1 秒得て発動（残り時間が0をわずかに下回っていても0から数える）、発動中なら Value1 × Value2 秒
#   - L1 と L3 を所持していれば最高レベル（L3）の値だけを使う（累積しない）
#   - レイジを複数レベル所持しているとき、どれかが発動中なら倍の秒数をすべてのレイジが得る
# 撃破は実際の経路（EnemyDataStore.Damage → OnEnemyDead → SociopathUseCase）を通す。
# 1回のスニペット内ではフレームが進まないため、獲得前後の効果時間はそのまま比較できる。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-001' -EnemyCount 3 -AutoRespawn $true -RespawnDelaySeconds 1
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:SociopathPrelude = @'
var buffs = scope.Container.Resolve<IBuffStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var buffDb = scope.Container.Resolve<App.Common.Data.Database.BuffDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

// BuffStateDataStore の内部状態（レイジの残り効果時間）を読み書きする
var statesField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_buffStates", BindingFlags.NonPublic | BindingFlags.Instance);
var stateType = typeof(App.Battle.DataStore.BuffStateDataStore).GetNestedType("BuffState", BindingFlags.NonPublic);
var masterField = stateType.GetField("Master");
var remainingField = stateType.GetField("RemainingTime");
var grantedField = stateType.GetField("IsGrantedOnly");

bool IsGranted(string nameKey)
{
    foreach (var s in RageStates())
    {
        if (((App.Common.Data.MasterData.BuffMasterData)masterField.GetValue(s)).NameKey == nameKey) return (bool)grantedField.GetValue(s);
    }
    return false;
}

List<object> RageStates()
{
    var list = new List<object>();
    foreach (var s in (System.Collections.IEnumerable)statesField.GetValue(buffs))
    {
        if (((App.Common.Data.MasterData.BuffMasterData)masterField.GetValue(s)).ConditionType == BuffConditionType.OnDamaged) list.Add(s);
    }
    return list;
}

// レイジの残り効果時間（NameKey → 秒）
float Remaining(string nameKey)
{
    foreach (var s in RageStates())
    {
        if (((App.Common.Data.MasterData.BuffMasterData)masterField.GetValue(s)).NameKey == nameKey) return (float)remainingField.GetValue(s);
    }
    return -999f;
}

void SetRemaining(string nameKey, float seconds)
{
    foreach (var s in RageStates())
    {
        if (((App.Common.Data.MasterData.BuffMasterData)masterField.GetValue(s)).NameKey == nameKey) remainingField.SetValue(s, seconds);
    }
}

void AddRage(int level)
{
    var upgrade = db.UpgradeMasterData.First(u => u.NameKey == "$Rage" && u.Level == level);
    session.AddUpgrade(upgrade);
    buffDb.TryGetBuffMasterData(upgrade.BuffId, out var buff);
    buffs.AddBuff(buff);
}

App.Common.Data.MasterData.UpgradeMasterData Sociopath(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Sociopath && u.Level == level);

// 生きている敵を1体、実際の撃破経路（EnemyDataStore.Damage → OnEnemyDead）で倒す。倒せたら true
bool KillOneEnemy()
{
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f);
    if (target == null) return false;
    enemies.Damage(new HitData(target.Id, 1e9f, App.Common.Data.HitDirectionType.None));
    return true;
}
'@

function Invoke-SociopathSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:SociopathPrelude + "`n" + $Body)
}

# 敵がいない（出し直しの待ち中）ときに少し待って取り直す
function Invoke-SociopathKillSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-SociopathSnippet -Body $Body
        if (-not $r.error) { return $r }
        Start-Sleep -Milliseconds 1500
    }
    throw $r.error
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- レイジもソシオパスも未所持 ---
    $r = Invoke-SociopathKillSnippet -Body @'
var before = buffs.IsRageActive;
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"active\":{before.ToString().ToLower()},\"count\":{RageStates().Count}}}";
'@
    Assert-ProbeValue -Name 'レイジ未所持では IsRageActive が false' -Actual $r.active -Expected $false | Out-Null
    Assert-ProbeValue -Name 'ソシオパス未所持では撃破してもレイジは発生しない' -Actual $r.count -Expected 0 | Out-Null

    # --- レイジ未所持・ソシオパス L1 所持 ---
    $r = Invoke-SociopathKillSnippet -Body @'
session.AddUpgrade(Sociopath(1));
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var l1 = Sociopath(1);
buffDb.TryGetBuffMasterData(l1.BuffId, out var rageBuff);
var key = rageBuff.NameKey;
var first = Remaining(key);
var count = RageStates().Count;
var granted = IsGranted(key);
var active = buffs.IsRageActive;
buffs.NotifyDamageTaken(3f);
var afterDamage = Remaining(key);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"key\":\"{key}\",\"first\":{F(first)},\"count\":{count},\"granted\":{granted.ToString().ToLower()},\"active\":{active.ToString().ToLower()},\"afterDamage\":{F(afterDamage)},\"second\":{F(Remaining(key))},\"value1\":{F(l1.Value1.value)},\"value2\":{F(l1.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1) 秒 / Value2=$($r.value2) 倍 / 未所持で発動するレイジ=$($r.key)"
    Assert-ProbeValue -Name 'レイジ未所持: 撃破でレイジが1つ発動する' -Actual $r.count -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'レイジ未所持: 発動したレイジはアップグレード無しの扱い' -Actual $r.granted -Expected $true | Out-Null
    Assert-ProbeValue -Name 'レイジ未所持: IsRageActive が true になる' -Actual $r.active -Expected $true | Out-Null
    Assert-Near -Name 'レイジ未所持: 非発動中の撃破で Value1 秒得る' -Actual $r.first -Expected $r.value1
    Assert-Near -Name 'レイジ未所持: アップグレード無しのレイジは被弾では延びない' -Actual $r.afterDamage -Expected $r.value1
    Assert-Near -Name 'レイジ未所持: 発動中の撃破で Value1 × Value2 秒を足す' -Actual $r.second -Expected ($r.value1 + $r.value1 * $r.value2)
    $l1Value1 = $r.value1
    $l1Value2 = $r.value2

    # --- その後レイジを取得: 置き換わって被弾で延びる ---
    $r = Invoke-SociopathSnippet -Body @'
// スニペットの間に時間が進むので、置き換え直前の残り時間をここで決める
SetRemaining("$RageBuffL1", 2.5f);
AddRage(1);
var carried = Remaining("$RageBuffL1");
var count = RageStates().Count;
var granted = IsGranted("$RageBuffL1");
buffs.NotifyDamageTaken(3f);
return $"{{\"carried\":{F(carried)},\"count\":{count},\"granted\":{granted.ToString().ToLower()},\"afterDamage\":{F(Remaining("$RageBuffL1"))}}}";
'@
    Assert-ProbeValue -Name 'レイジ取得: アップグレード無しのレイジは置き換わって1つのまま' -Actual $r.count -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'レイジ取得: 通常のレイジになる' -Actual $r.granted -Expected $false | Out-Null
    Assert-Near -Name 'レイジ取得: 残り時間を引き継ぐ' -Actual $r.carried -Expected 2.5
    Assert-Near -Name 'レイジ取得: 被弾で延びる（ダメージ3 × L1 の倍率1）' -Actual $r.afterDamage -Expected (2.5 + 3)

    # --- レイジ所持・非発動中 ---
    $r = Invoke-SociopathKillSnippet -Body @'
// Tick で0をわずかに下回った状態を再現する
SetRemaining("$RageBuffL1", -0.02f);
var before = buffs.IsRageActive;
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"before\":{before.ToString().ToLower()},\"remaining\":{F(Remaining("$RageBuffL1"))},\"active\":{buffs.IsRageActive.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '効果時間が切れると IsRageActive が false に戻る' -Actual $r.before -Expected $false | Out-Null
    Assert-Near -Name 'L1: レイジ非発動中の撃破でレイジを Value1 秒得る' -Actual $r.remaining -Expected $l1Value1
    Assert-ProbeValue -Name 'L1: レイジ非発動中の撃破でレイジが発動する' -Actual $r.active -Expected $true | Out-Null

    # --- レイジ所持・発動中 ---
    $r = Invoke-SociopathKillSnippet -Body @'
SetRemaining("$RageBuffL1", 3f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"remaining\":{F(Remaining("$RageBuffL1"))}}}";
'@
    Assert-Near -Name 'L1: レイジ発動中の撃破で Value1 × Value2 秒を得る' -Actual $r.remaining -Expected (3 + $l1Value1 * $l1Value2)

    # --- L1 と L3 を所持: 最高レベルのみ ---
    $r = Invoke-SociopathKillSnippet -Body @'
session.AddUpgrade(Sociopath(3));
SetRemaining("$RageBuffL1", 0f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var inactive = Remaining("$RageBuffL1");
SetRemaining("$RageBuffL1", 3f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var l3 = Sociopath(3);
return $"{{\"inactive\":{F(inactive)},\"active\":{F(Remaining("$RageBuffL1"))},\"value1\":{F(l3.Value1.value)},\"value2\":{F(l3.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.value1) 秒 / Value2=$($r.value2) 倍"
    Assert-Near -Name 'L1とL3を所持・非発動中: 最高レベルの Value1 秒を得る' -Actual $r.inactive -Expected $r.value1
    Assert-Near -Name 'L1とL3を所持・発動中: 最高レベルの Value1 × Value2 秒を得る' -Actual $r.active -Expected (3 + $r.value1 * $r.value2)
    $l3Gain = $r.value1 * $r.value2

    # --- レイジを複数レベル所持: どれかが発動中なら倍の秒数をすべてのレイジが得る ---
    $r = Invoke-SociopathKillSnippet -Body @'
AddRage(2);
SetRemaining("$RageBuffL1", 3f);
SetRemaining("$RageBuffL2", 0f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"l1\":{F(Remaining("$RageBuffL1"))},\"l2\":{F(Remaining("$RageBuffL2"))}}}";
'@
    Assert-Near -Name 'レイジ複数所持: 発動中のレイジは倍の秒数を足す' -Actual $r.l1 -Expected (3 + $l3Gain)
    Assert-Near -Name 'レイジ複数所持: 非発動のレイジも倍の秒数で発動する' -Actual $r.l2 -Expected $l3Gain
}
