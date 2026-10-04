#
# 二人組ボス「TickTock」の配置のつき直し（台本の CrossFormation / DiagonalFormation の MoveSeconds / BossTickTock）を実プレイで検証するプローブ。
#
# デバッグ対戦（Request-DebugArena）で BossGroup_TickTock と戦い、通常の台本の配置のつき直しを毎フレーム記録して、次を確かめる。
#   - 2体とも行動が明けてから時を止め、時止めの長さは台本の MoveSeconds
#   - 時止めの間に配置先へ連続的に移動する（瞬間移動しない＝1フレームの移動が移動距離の大半を占めない）
#   - 時止めの間は行動を始めず、弾も撃たない
#   - 時止めが解けた時点で、配置先（プレイヤーからその方向へ _keepDistance、横へ指定のずれ）に着いている
#   - 時止めの外では、ボスが1フレームで大きく飛ばない（ワープが残っていない）
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。デバッグ対戦の無敵で HP は減らない。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:FormationMoveGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
# 記録する秒数（弾幕2回ぶんで配置のつき直しが2回以上入る）
$Global:FormationMoveRecordSeconds = 22
# 配置先に着いたとみなすずれ（m）
$Global:FormationMoveArriveTolerance = 1.0
# 時止めの外で、1フレームの移動がこれを超えたらワープとみなす（m）
$Global:FormationMoveWarpThreshold = 1.0

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -BossGroupPath $Global:FormationMoveGroupPath -AutoRespawn $false -Wave 5
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Exit-BossProbeScene
}

function ProbeRun {
    try {
        Invoke-FormationMoveProbeBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Get-FormationDirection {
    # BossFormationSlotExtensions.ToDirection と同じ対応（上＝+Z、右＝+X、斜めは正規化）
    param([string]$Slot)
    $s = [Math]::Sqrt(0.5)
    $map = @{
        Up = @(0, 1); Down = @(0, -1); Right = @(1, 0); Left = @(-1, 0)
        UpLeft = @(-$s, $s); UpRight = @($s, $s); DownRight = @($s, -$s); DownLeft = @(-$s, -$s)
    }
    return $map[$Slot]
}

function Get-FormationTargetGap {
    # 配置先（プレイヤーからその方向へ距離、縦方向なら X・横方向なら Z へずれ）とボスの水平距離
    param($Member, $Row, [double]$Distance)
    $d = Get-FormationDirection -Slot $Member.slot
    if ($null -eq $d) { return 999 }
    $vertical = $Member.slot -eq 'Up' -or $Member.slot -eq 'Down'
    $tx = $Row.px + $d[0] * $Distance + $(if ($vertical) { $Member.offset } else { 0 })
    $tz = $Row.pz + $d[1] * $Distance + $(if ($vertical) { 0 } else { $Member.offset })
    return [Math]::Sqrt([Math]::Pow($Member.x - $tx, 2) + [Math]::Pow($Member.z - $tz, 2))
}

function Invoke-FormationMoveProbeBody {
    $config = Invoke-BossSnippet -Body @'
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var steps = group.Pattern.ToList();
var formationSteps = Enumerable.Range(0, steps.Count)
    .Where(i => steps[i].Type == BossPatternStepType.CrossFormation || steps[i].Type == BossPatternStepType.DiagonalFormation).ToList();
var move = steps[formationSteps[0]].MoveSeconds;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(group.Members[0].EnemyMasterData.PrefabPath).GetComponent<BossTickTock>();
var so = new UnityEditor.SerializedObject(prefab);
return $"{{\"move\":{move},\"steps\":\"{string.Join(" ", formationSteps)}\",\"distance\":{so.FindProperty("_keepDistance").floatValue}}}";
'@
    $formationSteps = @($config.steps.Split(' ') | ForEach-Object { [int]$_ })
    Write-Host "設定: 移動 $($config.move)秒 / 距離 $($config.distance)m / 配置のステップ $($formationSteps -join ',')"
    Assert-ProbeTrue -Name '台本の配置のつき直しに移動の秒数が入っている' -Condition ($config.move -gt 0) -Detail "$($config.move)秒" | Out-Null

    # --- 毎フレームの記録（時止め・ステップ・2体の段階・配置・ずれ・位置・プレイヤーの位置・弾の数） ---
    Invoke-BossSnippet -Body @'
var log = new List<string>();
var timeStop = scope.Container.Resolve<ITimeStopDataStore>();
var offsetProperty = typeof(BossAIBase).GetProperty("FormationLateralOffset", BindingFlags.NonPublic | BindingFlags.Instance);
var subs = new CompositeDisposable();
Observable.EveryUpdate().Subscribe(_ =>
{
    var runner = GetRunner();
    var a = GetBoss(0);
    var b = GetBoss(1);
    if (runner == null || a == null || b == null) return;
    string Member(BossAIBase x) =>
        $"{x.Status.CurrentValue.Phase}|{GetFormation(x)}|{(float)offsetProperty.GetValue(x):F2}|{x.transform.position.x:F3}|{x.transform.position.z:F3}";
    var pp = player.Position.Value;
    var bullets = GameObject.FindGameObjectsWithTag("Bullet").Length;
    log.Add($"{Time.time:F3},{timeStop.IsTimeStopped.CurrentValue},{runner.StepIndex},{Member(a)},{Member(b)},{pp.x:F3}|{pp.z:F3},{bullets}");
}).AddTo(subs);
AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
return "{}";
'@ | Out-Null

    Start-Sleep -Seconds $Global:FormationMoveRecordSeconds

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
return $"{{\"log\":\"{string.Join(";", log)}\"}}";
'@
    Stop-BossProbeRecorder

    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        function Parse([string]$m) {
            $p = $m.Split('|')
            [pscustomobject]@{ phase = $p[0]; slot = $p[1]; offset = [double]$p[2]; x = [double]$p[3]; z = [double]$p[4] }
        }
        $pp = $c[5].Split('|')
        [pscustomobject]@{ time = [double]$c[0]; ts = ($c[1] -eq 'True'); step = [int]$c[2]; a = (Parse $c[3]); b = (Parse $c[4]); px = [double]$pp[0]; pz = [double]$pp[1]; bullets = [int]$c[6] }
    })
    Write-Host "記録: $($rows.Count) 行"

    # --- 配置のつき直しの時止めを区間に分ける ---
    $segments = @()
    $startIndex = -1
    for ($i = 0; $i -lt $rows.Count; $i++) {
        $isFormationStop = $rows[$i].ts -and ($formationSteps -contains $rows[$i].step)
        if ($isFormationStop -and $startIndex -lt 0) { $startIndex = $i }
        if (-not $isFormationStop -and $startIndex -ge 0) {
            $segments += , @($startIndex, ($i - 1))
            $startIndex = -1
        }
    }
    # 記録の先頭・末尾で切れた区間は判定しない
    $segments = @($segments | Where-Object { $_[0] -gt 0 -and $_[1] + 1 -lt $rows.Count })
    if (-not (Assert-ProbeTrue -Name '配置のつき直しの時止めを2回以上記録できた' -Condition ($segments.Count -ge 2) -Detail "$($segments.Count) 回")) { return }

    $n = 0
    foreach ($segment in $segments) {
        $n++
        $label = "配置$n"
        $before = $rows[$segment[0] - 1]
        $first = $rows[$segment[0]]
        $end = $rows[$segment[1] + 1]
        $stopRows = @($rows[$segment[0]..$segment[1]])
        $duration = $end.time - $first.time
        Write-Host ("{0}: t={1:F3}〜{2:F3}（{3:F3}秒） A {4}→{5} / B {6}→{7}" -f $label, $first.time, $end.time, $duration, $before.a.slot, $end.a.slot, $before.b.slot, $end.b.slot)

        Assert-ProbeTrue -Name "$label 時止めの直前は2体とも行動していない" -Condition ($before.a.phase -eq 'Ready' -and $before.b.phase -eq 'Ready') -Detail "A=$($before.a.phase) B=$($before.b.phase)" | Out-Null
        Assert-ProbeValue -Name "$label 時止めの長さ＝移動の秒数（秒）" -Actual $duration -Expected $config.move -Tolerance 0.05 | Out-Null
        Assert-ProbeValue -Name "$label 時止め中に行動を始めた行数" -Actual @($stopRows | Where-Object { $_.a.phase -ne 'Ready' -or $_.b.phase -ne 'Ready' }).Count -Expected 0 | Out-Null
        $maxBullets = ($stopRows | Measure-Object -Property bullets -Maximum).Maximum
        Assert-ProbeTrue -Name "$label 時止め中は弾を撃たない（弾が増えない）" -Condition ($maxBullets -le $before.bullets) -Detail "直前 $($before.bullets) 発 / 時止め中の最大 $maxBullets 発" | Out-Null

        foreach ($key in 'a', 'b') {
            $role = $key.ToUpper()
            $total = [Math]::Sqrt([Math]::Pow($end.$key.x - $before.$key.x, 2) + [Math]::Pow($end.$key.z - $before.$key.z, 2))
            $path = @($before) + $stopRows + @($end)
            $maxStep = 0.0
            for ($i = 1; $i -lt $path.Count; $i++) {
                $maxStep = [Math]::Max($maxStep, [Math]::Sqrt([Math]::Pow($path[$i].$key.x - $path[$i - 1].$key.x, 2) + [Math]::Pow($path[$i].$key.z - $path[$i - 1].$key.z, 2)))
            }
            if ($total -gt 2) {
                Assert-ProbeTrue -Name "$label $role 時止めの間に連続的に動く（1フレームの移動が全体の半分未満）" -Condition ($maxStep -lt $total * 0.5) -Detail ("移動 {0:F2}m / 1フレームの最大 {1:F2}m / 記録 {2} 行" -f $total, $maxStep, $stopRows.Count) | Out-Null
            }
            else {
                Write-Host ("    {0} は配置先がほぼ同じ（移動 {1:F2}m）なので連続性は判定しない" -f $role, $total)
            }
            $gap = Get-FormationTargetGap -Member $end.$key -Row $end -Distance $config.distance
            Assert-ProbeValue -Name "$label $role 時止めが解けた時点の配置先からのずれ（$($end.$key.slot) / ずれ$($end.$key.offset)、m）" -Actual $gap -Expected 0 -Tolerance $Global:FormationMoveArriveTolerance | Out-Null
        }
    }

    # --- 時止めの外ではワープしない ---
    $maxOutside = 0
    $where = ''
    for ($i = 1; $i -lt $rows.Count; $i++) {
        if ($rows[$i].ts -or $rows[$i - 1].ts) { continue }
        foreach ($key in 'a', 'b') {
            $d = [Math]::Sqrt([Math]::Pow($rows[$i].$key.x - $rows[$i - 1].$key.x, 2) + [Math]::Pow($rows[$i].$key.z - $rows[$i - 1].$key.z, 2))
            if ($d -gt $maxOutside) { $maxOutside = $d; $where = "t=$($rows[$i].time) $key ステップ $($rows[$i].step)" }
        }
    }
    Assert-ProbeTrue -Name '時止めの外でボスが1フレームで大きく飛ばない' -Condition ($maxOutside -le $Global:FormationMoveWarpThreshold) -Detail ("1フレームの最大 {0:F2}m（{1}）" -f $maxOutside, $where) | Out-Null
}
