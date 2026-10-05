#
# 二人組ボス「TickTock」の配置のつき直し（台本の CrossFormation / DiagonalFormation の MoveSeconds / BossTickTock）を実プレイで検証するプローブ。
#
# デバッグ対戦（Request-DebugArena）で BossGroup_TickTock と戦い、通常の台本と、共有体力を削って入れた発狂フェイズ（横のずれあり）の
# 配置のつき直しを毎フレーム記録して、次を確かめる。
#   - 2体とも行動が明けてから時を止め、時止めの長さは台本の MoveSeconds
#   - 時止めの間に配置先へ連続的に移動する（瞬間移動しない＝1フレームの移動が移動距離の大半を占めない）
#   - プレイヤーを中心に回りこみながら移動する（向きは一方向にだけ進み、プレイヤーとの距離は起点と配置先の距離の間を保つ＝プレイヤーを突っ切らない）。
#     発狂フェイズでは横のずれで配置先の距離が変わるため、回りこみながら距離を詰める・離す移動も含まれる
#   - 発狂フェイズでは、帯の攻撃を終えてから次の配置の時止めまで、プレイヤーが動いてもボスは攻撃した位置に留まる
#     （発狂フェイズの間は時止めの外でプレイヤーを一定の速さで円を描くように動かす）
#   - 時止めの間は行動を始めず、弾も撃たない
#   - 時止めが解けた時点で、配置先（プレイヤーからその方向へ _keepDistance、横へ指定のずれ）に着いている
#   - 時止めの外では、ボスが1フレームで大きく飛ばない（ワープが残っていない）
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。デバッグ対戦の無敵で HP は減らない。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:FormationMoveGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
# 通常の台本を記録する秒数（弾幕2回ぶんで配置のつき直しが2回以上入る）
$Global:FormationMoveRecordSeconds = 22
# 発狂フェイズを記録する秒数（帯の攻撃ごとに横のずれつきで配置し直す）
$Global:FormationMoveRageRecordSeconds = 15
# 配置先に着いたとみなすずれ（m）
$Global:FormationMoveArriveTolerance = 1.0
# 時止めの外で、1フレームの移動がこれを超えたらワープとみなす（m）
$Global:FormationMoveWarpThreshold = 1.0
# 回りこみの途中で戻ってよい角度の合計（度）と、距離が起点・配置先の範囲からはみ出してよい量（m）。
# どちらも2体がすれ違うときの NavMeshAgent の回避による押し合いのぶん
$Global:FormationMoveBackAngleTolerance = 5
$Global:FormationMoveRadiusTolerance = 1.0
# 発狂フェイズの間にプレイヤーを動かす速さ（m/秒）と、円を一周する秒数
$Global:FormationMovePlayerSpeed = 3
$Global:FormationMovePlayerLoopSeconds = 4
# 帯の攻撃のあと留まっているとみなす、ボスの位置のずれ（m）
$Global:FormationMoveHoldTolerance = 0.1

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

function Get-AroundAngle {
    # プレイヤーから見たボスの向き（度。+Z を0、+X 側へ回ると増える）
    param($Member, $Row)
    return [Math]::Atan2($Member.x - $Row.px, $Member.z - $Row.pz) * 180 / [Math]::PI
}

function Get-AngleDelta {
    # 2つの向きの差（度、-180〜180）
    param([double]$From, [double]$To)
    $d = ($To - $From) % 360
    if ($d -gt 180) { $d -= 360 }
    if ($d -lt -180) { $d += 360 }
    return $d
}

function Get-PlayerDistance {
    param($Member, $Row)
    return [Math]::Sqrt([Math]::Pow($Member.x - $Row.px, 2) + [Math]::Pow($Member.z - $Row.pz, 2))
}

function Invoke-FormationMoveProbeBody {
    $config = Invoke-BossSnippet -Body @'
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var move = group.Pattern.Concat(group.RagePattern)
    .Where(s => s.Type == BossPatternStepType.CrossFormation || s.Type == BossPatternStepType.DiagonalFormation)
    .Select(s => s.MoveSeconds).Distinct().ToList();
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(group.Members[0].EnemyMasterData.PrefabPath).GetComponent<BossTickTock>();
var so = new UnityEditor.SerializedObject(prefab);
// 配置のステップの移動秒数がすべて同じであることを前提にする（違えば count が2以上になる）
return $"{{\"move\":{move[0]},\"moveKinds\":{move.Count},\"distance\":{so.FindProperty("_keepDistance").floatValue}}}";
'@
    Write-Host "設定: 移動 $($config.move)秒 / 距離 $($config.distance)m"
    Assert-ProbeTrue -Name '台本の配置のつき直し（通常・発狂とも）に同じ移動の秒数が入っている' -Condition ($config.move -gt 0 -and $config.moveKinds -eq 1) -Detail "$($config.move)秒 / 種類 $($config.moveKinds)" | Out-Null

    # --- 毎フレームの記録（時止め・ステップ・2体の段階・配置・ずれ・位置・プレイヤーの位置・弾の数） ---
    Invoke-BossSnippet -Body @'
var log = new List<string>();
var timeStop = scope.Container.Resolve<ITimeStopDataStore>();
var offsetProperty = typeof(BossAIBase).GetProperty("FormationLateralOffset", BindingFlags.NonPublic | BindingFlags.Instance);
var stepsField = typeof(App.Battle.DataStore.BossPatternRunner).GetField("_steps", BindingFlags.NonPublic | BindingFlags.Instance);
var actionProperty = typeof(BossAIBase).GetProperty("CurrentActionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
var subs = new CompositeDisposable();
// 発狂フェイズの間（drive に速さが入ったら）、時止めの外でプレイヤーを円を描くように動かす。ボスの Update より前に動かす
Observable.EveryUpdate(UnityFrameProvider.EarlyUpdate).Subscribe(_ =>
{
    var drive = AppDomain.CurrentDomain.GetData("formationProbe.drive") as float[];
    if (drive == null || timeStop.IsTimeStopped.CurrentValue) return;
    var angle = Time.time * Mathf.PI * 2f / drive[1];
    player.WarpTo(player.Position.Value + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * drive[0] * Time.deltaTime);
}).AddTo(subs);
Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate).Subscribe(_ =>
{
    var runner = GetRunner();
    var a = GetBoss(0);
    var b = GetBoss(1);
    if (runner == null || a == null || b == null) return;
    string Member(BossAIBase x) =>
        $"{x.Status.CurrentValue.Phase}|{GetFormation(x)}|{(float)offsetProperty.GetValue(x):F2}|{x.transform.position.x:F3}|{x.transform.position.z:F3}|{(int)actionProperty.GetValue(x)}";
    var pp = player.Position.Value;
    var bullets = GameObject.FindGameObjectsWithTag("Bullet").Length;
    // 発狂フェイズで台本が差し替わってもよいよう、ステップ番号ではなく今のステップが配置かどうかを残す
    var stepType = ((IReadOnlyList<BossPatternStep>)stepsField.GetValue(runner))[runner.StepIndex].Type;
    var isFormation = stepType == BossPatternStepType.CrossFormation || stepType == BossPatternStepType.DiagonalFormation;
    log.Add($"{Time.time:F3},{timeStop.IsTimeStopped.CurrentValue},{isFormation},{Member(a)},{Member(b)},{pp.x:F3}|{pp.z:F3},{bullets}");
}).AddTo(subs);
AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
return "{}";
'@ | Out-Null

    Start-Sleep -Seconds $Global:FormationMoveRecordSeconds

    # --- 共有体力を発狂の割合より少し下まで削り、横のずれつきの配置も記録する ---
    $cutSnippet = @'
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var ids = GetRunner().MemberIds;
var enemy = enemies.Enemies.First(e => e.Id == ids[0]);
var target = enemy.MaxHp * (group.RageHealthRatio - 0.02f);
enemies.Damage(new HitData(ids[0], enemy.Hp - target, App.Common.Data.HitDirectionType.None));
AppDomain.CurrentDomain.SetData("formationProbe.drive", new[] { __SPEED__f, __LOOP__f });
return $"{{\"time\":{Time.time},\"ratio\":{enemy.Hp / enemy.MaxHp}}}";
'@
    $cut = Invoke-BossSnippet -Body $cutSnippet.Replace('__SPEED__', "$Global:FormationMovePlayerSpeed").Replace('__LOOP__', "$Global:FormationMovePlayerLoopSeconds")
    Write-Host "発狂フェイズへ: 削った時刻 t=$($cut.time) / 体力の割合 $([Math]::Round($cut.ratio, 3))"
    Start-Sleep -Seconds $Global:FormationMoveRageRecordSeconds

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
AppDomain.CurrentDomain.SetData("formationProbe.drive", null);
return $"{{\"log\":\"{string.Join(";", log)}\"}}";
'@
    Stop-BossProbeRecorder

    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        function Parse([string]$m) {
            $p = $m.Split('|')
            [pscustomobject]@{ phase = $p[0]; slot = $p[1]; offset = [double]$p[2]; x = [double]$p[3]; z = [double]$p[4]; action = [int]$p[5] }
        }
        $pp = $c[5].Split('|')
        [pscustomobject]@{ time = [double]$c[0]; ts = ($c[1] -eq 'True'); formation = ($c[2] -eq 'True'); a = (Parse $c[3]); b = (Parse $c[4]); px = [double]$pp[0]; pz = [double]$pp[1]; bullets = [int]$c[6] }
    })
    Write-Host "記録: $($rows.Count) 行"

    # --- 配置のつき直しの時止めを区間に分ける ---
    $segments = @()
    $startIndex = -1
    for ($i = 0; $i -lt $rows.Count; $i++) {
        $isFormationStop = $rows[$i].ts -and $rows[$i].formation
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
    $radiusChanged = 0
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

                # 回りこみ: 向きは一方向にだけ進み、距離は起点と配置先の距離の間に収まる（プレイヤーを突っ切らない）。
                # 真反対（180度）へ移ると起点と終点だけでは回った向きが決まらないため、毎フレームの角度の変化を足して向きを決める
                $stepAngles = @()
                for ($i = 1; $i -lt $path.Count; $i++) {
                    $stepAngles += Get-AngleDelta -From (Get-AroundAngle -Member $path[$i - 1].$key -Row $path[$i - 1]) -To (Get-AroundAngle -Member $path[$i].$key -Row $path[$i])
                }
                $turn = ($stepAngles | Measure-Object -Sum).Sum
                $sign = [Math]::Sign($turn)
                # 2体が同じ円周を逆向きにすれ違うと NavMeshAgent の回避で押し合い、わずかに戻ることがある。戻った角度の合計で見る
                $backAngle = (@($stepAngles | Where-Object { $_ * $sign -lt 0 } | ForEach-Object { [Math]::Abs($_) }) + 0.0 | Measure-Object -Sum).Sum
                Assert-ProbeValue -Name "$label $role 回りこみの途中で逆へ戻った角度の合計（回った角度 $([Math]::Round($turn))度、度）" -Actual $backAngle -Expected 0 -Tolerance $Global:FormationMoveBackAngleTolerance | Out-Null
                $r0 = Get-PlayerDistance -Member $before.$key -Row $before
                $r1 = Get-PlayerDistance -Member $end.$key -Row $end
                if ([Math]::Abs($r1 - $r0) -gt 0.5) { $radiusChanged++ }
                $distances = @($path | ForEach-Object { Get-PlayerDistance -Member $_.$key -Row $_ })
                $minR = ($distances | Measure-Object -Minimum).Minimum
                $maxR = ($distances | Measure-Object -Maximum).Maximum
                Assert-ProbeTrue -Name "$label $role プレイヤーとの距離が起点と配置先の距離の間に収まる（突っ切らない）" -Condition ($minR -ge [Math]::Min($r0, $r1) - $Global:FormationMoveRadiusTolerance -and $maxR -le [Math]::Max($r0, $r1) + $Global:FormationMoveRadiusTolerance) -Detail ("起点 {0:F2}m → 配置先 {1:F2}m / 途中 {2:F2}〜{3:F2}m" -f $r0, $r1, $minR, $maxR) | Out-Null
            }
            else {
                Write-Host ("    {0} は配置先がほぼ同じ（移動 {1:F2}m）なので連続性は判定しない" -f $role, $total)
            }
            $gap = Get-FormationTargetGap -Member $end.$key -Row $end -Distance $config.distance
            Assert-ProbeValue -Name "$label $role 時止めが解けた時点の配置先からのずれ（$($end.$key.slot) / ずれ$($end.$key.offset)、m）" -Actual $gap -Expected 0 -Tolerance $Global:FormationMoveArriveTolerance | Out-Null
        }
    }

    # --- 発狂フェイズ: 帯の攻撃を終えてから次の配置の時止めまで、プレイヤーが動いても攻撃した位置に留まる ---
    $holdChecked = 0
    foreach ($segment in $segments) {
        if ($rows[$segment[0]].time -le $cut.time) { continue }
        foreach ($key in 'a', 'b') {
            $role = $key.ToUpper()
            # 時止めの直前から遡り、最後に行動していた行を探す
            $last = -1
            for ($i = $segment[0] - 1; $i -ge 0 -and $rows[$i].time -gt $cut.time; $i--) {
                if ($rows[$i].$key.phase -ne 'Ready') { $last = $i; break }
            }
            # 帯の攻撃（行動1・2）のあとだけを見る（弾幕のあとはプレイヤーを追う）
            if ($last -lt 0 -or ($rows[$last].$key.action -ne 1 -and $rows[$last].$key.action -ne 2)) { continue }
            $window = @($rows[($last + 1)..($segment[0] - 1)])
            if ($window.Count -lt 2) { continue }
            $origin = $window[0]
            $maxShift = ($window | ForEach-Object { [Math]::Sqrt([Math]::Pow($_.$key.x - $origin.$key.x, 2) + [Math]::Pow($_.$key.z - $origin.$key.z, 2)) } | Measure-Object -Maximum).Maximum
            $playerMove = [Math]::Sqrt([Math]::Pow($window[-1].px - $origin.px, 2) + [Math]::Pow($window[-1].pz - $origin.pz, 2))
            $holdChecked++
            Assert-ProbeValue -Name ("帯の攻撃のあと {0} は次の配置まで攻撃した位置に留まる（t={1:F2}〜{2:F2} / プレイヤーの移動 {3:F2}m、ボスのずれの最大 m）" -f $role, $origin.time, $window[-1].time, $playerMove) -Actual $maxShift -Expected 0 -Tolerance $Global:FormationMoveHoldTolerance | Out-Null
        }
    }
    Assert-ProbeTrue -Name '帯の攻撃のあとの留まりを判定できた' -Condition ($holdChecked -ge 2) -Detail "$holdChecked 回" | Out-Null

    Assert-ProbeTrue -Name '回りこみながら距離を詰める・離す移動（横のずれで配置先の距離が変わる）を記録できた' -Condition ($radiusChanged -ge 1) -Detail "$radiusChanged 回" | Out-Null

    # --- 時止めの外ではワープしない ---
    $maxOutside = 0
    $where = ''
    for ($i = 1; $i -lt $rows.Count; $i++) {
        if ($rows[$i].ts -or $rows[$i - 1].ts) { continue }
        foreach ($key in 'a', 'b') {
            $d = [Math]::Sqrt([Math]::Pow($rows[$i].$key.x - $rows[$i - 1].$key.x, 2) + [Math]::Pow($rows[$i].$key.z - $rows[$i - 1].$key.z, 2))
            if ($d -gt $maxOutside) { $maxOutside = $d; $where = "t=$($rows[$i].time) $key"; $whereIndex = $i }
        }
    }
    if ($maxOutside -gt $Global:FormationMoveWarpThreshold) {
        # 調査用に前後の記録を出す（時刻,時止め,配置のステップか,A,B,プレイヤー,弾）
        foreach ($row in $rows[[Math]::Max(0, $whereIndex - 3)..[Math]::Min($rows.Count - 1, $whereIndex + 2)]) {
            Write-Host ("    t={0:F3} ts={1} 配置={2} A={3}/{4}/{5:F1}/({6:F2},{7:F2}) B={8}/{9}/{10:F1}/({11:F2},{12:F2}) P=({13:F2},{14:F2})" -f $row.time, $row.ts, $row.formation, $row.a.phase, $row.a.slot, $row.a.offset, $row.a.x, $row.a.z, $row.b.phase, $row.b.slot, $row.b.offset, $row.b.x, $row.b.z, $row.px, $row.pz)
        }
    }
    Assert-ProbeTrue -Name '時止めの外でボスが1フレームで大きく飛ばない' -Condition ($maxOutside -le $Global:FormationMoveWarpThreshold) -Detail ("1フレームの最大 {0:F2}m（{1}）" -f $maxOutside, $where) | Out-Null
}
