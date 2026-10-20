#
# ヴァンパイア（VampireDataStore / BattleHitUseCase.OnEnemyDead）を確かめるプローブ。
#
# 敵を撃破するたびに体力を Value1 回復する（敵のランクを問わない。最大HPで頭打ち）。
# ジャイアントキリング（撃破した敵のランクに応じた回復）と同時に持てば合計して1回で回復する。
# デバッグ対戦（無敵）で周回型のマイナー（O-002）を出し（ジャイアントキリングの回復が乗るランク）、実際の撃破経路（EnemyDataStore.Damage → OnEnemyDead）で倒す。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-002' -EnemyCount 3 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:VampirePrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData Vampire(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Vampire && u.Level == level);

// 生きている敵を1体、実際の撃破経路で倒し、倒した敵のランクを返す（敵がいなければ null）
string KillOneEnemy()
{
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f && !e.IsDead);
    if (target == null) return null;
    enemies.TryGetEnemyMasterData(target.EnemyMasterDataId, out var md);
    enemies.Damage(new HitData(target.Id, 1e9f, App.Common.Data.HitDirectionType.None));
    return md.EnemyRankType.ToString();
}
'@

function Invoke-VampireSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-BossSnippet -Body ($Global:VampirePrelude + "`n" + $Body)
}

# 敵がいない（出し直しの待ち中）ときに少し待って取り直す
function Invoke-VampireKillSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-VampireSnippet -Body $Body
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

    # --- 未所持: 回復しない ---
    $r = Invoke-VampireKillSnippet -Body @'
player.Health.Value = 50f;
var rank = KillOneEnemy();
if (rank == null) return "{\"error\":\"敵がいません\"}";
return $"{{\"hp\":{F(player.Health.Value)},\"rank\":\"{rank}\"}}";
'@
    Write-Host "  倒した敵のランク: $($r.rank)"
    Assert-Near -Name '未所持では撃破しても回復しない' -Actual $r.hp -Expected 50

    # --- L1: 撃破で Value1 回復 ---
    $r = Invoke-VampireKillSnippet -Body @'
session.AddUpgrade(Vampire(1));
player.Health.Value = 50f;
if (KillOneEnemy() == null) return "{\"error\":\"敵がいません\"}";
return $"{{\"hp\":{F(player.Health.Value)},\"value1\":{F(Vampire(1).Value1.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.value1)"
    Assert-Near -Name 'L1: 撃破で体力を Value1 回復する' -Actual $r.hp -Expected (50 + [double]$r.value1)

    # --- 撃破のたびに回復する ---
    $r = Invoke-VampireKillSnippet -Body @'
player.Health.Value = 50f;
if (KillOneEnemy() == null) return "{\"error\":\"敵がいません\"}";
if (KillOneEnemy() == null) return "{\"error\":\"敵がいません\"}";
return $"{{\"hp\":{F(player.Health.Value)},\"value1\":{F(Vampire(1).Value1.value)}}}";
'@
    Assert-Near -Name 'L1: 2体撃破で Value1 × 2 回復する' -Actual $r.hp -Expected (50 + 2 * [double]$r.value1)

    # --- L1 と L3 を所持: 最高レベルの値だけ ---
    $r = Invoke-VampireKillSnippet -Body @'
session.AddUpgrade(Vampire(3));
player.Health.Value = 50f;
if (KillOneEnemy() == null) return "{\"error\":\"敵がいません\"}";
return $"{{\"hp\":{F(player.Health.Value)},\"value1\":{F(Vampire(3).Value1.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.value1)"
    Assert-Near -Name 'L1とL3: L3 の Value1 だけ回復する' -Actual $r.hp -Expected (50 + [double]$r.value1)
    $l3Value1 = [double]$r.value1

    # --- 最大HPで頭打ち ---
    $r = Invoke-VampireKillSnippet -Body @'
var max = player.MaxHealth.Value;
player.Health.Value = max - 0.5f;
if (KillOneEnemy() == null) return "{\"error\":\"敵がいません\"}";
return $"{{\"hp\":{F(player.Health.Value)},\"max\":{F(max)}}}";
'@
    Assert-Near -Name '回復は最大HPで頭打ち' -Actual $r.hp -Expected $r.max

    # --- ジャイアントキリングと同時に持てば合計して回復する ---
    $r = Invoke-VampireKillSnippet -Body @'
var giant = db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.HealOnKill && u.Level == 1);
session.AddUpgrade(giant);
player.Health.Value = 50f;
var rank = KillOneEnemy();
if (rank == null) return "{\"error\":\"敵がいません\"}";
var giantHeal = rank == "Minor" ? giant.Value1.value : rank == "Major" ? giant.Value2.value : 0f;
var hp = player.Health.Value;
player.Health.Value = player.MaxHealth.Value;
return $"{{\"hp\":{F(hp)},\"giantHeal\":{F(giantHeal)},\"rank\":\"{rank}\"}}";
'@
    Write-Host "  ジャイアントキリング L1 の回復量（$($r.rank)）: $($r.giantHeal)"
    Assert-ProbeTrue -Name '倒した敵はジャイアントキリングで回復するランク（回復量が0でない）' -Condition ([double]$r.giantHeal -gt 0) -Detail "ランク=$($r.rank) 回復量=$($r.giantHeal)" | Out-Null
    Assert-Near -Name 'ジャイアントキリングと合計して回復する' -Actual $r.hp -Expected (50 + $l3Value1 + [double]$r.giantHeal)
}
