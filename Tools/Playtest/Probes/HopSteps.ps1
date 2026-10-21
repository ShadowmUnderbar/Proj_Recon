#
# ホップステップ（HopStepsDataStore / PlayerDodgeParameterDataStore）を確かめるプローブ。
#
# 回避回数の上限を Value1 回増やし（増えた分はその場で使える）、回避のクールダウンに Value2 を掛ける。最高レベルのみ採用。
# 獲得はショップと同じく UpgradeSessionDataStore.AddUpgrade → UpgradeSideEffectApplier.Apply で行い、
# 回避は PlayerDodgeUseCase が可否判定後に呼ぶ SetCoolDownTime で消費する。クールダウンの回復は実時間で待って確かめる。
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

$Global:HopStepsPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var applier = scope.Container.Resolve<App.Battle.UseCase.UpgradeSideEffectApplier>();
var dodge = scope.Container.Resolve<IPlayerDodgeParameterDataStore>();
var baseParameter = scope.Container.Resolve<PlayerBaseParameterConfig>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData HopSteps(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.HopSteps && u.Level == level);

// ショップでの獲得と同じ経路で付与する
void Acquire(int level)
{
    var u = HopSteps(level);
    session.AddUpgrade(u);
    applier.Apply(u);
}

// 回避1回ぶんを消費し、設定されたクールダウン秒を返す
var coolDownField = dodge.GetType().GetField("_dodgeCoolDown", BindingFlags.NonPublic | BindingFlags.Instance);
float DodgeOnce()
{
    dodge.SetCoolDownTime();
    return (float)coolDownField.GetValue(dodge);
}
'@

function Invoke-HopStepsSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    $r = Invoke-BossSnippet -Body ($Global:HopStepsPrelude + "`n" + $Body)
    if ($r.error) { throw $r.error }
    return $r
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 2

    # --- 未所持: 基礎値のまま ---
    $r = Invoke-HopStepsSnippet -Body @'
dodge.DodgeCount.Value = dodge.MaxDodgeCount.Value;
var max = dodge.MaxDodgeCount.Value;
var cd = DodgeOnce();
dodge.DodgeCount.Value = dodge.MaxDodgeCount.Value;
return $"{{\"max\":{F(max)},\"cd\":{F(cd)},\"baseCount\":{baseParameter.DodgeCount},\"baseCd\":{F(baseParameter.DodgeCooldown)}}}";
'@
    Write-Host "  基礎値: 回避回数=$($r.baseCount) クールダウン=$($r.baseCd)秒"
    $baseCount = [double]$r.baseCount
    $baseCd = [double]$r.baseCd
    Assert-Near -Name '未所持: 回避回数の上限は基礎値' -Actual $r.max -Expected $baseCount
    Assert-Near -Name '未所持: クールダウンは基礎値' -Actual $r.cd -Expected $baseCd

    # --- L1: 上限+Value1、増えた分はその場で使える、クールダウンは Value2 倍 ---
    $r = Invoke-HopStepsSnippet -Body @'
dodge.DodgeCount.Value = dodge.MaxDodgeCount.Value;
Acquire(1);
var u = HopSteps(1);
var max = dodge.MaxDodgeCount.Value;
var count = dodge.DodgeCount.Value;
var cd = DodgeOnce();
dodge.DodgeCount.Value = dodge.MaxDodgeCount.Value;
return $"{{\"max\":{F(max)},\"count\":{F(count)},\"cd\":{F(cd)},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1) Value2=$($r.v2)"
    Assert-Near -Name 'L1: 回避回数の上限が Value1 増える' -Actual $r.max -Expected ($baseCount + [double]$r.v1)
    Assert-Near -Name 'L1: 増えた分の回避回数がその場で使える' -Actual $r.count -Expected ($baseCount + [double]$r.v1)
    Assert-Near -Name 'L1: クールダウンは基礎値 × Value2' -Actual $r.cd -Expected ($baseCd * [double]$r.v2)

    # --- L1〜L5 を所持: 最高レベルの値だけ（重ねない） ---
    $r = Invoke-HopStepsSnippet -Body @'
Acquire(2); Acquire(3); Acquire(4); Acquire(5);
var u = HopSteps(5);
var max = dodge.MaxDodgeCount.Value;
var count = dodge.DodgeCount.Value;
var cd = DodgeOnce();
return $"{{\"max\":{F(max)},\"count\":{F(count)},\"cd\":{F(cd)},\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)}}}";
'@
    Write-Host "  L5: Value1=$($r.v1) Value2=$($r.v2)"
    $l5Max = $baseCount + [double]$r.v1
    $l5Cd = $baseCd * [double]$r.v2
    Assert-Near -Name 'L1〜L5: 上限は L5 の Value1 だけ増える' -Actual $r.max -Expected $l5Max
    Assert-Near -Name 'L1〜L5: 回避回数も上限まで使える' -Actual $r.count -Expected $l5Max
    Assert-Near -Name 'L1〜L5: クールダウンは基礎値 × L5 の Value2' -Actual $r.cd -Expected $l5Cd

    # --- 上限まで連続で回避でき、それ以上はできない ---
    $r = Invoke-HopStepsSnippet -Body @'
var dodges = 1;
while (dodge.CanDodge && dodges < 100) { DodgeOnce(); dodges++; }
return $"{{\"dodges\":{dodges},\"count\":{F(dodge.DodgeCount.Value)}}}";
'@
    Assert-Near -Name 'L5: 上限の回数まで連続で回避できる' -Actual $r.dodges -Expected $l5Max

    # --- 短くなったクールダウンより前には回復せず、基礎値のクールダウンより前に回復する ---
    $earlyMs = [int]($l5Cd * 1000 / 2)
    Write-Host "  クールダウン $l5Cd 秒の半分（$($earlyMs / 1000) 秒）待つ"
    Start-Sleep -Milliseconds $earlyMs
    $r = Invoke-HopStepsSnippet -Body @'
return $"{{\"count\":{F(dodge.DodgeCount.Value)}}}";
'@
    Assert-Near -Name 'L5: クールダウンが明けるまでは回復しない' -Actual $r.count -Expected 0

    $waitMs = [int](($l5Cd + ($baseCd - $l5Cd) / 2) * 1000) - $earlyMs
    Write-Host "  クールダウン $l5Cd 秒・基礎値 $baseCd 秒の間まで（さらに $($waitMs / 1000) 秒）待つ"
    Start-Sleep -Milliseconds $waitMs
    $r = Invoke-HopStepsSnippet -Body @'
return $"{{\"count\":{F(dodge.DodgeCount.Value)}}}";
'@
    Assert-ProbeTrue -Name 'L5: 短くなったクールダウンで回避回数が回復する' -Condition ([double]$r.count -gt 0) -Detail "回避回数=$($r.count)" | Out-Null
}
