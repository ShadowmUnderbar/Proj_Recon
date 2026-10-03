#
# 二人組ボス「TickTock」の時止めの記憶攻撃（台本の TimeStopMemory / BossTickTock の行動3・4 / TimeStopDataStore）を実プレイで検証するプローブ。
#
# デバッグ対戦（Request-DebugArena）で BossGroup_TickTock と戦い、通常の台本（弾幕の交代2回のあと）に入っている時止めまで待って、次を確かめる。
#   - 2体とも行動が明けてから時を止め、時止め中は2体ともその場で待機する
#   - 時止め中は、1体ずつ上下左右のどこかへ配置して帯の予兆だけを MemoryCount 回見せる（当てない）。予兆の長さは MemoryTelegraphSeconds
#   - 時止め中はプレイヤーが動けない（W を押しても動かない。時止めの前は動く）、弾が止まる、被弾しない
#   - 時止めを解いて一息（WaitSeconds）おいてから、見せたときと同じ個体・配置・横のずれで、同じ順に帯を当てる
#     （予兆は ReplayTelegraphSeconds。立ち止まっていれば攻撃の瞬間に当たる）
#   - 攻撃が済んだら待機を解き、台本の先頭へ戻る。帯の表示が残らない
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。デバッグ対戦の無敵で HP は減らない（被弾の通知は流れる）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:TimeStopGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
# 時止めの前に、移動の入力が効くことを確かめる押下秒数
$Global:TimeStopControlPressSeconds = 0.5
# 時止め中に移動の入力を押し続ける秒数
$Global:TimeStopPressSeconds = 1.0

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

function Assert-WindupLength {
    # 予兆が始まったのは「予兆を記録した行」と「その1つ前の行」の間のどこか（BossRagePhase の Assert-TelegraphLength と同じ考え方）
    param([string]$Label, $Event, [double]$Expected)
    $shortest = $Event.endTime - $Event.startTime
    $longest = $Event.endTime - $Event.previousTime
    $ok = ($Expected -ge $shortest - 0.05) -and ($Expected -le $longest + 0.05)
    Assert-ProbeTrue -Name "$Label 予兆の長さが設定どおり" -Condition $ok -Detail ("{0:F3}〜{1:F3}秒（設定 {2}秒）" -f $shortest, $longest, $Expected) | Out-Null
}

function Get-TimeStopWindups {
    # 行（記録）から、指定の行動の予兆（Windup に入ってから抜けるまで）を個体ごとに拾い、時刻順に返す
    param($Rows, [int]$Action)
    $events = @()
    foreach ($key in 'a', 'b') {
        $open = $null
        for ($i = 0; $i -lt $Rows.Count; $i++) {
            $m = $Rows[$i].$key
            $isWindup = $m.phase -eq 'Windup' -and $m.action -eq $Action
            if ($isWindup -and $null -eq $open) {
                $open = [pscustomobject]@{
                    member = $key; slot = $m.slot; offset = $m.offset; strip = $true
                    startTime = $Rows[$i].time; previousTime = $(if ($i -gt 0) { $Rows[$i - 1].time } else { $Rows[$i].time })
                    endTime = $null; endPhase = $null; timeStopped = $Rows[$i].ts
                }
            }
            if ($isWindup -and $null -ne $open -and -not $m.strip) { $open.strip = $false }
            if (-not $isWindup -and $null -ne $open) {
                $open.endTime = $Rows[$i].time
                $open.endPhase = $m.phase
                $events += $open
                $open = $null
            }
        }
    }
    return @($events | Sort-Object startTime)
}

function Invoke-TimeStopProbeBody {
    Start-Sleep -Seconds 2

    $config = Invoke-BossSnippet -Body @'
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var step = group.Pattern.First(s => s.Type == BossPatternStepType.TimeStopMemory);
var md = group.Members[0].EnemyMasterData;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(md.PrefabPath).GetComponent<BossTickTock>();
var so = new UnityEditor.SerializedObject(prefab);
var strike = (BossLineStrikeConfig)so.FindProperty("_lineStrikeConfig").objectReferenceValue;
var stepIndex = group.Pattern.ToList().IndexOf(step);
return $"{{\"count\":{step.MemoryCount},\"action\":{step.ActionIndex},\"replayAction\":{step.ReplayActionIndex},\"breath\":{step.WaitSeconds},\"stepIndex\":{stepIndex},\"memoryTelegraph\":{strike.MemoryTelegraphSeconds},\"replayTelegraph\":{strike.ReplayTelegraphSeconds}}}";
'@
    Write-Host "設定: 予兆 $($config.count) 回（行動 $($config.action) / $($config.memoryTelegraph)秒）/ 一息 $($config.breath)秒 / 攻撃 行動 $($config.replayAction)（予兆 $($config.replayTelegraph)秒）/ 台本のステップ $($config.stepIndex)"

    # --- 毎フレームの記録 ---
    Invoke-BossSnippet -Body @'
var log = new List<string>();
var hits = new List<string>();
var timeStop = scope.Container.Resolve<ITimeStopDataStore>();
var actionProperty = typeof(BossAIBase).GetProperty("CurrentActionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
var offsetProperty = typeof(BossAIBase).GetProperty("FormationLateralOffset", BindingFlags.NonPublic | BindingFlags.Instance);
var stripField = typeof(BossTickTock).GetField("_lineStrikeView", BindingFlags.NonPublic | BindingFlags.Instance);
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

    // 弾の1フレームあたりの移動量の最大（止まっていれば0）
    var maxMove = 0f;
    var bullets = GameObject.FindGameObjectsWithTag("Bullet");
    var current = new Dictionary<int, Vector3>();
    foreach (var bullet in bullets)
    {
        var id = bullet.GetInstanceID();
        var position = bullet.transform.position;
        current[id] = position;
        if (lastBullets.TryGetValue(id, out var previous)) maxMove = Mathf.Max(maxMove, Vector3.Distance(previous, position));
    }
    lastBullets = current;

    string Member(BossAIBase x)
    {
        var strip = (BossLineStrikeView)stripField.GetValue(x);
        var visible = strip != null && strip.IsVisible;
        return $"{x.Status.CurrentValue.Phase}|{(int)actionProperty.GetValue(x)}|{GetFormation(x)}|{(float)offsetProperty.GetValue(x):F1}|{x.transform.position.x:F2}|{x.transform.position.z:F2}|{visible}|{(bool)holdField.GetValue(x)}";
    }
    var ma = Member(a); var mb = Member(b);
    var ts = timeStop.IsTimeStopped.CurrentValue;
    string Key(string m) { var p = m.Split('|'); return $"{p[0]}|{p[1]}|{p[2]}|{p[3]}|{p[6]}|{p[7]}"; }
    var key = $"{ts},{runner.StepIndex},{Key(ma)},{Key(mb)}";
    frame++;
    // 状態が変わったフレームと、時止め中は毎フレーム（弾・プレイヤーの停止を見る）、それ以外は3フレームに1回を残す
    if (key == lastKey && !ts && frame % 3 != 0) return;
    lastKey = key;
    var pp = player.Position.Value;
    log.Add($"{Time.time:F3},{ts},{runner.StepIndex},{ma},{mb},{pp.x:F3}|{pp.z:F3},{bullets.Length}|{maxMove:F4}");
}).AddTo(subs);

AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
AppDomain.CurrentDomain.SetData("bossProbe.hits", hits);
return "{}";
'@ | Out-Null

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

    # --- 時止めを待ち、止まっている間に W を押し続ける ---
    $state = $null
    for ($i = 0; $i -lt 120; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if ($state.ts) { break }
        Start-Sleep -Milliseconds 250
    }
    if (-not (Assert-ProbeTrue -Name '台本どおりに時止めが始まる' -Condition ($state.ts) -Detail "t=$($state.time)")) { return }
    $pressStart = $state.time
    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'W'; duration = "$($Global:TimeStopPressSeconds)" } | Out-Null
    $pressEnd = (Invoke-BossSnippet -Body $positionSnippet).time
    Write-Host ("時止め中に W を押した時刻 t={0:F2}〜{1:F2}" -f $pressStart, $pressEnd)

    # 予兆3回（約1.6秒ずつ）→ 一息 → 攻撃3回（約1.3秒ずつ）→ 台本の先頭へ戻るまで
    Start-Sleep -Seconds ([Math]::Ceiling(($config.memoryTelegraph + 0.5) * $config.count + $config.breath + ($config.replayTelegraph + 0.15 + 0.5 + 0.3) * $config.count + 3))

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
var hits = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.hits");
return $"{{\"log\":\"{string.Join(";", log)}\",\"hits\":\"{string.Join(";", hits)}\"}}";
'@

    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        function Parse([string]$m) {
            $p = $m.Split('|')
            [pscustomobject]@{ phase = $p[0]; action = [int]$p[1]; slot = $p[2]; offset = [double]$p[3]; x = [double]$p[4]; z = [double]$p[5]; strip = ($p[6] -eq 'True'); hold = ($p[7] -eq 'True') }
        }
        $pp = $c[5].Split('|')
        $bb = $c[6].Split('|')
        [pscustomobject]@{ time = [double]$c[0]; ts = ($c[1] -eq 'True'); step = [int]$c[2]; a = (Parse $c[3]); b = (Parse $c[4]); px = [double]$pp[0]; pz = [double]$pp[1]; bullets = [int]$bb[0]; bulletMove = [double]$bb[1] }
    })
    $hits = @($data.hits.Split(';') | Where-Object { $_ } | ForEach-Object {
        $h = $_.Split('|')
        [pscustomobject]@{ time = [double]$h[0]; ts = ($h[2] -eq 'True') }
    })
    Write-Host "記録: $($rows.Count) 行 / 被弾の通知 $($hits.Count) 回"

    $stopRows = @($rows | Where-Object { $_.ts })
    $firstStop = [Array]::IndexOf($rows, $stopRows[0])
    $lastStop = [Array]::IndexOf($rows, $stopRows[-1])
    $endTime = $rows[$lastStop + 1].time
    Write-Host ("時止め: t={0:F3}〜{1:F3}（{2:F2}秒）" -f $stopRows[0].time, $endTime, ($endTime - $stopRows[0].time))

    # --- 時止めの始まり ---
    $startRow = $stopRows[0]
    Assert-ProbeTrue -Name '時止めの時点で2体とも行動していない' -Condition ($startRow.a.phase -ne 'Active' -and $startRow.b.phase -ne 'Active') -Detail "A=$($startRow.a.phase) B=$($startRow.b.phase)" | Out-Null
    Assert-ProbeValue -Name '時止め中、2体ともその場で待機している（待機していない行数）' -Actual @($stopRows | Where-Object { -not ($_.a.hold -and $_.b.hold) }).Count -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '時止めは台本の TimeStopMemory ステップで起きる' -Condition (@($stopRows | Where-Object { $_.step -ne $config.stepIndex }).Count -eq 0) -Detail "ステップ $($config.stepIndex)" | Out-Null

    # --- 時止め中の予兆 ---
    $shown = @(Get-TimeStopWindups -Rows $rows -Action $config.action)
    Assert-ProbeValue -Name '時止め中に見せた予兆の回数' -Actual $shown.Count -Expected $config.count | Out-Null
    Assert-ProbeTrue -Name '予兆はすべて時止めの間に出る' -Condition (@($shown | Where-Object { -not $_.timeStopped -or $_.endTime -gt $endTime }).Count -eq 0) | Out-Null
    $overlap = 0
    for ($i = 1; $i -lt $shown.Count; $i++) { if ($shown[$i].startTime -lt $shown[$i - 1].endTime) { $overlap++ } }
    Assert-ProbeValue -Name '予兆は1回に1体だけ（重なった回数）' -Actual $overlap -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '予兆の配置は上下左右のどれか' -Condition (@($shown | Where-Object { $_.slot -notin 'Up', 'Down', 'Left', 'Right' }).Count -eq 0) -Detail (($shown | ForEach-Object { "$($_.member.ToUpper())/$($_.slot)" }) -join ' → ') | Out-Null
    Assert-ProbeTrue -Name '予兆の間は帯を出している' -Condition (@($shown | Where-Object { -not $_.strip }).Count -eq 0) | Out-Null
    for ($i = 0; $i -lt $shown.Count; $i++) { Assert-WindupLength -Label "予兆$($i + 1)" -Event $shown[$i] -Expected $config.memoryTelegraph }

    # --- 時止め中のプレイヤー・弾 ---
    Assert-ProbeValue -Name '時止め中は被弾しない（帯の中に立っていても当たらない）' -Actual @($hits | Where-Object { $_.ts }).Count -Expected 0 | Out-Null
    $pressRows = @($stopRows | Where-Object { $_.time -ge $pressStart -and $_.time -le $pressEnd + 0.3 })
    $stopMove = 0
    if ($pressRows.Count -gt 1) {
        $stopMove = [Math]::Sqrt([Math]::Pow($pressRows[-1].px - $pressRows[0].px, 2) + [Math]::Pow($pressRows[-1].pz - $pressRows[0].pz, 2))
    }
    Assert-ProbeTrue -Name '時止め中は W を押しても動かない' -Condition ($pressRows.Count -gt 1 -and $stopMove -lt 0.01) -Detail ("押している間の記録 {0} 行 / 移動 {1:F3}m" -f $pressRows.Count, $stopMove) | Out-Null
    $bulletRows = @($stopRows | Select-Object -Skip 1 | Where-Object { $_.bullets -gt 0 })
    if ($bulletRows.Count -gt 0) {
        $maxMove = ($bulletRows | Measure-Object -Property bulletMove -Maximum).Maximum
        Assert-ProbeValue -Name '時止め中は弾が止まる（1フレームの移動の最大、m）' -Actual $maxMove -Expected 0 -Tolerance 0.001 | Out-Null
    }
    else {
        Write-Host '    時止め中に弾が残っていなかったため、弾の停止は判定しない'
    }

    # --- 時止め明けの攻撃 ---
    $replays = @(Get-TimeStopWindups -Rows $rows -Action $config.replayAction)
    Assert-ProbeValue -Name '時止め明けの攻撃の回数' -Actual $replays.Count -Expected $shown.Count | Out-Null
    if ($replays.Count -gt 0) {
        $breath = $replays[0].startTime - $endTime
        Assert-ProbeTrue -Name '時止めを解いてから一息おいて攻撃を始める' -Condition ($breath -ge $config.breath - 0.05 -and $breath -le $config.breath + 0.5) -Detail ("{0:F3}秒（設定 {1}秒）" -f $breath, $config.breath) | Out-Null
    }
    $shownOrder = ($shown | ForEach-Object { "$($_.member)/$($_.slot)/$($_.offset)" }) -join ' → '
    $replayOrder = ($replays | ForEach-Object { "$($_.member)/$($_.slot)/$($_.offset)" }) -join ' → '
    Assert-ProbeTrue -Name '見せた予兆と同じ個体・配置・横のずれで、同じ順に攻撃する' -Condition ($shownOrder -eq $replayOrder) -Detail "予兆 [$shownOrder] / 攻撃 [$replayOrder]" | Out-Null
    for ($i = 0; $i -lt $replays.Count; $i++) {
        $replay = $replays[$i]
        Assert-WindupLength -Label "攻撃$($i + 1)" -Event $replay -Expected $config.replayTelegraph
        Assert-ProbeTrue -Name "攻撃$($i + 1) 予兆のあと帯を当てる段階に入る" -Condition ($replay.endPhase -eq 'Active') -Detail "予兆のあと $($replay.endPhase)" | Out-Null
        $hit = @($hits | Where-Object { [Math]::Abs($_.time - $replay.endTime) -le 0.05 }).Count
        Assert-ProbeTrue -Name "攻撃$($i + 1) 立ち止まっていれば攻撃の瞬間に当たる" -Condition ($hit -ge 1) -Detail ("攻撃 t={0:F3} / 当たった回数 {1}" -f $replay.endTime, $hit) | Out-Null
    }

    # --- 終わったあと ---
    $lastReplay = if ($replays.Count -gt 0) { $replays[-1].endTime } else { $endTime }
    $afterRows = @($rows | Where-Object { $_.time -gt $lastReplay + 1.0 })
    Assert-ProbeTrue -Name '攻撃が済むと待機を解いて台本の先頭へ戻る' -Condition ($afterRows.Count -gt 0 -and $afterRows[-1].step -ne $config.stepIndex -and -not $afterRows[-1].a.hold -and -not $afterRows[-1].b.hold) -Detail $(if ($afterRows.Count -gt 0) { "ステップ $($afterRows[-1].step) / 待機 A=$($afterRows[-1].a.hold) B=$($afterRows[-1].b.hold)" } else { '記録なし' }) | Out-Null
    Assert-ProbeValue -Name '時止めは1回だけ（解けたあとに止まった行数）' -Actual @($rows | Select-Object -Skip ($lastStop + 1) | Where-Object { $_.ts }).Count -Expected 0 | Out-Null
    $leftover = @($afterRows | Select-Object -First 3 | Where-Object { $_.a.strip -or $_.b.strip }).Count
    Assert-ProbeValue -Name '攻撃のあと帯の表示が残らない（行数）' -Actual $leftover -Expected 0 | Out-Null

    # --- 時止めの途中で全員倒すと時止めが解ける（解けないと次のウェーブでも動けなくなる） ---
    $state = $null
    for ($i = 0; $i -lt 160; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if ($state.ts) { break }
        Start-Sleep -Milliseconds 250
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
