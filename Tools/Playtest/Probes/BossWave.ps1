#
# ボスウェーブ（BossWaveUseCase / BossWaveDataStore）と、複数個体のボスの台本制御
# （BossGroupDataStore / BossPatternRunner / BossAIBase）を実プレイで検証するプローブ。
#
# BossWaveConfig のボスウェーブ（既定: 5）まで制限時間を満たしてウェーブを進め、
#   - 開始時に前のウェーブの敵が消え、プレイヤーが指定位置へ移り、ボス2体が出ること
#   - ボスウェーブ中は通常の敵が湧かず、制限時間を過ぎてもウェーブが進まないこと
#   - 台本（BossGroup_TwinShooter）どおりに
#       A が行動中は B を待機させる → A の硬直明けで B に交代（A を待機） → B の硬直明けで A・B が同時に行動 → 1秒休み
#     の順で動き、各段階の秒数が EnemyMasterData（予備動作0.8 / 攻撃0.2 / 硬直1.5）どおりであること
#   - スタンで行動不能になった個体の回復を待ってから相手が動き出すこと
#   - フリーズ中は行動の段階が進まないこと
#   - ボスを1体倒しても進まず、全員倒すとクリアになること（クリア画面の流れは GameClear プローブ）
# を確かめる。
#
# 個体の行動段階・待機は毎フレーム記録する（uloop の往復は数百msかかり、段階の切り替わりを直接は捉えられないため）。
# 記録用の購読は AppDomain のデータに置き、最後に破棄する。検証中は毎フレームHPを全快させて倒れないようにする。
#

$Global:BossProbeActionSeconds = 0.8 + 0.2 + 1.5
$Global:BossProbePatternWaitSeconds = 1.0
# 段階の切り替わりは MonoBehaviour の Update、台本の判定は VContainer の Tick で行うため、1〜2フレームずれる
$Global:BossProbeTimeTolerance = 0.15

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

# このプローブの判定は BossGroup_TwinShooter の台本を前提にしている。ボスウェーブに出すボスは別の構成に
# 差し替わっていることがあるため、実行中だけこの台本へ戻す
$Global:BossWaveProbeGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TwinShooter.asset'

function ProbePrepare {
    Enter-BossProbeScene
    try {
        $Global:BossWaveProbeSavedGroup = Set-BossWaveGroup -GroupPath $Global:BossWaveProbeGroupPath
    }
    catch {
        # 準備が途中で失敗すると ProbeCleanup が呼ばれないため、ここで戻す
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Restore-BossWaveGroup -GroupPath $Global:BossWaveProbeSavedGroup
    Exit-BossProbeScene
}

function Get-BossSnapshot {
    return Invoke-BossSnippet -Body @'
var runner = GetRunner();
var a = GetBoss(0);
var b = GetBoss(1);
float Elapsed(BossAIBase x) => x == null ? -1f : (float)elapsedField.GetValue(phaseMachineField.GetValue(x));
var nonBoss = enemies.Enemies.Count(e => !IsBossEnemy(e));
var boss = enemies.Enemies.Count(e => IsBossEnemy(e));
return $"{{\"wave\":{wave.CurrentWave.CurrentValue},\"pause\":{wave.IsWavePause.Value.ToString().ToLower()},\"step\":{(runner == null ? -1 : runner.StepIndex)},\"a\":\"{Describe(a)}\",\"b\":\"{Describe(b)}\",\"aElapsed\":{Elapsed(a)},\"bElapsed\":{Elapsed(b)},\"nonBoss\":{nonBoss},\"boss\":{boss},\"freezing\":{freeze.IsFreezing.CurrentValue.ToString().ToLower()}}}";
'@
}

function ConvertFrom-MemberText {
    # "Phase|hold|stun|dead" を分解する
    param([string]$Text)
    if ($Text -eq '-') { return $null }
    $p = $Text.Split('|')
    return [pscustomobject]@{ phase = $p[0]; hold = ($p[1] -eq 'True'); stun = ($p[2] -eq 'True'); dead = ($p[3] -eq 'True') }
}

function Invoke-BossProbeBody {
    $bossWaveNumber = Move-ToBossWaveShop

    # --- 開始前の状態を仕込む（残った敵を必ず1体以上にし、プレイヤーを中心から外す）と、毎フレームの記録を始める ---
    $pre = Invoke-BossSnippet -Body @'
if (enemies.Enemies.Count == 0 && enemies.TryGetEnemyMasterData("O-001", out var common))
{
    enemies.AddEnemyData(common, new Pose(new Vector3(8f, 0f, 8f), Quaternion.identity));
}
player.WarpTo(new Vector3(10f, 0f, -10f));

var log = new List<string>();
var maxNonBoss = new int[1];
string last = null;
var sub = Observable.EveryUpdate().Subscribe(_ =>
{
    // 検証中は倒れないようにする
    player.Health.Value = player.MaxHealth.Value;

    if (bossWave.IsBossSpawned)
    {
        var nonBossNow = enemies.Enemies.Count(e => !IsBossEnemy(e));
        if (nonBossNow > maxNonBoss[0]) maxNonBoss[0] = nonBossNow;
    }

    var runner = GetRunner();
    if (runner == null) return;
    var key = $"{runner.StepIndex},{Describe(GetBoss(0))},{Describe(GetBoss(1))}";
    if (key == last) return;
    last = key;
    log.Add($"{Time.frameCount},{Time.time:F3},{key}");
});
AppDomain.CurrentDomain.SetData("bossProbe.sub", sub);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
AppDomain.CurrentDomain.SetData("bossProbe.maxNonBoss", maxNonBoss);

return $"{{\"enemies\":{enemies.Enemies.Count},\"x\":{player.Position.Value.x},\"z\":{player.Position.Value.z}}}";
'@
    Write-Host "開始前: 敵 $($pre.enemies) 体 / プレイヤー ($($pre.x), $($pre.z))"

    # --- ボスウェーブ開始 ---
    Skip-BossProbeShop
    Start-Sleep -Milliseconds 500

    $start = Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var p = player.Position.Value;
var target = config.PlayerPosition;
var origin = config.BossOrigin.position;
var bossDist = enemies.Enemies.Where(e => IsBossEnemy(e))
    .Select(e => Vector3.Distance(e.Pose.position, origin)).DefaultIfEmpty(-1f).Max();
return $"{{\"playerDist\":{Vector3.Distance(p, target)},\"maxBossDistFromOrigin\":{bossDist}}}";
'@
    $snap = Get-BossSnapshot
    Assert-ProbeTrue -Name 'ボスウェーブが始まった（ポーズ解除）' -Condition (-not $snap.pause -and $snap.wave -eq $bossWaveNumber) | Out-Null
    Assert-ProbeValue -Name '前のウェーブの敵が消えている' -Actual $snap.nonBoss -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'ボスが2体出ている' -Actual $snap.boss -Expected 2 | Out-Null
    Assert-ProbeValue -Name 'プレイヤーが指定位置へ移った（距離）' -Actual $start.playerDist -Expected 0 -Tolerance 0.01 | Out-Null
    # メンバーのずれは ±5m。出現直後から動き出すため少し余裕を見る
    Assert-ProbeTrue -Name 'ボスが基準点の近くに出ている' -Condition ($start.maxBossDistFromOrigin -ge 0 -and $start.maxBossDistFromOrigin -lt 7) -Detail "最大距離=$($start.maxBossDistFromOrigin)" | Out-Null

    # --- 台本1周ぶん記録する（交代2回 + 同時行動1回 + 休み1秒 + 次周の開始まで） ---
    $cycleSeconds = $Global:BossProbeActionSeconds * 3 + $Global:BossProbePatternWaitSeconds
    Start-Sleep -Seconds ([Math]::Ceiling($cycleSeconds + 1.5))

    # 制限時間を超えてもボスウェーブは進まない
    Invoke-BossSnippet -Body 'wave.AddElapsedTime(9999f); return "{}";' | Out-Null
    Start-Sleep -Milliseconds 500
    $snap = Get-BossSnapshot
    Assert-ProbeTrue -Name '制限時間を過ぎてもボスウェーブは進まない' -Condition ($snap.wave -eq $bossWaveNumber -and -not $snap.pause) -Detail "wave=$($snap.wave) pause=$($snap.pause)" | Out-Null

    $logText = (Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
var maxNonBoss = ((int[])AppDomain.CurrentDomain.GetData("bossProbe.maxNonBoss"))[0];
return $"{{\"maxNonBoss\":{maxNonBoss},\"log\":\"{string.Join(";", log)}\"}}";
'@)
    Assert-ProbeValue -Name 'ボスウェーブ中に通常の敵が湧かない（最大数）' -Actual $logText.maxNonBoss -Expected 0 | Out-Null

    $rows = @($logText.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        [pscustomobject]@{ frame = [int]$c[0]; time = [double]$c[1]; step = [int]$c[2]; a = (ConvertFrom-MemberText $c[3]); b = (ConvertFrom-MemberText $c[4]) }
    })
    Write-Host "記録: $($rows.Count) 行"
    $rows | Select-Object -First 30 | ForEach-Object { Write-Host ("    f{0} t={1:F3} step={2} A={3}/{4} B={5}/{6}" -f $_.frame, $_.time, $_.step, $_.a.phase, $_.a.hold, $_.b.phase, $_.b.hold) }

    $aStart = $rows | Where-Object { $_.a -and $_.a.phase -eq 'Windup' } | Select-Object -First 1
    if (-not (Assert-ProbeTrue -Name 'A が行動を始めた' -Condition ($null -ne $aStart))) { return }
    Assert-ProbeTrue -Name 'A の行動中、B は待機している' -Condition ($aStart.b.phase -eq 'Ready' -and $aStart.b.hold) -Detail "B=$($aStart.b.phase)/hold=$($aStart.b.hold)" | Out-Null

    $bStart = $rows | Where-Object { $_.time -gt $aStart.time -and $_.b.phase -eq 'Windup' } | Select-Object -First 1
    $aEnd = $rows | Where-Object { $_.time -gt $aStart.time -and $_.a.phase -eq 'Ready' } | Select-Object -First 1
    Assert-ProbeValue -Name 'A の行動〜硬直の長さ（秒）' -Actual ($aEnd.time - $aStart.time) -Expected $Global:BossProbeActionSeconds -Tolerance $Global:BossProbeTimeTolerance | Out-Null
    Assert-ProbeValue -Name 'A の硬直明けから B が動き出すまで（秒）' -Actual ($bStart.time - $aEnd.time) -Expected 0 -Tolerance 0.1 | Out-Null
    Assert-ProbeTrue -Name 'B の行動中、A は待機して B は待機解除' -Condition ($bStart.a.hold -and -not $bStart.b.hold) -Detail "A.hold=$($bStart.a.hold) B.hold=$($bStart.b.hold)" | Out-Null

    $syncStart = $rows | Where-Object { $_.time -gt $bStart.time -and $_.a.phase -eq 'Windup' -and $_.b.phase -eq 'Windup' } | Select-Object -First 1
    $bEnd = $rows | Where-Object { $_.time -gt $bStart.time -and $_.b.phase -eq 'Ready' } | Select-Object -First 1
    if (-not (Assert-ProbeTrue -Name 'A・B が同じフレームで同時に行動を始めた' -Condition ($null -ne $syncStart))) { return }
    $prevOfSync = $rows | Where-Object { $_.time -lt $syncStart.time } | Select-Object -Last 1
    Assert-ProbeTrue -Name '同時行動の直前はどちらも待機（片方だけ先に始めていない）' -Condition ($prevOfSync.a.phase -eq 'Ready' -and $prevOfSync.b.phase -eq 'Ready') -Detail "直前 A=$($prevOfSync.a.phase) B=$($prevOfSync.b.phase)" | Out-Null
    Assert-ProbeValue -Name 'B の硬直明けから同時行動まで（秒）' -Actual ($syncStart.time - $bEnd.time) -Expected 0 -Tolerance 0.1 | Out-Null
    Assert-ProbeTrue -Name '同時行動中はどちらも待機させられていない' -Condition (-not $syncStart.a.hold -and -not $syncStart.b.hold) | Out-Null

    $exclusiveRows = @($rows | Where-Object { $_.time -ge $aStart.time -and $_.time -lt $syncStart.time })
    $overlap = @($exclusiveRows | Where-Object { $_.a.phase -ne 'Ready' -and $_.b.phase -ne 'Ready' })
    Assert-ProbeValue -Name '交代の間、A・B が同時に行動していた行数' -Actual $overlap.Count -Expected 0 | Out-Null

    $syncEnd = $rows | Where-Object { $_.time -gt $syncStart.time -and $_.a.phase -eq 'Ready' -and $_.b.phase -eq 'Ready' } | Select-Object -First 1
    $nextCycle = $rows | Where-Object { $_.time -gt $syncEnd.time -and $_.a.phase -eq 'Windup' } | Select-Object -First 1
    if ($nextCycle) {
        Assert-ProbeValue -Name '同時行動の後の休み（秒）' -Actual ($nextCycle.time - $syncEnd.time) -Expected $Global:BossProbePatternWaitSeconds -Tolerance $Global:BossProbeTimeTolerance | Out-Null
    } else {
        Assert-ProbeTrue -Name '台本が先頭へ戻って次の周が始まった' -Condition $false | Out-Null
    }

    # --- スタン: A の行動中に A をスタンさせると、A が回復するまで B は待機のまま ---
    $stunned = $null
    for ($i = 0; $i -lt 60; $i++) {
        $stunned = Invoke-BossSnippet -Body @'
var runner = GetRunner();
var a = GetBoss(0);
if (runner.StepIndex != 0 || a.Status.CurrentValue.Phase == BossActionPhase.Ready) return "{\"done\":false}";
presenter.SetStun(a.EnemyId, true);
return "{\"done\":true}";
'@
        if ($stunned.done) { break }
        Start-Sleep -Milliseconds 150
    }
    Assert-ProbeTrue -Name 'A の行動中にスタンさせた' -Condition $stunned.done | Out-Null
    Start-Sleep -Seconds 3
    $snap = Get-BossSnapshot
    $a = ConvertFrom-MemberText $snap.a
    $b = ConvertFrom-MemberText $snap.b
    Assert-ProbeTrue -Name 'スタンで A の行動が打ち切られた' -Condition ($a.stun -and $a.phase -eq 'Ready') -Detail $snap.a | Out-Null
    Assert-ProbeTrue -Name 'A のスタン中（行動の秒数を過ぎても）B は待機のまま' -Condition ($snap.step -eq 0 -and $b.hold -and $b.phase -eq 'Ready') -Detail "step=$($snap.step) B=$($snap.b)" | Out-Null

    Invoke-BossSnippet -Body 'presenter.SetStun(GetBoss(0).EnemyId, false); return "{}";' | Out-Null
    Start-Sleep -Milliseconds 400
    $snap = Get-BossSnapshot
    $a = ConvertFrom-MemberText $snap.a
    $b = ConvertFrom-MemberText $snap.b
    Assert-ProbeTrue -Name 'A のスタン解除で B が動き出し、A は待機' -Condition ($snap.step -eq 1 -and $b.phase -ne 'Ready' -and $a.hold) -Detail "step=$($snap.step) A=$($snap.a) B=$($snap.b)" | Out-Null

    # --- フリーズ: 行動の段階が進まない ---
    $before = Invoke-BossSnippet -Body @'
freeze.Freeze(1.5f);
var b = GetBoss(1);
return $"{{\"phase\":\"{b.Status.CurrentValue.Phase}\",\"elapsed\":{(float)elapsedField.GetValue(phaseMachineField.GetValue(b))}}}";
'@
    Start-Sleep -Milliseconds 800
    $snap = Get-BossSnapshot
    Assert-ProbeTrue -Name 'フリーズ中' -Condition $snap.freezing | Out-Null
    $b = ConvertFrom-MemberText $snap.b
    Assert-ProbeTrue -Name 'フリーズ中は B の行動段階が進まない' -Condition ($b.phase -eq $before.phase -and [Math]::Abs($snap.bElapsed - $before.elapsed) -lt 0.001) -Detail "前 $($before.phase)/$($before.elapsed) 後 $($b.phase)/$($snap.bElapsed)" | Out-Null
    Start-Sleep -Milliseconds 1200
    $snap = Get-BossSnapshot
    $b = ConvertFrom-MemberText $snap.b
    Assert-ProbeTrue -Name 'フリーズ明けに B の行動段階が進む' -Condition (-not $snap.freezing -and ($b.phase -ne $before.phase -or $snap.bElapsed -gt $before.elapsed)) -Detail "後 $($b.phase)/$($snap.bElapsed)" | Out-Null

    # --- 撃破: 1体では進まず、全員倒すとクリア ---
    Invoke-BossSnippet -Body 'enemies.Damage(new HitData(GetBoss(0).EnemyId, 99999f, App.Common.Data.HitDirectionType.None)); return "{}";' | Out-Null
    Start-Sleep -Seconds 1
    $snap = Get-BossSnapshot
    Assert-ProbeTrue -Name 'ボスを1体倒してもウェーブは進まない' -Condition ($snap.wave -eq $bossWaveNumber -and -not $snap.pause -and $snap.boss -eq 1) -Detail "wave=$($snap.wave) pause=$($snap.pause) boss=$($snap.boss)" | Out-Null
    $survivor = Invoke-BossSnippet -Body @'
var runner = GetRunner();
var b = UnityEngine.Object.FindObjectsOfType<BossAIBase>().FirstOrDefault(x => x.EnemyId == runner.MemberIds[1]);
return $"{{\"b\":\"{Describe(b)}\"}}";
'@
    $b = ConvertFrom-MemberText $survivor.b
    Assert-ProbeTrue -Name '残った B は待機させられたままにならない' -Condition (-not $b.hold) -Detail $survivor.b | Out-Null
    Start-Sleep -Seconds 3
    $snap2 = Invoke-BossSnippet -Body @'
var runner = GetRunner();
var b = UnityEngine.Object.FindObjectsOfType<BossAIBase>().FirstOrDefault(x => x.EnemyId == runner.MemberIds[1]);
return $"{{\"b\":\"{Describe(b)}\",\"step\":{runner.StepIndex}}}";
'@
    $log2 = (Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
return $"{{\"log\":\"{string.Join(";", log.Skip(Math.Max(0, log.Count - 12)))}\"}}";
'@).log
    $bActedAlone = @($log2.Split(';') | Where-Object { $_ -match ',(-|\w+\|\w+\|\w+\|True),(Windup|Active|Recovery)\|' }).Count -gt 0
    Assert-ProbeTrue -Name 'A 撃破後も B は台本どおり行動を続ける' -Condition $bActedAlone -Detail "末尾の記録: $log2" | Out-Null

    Invoke-BossSnippet -Body @'
var runner = GetRunner();
enemies.Damage(new HitData(runner.MemberIds[1], 99999f, App.Common.Data.HitDirectionType.None));
return "{}";
'@ | Out-Null
    Start-Sleep -Milliseconds 1500
    $state = Get-WaveState
    Assert-ProbeTrue -Name 'ボスを全員倒すとクリアになる（ウェーブは進まずポーズ）' -Condition ($state.isCleared -and $state.currentWave -eq $bossWaveNumber -and $state.isWavePause) -Detail "cleared=$($state.isCleared) wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null
}

function ProbeRun {
    try {
        Invoke-BossProbeBody
    }
    finally {
        # 途中で検証を打ち切っても、毎フレームの記録（HP全快を含む）を必ず止める
        Stop-BossProbeRecorder
    }
}
