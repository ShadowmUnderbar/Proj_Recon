#
# ラフレシア（RafflesiaDataStore / BattleHitUseCase.OnEnemyDead）を確かめるプローブ。
#
# ウイルスを持ったまま撃破された敵から、半径 Value1 m 以内の他の敵へ、
# 威力 × Value2・効果時間「残り効果時間 + Value3 秒」のウイルスを広げる。最高レベルのみ採用。
# デバッグ対戦（無敵）で敵を6体出し（倒すのは計4体なので出し直しを待たずに済む）、1つのスニペットの中で位置を並べ直してから、実際の撃破経路（EnemyDataStore.Damage → OnEnemyDead）で倒す。
# 位置は毎フレーム敵のビューが書き戻すので、並べ直しから撃破・読み取りまでを同じスニペット（同じフレーム）で行う。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-002' -EnemyCount 6 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:RafflesiaPrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var virus = scope.Container.Resolve<IVirusDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData Rafflesia(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Rafflesia && u.Level == level);

// 生きている敵3体を、倒す敵（中心）・near m 先・far m 先に並べ、中心の敵に威力 power・効果時間 duration のウイルスを付けて倒す。
// 近い敵・遠い敵に付いたウイルスを JSON の断片で返す（敵が3体いなければ null）
string KillCenter(float near, float far, float power, float duration)
{
    var alive = enemies.Enemies.Where(e => e.Hp > 0f && !e.IsDead).Take(3).ToList();
    if (alive.Count < 3) return null;
    var center = new Vector3(0f, 0f, 50f);
    alive[0].Pose = new Pose(center, Quaternion.identity);
    alive[1].Pose = new Pose(center + Vector3.right * near, Quaternion.identity);
    alive[2].Pose = new Pose(center + Vector3.left * far, Quaternion.identity);
    foreach (var e in alive) virus.Remove(e.Id);

    virus.Apply(alive[0].Id, power, duration);
    virus.TryGetVirus(alive[0].Id, out var before);
    enemies.Damage(new HitData(alive[0].Id, 1e9f, App.Common.Data.HitDirectionType.None));

    var nearHas = virus.TryGetVirus(alive[1].Id, out var nearStatus);
    var farHas = virus.TryGetVirus(alive[2].Id, out var farStatus);
    virus.Remove(alive[1].Id);
    virus.Remove(alive[2].Id);
    return $"\"dead\":{alive[0].IsDead.ToString().ToLower()},\"remaining\":{F(before.RemainingTime)},"
        + $"\"nearHas\":{nearHas.ToString().ToLower()},\"nearPower\":{F(nearStatus.Power)},\"nearDuration\":{F(nearStatus.MaxDuration)},"
        + $"\"farHas\":{farHas.ToString().ToLower()}";
}
'@

function Invoke-RafflesiaSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    # 倒した敵の出し直しを待って取り直す
    for ($try = 0; $try -lt 8; $try++) {
        $r = Invoke-BossSnippet -Body ($Global:RafflesiaPrelude + "`n" + $Body)
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
    Start-Sleep -Seconds 3

    # --- 未所持: 広がらない ---
    $r = Invoke-RafflesiaSnippet -Body @'
var hit = KillCenter(2f, 30f, 6f, 4f);
if (hit == null) return "{\"error\":\"敵が3体いません\"}";
return "{" + hit + "}";
'@
    Assert-ProbeValue -Name '未所持: 倒した敵が撃破されている' -Actual $r.dead -Expected $true | Out-Null
    Assert-ProbeValue -Name '未所持: 近くの敵にも広がらない' -Actual $r.nearHas -Expected $false | Out-Null

    # --- L1: 半径 Value1 以内に、威力 × Value2・残り効果時間 + Value3 で広がる ---
    $r = Invoke-RafflesiaSnippet -Body @'
session.AddUpgrade(Rafflesia(1));
var u = Rafflesia(1);
// 半径の内側（Value1 - 0.5m）と外側（Value1 + 0.5m）に置く
var hit = KillCenter(u.Value1.value - 0.5f, u.Value1.value + 0.5f, 6f, 4f);
if (hit == null) return "{\"error\":\"敵が3体いません\"}";
return "{" + hit + $",\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)},\"v3\":{F(u.Value3.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1)m Value2=$($r.v2) Value3=$($r.v3)秒 倒した敵の残り効果時間=$($r.remaining)秒"
    Assert-ProbeValue -Name 'L1: 半径の内側の敵に広がる' -Actual $r.nearHas -Expected $true | Out-Null
    Assert-ProbeValue -Name 'L1: 半径の外側の敵には広がらない' -Actual $r.farHas -Expected $false | Out-Null
    Assert-Near -Name 'L1: 広がったウイルスの威力は 威力 × Value2' -Actual $r.nearPower -Expected (6 * [double]$r.v2)
    Assert-Near -Name 'L1: 広がったウイルスの効果時間は 残り効果時間 + Value3' -Actual $r.nearDuration -Expected ([double]$r.remaining + [double]$r.v3)

    # --- L3（L1 と同時所持）: 最高レベルの値だけ ---
    $r = Invoke-RafflesiaSnippet -Body @'
session.AddUpgrade(Rafflesia(3));
var u = Rafflesia(3);
var hit = KillCenter(u.Value1.value - 0.5f, u.Value1.value + 0.5f, 6f, 4f);
if (hit == null) return "{\"error\":\"敵が3体いません\"}";
return "{" + hit + $",\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)},\"v3\":{F(u.Value3.value)}}}";
'@
    Write-Host "  L3: Value1=$($r.v1)m Value2=$($r.v2) Value3=$($r.v3)秒"
    Assert-ProbeValue -Name 'L1とL3: L3 の半径の内側の敵に広がる' -Actual $r.nearHas -Expected $true | Out-Null
    Assert-ProbeValue -Name 'L1とL3: L3 の半径の外側の敵には広がらない' -Actual $r.farHas -Expected $false | Out-Null
    Assert-Near -Name 'L1とL3: 威力は L3 の Value2 倍（重ねない）' -Actual $r.nearPower -Expected (6 * [double]$r.v2)
    Assert-Near -Name 'L1とL3: 効果時間は 残り効果時間 + Value3' -Actual $r.nearDuration -Expected ([double]$r.remaining + [double]$r.v3)

    # --- ウイルスを持たずに倒れた敵からは広がらない ---
    $r = Invoke-RafflesiaSnippet -Body @'
var alive = enemies.Enemies.Where(e => e.Hp > 0f && !e.IsDead).Take(2).ToList();
if (alive.Count < 2) return "{\"error\":\"敵が2体いません\"}";
alive[0].Pose = new Pose(new Vector3(0f, 0f, 50f), Quaternion.identity);
alive[1].Pose = new Pose(new Vector3(1f, 0f, 50f), Quaternion.identity);
virus.Remove(alive[0].Id);
virus.Remove(alive[1].Id);
enemies.Damage(new HitData(alive[0].Id, 1e9f, App.Common.Data.HitDirectionType.None));
var has = virus.TryGetVirus(alive[1].Id, out _);
return $"{{\"has\":{has.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name 'ウイルスを持たずに倒れた敵からは広がらない' -Actual $r.has -Expected $false | Out-Null
}
