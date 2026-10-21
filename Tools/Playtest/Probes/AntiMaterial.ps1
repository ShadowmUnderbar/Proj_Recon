#
# アンチマテリアル（AntiMaterialDataStore / BattleHitUseCase / EnemyDataStore.Damage）を確かめるプローブ。
#
# 射撃（プレイヤー・味方の弾。HitData.ShotType がある命中）の最低保証ダメージを Value1 に引き上げる。最高レベルのみ採用。
# 射撃以外（感電の伝播・回避の跳ね返し・ウイルス）は既定の最低保証（1）のまま。
# 敵の当たり判定（HitBoxView.OnHit）に命中を直接流し、Presenter → BattleHitUseCase → EnemyDataStore.Damage の実経路で減った体力を測る。
# 0.1 ダメージを与えるので、弱点・耐性方向の倍率がどちらに掛かっても最低保証まで引き上げられる。
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

$Global:AntiMaterialPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData AntiMaterial(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.AntiMaterial && u.Level == level);

// 生きている敵の当たり判定に damage の命中を流し、減った体力を返す（敵がいなければ -1）
float Hit(float damage, ShotType? shotType)
{
    var target = enemies.Enemies.FirstOrDefault(e => e.Hp > 0f && !e.IsDead);
    if (target == null) return -1f;
    var hitBox = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None)
        .FirstOrDefault(h => h.Id == target.Id);
    if (hitBox == null) return -1f;
    target.Hp = 1000f;
    hitBox.OnHit(damage, -1, hitBox.transform.position + Vector3.forward, out _, 1, shotType);
    return 1000f - target.Hp;
}
'@

function Invoke-AntiMaterialSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    for ($try = 0; $try -lt 5; $try++) {
        $r = Invoke-BossSnippet -Body ($Global:AntiMaterialPrelude + "`n" + $Body)
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

    # --- 未所持: 射撃でも最低保証は1 ---
    $r = Invoke-AntiMaterialSnippet -Body @'
var shot = Hit(0.1f, ShotType.Normal);
if (shot < 0f) return "{\"error\":\"敵がいません\"}";
return $"{{\"shot\":{F(shot)}}}";
'@
    Assert-Near -Name '未所持: 射撃の最低保証は1' -Actual $r.shot -Expected 1

    # --- L1: 射撃は Value1、射撃以外は1のまま ---
    $r = Invoke-AntiMaterialSnippet -Body @'
session.AddUpgrade(AntiMaterial(1));
var normal = Hit(0.1f, ShotType.Normal);
var merge = Hit(0.1f, ShotType.Merge);
var other = Hit(0.1f, null);
if (normal < 0f || merge < 0f || other < 0f) return "{\"error\":\"敵がいません\"}";
return $"{{\"normal\":{F(normal)},\"merge\":{F(merge)},\"other\":{F(other)},\"v1\":{F(AntiMaterial(1).Value1.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1)"
    Assert-Near -Name 'L1: ノーマルショットの最低保証は Value1' -Actual $r.normal -Expected $r.v1
    Assert-Near -Name 'L1: マージショットの最低保証も Value1' -Actual $r.merge -Expected $r.v1
    Assert-Near -Name 'L1: 射撃以外（ShotType なし）の最低保証は1のまま' -Actual $r.other -Expected 1

    # --- L4（L1 と同時所持）: 最高レベルの値だけ ---
    $r = Invoke-AntiMaterialSnippet -Body @'
session.AddUpgrade(AntiMaterial(4));
var waltz = Hit(0.1f, ShotType.Waltz);
var other = Hit(0.1f, null);
if (waltz < 0f || other < 0f) return "{\"error\":\"敵がいません\"}";
return $"{{\"waltz\":{F(waltz)},\"other\":{F(other)},\"v1\":{F(AntiMaterial(4).Value1.value)}}}";
'@
    Write-Host "  L4: Value1=$($r.v1)"
    Assert-Near -Name 'L1とL4: ワルツショットの最低保証は L4 の Value1（重ねない）' -Actual $r.waltz -Expected $r.v1
    Assert-Near -Name 'L1とL4: 射撃以外の最低保証は1のまま' -Actual $r.other -Expected 1

    # --- 最低保証より大きいダメージは変わらない（同じ向きの命中で、所持前後を比べる） ---
    $r = Invoke-AntiMaterialSnippet -Body @'
var withUpgrade = Hit(50f, ShotType.Normal);
var withoutShot = Hit(50f, null);
if (withUpgrade < 0f || withoutShot < 0f) return "{\"error\":\"敵がいません\"}";
return $"{{\"shot\":{F(withUpgrade)},\"other\":{F(withoutShot)}}}";
'@
    Assert-Near -Name '最低保証より大きいダメージは射撃でも変わらない' -Actual $r.shot -Expected $r.other
}
