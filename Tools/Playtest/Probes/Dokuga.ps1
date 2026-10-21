#
# 毒牙（DokugaDataStore / DodgeCounterAttackUseCase）とウイルス（VirusDataStore / VirusUseCase）を数値で検証するプローブ。
#
# 実際の回避経路を通す: プレイヤーを敵の手前に置き、敵を貫く向きに回避させる
# （PlayerDodgeUseCase の接触判定 → 回避終了の跳ね返し攻撃 → フリーズ明けのダメージ → ウイルス付与）。
#
# 1. 毒牙未所持では、接触ダメージを与えてもウイルスは付かない
# 2. 毒牙L1（威力5・5秒）: 接触した敵に威力5・最長5秒のウイルスが付き、毎秒1ずつ体力が減る。
#    接触していない敵（跳ね返し攻撃の扇形・直線だけで当たった敵）には付かない
# 3. 毒牙L3も獲得（最高レベルのみ採用＝威力15・10秒）: 同じ敵へもう一度当てると、
#    威力は加算（5+15=20）、効果時間は残りより長いので最長・残りとも10秒になる
#
# 対象の敵は体力を1000に上書きし、跳ね返し攻撃やウイルスで倒れないようにする。
#

$Global:DokugaProbeL1Id = '129'
$Global:DokugaProbeL3Id = '131'

function ProbePrepare {
    # デバッグ用「開始時アップグレード」に毒牙やダメージ強化が入っていると前提が崩れるため、プローブの間だけ空にする
    $Global:ProbeSavedStartUpgradeIds = Invoke-UnityCode -Snippet @'
using UnityEditor;
using App.Common.Data;

var saved = EditorPrefs.GetString(DebugConfig.StartUpgradeIdsKey, string.Empty);
EditorPrefs.SetString(DebugConfig.StartUpgradeIdsKey, string.Empty);

return saved;
'@

    Write-Host "開始時アップグレードを退避しました: [$Global:ProbeSavedStartUpgradeIds]"
}

function ProbeCleanup {
    $saved = $Global:ProbeSavedStartUpgradeIds

    # -replaceはパターン側が正規表現なので、バックスラッシュ1文字は '\\' と書く
    $escaped = $saved -replace '\\', '\\' -replace '"', '\"'

    Invoke-UnityCode -Snippet @"
using UnityEditor;
using App.Common.Data;

EditorPrefs.SetString(DebugConfig.StartUpgradeIdsKey, "$escaped");

return "restored";
"@ | Out-Null

    Write-Host "開始時アップグレードを戻しました: [$saved]"
}

# 敵を貫く回避を始める。TargetId が -1 なら、生存中の敵からプレイヤーに近い1体を選ぶ。
# GrantId を渡すと、回避の前にそのアップグレードを獲得する
function Start-DokugaDodge {
    param([int]$TargetId = -1, [string]$GrantId = '')

    $snippet = @'
using System.Linq;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var enemies = c.Resolve<IEnemyDataStore>();
var dodge = c.Resolve<IPlayerDodgeParameterDataStore>();
var counter = c.Resolve<IDodgeCounterAttackDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var session = c.Resolve<IUpgradeSessionDataStore>();
var db = c.Resolve<UpgradeDatabase>();

var grantId = "__GRANT__";
if (grantId != "")
{
    if (!db.TryGetUpgradeMasterData(grantId, out var master)) throw new System.Exception($"Upgrade(id={grantId}) がDBにありません");
    session.AddUpgrade(master);
}

var targetId = __TARGET__;
var playerPos = player.Position.Value;
var target = targetId >= 0
    ? enemies.Enemies.FirstOrDefault(e => e.Id == targetId && !e.IsDead)
    : enemies.Enemies.Where(e => !e.IsDead).OrderBy(e => (e.Pose.position - playerPos).sqrMagnitude).FirstOrDefault();
if (target == null) return "{\"id\":-1}";

// 初回だけ体力を上書きする（2回目以降に戻すと、減った量の観測が途切れる）
if (targetId < 0) target.Hp = 1000f;

// 敵を貫く直線: 敵の3m手前から3m先まで（高さはプレイヤーに合わせる）
var dir = target.Pose.position - playerPos;
dir.y = 0f;
dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
var start = target.Pose.position - dir * 3f;
start.y = playerPos.y;
var end = target.Pose.position + dir * 3f;
end.y = playerPos.y;

// ウイルス由来のダメージだけをログへ流す（弾の流れ弾など他の命中と区別するため、呼び出し元で判定する）
if (targetId < 0)
{
    // 接触した敵をログへ流す（ウイルスが付くべき敵の一覧）。購読は初回の回避でだけ足す
    counter.OnEnemyContacted.Subscribe(id => Debug.Log($"[DokugaProbe] contacted {id}"));

    var tid = target.Id;
    var last = target.Hp;
    enemies.OnEnemyDamaged.Where(x => x == tid).Subscribe(x =>
    {
        enemies.TryGetEnemyData(tid, out var d);
        var fromVirus = new System.Diagnostics.StackTrace(false).GetFrames()
            .Any(f => f.GetMethod()?.DeclaringType?.Name == "VirusUseCase");
        if (fromVirus) Debug.Log($"[DokugaProbe] virus t={Time.time:F3} dmg={last - d.Hp:F3}");
        last = d.Hp;
    });
}

counter.ResetContacts();
player.Position.Value = start;
dodge.StartDodge(start, end);

return $"{{\"id\":{target.Id}}}";
'@
    $snippet = $snippet.Replace('__GRANT__', $GrantId).Replace('__TARGET__', "$TargetId")
    return Invoke-UnityJson -Snippet $snippet
}

# 敵ごとのウイルス・体力を返す
function Get-DokugaState {
    param([Parameter(Mandatory)] [int]$TargetId)

    $snippet = @'
using System.Linq;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var enemies = c.Resolve<IEnemyDataStore>();
var virus = c.Resolve<IVirusDataStore>();
var dodge = c.Resolve<IPlayerDodgeParameterDataStore>();
var freeze = c.Resolve<IFreezeDataStore>();

var targetId = __TARGET__;
enemies.TryGetEnemyData(targetId, out var target);
var hasVirus = virus.TryGetVirus(targetId, out var status);

var infected = enemies.Enemies.Where(e => e.Id != targetId && virus.TryGetVirus(e.Id, out _)).Select(e => e.Id.ToString());

return $"{{\"time\":{UnityEngine.Time.time},\"hp\":{(target != null ? target.Hp : -1f)},\"hasVirus\":{hasVirus.ToString().ToLower()},\"power\":{status.Power},\"max\":{status.MaxDuration},\"remain\":{status.RemainingTime},\"otherInfected\":[{string.Join(",", infected)}],\"isDodging\":{dodge.IsDodging.CurrentValue.ToString().ToLower()},\"isFreezing\":{freeze.IsFreezing.CurrentValue.ToString().ToLower()}}}";
'@
    return Invoke-UnityJson -Snippet $snippet.Replace('__TARGET__', "$TargetId")
}

# ログから接触した敵Idを集める
function Get-DokugaContactedIds {
    $logs = Invoke-Uloop -Command 'get-logs' -Params @{ 'search-text' = '[DokugaProbe] contacted'; 'max-count' = 100 }
    return @($logs.Logs | ForEach-Object { [int](($_.Message -split ' ')[-1]) } | Select-Object -Unique)
}

# ウイルス由来のダメージ（時刻 From〜To の間）が、1回あたり DamagePerTick で、約1秒ごとに入ったかを見る
function Assert-DokugaVirusTicks {
    param([string]$Name, [double]$From, [double]$To, [double]$DamagePerTick)

    $logs = Invoke-Uloop -Command 'get-logs' -Params @{ 'search-text' = '[DokugaProbe] virus'; 'max-count' = 200 }
    $ticks = @($logs.Logs | ForEach-Object {
        if ($_.Message -match 't=([\d.]+) dmg=([\d.-]+)') { [pscustomobject]@{ t = [double]$Matches[1]; dmg = [double]$Matches[2] } }
    } | Where-Object { $_.t -gt $From -and $_.t -le $To } | Sort-Object t)

    $elapsed = $To - $From
    Assert-ProbeTrue -Name "${Name}: 経過秒数ぶんウイルスのダメージが入る" `
        -Condition ($ticks.Count -ge 1 -and [Math]::Abs($ticks.Count - $elapsed) -le 1) `
        -Detail "回数=$($ticks.Count) / 経過=$([Math]::Round($elapsed, 2))秒" | Out-Null

    $wrong = @($ticks | Where-Object { [Math]::Abs($_.dmg - $DamagePerTick) -gt 0.001 })
    Assert-ProbeTrue -Name "${Name}: 1回あたり $DamagePerTick ダメージ" -Condition ($ticks.Count -ge 1 -and $wrong.Count -eq 0) `
        -Detail "dmg=[$(($ticks | ForEach-Object { $_.dmg }) -join ',')]" | Out-Null

    for ($i = 1; $i -lt $ticks.Count; $i++) {
        $interval = $ticks[$i].t - $ticks[$i - 1].t
        Assert-ProbeValue -Name "${Name}: ダメージの間隔は1秒" -Actual $interval -Expected 1 -Tolerance 0.05 | Out-Null
    }
}

function ProbeRun {
    # 敵が湧くまで待つ
    $first = $null
    for ($i = 0; $i -lt 20; $i++) {
        $first = Start-DokugaDodge
        if ([int]$first.id -ge 0) { break }
        Start-Sleep -Seconds 2
    }
    Assert-ProbeTrue -Name '対象の敵が見つかる' -Condition ([int]$first.id -ge 0) | Out-Null
    if ([int]$first.id -lt 0) { return }
    $targetId = [int]$first.id

    # --- 1. 毒牙未所持 ---
    Start-Sleep -Milliseconds 1500
    $none = Get-DokugaState -TargetId $targetId
    $contacted = Get-DokugaContactedIds
    Assert-ProbeTrue -Name '回避で対象の敵に接触した' -Condition ($contacted -contains $targetId) -Detail "contacted=[$($contacted -join ',')]" | Out-Null
    Assert-ProbeTrue -Name '接触ダメージが入った（体力1000から減った）' -Condition ([double]$none.hp -lt 1000) -Detail "hp=$($none.hp)" | Out-Null
    Assert-ProbeTrue -Name '毒牙未所持ではウイルスが付かない' -Condition (-not [bool]$none.hasVirus) | Out-Null

    # --- 2. 毒牙L1 ---
    Start-DokugaDodge -TargetId $targetId -GrantId $Global:DokugaProbeL1Id | Out-Null
    Start-Sleep -Milliseconds 1500
    $l1 = Get-DokugaState -TargetId $targetId
    $contacted = Get-DokugaContactedIds
    Assert-ProbeTrue -Name 'L1: 再び対象の敵に接触した' -Condition ($contacted -contains $targetId) | Out-Null
    Assert-ProbeTrue -Name 'L1: 接触した敵にウイルスが付く' -Condition ([bool]$l1.hasVirus) | Out-Null
    Assert-ProbeValue -Name 'L1: 威力はValue1(5)' -Actual ([double]$l1.power) -Expected 5 | Out-Null
    Assert-ProbeValue -Name 'L1: 最長効果時間はValue2(5秒)' -Actual ([double]$l1.max) -Expected 5 | Out-Null
    Assert-ProbeTrue -Name 'L1: 残り効果時間は付与からの経過ぶん減っている' -Condition ([double]$l1.remain -gt 3 -and [double]$l1.remain -le 5) -Detail "remain=$($l1.remain)" | Out-Null

    $otherInfected = @($l1.otherInfected | Where-Object { $contacted -notcontains [int]$_ })
    Assert-ProbeTrue -Name 'L1: 接触していない敵にはウイルスが付かない' -Condition ($otherInfected.Count -eq 0) -Detail "infected=[$($l1.otherInfected -join ',')]" | Out-Null

    # 毎秒1ダメージ（5÷5）。弾の流れ弾も当たりうるため、体力の差ではなくウイルス由来のダメージだけを数える
    Start-Sleep -Milliseconds 2000
    $l1Later = Get-DokugaState -TargetId $targetId
    Assert-DokugaVirusTicks -Name 'L1' -From ([double]$l1.time) -To ([double]$l1Later.time) -DamagePerTick 1

    # --- 3. 毒牙L3も獲得して重ねる ---
    Start-DokugaDodge -TargetId $targetId -GrantId $Global:DokugaProbeL3Id | Out-Null
    Start-Sleep -Milliseconds 1500
    $l3 = Get-DokugaState -TargetId $targetId
    Assert-ProbeTrue -Name 'L3: ウイルスが付いたまま' -Condition ([bool]$l3.hasVirus) | Out-Null
    Assert-ProbeValue -Name 'L3: 威力は加算される（5+15）' -Actual ([double]$l3.power) -Expected 20 | Out-Null
    Assert-ProbeValue -Name 'L3: 最長効果時間は新しい10秒' -Actual ([double]$l3.max) -Expected 10 | Out-Null
    Assert-ProbeTrue -Name 'L3: 残り効果時間も10秒に置き換わる' -Condition ([double]$l3.remain -gt 8 -and [double]$l3.remain -le 10) -Detail "remain=$($l3.remain)" | Out-Null

    # 毎秒2ダメージ（20÷10）
    Start-Sleep -Milliseconds 2000
    $l3Later = Get-DokugaState -TargetId $targetId
    Assert-DokugaVirusTicks -Name 'L3' -From ([double]$l3.time) -To ([double]$l3Later.time) -DamagePerTick 2
}
