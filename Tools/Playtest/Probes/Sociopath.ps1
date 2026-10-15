#
# ソシオパス（SociopathDataStore / SociopathUseCase）と、バフ「レイジ」の状態（IBuffStateDataStore.IsRageActive / GainRage / NotifyDamageTaken）を確かめるプローブ。
#
# レイジは獲得元（逆ギレの被弾・ソシオパスの撃破）が何でも、逆ギレを何レベル持っていても、残り時間1本・効果1つの状態。
# デバッグ対戦（Request-DebugArena）で周回型の雑魚（O-001）を出し、次を見る。
#   - 逆ギレもソシオパスも未所持なら IsRageActive は false、撃破してもレイジは発生せず攻撃倍率は1
#   - 逆ギレ未所持・ソシオパス L1 所持: 撃破でレイジが Value1 秒発動し、攻撃倍率は BuffId のバフの効果値。
#     被弾では延びない（被弾でレイジを得るのは逆ギレの効果）。発動中の撃破では Value1 × Value2 秒を足す
#   - その後逆ギレを取得: 残り時間はそのまま、被弾でダメージ × 倍率だけ延びる
#   - 被弾で得たレイジと撃破で得たレイジは同じ残り時間に足される（例: 被弾で3秒 → 撃破で1秒 → 4秒）
#   - 非発動中の撃破で Value1 秒得て発動（残り時間が0をわずかに下回っていても0から数える）、発動中なら Value1 × Value2 秒
#   - ソシオパス L1 と L3 を所持していれば最高レベル（L3）の値だけを使う（累積しない）
#   - 逆ギレを複数レベル所持: 被弾時は最高レベルの倍率だけを使い、攻撃倍率は重ならない。効果時間が切れると攻撃倍率は1に戻る
# 撃破は実際の経路（EnemyDataStore.Damage → OnEnemyDead → SociopathUseCase）を通す。
# スニペットの間は時間が進むので、比べる値は同じスニペット内で決める。
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

// レイジの残り効果時間（BuffStateDataStore の内部状態）を読み書きする
var rageField = typeof(App.Battle.DataStore.BuffStateDataStore).GetField("_rageRemainingTime", BindingFlags.NonPublic | BindingFlags.Instance);
float Rage() => (float)rageField.GetValue(buffs);
void SetRage(float seconds) => rageField.SetValue(buffs, seconds);
float Attack() => buffs.CalcMultiply(BuffEffectType.AttackPower);

// 逆ギレ（被弾でレイジを得るアップグレード）の各レベルが付与するバフ
App.Common.Data.MasterData.BuffMasterData SnapBackBuff(int level)
{
    var upgrade = db.UpgradeMasterData.First(u => u.NameKey == "$SnapBack" && u.Level == level);
    buffDb.TryGetBuffMasterData(upgrade.BuffId, out var buff);
    return buff;
}

void AddSnapBack(int level)
{
    session.AddUpgrade(db.UpgradeMasterData.First(u => u.NameKey == "$SnapBack" && u.Level == level));
    buffs.AddBuff(SnapBackBuff(level));
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

    # --- 逆ギレもソシオパスも未所持 ---
    $r = Invoke-SociopathKillSnippet -Body @'
var before = buffs.IsRageActive;
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"active\":{before.ToString().ToLower()},\"rage\":{F(Rage())},\"attack\":{F(Attack())}}}";
'@
    Assert-ProbeValue -Name '未所持では IsRageActive が false' -Actual $r.active -Expected $false | Out-Null
    Assert-Near -Name 'ソシオパス未所持では撃破してもレイジは発生しない' -Actual $r.rage -Expected 0
    Assert-Near -Name 'レイジ非発動中の攻撃倍率は1' -Actual $r.attack -Expected 1

    # --- 逆ギレ未所持・ソシオパス L1 所持 ---
    $r = Invoke-SociopathKillSnippet -Body @'
session.AddUpgrade(Sociopath(1));
var l1 = Sociopath(1);
buffDb.TryGetBuffMasterData(l1.BuffId, out var rageBuff);
SetRage(0f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var first = Rage();
var active = buffs.IsRageActive;
var attack = Attack();
buffs.NotifyDamageTaken(3f);
var afterDamage = Rage();
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"first\":{F(first)},\"active\":{active.ToString().ToLower()},\"attack\":{F(attack)},\"effect\":{F(rageBuff.EffectValue)},\"afterDamage\":{F(afterDamage)},\"second\":{F(Rage())},\"value1\":{F(l1.Value1.value)},\"value2\":{F(l1.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1) 秒 / Value2=$($r.value2) 倍 / レイジの効果値=$($r.effect)"
    Assert-Near -Name '逆ギレ未所持: 非発動中の撃破でレイジを Value1 秒得る' -Actual $r.first -Expected $r.value1
    Assert-ProbeValue -Name '逆ギレ未所持: IsRageActive が true になる' -Actual $r.active -Expected $true | Out-Null
    Assert-Near -Name '逆ギレ未所持: 攻撃倍率は BuffId のバフの効果値' -Actual $r.attack -Expected $r.effect
    Assert-Near -Name '逆ギレ未所持: 被弾では延びない' -Actual $r.afterDamage -Expected $r.value1
    Assert-Near -Name '逆ギレ未所持: 発動中の撃破で Value1 × Value2 秒を足す' -Actual $r.second -Expected ($r.value1 + $r.value1 * $r.value2)
    $l1Value1 = $r.value1
    $l1Value2 = $r.value2
    $rageEffect = $r.effect

    # --- その後逆ギレを取得 ---
    $r = Invoke-SociopathSnippet -Body @'
SetRage(2.5f);
AddSnapBack(1);
var carried = Rage();
buffs.NotifyDamageTaken(3f);
return $"{{\"carried\":{F(carried)},\"afterDamage\":{F(Rage())},\"rate\":{F(SnapBackBuff(1).ConditionValue)}}}";
'@
    Assert-Near -Name '逆ギレ取得: レイジの残り時間はそのまま' -Actual $r.carried -Expected 2.5
    Assert-Near -Name '逆ギレ取得: 被弾でダメージ × 倍率だけ延びる' -Actual $r.afterDamage -Expected (2.5 + 3 * $r.rate)

    # --- 被弾で得たレイジと撃破で得たレイジは同じ残り時間に足される ---
    $r = Invoke-SociopathKillSnippet -Body @'
SetRage(0f);
// 被弾でレイジを3秒得る（逆ギレ L1 の倍率で割ったダメージを受ける）
buffs.NotifyDamageTaken(3f / SnapBackBuff(1).ConditionValue);
var afterDamage = Rage();
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"afterDamage\":{F(afterDamage)},\"afterKill\":{F(Rage())}}}";
'@
    Assert-Near -Name '例: 逆ギレの被弾でレイジを3秒得る' -Actual $r.afterDamage -Expected 3
    Assert-Near -Name '例: その後ソシオパス L1 の撃破で Value1 × Value2 秒を得て4秒になる' -Actual $r.afterKill -Expected (3 + $l1Value1 * $l1Value2)

    # --- 逆ギレ所持・非発動中 ---
    $r = Invoke-SociopathKillSnippet -Body @'
// Tick で0をわずかに下回った状態を再現する
SetRage(-0.02f);
var before = buffs.IsRageActive;
var attackBefore = Attack();
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
return $"{{\"before\":{before.ToString().ToLower()},\"attackBefore\":{F(attackBefore)},\"rage\":{F(Rage())},\"active\":{buffs.IsRageActive.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '効果時間が切れると IsRageActive が false に戻る' -Actual $r.before -Expected $false | Out-Null
    Assert-Near -Name '効果時間が切れると攻撃倍率は1に戻る' -Actual $r.attackBefore -Expected 1
    Assert-Near -Name 'L1: レイジ非発動中の撃破でレイジを Value1 秒得る' -Actual $r.rage -Expected $l1Value1
    Assert-ProbeValue -Name 'L1: レイジ非発動中の撃破でレイジが発動する' -Actual $r.active -Expected $true | Out-Null

    # --- ソシオパス L1 と L3 を所持: 最高レベルのみ ---
    $r = Invoke-SociopathKillSnippet -Body @'
session.AddUpgrade(Sociopath(3));
SetRage(0f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var inactive = Rage();
SetRage(3f);
if (!KillOneEnemy()) return "{\"error\":\"敵がいません\"}";
var l3 = Sociopath(3);
return $"{{\"inactive\":{F(inactive)},\"active\":{F(Rage())},\"value1\":{F(l3.Value1.value)},\"value2\":{F(l3.Value2.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.value1) 秒 / Value2=$($r.value2) 倍"
    Assert-Near -Name 'ソシオパス L1とL3・非発動中: 最高レベルの Value1 秒を得る' -Actual $r.inactive -Expected $r.value1
    Assert-Near -Name 'ソシオパス L1とL3・発動中: 最高レベルの Value1 × Value2 秒を得る' -Actual $r.active -Expected (3 + $r.value1 * $r.value2)

    # --- 逆ギレを複数レベル所持: 残り時間1本・効果は重ならない ---
    $r = Invoke-SociopathSnippet -Body @'
AddSnapBack(3);
AddSnapBack(2);
SetRage(1f);
buffs.NotifyDamageTaken(2f);
return $"{{\"rage\":{F(Rage())},\"rate\":{F(SnapBackBuff(3).ConditionValue)},\"attack\":{F(Attack())}}}";
'@
    Assert-Near -Name '逆ギレ複数所持: 被弾時は最高レベルの倍率だけで延びる' -Actual $r.rage -Expected (1 + 2 * $r.rate)
    Assert-Near -Name '逆ギレ複数所持: 攻撃倍率は重ならない' -Actual $r.attack -Expected $rageEffect
}
