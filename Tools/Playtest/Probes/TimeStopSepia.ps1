#
# ボスの時止めの間、プレイヤー側にセピア調がかかることを実プレイで検証するプローブ
# （TimeStopEffectUseCase / TimeStopConfig のプリセット / SepiaToneDataStore / SepiaToneView）。
#
# デバッグ対戦（Request-DebugArena）で BossGroup_TickTock と戦い、台本の時止め（TimeStopOrbit）まで待って、次を確かめる。
# 配置のつき直し（CrossFormation の MoveSeconds）でも短く時が止まるため、TimeStopOrbit のステップで止まった区間だけを見る。
#   - 時止めの前は、どのグループにもセピアがかかっていない（_SepiaToneWeights が0）
#   - 時止めが始まると、プリセットの対象グループ（既定はプレイヤーと背景＝床）の強さがフェードインの秒数で Intensity まで上がり、
#     対象外のグループ（既定は敵・UI）は0のまま
#   - 時止めが解けると、フェードアウトの秒数で0へ戻る
#   - プレイヤー側の Renderer（自機・照準・ゲージ以外）にプレイヤーのグループのビットが立ち、セピア対応のシェーダーを使っている
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。オーバークロックが発動するとセピアが重なるため、開始時アップグレードを空にして発動させない。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:TimeStopSepiaGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -BossGroupPath $Global:TimeStopSepiaGroupPath -AutoRespawn $false -Wave 5
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
        Invoke-TimeStopSepiaProbeBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Invoke-TimeStopSepiaProbeBody {
    Start-Sleep -Seconds 2

    $config = Invoke-BossSnippet -Body @'
var config = UnityEditor.AssetDatabase.LoadAssetAtPath<TimeStopConfig>("Assets/App/MasterData/TimeStop/TimeStopConfig.asset");
var preset = config.SepiaTonePreset;
var group = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>("Assets/App/MasterData/Boss/BossGroup_TickTock.asset");
var orbitStep = group.Pattern.ToList().FindIndex(s => s.Type == BossPatternStepType.TimeStopOrbit);
var breath = group.Pattern[orbitStep].WaitSeconds;
return $"{{\"groups\":{(int)preset.TargetGroups},\"intensity\":{preset.Intensity},\"fadeIn\":{preset.FadeInSeconds},\"fadeOut\":{preset.FadeOutSeconds},\"stepIndex\":{orbitStep},\"breath\":{breath}}}";
'@
    Write-Host "設定: 対象グループ $($config.groups)（1＝背景 2＝敵 4＝プレイヤー 8＝UI の和）/ 強さ $($config.intensity) / フェードイン $($config.fadeIn)秒 / フェードアウト $($config.fadeOut)秒"
    Assert-ProbeTrue -Name 'プリセットの対象にプレイヤーのグループが入っている' -Condition (($config.groups -band 4) -ne 0) -Detail "対象グループ $($config.groups)" | Out-Null

    # --- プレイヤー側の Renderer がプレイヤーのグループに属し、セピア対応のシェーダーを使っている ---
    $renderers = Invoke-BossSnippet -Body @'
var bit = App.Battle.Data.SepiaToneRenderingLayer.LayerMask(App.Battle.Data.SepiaToneGroup.Player);
var playerRoot = UnityEngine.Object.FindObjectsOfType<App.Battle.Views.SepiaToneTargetView>(true)
    .Where(v => v.name.StartsWith("Player") && !v.name.Contains("Gauge")).ToList();
var total = 0; var tagged = 0; var supported = 0; var names = new List<string>();
foreach (var view in playerRoot)
foreach (var r in view.GetComponentsInChildren<Renderer>(true))
{
    total++;
    if ((r.renderingLayerMask & bit) != 0) tagged++;
    var shader = r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-";
    if (shader.StartsWith("App/")) supported++; else names.Add($"{r.name}:{shader}");
}
return $"{{\"total\":{total},\"tagged\":{tagged},\"supported\":{supported},\"others\":\"{string.Join(" ", names)}\"}}";
'@
    Assert-ProbeTrue -Name 'プレイヤー側の Renderer にプレイヤーのグループのビットが立っている' -Condition ($renderers.total -gt 0 -and $renderers.tagged -eq $renderers.total) -Detail "$($renderers.tagged)/$($renderers.total)" | Out-Null
    Assert-ProbeTrue -Name 'プレイヤー側の Renderer はセピア対応のシェーダー（App/）を使っている' -Condition ($renderers.supported -eq $renderers.total) -Detail "$($renderers.supported)/$($renderers.total) $($renderers.others)" | Out-Null

    # --- 毎フレームの記録（時止めの状態とシェーダーへ配られた強さ） ---
    Invoke-BossSnippet -Body @'
var log = new List<string>();
var timeStop = scope.Container.Resolve<ITimeStopDataStore>();
var overclock = scope.Container.Resolve<IOverclockDataStore>();
var weightsId = Shader.PropertyToID("_SepiaToneWeights");
var subs = new CompositeDisposable();
Observable.EveryUpdate().Subscribe(_ =>
{
    var w = Shader.GetGlobalVector(weightsId);
    var step = GetRunner()?.StepIndex ?? -1;
    log.Add($"{Time.time:F3},{timeStop.IsTimeStopped.CurrentValue},{overclock.IsActive.CurrentValue},{w.x:F3},{w.y:F3},{w.z:F3},{w.w:F3},{step}");
}).AddTo(subs);
AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.log", log);
return "{}";
'@ | Out-Null

    $positionSnippet = @'
var ts = scope.Container.Resolve<ITimeStopDataStore>().IsTimeStopped.CurrentValue;
var step = GetRunner()?.StepIndex ?? -1;
return $"{{\"time\":{Time.time},\"ts\":{ts.ToString().ToLower()},\"step\":{step}}}";
'@
    $state = $null
    for ($i = 0; $i -lt 300; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if ($state.ts -and $state.step -eq $config.stepIndex) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not (Assert-ProbeTrue -Name '台本どおりに回りこみの時止めが始まる' -Condition ($state.ts -and $state.step -eq $config.stepIndex) -Detail "t=$($state.time) ステップ $($state.step)")) { return }
    # 時止めが解けるのを待ち、フェードアウトを見届ける（時止めの長さは台本しだいなので、解けるまで見る）
    for ($i = 0; $i -lt 300; $i++) {
        $state = Invoke-BossSnippet -Body $positionSnippet
        if (-not $state.ts) { break }
        Start-Sleep -Milliseconds 100
    }
    Start-Sleep -Seconds ([Math]::Ceiling($config.fadeOut + 1))

    $data = Invoke-BossSnippet -Body @'
var log = (List<string>)AppDomain.CurrentDomain.GetData("bossProbe.log");
return $"{{\"log\":\"{string.Join(";", log)}\"}}";
'@
    $rows = @($data.log.Split(';') | Where-Object { $_ } | ForEach-Object {
        $c = $_.Split(',')
        [pscustomobject]@{ time = [double]$c[0]; ts = ($c[1] -eq 'True'); oc = ($c[2] -eq 'True'); bg = [double]$c[3]; enemy = [double]$c[4]; player = [double]$c[5]; ui = [double]$c[6]; step = [int]$c[7] }
    })
    Write-Host "記録: $($rows.Count) 行"
    Assert-ProbeValue -Name '記録中にオーバークロックは発動していない（行数）' -Actual @($rows | Where-Object { $_.oc }).Count -Expected 0 | Out-Null

    # 配置のつき直しの時止めは除き、回りこみのステップで止まった区間だけを見る
    $stopRows = @($rows | Where-Object { $_.ts -and $_.step -eq $config.stepIndex })
    $firstStop = [Array]::IndexOf($rows, $stopRows[0])
    $lastStop = [Array]::IndexOf($rows, $stopRows[-1])
    $start = $stopRows[0].time
    $end = $rows[$lastStop + 1].time
    Write-Host ("時止め: t={0:F3}〜{1:F3}" -f $start, $end)

    # 直前の配置のつき直しの時止め（弾幕の前）のフェードアウトは済んでいる、時止めの直前1秒を見る
    $beforeRows = @($rows | Select-Object -First $firstStop | Where-Object { $_.time -ge $start - 1 })
    $beforeMax = if ($beforeRows.Count -gt 0) { ($beforeRows | ForEach-Object { [Math]::Max([Math]::Max($_.bg, $_.enemy), [Math]::Max($_.player, $_.ui)) } | Measure-Object -Maximum).Maximum } else { 0 }
    Assert-ProbeValue -Name '時止めの前はどのグループにもセピアがかかっていない（強さの最大）' -Actual $beforeMax -Expected 0 -Tolerance 0.001 | Out-Null

    # フェードインを終えたあとの時止め中
    $fullRows = @($stopRows | Where-Object { $_.time -ge $start + $config.fadeIn + 0.05 })
    if ($fullRows.Count -gt 0) {
        $minPlayer = ($fullRows | Measure-Object -Property player -Minimum).Minimum
        Assert-ProbeValue -Name '時止め中（フェードイン後）のプレイヤーのセピアの強さ（最小）' -Actual $minPlayer -Expected $config.intensity -Tolerance 0.01 | Out-Null
    }
    else {
        Assert-ProbeTrue -Name '時止めがフェードインより長く続く' -Condition $false -Detail ("時止め {0:F2}秒" -f ($end - $start)) | Out-Null
    }
    # プレイヤー以外のグループは、プリセットの対象なら Intensity、対象外なら0のまま
    foreach ($group in @(@{ name = '背景'; bit = 1; key = 'bg' }, @{ name = '敵'; bit = 2; key = 'enemy' }, @{ name = 'UI'; bit = 8; key = 'ui' })) {
        $key = $group.key
        if (($config.groups -band $group.bit) -ne 0) {
            if ($fullRows.Count -gt 0) {
                $min = ($fullRows | Measure-Object -Property $key -Minimum).Minimum
                Assert-ProbeValue -Name "時止め中（フェードイン後）の$($group.name)のセピアの強さ（最小）" -Actual $min -Expected $config.intensity -Tolerance 0.01 | Out-Null
            }
        }
        else {
            $max = ($stopRows | Measure-Object -Property $key -Maximum).Maximum
            Assert-ProbeValue -Name "時止め中も$($group.name)にはかからない（強さの最大）" -Actual $max -Expected 0 -Tolerance 0.001 | Out-Null
        }
    }

    # フェードイン: 始まって最初のフレームはまだ途中で、設定の秒数で上がりきる
    $reached = @($stopRows | Where-Object { $_.player -ge $config.intensity - 0.001 } | Select-Object -First 1)
    if ($reached.Count -gt 0) {
        $fadeIn = $reached[0].time - $start
        Assert-ProbeTrue -Name 'フェードインの秒数で上がりきる' -Condition ($fadeIn -le $config.fadeIn + 0.1) -Detail ("{0:F3}秒（設定 {1}秒）" -f $fadeIn, $config.fadeIn) | Out-Null
    }

    # フェードアウト: 解けたあと設定の秒数で0へ戻り、その後も0のまま
    $afterRows = @($rows | Select-Object -Skip ($lastStop + 1))
    $cleared = @($afterRows | Where-Object { $_.player -le 0.001 } | Select-Object -First 1)
    $fadeOut = if ($cleared.Count -gt 0) { $cleared[0].time - $end } else { 999 }
    Assert-ProbeTrue -Name '時止めが解けるとフェードアウトの秒数で元の色に戻る' -Condition ($fadeOut -le $config.fadeOut + 0.1) -Detail ("{0:F3}秒（設定 {1}秒）" -f $fadeOut, $config.fadeOut) | Out-Null
    # 一息が明けると次の配置のつき直しでまた時が止まるため、一息の間（回りこみのステップに留まっている間）だけを見る
    $settled = @($afterRows | Where-Object { $_.time -gt $end + $config.fadeOut + 0.2 -and $_.step -eq $config.stepIndex })
    $settledMax = if ($settled.Count -gt 0) { ($settled | ForEach-Object { [Math]::Max([Math]::Max($_.bg, $_.enemy), [Math]::Max($_.player, $_.ui)) } | Measure-Object -Maximum).Maximum } else { 999 }
    Assert-ProbeValue -Name '戻ったあとはどのグループにもセピアが残らない（強さの最大）' -Actual $settledMax -Expected 0 -Tolerance 0.001 | Out-Null
}
