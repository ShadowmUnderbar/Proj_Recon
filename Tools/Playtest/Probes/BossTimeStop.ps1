#
# 二人組ボス「TickTock」の時止めの回りこみ攻撃（台本の TimeStopOrbit / BossTickTock の行動3・4 / TimeStopDataStore）を実プレイで検証するプローブ。
#
# デバッグ対戦（Request-DebugArena）で BossGroup_TickTock と戦い、通常の台本（弾幕の交代2回のあと）に入っている時止めまで待って、次を確かめる。
#   - 2体とも行動が明けてから時を止め、時止めと同時に2体が回りこみ始める（1体は回りこみ連射＝ActionIndex、もう1体は回りこむだけ＝PartnerActionIndex）
#   - 2体ともプレイヤーを中心に同じ向きへ90度回りこむ（距離は _keepDistance のまま）。回りこみ終えたら回りこんだ先の配置につく
#   - 時止めの長さは回りこみの秒数（_orbitSeconds）。連射役だけが _orbitFireInterval ごとに撃ち、撃った弾は時止めの間その場で止まる
#   - 時止め中はプレイヤーが動けない（W を押しても動かない。時止めの前は動く）、被弾しない
#   - 時止めを解くと止めていた弾が動き出し、立ち止まっているプレイヤーの位置へ向かう（水平距離で見る。
#     銃口の高さが当たり判定より高く頭上を抜けることがあるため、被弾そのものは判定しない）。一息（WaitSeconds）の間は2体とも待機し、そのあと台本の先頭へ戻る
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。デバッグ対戦の無敵で HP は減らない（被弾の通知は流れる）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:TimeStopGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
# 時止めの前に、移動の入力が効くことを確かめる押下秒数
$Global:TimeStopControlPressSeconds = 0.5
# 時止め中に移動の入力を押し続ける秒数
$Global:TimeStopPressSeconds = 0.5

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -BossGroupPath $Global:TimeStopGroupPath -AutoRespawn $false -Wave 5
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
        Invoke-TimeStopProbeBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Get-OrbitAngle {
    # プレイヤーから見たボスの向き（度。+Z を0、時計回り＝+X 側へ回ると増える）
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

function Get-Turn90 {
    # BossFormationSlotExtensions.Turn90 と同じ対応（時計回りなら sign=+1）
    param([string]$Slot, [int]$Sign)
    $clockwise = @{ Up = 'Right'; Right = 'Down'; Down = 'Left'; Left = 'Up'; UpLeft = 'UpRight'; UpRight = 'DownRight'; DownRight = 'DownLeft'; DownLeft = 'UpLeft' }
    if ($Sign -gt 0) { return $clockwise[$Slot] }
    foreach ($key in $clockwise.Keys) { if ($clockwise[$key] -eq $Slot) { return $key } }
    return $null
}

function Invoke-TimeStopProbeBody {
    Start-Sleep -Seconds 2

    $config = Invoke-BossSnippet -Body @'
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var step = group.Pattern.First(s => s.Type == BossPatternStepType.TimeStopOrbit);
var md = group.Members[0].EnemyMasterData;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(md.PrefabPath).GetComponent<BossTickTock>();
var so = new UnityEditor.SerializedObject(prefab);
var stepIndex = group.Pattern.ToList().IndexOf(step);
return $"{{\"action\":{step.ActionIndex},\"partnerAction\":{step.PartnerActionIndex},\"breath\":{step.WaitSeconds},\"stepIndex\":{stepIndex},\"orbit\":{so.FindProperty("_orbitSeconds").floatValue},\"interval\":{so.FindProperty("_orbitFireInterval").floatValue},\"distance\":{so.FindProperty("_keepDistance").floatValue},\"bulletSpeed\":{md.BulletSpeed}}}";
'@
    Write-Host "設定: 回りこみ連射 行動 $($config.action) / 回りこみ 行動 $($config.partnerAction) / 回りこみ $($config.orbit)秒・連射の間隔 $($config.interval)秒・距離 $($config.distance)m / 一息 $($config.breath)秒 / 台本のステップ $($config.stepIndex)"

    # --- 毎フレームの記録 ---
    $recorder = @'
var log = new List<string>();
var hits = new List<string>();
var timeStop = scope.Container.Resolve<ITimeStopDataStore>();
var actionProperty = typeof(BossAIBase).GetProperty("CurrentActionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
var lastBullets = new Dictionary<int, Vector3>();
string lastKey = null;
var frame = 0;

var subs = new CompositeDisposable();
player.OnDamaged.Subscribe(amount => hits.Add($"{Time.time:F3}|{amount}|{timeStop.IsTimeStopped.CurrentValue}")).AddTo(subs);
Observable.EveryUpdate().Subscribe(_ =>
{
    var runner = GetRunner();
    var a = GetBoss(0);
    var b = GetBoss(1);
    if (runner == null || a == null || b == null) return;

    // 弾の数と、1フレームあたりの移動量の最大（止まっていれば0）、プレイヤーとの水平距離の最小
    var maxMove = 0f;
    var minDistance = 999f;
    var pp = player.Position.Value;
    var bullets = GameObject.FindGameObjectsWithTag("Bullet");
    var current = new Dictionary<int, Vector3>();
    foreach (var bullet in bullets)
    {
        var id = bullet.GetInstanceID();
        var position = bullet.transform.position;
        current[id] = position;
        minDistance = Mathf.Min(minDistance, new Vector2(position.x - pp.x, position.z - pp.z).magnitude);
        if (lastBullets.TryGetValue(id, out var previous)) maxMove = Mathf.Max(maxMove, Vector3.Distance(previous, position));
    }
    lastBullets = current;

    string Member(BossAIBase x) =>
        $"{x.Status.CurrentValue.Phase}|{(int)actionProperty.GetValue(x)}|{GetFormation(x)}|{x.transform.position.x:F3}|{x.transform.position.z:F3}|{(bool)holdField.GetValue(x)}";
    var ma = Member(a); var mb = Member(b);
    var ts = timeStop.IsTimeStopped.CurrentValue;
    string Key(string m) { var p = m.Split('|'); return $"{p[0]}|{p[1]}|{p[2]}|{p[5]}"; }
    var key = $"{ts},{runner.StepIndex},{Key(ma)},{Key(mb)}";
    frame++;
    // 状態が変わったフレームと、時止めのステップの間（解いたあとの一息で弾が動き出すのも見る）は毎フレーム、それ以外は3フレームに1回を残す
    if (key == lastKey && runner.StepIndex != __STEP__ && frame % 3 != 0) return;
    lastKey = key;
    log.Add($"{Time.time:F3},{ts},{runner.StepIndex},{ma},{mb},{pp.x:F3}|{pp.z:F3},{bullets.Length}|{maxMove:F4}|{minDistance:F2}");
}).AddTo(subs);

AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
AppDomain.CurrentDomain.SetData("bossProbe.hits", hits);
return "{}";
'@
    Invoke-BossSnippet -Body $recorder.Replace('__STEP__', "$($config.stepIndex)") | Out-Null

    $positionSnippet = @'
var p = player.Position.Value;
var ts = scope.Container.Resolve<ITimeStopDataStore>().IsTimeStopped.CurrentValue;
return $"{{\"x\":{p.x},\"z\":{p.z},\"time\":{Time.time},\"ts\":{ts.ToString().ToLower()}}}";
'@

    # --- 時止めの前は移動の入力が効く ---
    $before = Invoke-BossSnippet -Body $positionSnippet
    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'W'; duration = "$($Global:TimeStopControlPressSeconds)" } | Out-Null
    Start-Sleep -Milliseconds 300
    $after = Invoke-BossSnippet -Body $positionSnippet
    $controlMove = [Math]::Sqrt([Math]::Pow($after.x - $before.x, 2) + [Math]::Pow($after.z - $before.z, 2))
    Assert-ProbeTrue -Name '時止めの前は W で移動できる' -Condition ($controlMove -gt 0.3) -Detail ("移動 {0:F2}m" -f $controlMove) | Out-Null

    # --- 時止めを待ち、止まっている間に W を押す（時止めは回りこみの秒数しか続かないので、間に合った分だけ判定する） ---
    $state = $null
    for ($i = 0; $i -lt 300; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if ($state.ts) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not (Assert-ProbeTrue -Name '台本どおりに時止めが始まる' -Condition ($state.ts) -Detail "t=$($state.time)")) { return }
    $pressStart = $state.time
    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'W'; duration = "$($Global:TimeStopPressSeconds)" } | Out-Null
    $pressEnd = (Invoke-BossSnippet -Body $positionSnippet).time
    Write-Host ("時止め中に W を押した時刻 t={0:F2}〜{1:F2}" -f $pressStart, $pressEnd)

    # 解いてから止めていた弾がプレイヤーに届くまで（距離÷弾速に余裕を足す）
    $arrival = $config.distance / [Math]::Max(0.1, $config.bulletSpeed) + 1.5

    # 回りこみ → 弾が届く・一息 → 台本の先頭へ戻るまで
    Start-Sleep -Seconds ([Math]::Ceiling($config.orbit + [Math]::Max($arrival, $config.breath) + 3))

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
var hits = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.hits");
return $"{{\"log\":\"{string.Join(";", log)}\",\"hits\":\"{string.Join(";", hits)}\"}}";
'@

    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        function Parse([string]$m) {
            $p = $m.Split('|')
            [pscustomobject]@{ phase = $p[0]; action = [int]$p[1]; slot = $p[2]; x = [double]$p[3]; z = [double]$p[4]; hold = ($p[5] -eq 'True') }
        }
        $pp = $c[5].Split('|')
        $bb = $c[6].Split('|')
        [pscustomobject]@{ time = [double]$c[0]; ts = ($c[1] -eq 'True'); step = [int]$c[2]; a = (Parse $c[3]); b = (Parse $c[4]); px = [double]$pp[0]; pz = [double]$pp[1]; bullets = [int]$bb[0]; bulletMove = [double]$bb[1]; bulletDistance = [double]$bb[2] }
    })
    $hits = @($data.hits.Split(';') | Where-Object { $_ } | ForEach-Object {
        $h = $_.Split('|')
        [pscustomobject]@{ time = [double]$h[0]; ts = ($h[2] -eq 'True') }
    })
    Write-Host "記録: $($rows.Count) 行 / 被弾の通知 $($hits.Count) 回"

    $stopRows = @($rows | Where-Object { $_.ts })
    $firstStop = [Array]::IndexOf($rows, $stopRows[0])
    $lastStop = [Array]::IndexOf($rows, $stopRows[-1])
    $beforeRow = $rows[[Math]::Max(0, $firstStop - 1)]
    $startRow = $stopRows[0]
    $endRow = $rows[$lastStop + 1]
    $duration = $endRow.time - $startRow.time
    Write-Host ("時止め: t={0:F3}〜{1:F3}（{2:F2}秒）" -f $startRow.time, $endRow.time, $duration)

    # --- 時止めの始まり ---
    Assert-ProbeTrue -Name '時止めの直前は2体とも行動していない' -Condition ($beforeRow.a.phase -eq 'Ready' -and $beforeRow.b.phase -eq 'Ready') -Detail "A=$($beforeRow.a.phase) B=$($beforeRow.b.phase)" | Out-Null
    Assert-ProbeTrue -Name '時止めは台本の TimeStopOrbit ステップで起きる' -Condition (@($stopRows | Where-Object { $_.step -ne $config.stepIndex }).Count -eq 0) -Detail "ステップ $($config.stepIndex)" | Out-Null
    $actions = @($startRow.a.action, $startRow.b.action) | Sort-Object
    $expectedActions = @($config.action, $config.partnerAction) | Sort-Object
    Assert-ProbeTrue -Name '時止めと同時に、1体は回りこみ連射・もう1体は回りこみを始める' -Condition (($actions -join ',') -eq ($expectedActions -join ',') -and $startRow.a.phase -ne 'Ready' -and $startRow.b.phase -ne 'Ready') -Detail "A=$($startRow.a.phase)/行動$($startRow.a.action) B=$($startRow.b.phase)/行動$($startRow.b.action)" | Out-Null
    Assert-ProbeValue -Name '回りこみ中は2体とも待機していない（待機している行数）' -Actual @($stopRows | Where-Object { $_.a.hold -or $_.b.hold }).Count -Expected 0 | Out-Null
    Assert-ProbeValue -Name '時止めの長さ＝回りこみの秒数（秒）' -Actual $duration -Expected $config.orbit -Tolerance 0.1 | Out-Null

    # --- 回りこみ ---
    $signs = @()
    foreach ($key in 'a', 'b') {
        $label = $key.ToUpper()
        $startAngle = Get-OrbitAngle -Member $beforeRow.$key -Row $beforeRow
        $endAngle = Get-OrbitAngle -Member $endRow.$key -Row $endRow
        $delta = Get-AngleDelta -From $startAngle -To $endAngle
        $sign = [Math]::Sign($delta)
        $signs += $sign
        Assert-ProbeValue -Name "$label プレイヤーを中心に回りこんだ角度の大きさ（度）" -Actual ([Math]::Abs($delta)) -Expected 90 -Tolerance 3 | Out-Null

        # 回りこみの途中も一定距離を保ち、向きは一方向にだけ進む
        $distances = @($stopRows | ForEach-Object { [Math]::Sqrt([Math]::Pow($_.$key.x - $_.px, 2) + [Math]::Pow($_.$key.z - $_.pz, 2)) })
        $maxGap = ($distances | ForEach-Object { [Math]::Abs($_ - $config.distance) } | Measure-Object -Maximum).Maximum
        Assert-ProbeValue -Name "$label 回りこみ中のプレイヤーとの距離のずれの最大（m）" -Actual $maxGap -Expected 0 -Tolerance 0.3 | Out-Null
        $back = 0
        for ($i = 1; $i -lt $stopRows.Count; $i++) {
            $step = Get-AngleDelta -From (Get-OrbitAngle -Member $stopRows[$i - 1].$key -Row $stopRows[$i - 1]) -To (Get-OrbitAngle -Member $stopRows[$i].$key -Row $stopRows[$i])
            if ($step * $sign -lt -0.5) { $back++ }
        }
        Assert-ProbeValue -Name "$label 回りこみの途中で逆へ戻った回数" -Actual $back -Expected 0 | Out-Null

        $expectedSlot = Get-Turn90 -Slot $beforeRow.$key.slot -Sign $sign
        $settled = @($rows | Where-Object { $_.time -gt $endRow.time + 0.1 } | Select-Object -First 1)
        $settledSlot = if ($settled.Count -gt 0) { $settled[0].$key.slot } else { '記録なし' }
        Assert-ProbeTrue -Name "$label 回りこみ終えたら回りこんだ先の配置につく" -Condition ($settledSlot -eq $expectedSlot) -Detail "$($beforeRow.$key.slot) → $settledSlot（期待 $expectedSlot）" | Out-Null
    }
    Assert-ProbeTrue -Name '2体とも同じ向きに回りこむ' -Condition ($signs[0] -eq $signs[1] -and $signs[0] -ne 0) -Detail ("A={0} B={1}（+1＝時計回り）" -f $signs[0], $signs[1]) | Out-Null

    # --- 連射と弾の停止 ---
    $fired = $endRow.bullets - $beforeRow.bullets
    $expectedShots = [Math]::Ceiling($config.orbit / $config.interval)
    Assert-ProbeTrue -Name '時止め中に連射役が撃った弾の数（回りこみの秒数÷間隔）' -Condition ([Math]::Abs($fired - $expectedShots) -le 1) -Detail "増えた弾 $fired 発（目安 $expectedShots 発）" | Out-Null
    $bulletRows = @($stopRows | Select-Object -Skip 1 | Where-Object { $_.bullets -gt 0 })
    if ($bulletRows.Count -gt 0) {
        $maxMove = ($bulletRows | Measure-Object -Property bulletMove -Maximum).Maximum
        Assert-ProbeValue -Name '時止め中は弾が止まる（撃ったばかりの弾も。1フレームの移動の最大、m）' -Actual $maxMove -Expected 0 -Tolerance 0.001 | Out-Null
    }
    $releasedRows = @($rows | Where-Object { $_.time -gt $endRow.time -and $_.time -le $endRow.time + 0.5 -and $_.bullets -gt 0 })
    $releasedMove = if ($releasedRows.Count -gt 0) { ($releasedRows | Measure-Object -Property bulletMove -Maximum).Maximum } else { 0 }
    Assert-ProbeTrue -Name '時止めを解くと止めていた弾が動き出す' -Condition ($releasedMove -gt 0.01) -Detail ("解いた直後の1フレームの移動の最大 {0:F3}m" -f $releasedMove) | Out-Null

    # --- 時止め中のプレイヤー ---
    Assert-ProbeValue -Name '時止め中は被弾しない' -Actual @($hits | Where-Object { $_.ts }).Count -Expected 0 | Out-Null
    $pressRows = @($stopRows | Where-Object { $_.time -ge $pressStart -and $_.time -le $pressEnd + 0.3 })
    if ($pressRows.Count -gt 1) {
        $stopMove = [Math]::Sqrt([Math]::Pow($pressRows[-1].px - $pressRows[0].px, 2) + [Math]::Pow($pressRows[-1].pz - $pressRows[0].pz, 2))
        Assert-ProbeTrue -Name '時止め中は W を押しても動かない' -Condition ($stopMove -lt 0.01) -Detail ("押している間の記録 {0} 行 / 移動 {1:F3}m" -f $pressRows.Count, $stopMove) | Out-Null
    }
    else {
        Write-Host '    W を押せたのが時止めが解けたあとだったため、時止め中の移動は判定しない'
    }
    $arrivalRows = @($rows | Where-Object { $_.time -gt $endRow.time -and $_.time -le $endRow.time + $arrival })
    $closest = if ($arrivalRows.Count -gt 0) { ($arrivalRows | Measure-Object -Property bulletDistance -Minimum).Minimum } else { 999 }
    Assert-ProbeTrue -Name '時止めを解くと、止めていた弾が立ち止まっているプレイヤーの位置へ届く（水平距離）' -Condition ($closest -lt 1.0) -Detail ("解いてから {0:F1}秒（距離÷弾速＋余裕）の間の最小の水平距離 {1:F2}m" -f $arrival, $closest) | Out-Null

    # --- 一息と、終わったあと ---
    $breathRows = @($rows | Where-Object { $_.time -gt $endRow.time + 0.05 -and $_.time -lt $endRow.time + $config.breath - 0.1 })
    Assert-ProbeTrue -Name '時止めを解いてから一息の間、2体とも待機する' -Condition ($breathRows.Count -gt 0 -and @($breathRows | Where-Object { -not ($_.a.hold -and $_.b.hold) }).Count -eq 0) -Detail "記録 $($breathRows.Count) 行" | Out-Null
    $afterRows = @($rows | Where-Object { $_.time -gt $endRow.time + $config.breath + 0.3 })
    Assert-ProbeTrue -Name '一息のあと待機を解いて台本の先頭へ戻る' -Condition ($afterRows.Count -gt 0 -and $afterRows[-1].step -ne $config.stepIndex -and -not $afterRows[-1].a.hold -and -not $afterRows[-1].b.hold) -Detail $(if ($afterRows.Count -gt 0) { "ステップ $($afterRows[-1].step) / 待機 A=$($afterRows[-1].a.hold) B=$($afterRows[-1].b.hold)" } else { '記録なし' }) | Out-Null
    Assert-ProbeValue -Name '時止めは1回だけ（解けたあとに止まった行数）' -Actual @($rows | Select-Object -Skip ($lastStop + 1) | Where-Object { $_.ts }).Count -Expected 0 | Out-Null

    # --- 時止めの途中で全員倒すと時止めが解ける（解けないと次のウェーブでも動けなくなる） ---
    $state = $null
    for ($i = 0; $i -lt 400; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if ($state.ts) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not (Assert-ProbeTrue -Name '台本を一周して2回目の時止めが始まる' -Condition ($state.ts) -Detail "t=$($state.time)")) { return }
    Invoke-BossSnippet -Body @'
var ids = GetRunner().MemberIds;
var enemy = enemies.Enemies.First(e => e.Id == ids[0]);
enemies.Damage(new HitData(ids[0], enemy.Hp + 1f, App.Common.Data.HitDirectionType.None));
return "{}";
'@ | Out-Null
    Start-Sleep -Milliseconds 500
    $killed = Invoke-BossSnippet -Body @'
var ts = scope.Container.Resolve<ITimeStopDataStore>().IsTimeStopped.CurrentValue;
var bosses = enemies.Enemies.Count(IsBossEnemy);
return $"{{\"ts\":{ts.ToString().ToLower()},\"bosses\":{bosses},\"groups\":{bossGroups.HasAliveGroup.ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name '時止めの途中で全員倒すと時止めが解ける' -Condition ((-not $killed.ts) -and $killed.bosses -eq 0 -and -not $killed.groups) -Detail "時止め=$($killed.ts) ボス=$($killed.bosses) グループ=$($killed.groups)" | Out-Null
    $before = Invoke-BossSnippet -Body $positionSnippet
    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'W'; duration = "$($Global:TimeStopControlPressSeconds)" } | Out-Null
    Start-Sleep -Milliseconds 300
    $after = Invoke-BossSnippet -Body $positionSnippet
    $move = [Math]::Sqrt([Math]::Pow($after.x - $before.x, 2) + [Math]::Pow($after.z - $before.z, 2))
    Assert-ProbeTrue -Name '倒したあとは W で移動できる' -Condition ($move -gt 0.3) -Detail ("移動 {0:F2}m" -f $move) | Out-Null
}
