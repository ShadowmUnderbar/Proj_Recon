#
# 二人組ボス「TickTock」の発狂フェイズ（BossGroupConfig の RagePattern / BossTickTock の帯の攻撃）を実プレイで検証するプローブ。
#
# 共有体力を発狂フェイズの割合（BossGroupConfig.RageHealthRatio）より下まで削り、次を確かめる。
#   - 削った直後に台本が発狂フェイズへ切り替わり、行動中だった個体は打ち切られる
#   - 帯の攻撃ごとに、2体が縦と横の組で配置につき直し、横のずれは候補（-5 / 0 / +5）から選ばれ、少なくとも1体は0（プレイヤーと重なる）
#   - 2体が同じフレームで予兆を出し、予兆の秒数（BossLineStrikeConfig.TelegraphSeconds）のあと同じフレームで攻撃する
#   - 予兆の間は帯が表示され、攻撃のあと消える
#   - 立ち止まっていれば攻撃の瞬間に当たり、予兆の間に帯の外へ出れば当たらない
#   - 帯の攻撃を3〜6回（ランダム）やったら、×字の配置（隣り合う斜めの角）へつき直し、予兆1回のあと同じ向き・位置で3連続攻撃する
#     （その間もボス本体はプレイヤーとの相対位置を保って追従し、帯だけが地面に固定される）
#     （1回目のあとプレイヤーを帯の外へ出し、2・3回目は向き直さずに外れることで、向き・位置の固定を確かめる）
#   - ×字のあとは弾幕を1回挟み、また帯の攻撃に戻る
# 個体の状態・帯の表示・プレイヤーの被弾は毎フレーム記録し、まとめて判定する。記録中は毎フレームHPを全快させる。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:RageProbeGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'

# 予兆の途中でプレイヤーを帯の外へ出す量（m）。ずれ ±5m・幅4m の帯のどれからも外れるよう、斜めに大きく動かす
$Global:RageProbeDodgeOffset = 8

function ProbePrepare {
    Enter-BossProbeScene
    try {
        $Global:RageProbeSavedGroup = Set-BossWaveGroup -GroupPath $Global:RageProbeGroupPath
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Restore-BossWaveGroup -GroupPath $Global:RageProbeSavedGroup
    Exit-BossProbeScene
}

function ProbeRun {
    try {
        Invoke-RageProbeBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Assert-TelegraphLength {
    # 予兆が始まったのは「予兆を記録した行」と「その1つ前の行」の間のどこか。
    # スニペットの実行直後はエディタのフレームが止まって時刻が飛ぶため、記録した行の時刻だけで測ると短く出る。
    # 開始の取りうる範囲から予兆の長さの範囲を出し、設定値がその範囲（±0.05秒）に入るかを見る
    param([string]$Label, $Rows, [int]$WindupIndex, [double]$ActiveTime, [double]$Expected)
    $windupTime = $Rows[$WindupIndex].time
    $previousTime = if ($WindupIndex -gt 0) { $Rows[$WindupIndex - 1].time } else { $windupTime }
    $shortest = $ActiveTime - $windupTime
    $longest = $ActiveTime - $previousTime
    $ok = ($Expected -ge $shortest - 0.05) -and ($Expected -le $longest + 0.05)
    Assert-ProbeTrue -Name "$Label 予兆の長さが設定どおり" -Condition $ok -Detail ("{0:F3}〜{1:F3}秒（設定 {2}秒）" -f $shortest, $longest, $Expected) | Out-Null
}

function Invoke-RageProbeBody {
    $bossWaveNumber = Move-ToBossWaveShop

    $config = Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var group = config.BossGroup;
var md = group.Members[0].EnemyMasterData;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(md.PrefabPath).GetComponent<BossTickTock>();
var so = new UnityEditor.SerializedObject(prefab);
var strike = (BossLineStrikeConfig)so.FindProperty("_lineStrikeConfig").objectReferenceValue;
return $"{{\"ratio\":{group.RageHealthRatio},\"distance\":{so.FindProperty("_keepDistance").floatValue},\"telegraph\":{strike.TelegraphSeconds},\"strike\":{strike.StrikeSeconds},\"width\":{strike.Width},\"length\":{strike.Length},\"repeatCount\":{strike.RepeatCount},\"repeatInterval\":{strike.RepeatInterval}}}";
'@
    Write-Host "設定: 発狂は体力 $($config.ratio) 以下 / 距離 $($config.distance)m / 予兆 $($config.telegraph)秒 / 攻撃 $($config.strike)秒 / 帯 幅$($config.width)m 長さ$($config.length)m"

    # --- 毎フレームの記録。2回目の帯の攻撃では、予兆の途中でプレイヤーを帯の外へ出す ---
    $recorderPrefix = "const float DodgeOffset = $($Global:RageProbeDodgeOffset)f;`n"
    Invoke-BossSnippet -Body ($recorderPrefix + @'
var log = new List<string>();
var hits = new List<string>();
var config = scope.Container.Resolve<BossWaveConfig>();
var rageSteps = config.BossGroup.RagePattern;
var stepsField = typeof(App.Battle.DataStore.BossPatternRunner).GetField("_steps", BindingFlags.NonPublic | BindingFlags.Instance);
var actionProperty = typeof(BossAIBase).GetProperty("CurrentActionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
var offsetProperty = typeof(BossAIBase).GetProperty("FormationLateralOffset", BindingFlags.NonPublic | BindingFlags.Instance);
var stripField = typeof(BossTickTock).GetField("_lineStrikeView", BindingFlags.NonPublic | BindingFlags.Instance);
var strikesSeen = 0;
var lastAPhase = BossActionPhase.Ready;
var dodge = new float[] { 0f, -1f }; // [0]: 0=未 / 1=待機中 / 2=済み、[1]: 予兆を出した時刻
var xDodge = 0; // ×字の連続攻撃: 0=未 / 1=1回目が当たった / 2=外へ出した
string lastKey = null;
var frame = 0;

var subs = new CompositeDisposable();
player.OnDamaged.Subscribe(amount => hits.Add($"{Time.time:F3}|{amount}")).AddTo(subs);
Observable.EveryUpdate().Subscribe(_ =>
{
    player.Health.Value = player.MaxHealth.Value;
    var runner = GetRunner();
    if (runner == null) return;
    var a = GetBoss(0);
    var b = GetBoss(1);
    if (a == null || b == null) return;

    // 2回目の帯の攻撃: 予兆を出して0.5秒たったら、プレイヤーを帯の外へ出す
    var aPhase = a.Status.CurrentValue.Phase;
    var aAction = (int)actionProperty.GetValue(a);
    if (aAction == BossTickTock.LineStrikeActionIndex && lastAPhase == BossActionPhase.Windup && aPhase == BossActionPhase.Active) strikesSeen++;
    if (dodge[0] == 0f && strikesSeen == 1 && aAction == BossTickTock.LineStrikeActionIndex && aPhase == BossActionPhase.Windup)
    {
        dodge[0] = 1f; dodge[1] = Time.time;
    }
    if (dodge[0] == 1f && Time.time - dodge[1] >= 0.5f)
    {
        player.WarpTo(player.Position.Value + new Vector3(DodgeOffset, 0f, DodgeOffset));
        dodge[0] = 2f;
        log.Add($"DODGE,{Time.time:F3}");
    }
    // ×字の連続攻撃: 1回目が当たった次のフレームで帯の外へ出す（2・3回目が向き直さずに外れることを見る）
    if (xDodge == 1)
    {
        player.WarpTo(player.Position.Value + new Vector3(DodgeOffset, 0f, 0f));
        xDodge = 2;
        log.Add($"XDODGE,{Time.time:F3}");
    }
    if (xDodge == 0 && aAction == BossTickTock.RepeatLineStrikeActionIndex && lastAPhase == BossActionPhase.Windup && aPhase == BossActionPhase.Active) xDodge = 1;
    lastAPhase = aPhase;

    string Member(BossAIBase x)
    {
        var strip = (BossLineStrikeView)stripField.GetValue(x);
        var visible = strip != null && strip.IsVisible;
        return $"{x.Status.CurrentValue.Phase}|{(int)actionProperty.GetValue(x)}|{GetFormation(x)}|{(float)offsetProperty.GetValue(x):F1}|{x.transform.position.x:F2}|{x.transform.position.z:F2}|{visible}";
    }
    var raged = ReferenceEquals(stepsField.GetValue(runner), rageSteps);
    var ma = Member(a); var mb = Member(b);
    // 位置以外（段階・行動・配置・ずれ・帯）が変わったフレームと、3フレームに1回を残す
    string Key(string m) { var p = m.Split('|'); return $"{p[0]}|{p[1]}|{p[2]}|{p[3]}|{p[6]}"; }
    var key = $"{raged},{runner.StepIndex},{Key(ma)},{Key(mb)}";
    frame++;
    if (key == lastKey && frame % 3 != 0) return;
    lastKey = key;
    var pp = player.Position.Value;
    log.Add($"{Time.time:F3},{raged},{runner.StepIndex},{ma},{mb},{pp.x:F2}|{pp.z:F2}");
}).AddTo(subs);

AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
AppDomain.CurrentDomain.SetData("bossProbe.hits", hits);
return "{}";
'@) | Out-Null

    Skip-BossProbeShop
    Start-Sleep -Seconds 2

    # --- 共有体力を発狂の割合より少し下まで削る ---
    $cut = Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var ids = GetRunner().MemberIds;
var enemy = enemies.Enemies.First(e => e.Id == ids[0]);
var target = enemy.MaxHp * (config.BossGroup.RageHealthRatio - 0.02f);
var phaseBefore = $"{GetBoss(0).Status.CurrentValue.Phase}/{GetBoss(1).Status.CurrentValue.Phase}";
enemies.Damage(new HitData(ids[0], enemy.Hp - target, App.Common.Data.HitDirectionType.None));
return $"{{\"time\":{Time.time},\"ratio\":{enemy.Hp / enemy.MaxHp},\"before\":\"{phaseBefore}\"}}";
'@
    Write-Host "削った時刻 t=$($cut.time) / 体力の割合 $([Math]::Round($cut.ratio, 3)) / 削る前の段階 A/B=$($cut.before)"

    # 帯の攻撃（最大6回）→ ×字の連続攻撃 → 弾幕1回 → 帯の攻撃（次の組の1回目）まで
    $strikeCycle = $config.telegraph + $config.strike + 0.5 + 0.5
    $xCycle = $config.telegraph + $config.repeatInterval * ($config.repeatCount - 1) + $config.strike + 0.5 + 0.5
    Start-Sleep -Seconds ([Math]::Ceiling($strikeCycle * 7 + $xCycle + 9.2 + 3))

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
var hits = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.hits");
return $"{{\"log\":\"{string.Join(";", log)}\",\"hits\":\"{string.Join(";", hits)}\"}}";
'@

    $dodgeTime = $null
    $xDodgeTime = $null
    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        if ($_.StartsWith('DODGE')) { $dodgeTime = [double]$_.Split(',')[1]; return }
        if ($_.StartsWith('XDODGE')) { $xDodgeTime = [double]$_.Split(',')[1]; return }
        $c = $_.Split(',')
        function Parse([string]$m) {
            $p = $m.Split('|')
            [pscustomobject]@{ phase = $p[0]; action = [int]$p[1]; slot = $p[2]; offset = [double]$p[3]; x = [double]$p[4]; z = [double]$p[5]; strip = ($p[6] -eq 'True') }
        }
        $pp = $c[5].Split('|')
        [pscustomobject]@{ time = [double]$c[0]; raged = ($c[1] -eq 'True'); step = [int]$c[2]; a = (Parse $c[3]); b = (Parse $c[4]); px = [double]$pp[0]; pz = [double]$pp[1] }
    })
    $hitTimes = @($data.hits.Split(';') | Where-Object { $_ } | ForEach-Object { [double]$_.Split('|')[0] })
    Write-Host "記録: $($rows.Count) 行 / 被弾 $($hitTimes.Count) 回 / 帯の外へ出た時刻 t=$dodgeTime"

    # --- 発狂フェイズへの切り替え ---
    $switch = $rows | Where-Object { $_.raged } | Select-Object -First 1
    if (-not (Assert-ProbeTrue -Name '体力を削ると発狂フェイズへ切り替わる' -Condition ($null -ne $switch))) { return }
    # 切り替えは次の Tick。ただし削るスニペットの実行中はエディタのフレームが止まり Time.time が飛ぶので、その停止ぶんを見込む
    Assert-ProbeValue -Name '削ってから切り替わるまで（秒）' -Actual ($switch.time - $cut.time) -Expected 0 -Tolerance 0.5 | Out-Null
    Assert-ProbeTrue -Name '切り替えで行動中だった個体は打ち切られる（両方とも待機）' -Condition ($switch.a.phase -eq 'Ready' -and $switch.b.phase -eq 'Ready') -Detail "削る前 $($cut.before) → 切り替え時 $($switch.a.phase)/$($switch.b.phase)" | Out-Null

    # --- 帯の攻撃ごとの判定 ---
    $rageRows = @($rows | Where-Object { $_.time -ge $switch.time })
    $windups = @()
    $xWindups = @()
    for ($i = 1; $i -lt $rageRows.Count; $i++) {
        $prev = $rageRows[$i - 1]; $cur = $rageRows[$i]
        if ($cur.a.phase -eq 'Windup' -and $prev.a.phase -ne 'Windup') {
            if ($cur.a.action -eq 1) { $windups += $i }
            if ($cur.a.action -eq 2) { $xWindups += $i }
        }
    }
    $firstX = if ($xWindups.Count -gt 0) { $rageRows[$xWindups[0]].time } else { [double]::MaxValue }
    $normalBeforeX = @($windups | Where-Object { $rageRows[$_].time -lt $firstX }).Count
    Write-Host "帯の攻撃の予兆: 通常 $($windups.Count) 回（×字の前に $normalBeforeX 回）/ ×字 $($xWindups.Count) 回"
    Assert-ProbeTrue -Name '×字の前の通常の帯の攻撃は3〜6回' -Condition ($normalBeforeX -ge 3 -and $normalBeforeX -le 6) -Detail "$normalBeforeX 回" | Out-Null

    $strikeIndex = 0
    foreach ($wi in $windups) {
        $strikeIndex++
        $w = $rageRows[$wi]
        $label = "帯$strikeIndex"
        if (-not ($rageRows | Where-Object { $_.time -gt $w.time -and $_.a.phase -eq 'Active' } | Select-Object -First 1)) {
            Write-Host "    $label は記録の終わりまでに攻撃へ至らなかったため判定しない"
            break
        }
        Assert-ProbeTrue -Name "$label 2体が同じフレームで予兆を出す" -Condition ($w.b.action -eq 1 -and $w.b.phase -eq 'Windup') -Detail "B=$($w.b.phase)/行動$($w.b.action)" | Out-Null

        $vertical = @('Up', 'Down')
        Assert-ProbeTrue -Name "$label 配置は縦と横の組" -Condition (($vertical -contains $w.a.slot) -ne ($vertical -contains $w.b.slot)) -Detail "A=$($w.a.slot) B=$($w.b.slot)" | Out-Null
        $offsetsOk = (@(-5, 0, 5) -contains $w.a.offset) -and (@(-5, 0, 5) -contains $w.b.offset)
        Assert-ProbeTrue -Name "$label 横のずれは候補（-5 / 0 / +5）から選ばれ、少なくとも1体は0" -Condition ($offsetsOk -and ($w.a.offset -eq 0 -or $w.b.offset -eq 0)) -Detail "A=$($w.a.offset) B=$($w.b.offset)" | Out-Null

        # 位置: プレイヤー＋方向×距離＋横×ずれ（予兆の時点で動いていない）
        foreach ($role in @('a', 'b')) {
            $m = $w.$role
            $d = switch ($m.slot) { 'Up' { @(0, 1) } 'Down' { @(0, -1) } 'Left' { @(-1, 0) } 'Right' { @(1, 0) } }
            $l = if ($vertical -contains $m.slot) { @(1, 0) } else { @(0, 1) }
            $ex = $w.px + $d[0] * $config.distance + $l[0] * $m.offset
            $ez = $w.pz + $d[1] * $config.distance + $l[1] * $m.offset
            $err = [Math]::Sqrt([Math]::Pow($m.x - $ex, 2) + [Math]::Pow($m.z - $ez, 2))
            Assert-ProbeValue -Name "$label 配置の位置のずれ（$role / $($m.slot) ずれ$($m.offset)、m）" -Actual $err -Expected 0 -Tolerance 1.0 | Out-Null
        }

        $active = $rageRows | Where-Object { $_.time -gt $w.time -and $_.a.phase -eq 'Active' } | Select-Object -First 1
        Assert-TelegraphLength -Label $label -Rows $rageRows -WindupIndex $wi -ActiveTime $active.time -Expected $config.telegraph
        Assert-ProbeTrue -Name "$label 2体が同じフレームで攻撃する" -Condition ($active.b.phase -eq 'Active') -Detail "B=$($active.b.phase)" | Out-Null
        $during = @($rageRows | Where-Object { $_.time -gt $w.time -and $_.time -lt $active.time })
        $stripShown = @($during | Where-Object { $_.a.strip -and $_.b.strip }).Count -eq $during.Count -and $during.Count -gt 0
        Assert-ProbeTrue -Name "$label 予兆の間は2体とも帯を出している" -Condition $stripShown -Detail "$(@($during | Where-Object { $_.a.strip -and $_.b.strip }).Count)/$($during.Count) 行" | Out-Null
        $after = $rageRows | Where-Object { $_.time -gt $active.time + $config.strike + 0.1 } | Select-Object -First 1
        Assert-ProbeTrue -Name "$label 攻撃のあと帯が消える" -Condition (-not $after.a.strip -and -not $after.b.strip) | Out-Null

        $hitAtStrike = @($hitTimes | Where-Object { [Math]::Abs($_ - $active.time) -le 0.1 }).Count
        if ($strikeIndex -eq 2) {
            Assert-ProbeTrue -Name "$label 予兆の間に帯の外へ出れば当たらない" -Condition ($null -ne $dodgeTime -and $dodgeTime -lt $active.time -and $hitAtStrike -eq 0) -Detail "外へ出た t=$dodgeTime / 攻撃 t=$($active.time) / 当たった回数 $hitAtStrike" | Out-Null
        }
        else {
            Assert-ProbeTrue -Name "$label 立ち止まっていれば攻撃の瞬間に当たる" -Condition ($hitAtStrike -ge 1) -Detail "攻撃 t=$($active.time) / 当たった回数 $hitAtStrike" | Out-Null
        }
    }

    # --- ×字の配置で予兆1回 → 同じ向き・位置で3連続 ---
    if (Assert-ProbeTrue -Name '×字の連続攻撃を行った' -Condition ($xWindups.Count -ge 1)) {
        $xw = $rageRows[$xWindups[0]]
        $diagonal = @('UpLeft', 'UpRight', 'DownRight', 'DownLeft')
        $ring = @{ UpLeft = 0; UpRight = 1; DownRight = 2; DownLeft = 3 }
        $adjacent = ($diagonal -contains $xw.a.slot) -and ($diagonal -contains $xw.b.slot) -and ((($ring[$xw.a.slot] - $ring[$xw.b.slot] + 4) % 4) -in @(1, 3))
        Assert-ProbeTrue -Name '×字: 2体が隣り合う斜めの角につく（帯が直交して×になる）' -Condition $adjacent -Detail "A=$($xw.a.slot) B=$($xw.b.slot)" | Out-Null
        Assert-ProbeTrue -Name '×字: 2体が同じフレームで予兆を出す' -Condition ($xw.b.action -eq 2 -and $xw.b.phase -eq 'Windup') | Out-Null
        $s2 = [Math]::Sqrt(0.5)
        foreach ($role in @('a', 'b')) {
            $m = $xw.$role
            $d = switch ($m.slot) { 'UpLeft' { @(-$s2, $s2) } 'UpRight' { @($s2, $s2) } 'DownRight' { @($s2, -$s2) } 'DownLeft' { @(-$s2, -$s2) } default { @(0, 0) } }
            $err = [Math]::Sqrt([Math]::Pow($m.x - ($xw.px + $d[0] * $config.distance), 2) + [Math]::Pow($m.z - ($xw.pz + $d[1] * $config.distance), 2))
            Assert-ProbeValue -Name "×字: 配置の位置のずれ（$role / $($m.slot)、m）" -Actual $err -Expected 0 -Tolerance 1.0 | Out-Null
        }

        $xActive = $rageRows | Where-Object { $_.time -gt $xw.time -and $_.a.phase -eq 'Active' } | Select-Object -First 1
        $xEnd = $rageRows | Where-Object { $_.time -gt $xActive.time -and $_.a.phase -ne 'Active' } | Select-Object -First 1
        Assert-TelegraphLength -Label '×字: 予兆は1回だけ（1回目の前）' -Rows $rageRows -WindupIndex $xWindups[0] -ActiveTime $xActive.time -Expected $config.telegraph
        $expectedActive = $config.repeatInterval * ($config.repeatCount - 1) + $config.strike
        Assert-ProbeValue -Name '×字: 連続攻撃の長さ（2回目以降は予兆なし、秒）' -Actual ($xEnd.time - $xActive.time) -Expected $expectedActive -Tolerance 0.1 | Out-Null
        $xRows = @($rageRows | Where-Object { $_.time -ge $xw.time -and $_.time -lt $xEnd.time })
        # 途中でプレイヤーを帯の外へ出しても、ボスはプレイヤーとの位置関係を保ったまま追従する（帯は地面に固定）
        $relDrift = ($xRows | ForEach-Object {
                [Math]::Abs(($_.a.x - $_.px) - ($xw.a.x - $xw.px)) + [Math]::Abs(($_.a.z - $_.pz) - ($xw.a.z - $xw.pz)) +
                [Math]::Abs(($_.b.x - $_.px) - ($xw.b.x - $xw.px)) + [Math]::Abs(($_.b.z - $_.pz) - ($xw.b.z - $xw.pz)) } | Measure-Object -Maximum).Maximum
        Assert-ProbeValue -Name '×字: 予兆から連続攻撃の終わりまで、ボスはプレイヤーとの相対位置を保つ（m）' -Actual $relDrift -Expected 0 -Tolerance 0.3 | Out-Null
        $playerMoved = ($xRows | ForEach-Object { [Math]::Abs($_.px - $xw.px) + [Math]::Abs($_.pz - $xw.pz) } | Measure-Object -Maximum).Maximum
        Assert-ProbeTrue -Name '×字: 相対位置の判定の間にプレイヤーが動いている（追従を確かめられた）' -Condition ($playerMoved -gt 1) -Detail ("プレイヤーの移動 {0:F2}m" -f $playerMoved) | Out-Null
        $stripAll = @($xRows | Where-Object { $_.time -gt $xw.time -and (-not $_.a.strip -or -not $_.b.strip) }).Count
        Assert-ProbeValue -Name '×字: 予兆から連続攻撃の間、帯は出たまま（消えた行数）' -Actual $stripAll -Expected 0 | Out-Null

        # 当たり: 1回目は当たる。そのあと帯の外へ出したので、向き直さない2・3回目は当たらない
        $first = @($hitTimes | Where-Object { [Math]::Abs($_ - $xActive.time) -le 0.1 }).Count
        $later = @($hitTimes | Where-Object { $_ -gt $xActive.time + 0.1 -and $_ -le $xEnd.time + 0.1 }).Count
        Assert-ProbeTrue -Name '×字: 1回目は当たる' -Condition ($first -ge 1) -Detail "当たった回数 $first" | Out-Null
        Assert-ProbeTrue -Name '×字: 1回目のあと帯の外へ出ると2・3回目は当たらない（向き・位置は固定）' -Condition ($null -ne $xDodgeTime -and $xDodgeTime -lt $xActive.time + $config.repeatInterval -and $later -eq 0) -Detail "外へ出た t=$xDodgeTime / 2回目 t=$($xActive.time + $config.repeatInterval) / 当たった回数 $later" | Out-Null

        # ×字のあと弾幕を1回挟み、また帯の攻撃に戻る
        $nextSet = $windups | Where-Object { $rageRows[$_].time -gt $xEnd.time } | Select-Object -First 1
        if ($null -ne $nextSet) {
            $nextTime = $rageRows[$nextSet].time
            $barrage = @($rageRows | Where-Object { $_.time -gt $xEnd.time -and $_.time -lt $nextTime -and (($_.a.action -eq 0 -and $_.a.phase -eq 'Active') -or ($_.b.action -eq 0 -and $_.b.phase -eq 'Active')) })
            Assert-ProbeTrue -Name '×字のあと弾幕を1回挟み、また帯の攻撃に戻る' -Condition ($barrage.Count -gt 0) -Detail "弾幕の記録 $($barrage.Count) 行" | Out-Null
        }
        else {
            Assert-ProbeTrue -Name '×字のあと弾幕を挟んで帯の攻撃に戻る（記録の時間内に戻らなかった）' -Condition $false | Out-Null
        }
        $between = @($rageRows | Where-Object { $_.time -gt $rageRows[$windups[0]].time -and $_.time -lt $xw.time -and (($_.a.action -eq 0 -and $_.a.phase -ne 'Ready') -or ($_.b.action -eq 0 -and $_.b.phase -ne 'Ready')) })
        Assert-ProbeValue -Name '帯の攻撃の組の途中では弾幕を撃たない（行数）' -Actual $between.Count -Expected 0 | Out-Null
    }

    # --- 撃破で終わる（発狂中でも全員倒せばクリアになる） ---
    Invoke-BossSnippet -Body 'enemies.Damage(new HitData(GetRunner().MemberIds[0], 99999f, App.Common.Data.HitDirectionType.None)); return "{}";' | Out-Null
    Start-Sleep -Milliseconds 1500
    $state = Get-WaveState
    Assert-ProbeTrue -Name '発狂フェイズ中でも二人組を倒せばクリアになる' -Condition ($state.isCleared -and $state.currentWave -eq $bossWaveNumber -and $state.isWavePause) -Detail "cleared=$($state.isCleared) wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null
    $strips = Invoke-BossSnippet -Body 'return $"{{\"count\":{UnityEngine.Object.FindObjectsOfType<BossLineStrikeView>().Length}}}";'
    Assert-ProbeValue -Name '撃破後に帯の表示が残らない' -Actual $strips.count -Expected 0 | Out-Null
}
