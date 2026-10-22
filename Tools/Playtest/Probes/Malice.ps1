#
# マリス（MaliceDataStore / VirusUseCase）を確かめるプローブ。
#
# プレイヤーから半径 Value1 m 以内（水平距離）の敵は、ウイルスの残り効果時間が減らない（切れない）。
# そのうえで1秒ごとに、ウイルスが無ければ威力 Value2・効果時間 Value3 秒で付け、あれば威力だけ Value2 足す（効果時間には触れない）。
# なので範囲内にいる間は毎秒威力が積み上がり、ほかのウイルスが付いた敵でも最長効果時間は置き換わらない。
# デバッグ対戦（無敵）で敵を2体出し、フリーズで動きを止めてから、敵のビューの transform を動かして
# 1体を半径の内側、1体を外側に置く（敵データの位置は毎フレーム transform から書き戻される）。
# 経過時間は Unity 側の Time.time で数え、呼び出しの遅れで判定がずれないようにする。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'O-002' -EnemyCount 2 -AutoRespawn $true -RespawnDelaySeconds 1 -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    try {
        Invoke-BossSnippet -Body @'
(AppDomain.CurrentDomain.GetData("maliceProbe.sub") as IDisposable)?.Dispose();
AppDomain.CurrentDomain.SetData("maliceProbe.sub", null);
return "{}";
'@ | Out-Null
    }
    catch { }
    Exit-BossProbeScene
}

$Global:MalicePrelude = @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var virus = scope.Container.Resolve<IVirusDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.###", ci);

App.Common.Data.MasterData.UpgradeMasterData Malice(int level) =>
    db.UpgradeMasterData.First(u => u.UpgradeType == UpgradeType.Malice && u.Level == level);

int NearId() => (int)AppDomain.CurrentDomain.GetData("maliceProbe.near");
int FarId() => (int)AppDomain.CurrentDomain.GetData("maliceProbe.far");
int[] Counter() => (int[])AppDomain.CurrentDomain.GetData("maliceProbe.counter");

string Sample()
{
    var nearHas = virus.TryGetVirus(NearId(), out var near);
    var farHas = virus.TryGetVirus(FarId(), out _);
    var c = Counter();
    return $"{{\"t\":{F(Time.time - (float)AppDomain.CurrentDomain.GetData("maliceProbe.t0"))},\"nearHas\":{nearHas.ToString().ToLower()},\"power\":{F(near.Power)},\"maxDuration\":{F(near.MaxDuration)},\"remaining\":{F(near.RemainingTime)},\"farHas\":{farHas.ToString().ToLower()},\"nearNew\":{c[0]},\"nearApplied\":{c[1]}}}";
}
'@

function Invoke-MaliceSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    $r = Invoke-BossSnippet -Body ($Global:MalicePrelude + "`n" + $Body)
    if ($r.error) { throw $r.error }
    return $r
}

function Assert-Near {
    param([string]$Name, [double]$Actual, [double]$Expected)
    Assert-ProbeTrue -Name $Name -Condition ([math]::Abs($Actual - $Expected) -lt 0.001) -Detail "実測=$Actual 期待=$Expected" | Out-Null
}

function ProbeRun {
    Start-Sleep -Seconds 3

    # --- 敵を止めて、半径の内側と外側に置き、L1 を付与する ---
    $r = Invoke-MaliceSnippet -Body @'
var u = Malice(1);
var alive = enemies.Enemies.Where(e => e.Hp > 0f && !e.IsDead).Take(2).ToList();
if (alive.Count < 2) return "{\"error\":\"敵が2体いません\"}";
var views = UnityEngine.Object.FindObjectsByType<App.Battle.Views.EnemyView>(FindObjectsSortMode.None);
Transform ViewOf(int id) => UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None)
    .First(h => h.Id == id).GetComponentInParent<App.Battle.Views.EnemyView>().transform;

freeze.Freeze(30f);
var center = player.Position.Value;
var near = alive[0];
var far = alive[1];
var nearT = ViewOf(near.Id);
var farT = ViewOf(far.Id);
nearT.position = new Vector3(center.x + u.Value1.value - 1f, nearT.position.y, center.z);
farT.position = new Vector3(center.x - u.Value1.value - 3f, farT.position.y, center.z);
near.Hp = 100000f;
far.Hp = 100000f;
virus.Remove(near.Id);
virus.Remove(far.Id);

// 内側の敵への付与を数える（[0]=新しく付いた回数, [1]=付与の回数）
(AppDomain.CurrentDomain.GetData("maliceProbe.sub") as IDisposable)?.Dispose();
var counter = new int[2];
var sub = virus.OnApplied.Subscribe(a => { if (a.EnemyId == near.Id) { if (a.IsNew) counter[0]++; counter[1]++; } });
AppDomain.CurrentDomain.SetData("maliceProbe.sub", sub);
AppDomain.CurrentDomain.SetData("maliceProbe.counter", counter);
AppDomain.CurrentDomain.SetData("maliceProbe.near", near.Id);
AppDomain.CurrentDomain.SetData("maliceProbe.far", far.Id);

session.AddUpgrade(u);
AppDomain.CurrentDomain.SetData("maliceProbe.t0", Time.time);
return $"{{\"v1\":{F(u.Value1.value)},\"v2\":{F(u.Value2.value)},\"v3\":{F(u.Value3.value)}}}";
'@
    Write-Host "  L1: Value1=$($r.v1)m Value2=$($r.v2) Value3=$($r.v3)秒"
    $v2 = [double]$r.v2
    $v3 = [double]$r.v3

    # 付与の直後（1秒の途中）・2.5秒・4.5秒・6.5秒で状態を見る
    $samples = @()
    foreach ($target in @(0.5, 2.5, 4.5, 6.5)) {
        $s = Invoke-MaliceSnippet -Body 'return Sample();'
        $remainMs = [int](($target - [double]$s.t) * 1000)
        if ($remainMs -gt 0) {
            Start-Sleep -Milliseconds $remainMs
            $s = Invoke-MaliceSnippet -Body 'return Sample();'
        }
        Write-Host ("  経過 {0:0.00} 秒: 内側の敵 ウイルス={1} 威力={2} / 外側の敵 ウイルス={3}" -f [double]$s.t, $s.nearHas, $s.power, $s.farHas)
        $samples += $s
    }

    Assert-ProbeValue -Name '1秒経つまでは付かない' -Actual $samples[0].nearHas -Expected $false | Out-Null
    for ($i = 1; $i -lt $samples.Count; $i++) {
        $s = $samples[$i]
        $fires = [math]::Floor([double]$s.t + 0.0001)
        Assert-ProbeValue -Name ("経過{0:0.0}秒: 半径の内側の敵にウイルスが付いている" -f [double]$s.t) -Actual $s.nearHas -Expected $true | Out-Null
        Assert-Near -Name ("経過{0:0.0}秒: 威力が毎秒 Value2 ずつ積み上がっている（{1}回分）" -f [double]$s.t, $fires) -Actual $s.power -Expected ($v2 * $fires)
        Assert-ProbeValue -Name ("経過{0:0.0}秒: 半径の外側の敵には付かない" -f [double]$s.t) -Actual $s.farHas -Expected $false | Out-Null
    }
    $last = $samples[$samples.Count - 1]
    Assert-Near -Name '最長効果時間は Value3 のまま' -Actual $last.maxDuration -Expected $v3
    Assert-Near -Name '途中で切れて新しく付き直していない（新規の付与は1回だけ）' -Actual $last.nearNew -Expected 1

    # --- ほかのウイルス（威力30・最長6秒）が付いた敵: 最長効果時間は置き換わらず、範囲内では残り効果時間が減らない ---
    $r = Invoke-MaliceSnippet -Body @'
virus.Remove(NearId());
virus.Apply(NearId(), 30f, 6f);
AppDomain.CurrentDomain.SetData("maliceProbe.t0", Time.time);
return Sample();
'@
    $start = [double]$r.remaining
    Start-Sleep -Milliseconds 2500
    $r = Invoke-MaliceSnippet -Body 'return Sample();'
    Write-Host ("  ほかのウイルス: 経過 {0:0.00} 秒 威力={1} 最長={2} 残り={3}（開始時の残り={4}）" -f [double]$r.t, $r.power, $r.maxDuration, $r.remaining, $start)
    Assert-Near -Name 'ほかのウイルス: 最長効果時間は6秒のまま（マリスの1秒に置き換わらない）' -Actual $r.maxDuration -Expected 6
    Assert-Near -Name 'ほかのウイルス: 範囲内では残り効果時間が減らない' -Actual $r.remaining -Expected $start
    Assert-ProbeTrue -Name 'ほかのウイルス: 威力にはマリスの分が足されている' -Condition ([double]$r.power -gt 30) -Detail "威力=$($r.power)" | Out-Null

    # --- 範囲の外へ出すと、残り効果時間がまた減り始める ---
    $r = Invoke-MaliceSnippet -Body @'
var nearT = UnityEngine.Object.FindObjectsByType<App.Battle.Views.HitBoxView>(FindObjectsSortMode.None)
    .First(h => h.Id == NearId()).GetComponentInParent<App.Battle.Views.EnemyView>().transform;
var center = player.Position.Value;
nearT.position = new Vector3(center.x, nearT.position.y, center.z + Malice(1).Value1.value + 5f);
AppDomain.CurrentDomain.SetData("maliceProbe.t0", Time.time);
return Sample();
'@
    $start = [double]$r.remaining
    Start-Sleep -Milliseconds 1500
    $r = Invoke-MaliceSnippet -Body 'return Sample();'
    $decreased = $start - [double]$r.remaining
    Write-Host ("  範囲外: 経過 {0:0.00} 秒 残り={1}（開始時の残り={2}）" -f [double]$r.t, $r.remaining, $start)
    # 動かした位置が敵データに届くまで数フレームかかり、その間は範囲内として止まるので、減った量は経過時間より少し小さくなる
    Assert-ProbeTrue -Name '範囲外: 残り効果時間がまた減り始める（減った量が 経過時間-0.3秒〜経過時間）' -Condition ($decreased -gt ([double]$r.t - 0.3) -and $decreased -le ([double]$r.t + 0.01)) -Detail "減った量=$decreased 経過=$($r.t)" | Out-Null
}
