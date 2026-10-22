#
# シールド敵（Shield）の移動と向きを検証するプローブ。
#
# デバッグ対戦でシールド敵（SH-001）を1体だけ出し、行き先（照準の先）とはプレイヤーを挟んだ反対側へワープさせる。
# まっすぐ向かうとプレイヤーのそばを横切る配置から、回り込む間の距離と向きを測る。
# 1. プレイヤーとの水平距離が、保つ距離（攻撃距離の半分）の内側に入らない（NavMesh の誤差ぶんだけ許す）
# 2. 行き先の側へ回り込んで近づく（最後は行き先の近くにいる）
# 3. 向きは常にプレイヤーへ向き直っていく（最後はプレイヤーの方を向いている）
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

# 保つ距離に対して許す食い込み（1フレームぶんの移動。内側に入った分は次のフレームで押し戻す）
$Global:ShieldKeepDistanceTolerance = 0.97
# 回り込みを見る時間[秒]と、測る間隔[ミリ秒]。旋回速度は最も遅いと20度/秒なので、ほぼ真後ろ（約170度）から向き直れる長さにする
$Global:ShieldObserveSeconds = 10
$Global:ShieldSampleIntervalMs = 250

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -EnemyCode 'SH-001' -EnemyCount 1 -Wave 1 -AutoRespawn $false -Invincible $true
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

$Global:ShieldPrelude = @'
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;
using App.Battle.Views.Enemy.AI;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var shield = UnityEngine.Object.FindObjectOfType<Shield>();
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
// 敵 AI が距離を保つ相手（PlayerTransform）の位置で測る
var playerTransform = shield == null ? null : (Transform)typeof(App.Battle.Interface.EnemyAI.EnemyAIBase).GetField("PlayerTransform", flags).GetValue(shield);
var player = playerTransform != null ? playerTransform.position : Vector3.zero;
var statePosition = scope.Container.Resolve<IPlayerStateDataStore>().Position.Value;
var ci = System.Globalization.CultureInfo.InvariantCulture;

'@

function Get-ShieldSample {
    return Invoke-UnityJson -Snippet ($Global:ShieldPrelude + @'
if (shield == null) return "{\"found\":false}";
var toPlayer = player - shield.transform.position; toPlayer.y = 0f;
var forward = shield.transform.forward; forward.y = 0f;
var angle = Vector3.Angle(forward, toPlayer);
var data = (App.Battle.Data.EnemyData)typeof(App.Battle.Interface.EnemyAI.EnemyAIBase).GetField("EnemyData", flags).GetValue(shield);
var isFirst = (bool)typeof(Shield).GetField("_isMovingToTargetPosition1", flags).GetValue(shield);
var goal = (Vector3)typeof(Shield).GetField(isFirst ? "_aimTargetPosition1" : "_aimTargetPosition2", flags).GetValue(shield)
         + (Vector3)typeof(Shield).GetField("_positionOffset", flags).GetValue(shield);
var toGoal = goal - shield.transform.position; toGoal.y = 0f;
return "{\"found\":true"
    + ",\"distance\":" + toPlayer.magnitude.ToString(ci)
    + ",\"keep\":" + (data.AttackDistanceRange * 0.5f).ToString(ci)
    + ",\"angle\":" + angle.ToString(ci)
    + ",\"goalDistance\":" + toGoal.magnitude.ToString(ci)
    + ",\"destDistance\":" + Vector3.Distance(new Vector3(shield.GetComponent<NavMeshAgent>().destination.x, 0f, shield.GetComponent<NavMeshAgent>().destination.z), new Vector3(player.x, 0f, player.z)).ToString(ci)
    + ",\"rawGoalDistance\":" + Vector3.Distance(new Vector3(goal.x, 0f, goal.z), new Vector3(player.x, 0f, player.z)).ToString(ci)
    + ",\"stateGap\":" + Vector3.Distance(new Vector3(player.x, 0f, player.z), new Vector3(statePosition.x, 0f, statePosition.z)).ToString(ci)
    + "}";
'@)
}

function ProbeRun {
    Start-Sleep -Seconds 3

    $first = Get-ShieldSample
    Assert-ProbeTrue -Name 'シールド敵が出ている' -Condition $first.found | Out-Null
    if (-not $first.found) { return }

    # 行き先とはプレイヤーを挟んだ反対側（保つ距離の1.2倍）へワープさせ、向きもプレイヤーと逆へ向けておく
    $warp = Invoke-UnityCode -Snippet ($Global:ShieldPrelude + @'
var isFirst = (bool)typeof(Shield).GetField("_isMovingToTargetPosition1", flags).GetValue(shield);
var goal = (Vector3)typeof(Shield).GetField(isFirst ? "_aimTargetPosition1" : "_aimTargetPosition2", flags).GetValue(shield);
var toGoal = goal - player; toGoal.y = 0f;
if (toGoal.sqrMagnitude < 0.01f) toGoal = Vector3.forward;
var data = (App.Battle.Data.EnemyData)typeof(App.Battle.Interface.EnemyAI.EnemyAIBase).GetField("EnemyData", flags).GetValue(shield);
var keep = data.AttackDistanceRange * 0.5f;
var opposite = player - toGoal.normalized * keep * 1.2f;
opposite.y = shield.transform.position.y;
var agent = shield.GetComponent<NavMeshAgent>();
var warped = agent.Warp(opposite);
shield.transform.rotation = Quaternion.LookRotation(-(player - opposite).normalized);
return warped ? "ok" : "warp-failed";
'@)
    Assert-ProbeTrue -Name '行き先の反対側へワープできた' -Condition ($warp -eq 'ok') -Detail "($warp)" | Out-Null

    $samples = @()
    $count = [int]($Global:ShieldObserveSeconds * 1000 / $Global:ShieldSampleIntervalMs)
    for ($i = 0; $i -lt $count; $i++) {
        $samples += Get-ShieldSample
        Start-Sleep -Milliseconds $Global:ShieldSampleIntervalMs
    }

    $keep = [double]$samples[0].keep
    $minDistance = ($samples | Measure-Object -Property distance -Minimum).Minimum
    $last = $samples[-1]
    $detail = "保つ距離=$keep 最小=$([math]::Round($minDistance, 2)) 最後の行き先までの距離=$([math]::Round($last.goalDistance, 2)) 最後の向きのずれ=$([math]::Round($last.angle, 1))度"

    Assert-ProbeTrue -Name 'プレイヤーとの距離が保つ距離の内側に入らない' -Condition ($minDistance -ge $keep * $Global:ShieldKeepDistanceTolerance) -Detail "($detail)" | Out-Null
    Assert-ProbeTrue -Name '回り込んで行き先の近く（5m以内）まで来る' -Condition ($last.goalDistance -le 5) -Detail "($detail)" | Out-Null
    Assert-ProbeTrue -Name 'プレイヤーの方へ向き直っている（ずれ15度以内）' -Condition ($last.angle -le 15) -Detail "($detail)" | Out-Null

    # 向きのずれは、ワープ直後（180度）から減っていく
    Assert-ProbeTrue -Name '向きのずれがワープ直後より小さくなっていく' -Condition ($samples[-1].angle -lt $samples[0].angle) `
        -Detail "(直後=$([math]::Round($samples[0].angle, 1))度 最後=$([math]::Round($last.angle, 1))度)" | Out-Null

    $trace = ($samples | Select-Object -First 16 | ForEach-Object { "$([math]::Round($_.distance, 1))m/行$([math]::Round($_.destDistance, 1))m/$([math]::Round($_.angle, 0))度" }) -join ' '
    Write-Host "  行き先のプレイヤーからの距離（最後）: エージェント=$([math]::Round($last.destDistance, 2))m 押し出し前=$([math]::Round($last.rawGoalDistance, 2))m"
    Write-Host "  プレイヤーの Transform と論理位置の差: $([math]::Round($last.stateGap, 2))m"
    Write-Host "  経過（距離/向きのずれ）: $trace"
}
