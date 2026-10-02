#
# 体力を共有する二人組ボス（BossGroup_AxisPair / BossAxisBarrage）を実プレイで検証するプローブ。
#
# ボスウェーブ（BossWaveConfig）に二人組を出し、次を確かめる。
#   - 配置（CrossFormation）: 必ず1体がプレイヤーの縦方向（上下＝±Z）、もう1体が横方向（左右＝±X）につき、
#     プレイヤーからその方向へ一定距離（BossAxisBarrage._keepDistance）の位置へ瞬間移動する
#   - 弾幕役: 規定秒（EnemyMasterData の ActiveTime）のあいだ、移動方向と直交する向き（プレイヤーの側）へ一定間隔で撃つ。
#     自分の軸の線上だけを動き（縦方向にいれば横へ、横方向にいれば縦へ）、プレイヤーに並ぶよう追う
#   - 追跡役: 弾を撃たず、自分の側でプレイヤーとの距離を保って追う
#   - 弾幕が終わると配置し直し、もう片方が弾幕役になる
#   - 体力: どちらに当てても共有の体力が減り、0になると2体同時にいなくなる。撃破（ポイント・撃破数）は当てた1体ぶんだけ
#   - 体力ゲージ: ボスごとに足元へ出て位置についてくる。色は BossLifeGaugeConfig（プレイヤーのゲージは変えない）。割合は共有体力÷最大体力で2体とも同じ。撃破で消える
# プレイヤーは記録中に一定速度で動かし、追跡・軸に沿った移動を確かめる。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:AxisPairGroupPath = 'Assets/App/MasterData/Boss/BossGroup_AxisPair.asset'

function ProbePrepare {
    Enter-BossProbeScene
    try {
        $Global:AxisPairSavedGroup = Set-BossWaveGroup -GroupPath $Global:AxisPairGroupPath
    }
    catch {
        # 準備が途中で失敗すると ProbeCleanup が呼ばれないため、ここで戻す
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Restore-BossWaveGroup -GroupPath $Global:AxisPairSavedGroup
    Exit-BossProbeScene
}

function ProbeRun {
    try {
        Invoke-AxisPairProbeBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Invoke-AxisPairProbeBody {
    $bossWaveNumber = Move-ToBossWaveShop

    # 設定値（距離・弾の間隔・弾幕の秒数）はアセットから読む
    $config = Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var md = config.BossGroup.Members[0].EnemyMasterData;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(md.PrefabPath).GetComponent<BossAxisBarrage>();
var so = new UnityEditor.SerializedObject(prefab);
return $"{{\"distance\":{so.FindProperty("_keepDistance").floatValue},\"interval\":{so.FindProperty("_fireInterval").floatValue},\"active\":{md.ActiveTime},\"windup\":{md.WindupTime},\"recovery\":{md.RecoveryTime}}}";
'@
    Write-Host "設定: 距離 $($config.distance)m / 弾の間隔 $($config.interval)秒 / 弾幕 $($config.active)秒（予備動作 $($config.windup) / 硬直 $($config.recovery)）"

    # --- 毎フレームの記録（配置・行動段階・位置・新しく出た弾）とプレイヤーの移動・撃破の記録を仕込む ---
    Invoke-BossSnippet -Body @'
var log = new List<string>();
var dead = new List<int>();
var removed = new List<int>();
var drive = new float[3];
var seenBullets = new HashSet<int>();
var attackerField = typeof(App.Battle.Views.Enemy.Bullet.BaseBulletView).GetField("_attackerId", BindingFlags.NonPublic | BindingFlags.Instance);
var frame = 0;
string lastKey = null;

var subs = new CompositeDisposable();
enemies.OnEnemyDead.Subscribe(id => dead.Add(id)).AddTo(subs);
enemies.OnEnemyRemoved.Subscribe(id => removed.Add(id)).AddTo(subs);
Observable.EveryUpdate().Subscribe(_ =>
{
    player.Health.Value = player.MaxHealth.Value;
    if (drive[0] != 0f || drive[2] != 0f)
    {
        player.WarpTo(player.Position.Value + new Vector3(drive[0], 0f, drive[2]) * Time.deltaTime);
    }

    var runner = GetRunner();
    if (runner == null) return;
    var a = GetBoss(0);
    var b = GetBoss(1);

    // 新しく出た弾を撃った個体ごとに数え、向きが「配置の方向の逆（プレイヤーの側）」とどれだけ一致するかの最小値を取る
    var newA = 0; var newB = 0; var minDotA = 1f; var minDotB = 1f;
    foreach (var bullet in UnityEngine.Object.FindObjectsOfType<App.Battle.Views.Enemy.Bullet.BaseBulletView>())
    {
        if (!seenBullets.Add(bullet.GetInstanceID())) continue;
        var attacker = (int)attackerField.GetValue(bullet);
        var shooter = attacker == a?.EnemyId ? a : attacker == b?.EnemyId ? b : null;
        if (shooter == null) continue;
        var dot = Vector3.Dot(bullet.transform.forward, -GetFormation(shooter).ToDirection());
        if (shooter == a) { newA++; minDotA = Mathf.Min(minDotA, dot); } else { newB++; minDotB = Mathf.Min(minDotB, dot); }
    }

    // 段階はスタン中なら末尾に「*」を付ける（弾幕がスタンで打ち切られたかを見分けるため）
    string Member(BossAIBase x) => x == null ? "-|None|0|0" : $"{x.Status.CurrentValue.Phase}{(x.Status.CurrentValue.IsStun ? "*" : "")}|{GetFormation(x)}|{x.transform.position.x:F2}|{x.transform.position.z:F2}";
    var key = $"{runner.StepIndex},{Member(a).Split('|')[0]},{GetFormation(a)},{Member(b).Split('|')[0]},{GetFormation(b)}";
    frame++;
    // 段階・配置が変わったフレーム、弾が出たフレーム、それ以外は3フレームに1回だけ残す
    if (key == lastKey && newA == 0 && newB == 0 && frame % 3 != 0) return;
    lastKey = key;
    var p = player.Position.Value;
    log.Add($"{Time.time:F3},{runner.StepIndex},{Member(a)},{Member(b)},{p.x:F2}|{p.z:F2},{newA},{newB},{minDotA:F3},{minDotB:F3}");
}).AddTo(subs);

AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
AppDomain.CurrentDomain.SetData("bossProbe.dead", dead);
AppDomain.CurrentDomain.SetData("bossProbe.removed", removed);
AppDomain.CurrentDomain.SetData("bossProbe.drive", drive);
return "{}";
'@ | Out-Null

    # --- ボスウェーブ開始。少し待ってからプレイヤーを斜めに動かし始める ---
    Skip-BossProbeShop
    Start-Sleep -Milliseconds 800
    $start = Invoke-BossSnippet -Body @'
var ids = GetRunner().MemberIds;
var boss = enemies.Enemies.Count(IsBossEnemy);
var nonBoss = enemies.Enemies.Count(e => !IsBossEnemy(e));
var hpA = enemies.Enemies.First(e => e.Id == ids[0]).Hp;
var hpB = enemies.Enemies.First(e => e.Id == ids[1]).Hp;
var drive = (float[])AppDomain.CurrentDomain.GetData("bossProbe.drive");
drive[0] = 2.5f; drive[2] = 1.5f;
return $"{{\"boss\":{boss},\"nonBoss\":{nonBoss},\"hpA\":{hpA},\"hpB\":{hpB}}}";
'@
    Assert-ProbeValue -Name 'ボスが2体出ている' -Actual $start.boss -Expected 2 | Out-Null
    Assert-ProbeValue -Name '通常の敵はいない' -Actual $start.nonBoss -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '2体の体力が共有されている（同じ値）' -Condition ($start.hpA -eq $start.hpB -and $start.hpA -gt 0) -Detail "A=$($start.hpA) B=$($start.hpB)" | Out-Null

    $gauge = Get-AxisPairGauges
    Assert-ProbeValue -Name '体力ゲージがボスの数だけ出ている' -Actual $gauge.count -Expected 2 | Out-Null
    # 色: ボスのゲージは BossLifeGaugeConfig の色で上書きされ、プレイヤーのゲージはマテリアルの色のまま
    $color = Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossLifeGaugeConfig>();
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.BossLifeGaugeStoreView>();
var block = new MaterialPropertyBlock();
var healthId = Shader.PropertyToID("_HealthColor");
var lowId = Shader.PropertyToID("_LowHealthColor");
var bossOk = 0; var bossCount = 0;
foreach (var gauge in store.GetComponentsInChildren<App.Battle.Views.PlayerLifeGaugeView>())
{
    var renderer = gauge.GetComponentInChildren<Renderer>();
    renderer.GetPropertyBlock(block);
    bossCount++;
    if (block.GetColor(healthId) == config.HealthColor && block.GetColor(lowId) == config.LowHealthColor) bossOk++;
}
var playerGauge = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.PlayerLifeGaugeView>().First(g => g.transform.parent != store.transform);
var playerRenderer = playerGauge.GetComponentInChildren<Renderer>();
playerRenderer.GetPropertyBlock(block);
var playerOverridden = block.HasColor(healthId);
var playerColor = playerRenderer.sharedMaterial.GetColor(healthId);
return $"{{\"bossCount\":{bossCount},\"bossOk\":{bossOk},\"playerOverridden\":{playerOverridden.ToString().ToLower()},\"playerColor\":\"{playerColor}\",\"bossColor\":\"{config.HealthColor}\"}}";
'@
    Assert-ProbeTrue -Name 'ボスのゲージは設定（BossLifeGaugeConfig）の色' -Condition ($color.bossCount -eq 2 -and $color.bossOk -eq 2) -Detail "設定どおり $($color.bossOk)/$($color.bossCount) 個（$($color.bossColor)）" | Out-Null
    Assert-ProbeTrue -Name 'プレイヤーのゲージは色を上書きしない（マテリアルの色のまま）' -Condition (-not $color.playerOverridden) -Detail "マテリアルの色 $($color.playerColor)" | Out-Null

    Assert-ProbeTrue -Name '体力ゲージは満タン（割合1）' -Condition ($gauge.a.exists -and $gauge.b.exists -and $gauge.a.ratio -eq 1 -and $gauge.b.ratio -eq 1) -Detail "A=$($gauge.a.ratio) B=$($gauge.b.ratio)" | Out-Null

    # 弾幕2回ぶん（交代を含む）＋余裕
    $segment = $config.windup + $config.active + $config.recovery
    Start-Sleep -Seconds ([Math]::Ceiling($segment * 2 + 2))

    # 移動中（プレイヤーを動かしている間）にゲージが足元へついてきているか
    $gauge = Get-AxisPairGauges
    Assert-ProbeValue -Name '体力ゲージがボスの足元についてくる（A、XZのずれ m）' -Actual $gauge.a.offset -Expected 0 -Tolerance 0.05 | Out-Null
    Assert-ProbeValue -Name '体力ゲージがボスの足元についてくる（B、XZのずれ m）' -Actual $gauge.b.offset -Expected 0 -Tolerance 0.05 | Out-Null

    $logText = (Invoke-BossSnippet -Body @'
var drive = (float[])AppDomain.CurrentDomain.GetData("bossProbe.drive");
drive[0] = 0f; drive[2] = 0f;
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
return $"{{\"log\":\"{string.Join(";", log)}\"}}";
'@).log

    $rows = @($logText.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        $a = $c[2].Split('|'); $b = $c[3].Split('|'); $p = $c[4].Split('|')
        [pscustomobject]@{
            time = [double]$c[0]; step = [int]$c[1]
            a = [pscustomobject]@{ phase = $a[0]; slot = $a[1]; x = [double]$a[2]; z = [double]$a[3] }
            b = [pscustomobject]@{ phase = $b[0]; slot = $b[1]; x = [double]$b[2]; z = [double]$b[3] }
            px = [double]$p[0]; pz = [double]$p[1]
            newA = [int]$c[5]; newB = [int]$c[6]; dotA = [double]$c[7]; dotB = [double]$c[8]
        }
    })
    Write-Host "記録: $($rows.Count) 行"

    Test-AxisSegment -Rows $rows -Config $config -Actor 'a' -Follower 'b' -Label '1回目（A が弾幕）'
    $firstEnd = $rows | Where-Object { $_.a.phase -eq 'Recovery' } | Select-Object -Last 1
    Test-AxisSegment -Rows @($rows | Where-Object { $_.time -gt $firstEnd.time }) -Config $config -Actor 'b' -Follower 'a' -Label '2回目（B が弾幕）'

    # --- 体力の共有: どちらに当てても同じだけ減る ---
    $hp = Invoke-BossSnippet -Body @'
var ids = GetRunner().MemberIds;
float Hp(int id) => enemies.Enemies.First(e => e.Id == id).Hp;
var before = Hp(ids[0]);
enemies.Damage(new HitData(ids[0], 10f, App.Common.Data.HitDirectionType.None));
var afterA0 = Hp(ids[0]); var afterA1 = Hp(ids[1]);
enemies.Damage(new HitData(ids[1], 10f, App.Common.Data.HitDirectionType.None));
var afterB0 = Hp(ids[0]); var afterB1 = Hp(ids[1]);
return $"{{\"before\":{before},\"afterA0\":{afterA0},\"afterA1\":{afterA1},\"afterB0\":{afterB0},\"afterB1\":{afterB1}}}";
'@
    Assert-ProbeTrue -Name 'A に当てると A・B の体力が同じだけ減る' -Condition ($hp.afterA0 -lt $hp.before -and $hp.afterA0 -eq $hp.afterA1) -Detail "前 $($hp.before) → A $($hp.afterA0) / B $($hp.afterA1)" | Out-Null
    Assert-ProbeTrue -Name 'B に当てても A・B の体力が同じだけ減る' -Condition ($hp.afterB1 -lt $hp.afterA1 -and $hp.afterB0 -eq $hp.afterB1) -Detail "A $($hp.afterB0) / B $($hp.afterB1)" | Out-Null

    $gauge = Get-AxisPairGauges
    Assert-ProbeValue -Name '体力ゲージ A の割合が共有体力÷最大体力' -Actual $gauge.a.ratio -Expected $gauge.a.expected -Tolerance 0.0001 | Out-Null
    Assert-ProbeValue -Name '体力ゲージ B の割合が共有体力÷最大体力' -Actual $gauge.b.ratio -Expected $gauge.b.expected -Tolerance 0.0001 | Out-Null
    Assert-ProbeTrue -Name '体力ゲージは2体とも同じだけ減っている' -Condition ($gauge.a.ratio -eq $gauge.b.ratio -and $gauge.a.ratio -lt 1) -Detail "A=$($gauge.a.ratio) B=$($gauge.b.ratio)" | Out-Null

    # --- 同時撃破: 当てた方だけが撃破扱い、もう片方は撃破扱いにせず消える ---
    $kill = Invoke-BossSnippet -Body @'
var ids = GetRunner().MemberIds.ToArray();
var dead = (List<int>)AppDomain.CurrentDomain.GetData("bossProbe.dead");
var removed = (List<int>)AppDomain.CurrentDomain.GetData("bossProbe.removed");
dead.Clear(); removed.Clear();
enemies.Damage(new HitData(ids[1], 99999f, App.Common.Data.HitDirectionType.None));
var bossLeft = enemies.Enemies.Count(e => ids.Contains(e.Id) && !e.IsDead);
return $"{{\"dead\":\"{string.Join("|", dead.Where(ids.Contains).Select(id => Array.IndexOf(ids, id)))}\",\"removed\":\"{string.Join("|", removed.Where(ids.Contains).Select(id => Array.IndexOf(ids, id)))}\",\"aliveAfter\":{bossLeft}}}";
'@
    Assert-ProbeTrue -Name '共有体力が0で撃破扱いになるのは当てた B だけ（ポイント・撃破数は1体ぶん）' -Condition ($kill.dead -eq '1') -Detail "撃破=[$($kill.dead)]" | Out-Null
    Assert-ProbeTrue -Name 'A は撃破扱いにせず同時に消える' -Condition (($kill.removed -split '\|') -contains '0') -Detail "消去=[$($kill.removed)]" | Out-Null
    Assert-ProbeValue -Name '撃破直後に生き残っているボス' -Actual $kill.aliveAfter -Expected 0 | Out-Null

    Start-Sleep -Milliseconds 1500
    $gauge = Invoke-BossSnippet -Body @'
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.BossLifeGaugeStoreView>();
var gauges = (System.Collections.IDictionary)typeof(App.Battle.Views.BossLifeGaugeStoreView).GetField("_gauges", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store);
return $"{{\"count\":{gauges.Count},\"children\":{store.transform.childCount}}}";
'@
    Assert-ProbeValue -Name '撃破後に体力ゲージが消える（管理数）' -Actual $gauge.count -Expected 0 | Out-Null
    Assert-ProbeValue -Name '撃破後に体力ゲージが消える（オブジェクト数）' -Actual $gauge.children -Expected 0 | Out-Null
    $state = Get-WaveState
    Assert-ProbeTrue -Name '二人組を倒すと次のウェーブへ進む' -Condition ($state.currentWave -eq ($bossWaveNumber + 1) -and $state.isWavePause) -Detail "wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null
}

# ボスの体力ゲージ（BossLifeGaugeStoreView）の状態: 個数、メンバーごとのゲージと足元のずれ（XZ、m）、目標の割合
$Global:AxisPairGaugeSnippet = @'
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.BossLifeGaugeStoreView>();
var gauges = (System.Collections.IDictionary)typeof(App.Battle.Views.BossLifeGaugeStoreView).GetField("_gauges", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store);
var targetField = typeof(App.Battle.Views.PlayerLifeGaugeView).GetField("_targetHealth", BindingFlags.NonPublic | BindingFlags.Instance);
var runner = GetRunner();
string Gauge(int slot)
{
    if (runner == null) return "{\"exists\":false}";
    var id = runner.MemberIds[slot];
    if (!gauges.Contains(id)) return "{\"exists\":false}";
    var gauge = (App.Battle.Views.PlayerLifeGaugeView)gauges[id];
    var boss = GetBoss(slot);
    var offset = boss == null ? -1f : Vector2.Distance(new Vector2(gauge.transform.position.x, gauge.transform.position.z), new Vector2(boss.transform.position.x, boss.transform.position.z));
    var enemy = enemies.Enemies.FirstOrDefault(e => e.Id == id);
    var expected = enemy == null || enemy.MaxHp <= 0f ? -1f : Mathf.Clamp01(enemy.Hp / enemy.MaxHp);
    return $"{{\"exists\":true,\"offset\":{offset},\"ratio\":{(float)targetField.GetValue(gauge)},\"expected\":{expected}}}";
}
return $"{{\"count\":{gauges.Count},\"a\":{Gauge(0)},\"b\":{Gauge(1)}}}";
'@

function Get-AxisPairGauges {
    return Invoke-BossSnippet -Body $Global:AxisPairGaugeSnippet
}

function Get-AxisValue {
    # 個体の位置から、指定した軸（'x' / 'z'）の座標を返す
    param($Member, [string]$Axis)
    if ($Axis -eq 'x') { return $Member.x } else { return $Member.z }
}

function Get-SlotDirection {
    param([string]$Slot)
    switch ($Slot) {
        'Up' { return @(0, 1) }
        'Down' { return @(0, -1) }
        'Left' { return @(-1, 0) }
        'Right' { return @(1, 0) }
        default { return @(0, 0) }
    }
}

function Test-AxisSegment {
    # 1回ぶんの弾幕（配置 → 弾幕役の行動 → 終了）を判定する
    param($Rows, $Config, [string]$Actor, [string]$Follower, [string]$Label)

    $windup = $Rows | Where-Object { $_.$Actor.phase -eq 'Windup' } | Select-Object -First 1
    if (-not (Assert-ProbeTrue -Name "$Label 弾幕役が行動を始めた" -Condition ($null -ne $windup))) { return }

    $actorSlot = $windup.$Actor.slot
    $followerSlot = $windup.$Follower.slot
    $vertical = @('Up', 'Down')
    $actorIsVertical = $vertical -contains $actorSlot
    $followerIsVertical = $vertical -contains $followerSlot
    Assert-ProbeTrue -Name "$Label 配置は縦と横の組" -Condition ($actorSlot -ne 'None' -and $followerSlot -ne 'None' -and $actorIsVertical -ne $followerIsVertical) -Detail "弾幕役=$actorSlot 追跡役=$followerSlot" | Out-Null

    # 瞬間移動の直後（行動開始の時点）は、どちらもプレイヤーからその方向へ一定距離
    foreach ($role in @($Actor, $Follower)) {
        $d = Get-SlotDirection $windup.$role.slot
        $ex = $windup.px + $d[0] * $Config.distance
        $ez = $windup.pz + $d[1] * $Config.distance
        $err = [Math]::Sqrt([Math]::Pow($windup.$role.x - $ex, 2) + [Math]::Pow($windup.$role.z - $ez, 2))
        Assert-ProbeValue -Name "$Label 配置直後の位置のずれ（$role / $($windup.$role.slot)、m）" -Actual $err -Expected 0 -Tolerance 1.0 | Out-Null
    }

    $active = @($Rows | Where-Object { $_.time -ge $windup.time -and $_.$Actor.phase -eq 'Active' })
    if (-not (Assert-ProbeTrue -Name "$Label 弾幕（攻撃の段階）に入った" -Condition ($active.Count -gt 0))) { return }
    $activeStart = $active[0].time
    $activeEnd = ($Rows | Where-Object { $_.time -gt $activeStart -and $_.$Actor.phase -ne 'Active' } | Select-Object -First 1).time
    $endIndex = [Array]::IndexOf($Rows, ($Rows | Where-Object { $_.time -eq $activeEnd } | Select-Object -First 1))
    $Rows[[Math]::Max(0, $endIndex - 2)..[Math]::Min($Rows.Count - 1, $endIndex + 2)] | ForEach-Object { Write-Host ("      t={0:F3} step={1} A={2}/{3} B={4}/{5}" -f $_.time, $_.step, $_.a.phase, $_.a.slot, $_.b.phase, $_.b.slot) }
    $lastRow = $Rows | Select-Object -Last 1
    Write-Host ("    {0}: 予備動作 t={1:F3} / 弾幕 t={2:F3}〜{3:F3} / 記録の末尾 t={4:F3}（{5}={6}）" -f $Label, $windup.time, $activeStart, $activeEnd, $lastRow.time, $Actor, $lastRow.$Actor.phase)
    Assert-ProbeValue -Name "$Label 弾幕の長さ（秒）" -Actual ($activeEnd - $activeStart) -Expected $Config.active -Tolerance 0.15 | Out-Null

    $windowRows = @($Rows | Where-Object { $_.time -ge $activeStart -and $_.time -lt $activeEnd })
    $actorNew = if ($Actor -eq 'a') { 'newA' } else { 'newB' }
    $followerNew = if ($Actor -eq 'a') { 'newB' } else { 'newA' }
    $actorDot = if ($Actor -eq 'a') { 'dotA' } else { 'dotB' }
    $fired = ($windowRows | Measure-Object -Property $actorNew -Sum).Sum
    $followerFired = ($windowRows | Measure-Object -Property $followerNew -Sum).Sum
    $expectedShots = [Math]::Ceiling($Config.active / $Config.interval)
    Assert-ProbeValue -Name "$Label 弾幕役が撃った弾数（弾幕の秒数 ÷ 間隔）" -Actual $fired -Expected $expectedShots -Tolerance 2 | Out-Null
    Assert-ProbeValue -Name "$Label 追跡役は撃たない（弾数）" -Actual $followerFired -Expected 0 | Out-Null
    $minDot = ($windowRows | Measure-Object -Property $actorDot -Minimum).Minimum
    Assert-ProbeTrue -Name "$Label 弾は移動方向と直交してプレイヤーの側へ向かう" -Condition ($minDot -gt 0.99) -Detail "向きの一致（内積）の最小値=$minDot" | Out-Null

    # 弾幕役は自分の軸の線上だけを動き、プレイヤーと並ぶよう追う
    $lineAxis = if ($actorIsVertical) { 'z' } else { 'x' }
    $moveAxis = if ($actorIsVertical) { 'x' } else { 'z' }
    $lineValues = @($windowRows | ForEach-Object { Get-AxisValue $_.$Actor $lineAxis })
    $lineDrift = ($lineValues | Measure-Object -Maximum).Maximum - ($lineValues | Measure-Object -Minimum).Minimum
    Assert-ProbeValue -Name "$Label 弾幕役は線上を動く（$lineAxis 座標の振れ幅、m）" -Actual $lineDrift -Expected 0 -Tolerance 0.5 | Out-Null
    $moveValues = @($windowRows | ForEach-Object { Get-AxisValue $_.$Actor $moveAxis })
    $moved = ($moveValues | Measure-Object -Maximum).Maximum - ($moveValues | Measure-Object -Minimum).Minimum
    Assert-ProbeTrue -Name "$Label 弾幕役は $moveAxis 方向へ動いている" -Condition ($moved -gt 3) -Detail "移動幅=$([Math]::Round($moved, 2))m" | Out-Null
    $tail = @($windowRows | Where-Object { $_.time -ge $activeEnd - 2 })
    $alignErr = ($tail | ForEach-Object { [Math]::Abs((Get-AxisValue $_.$Actor $moveAxis) - $(if ($moveAxis -eq 'x') { $_.px } else { $_.pz })) } | Measure-Object -Average).Average
    Assert-ProbeValue -Name "$Label 弾幕役がプレイヤーと並んでいる（終盤2秒の $moveAxis 方向のずれの平均、m）" -Actual $alignErr -Expected 0 -Tolerance 1.5 | Out-Null

    # 追跡役は自分の側で距離を保ってプレイヤーを追う
    $fd = Get-SlotDirection $followerSlot
    $followErr = ($tail | ForEach-Object {
        $ex = $_.px + $fd[0] * $Config.distance; $ez = $_.pz + $fd[1] * $Config.distance
        [Math]::Sqrt([Math]::Pow($_.$Follower.x - $ex, 2) + [Math]::Pow($_.$Follower.z - $ez, 2))
    } | Measure-Object -Average).Average
    Assert-ProbeValue -Name "$Label 追跡役が距離を保って追っている（終盤2秒の配置先からのずれの平均、m）" -Actual $followErr -Expected 0 -Tolerance 2.0 | Out-Null
}
