#
# デバッグ対戦（App/デバッグ: 敵と対戦 / DebugArenaLauncher）の入口を確かめるプローブ。
#
# 再生前に DebugArenaLauncher.Prepare で BossGroup_TickTock との対戦を予約し、Battle シーンを再生する。
#   - 予約は組み立て時に1回だけ取り出され（EditorPrefs から消える）、DebugArenaSettings が有効になる
#   - セット選択を出さずにすぐ始まり、プレイヤーは BossWaveConfig のプレイヤー位置、ボスグループが出る
#   - 指定したウェーブ（ボスウェーブの番号）から始まり、ボスの体力はそのウェーブの倍率。ボスウェーブとしては扱わない（ボスが重ねて出ない）
#   - 周期スポーンで雑魚が湧かない。制限時間を過ぎてもウェーブが進まない（ショップも開かない）
#   - 無敵: 被弾しても HP は減らないが、被弾の通知（OnDamaged）は流れる
#   - 全員倒すと、出し直しの秒数のあとで同じボスグループが新しい個体で出る
# 他のボス系プローブと同じく、開始時アップグレードは実行中だけ空にする（BossProbeCommon）。
#
# ボスの挙動を確かめる新しいプローブは、Move-ToBossWaveShop でボスウェーブまで進める代わりに
# この予約（Request-DebugArena）で始めると、ウェーブ1〜4を通らずに数秒で対象のボスと戦える。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:DebugArenaGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
$Global:DebugArenaRespawnDelay = 1.5
# ボスウェーブの番号（既定5）を指定し、ボスウェーブの出現と重ならないことも確かめる
$Global:DebugArenaWave = 5

function ProbePrepare {
    Enter-BossProbeScene
    try {
        Request-DebugArena -BossGroupPath $Global:DebugArenaGroupPath -RespawnDelaySeconds $Global:DebugArenaRespawnDelay -Wave $Global:DebugArenaWave
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
        Invoke-DebugArenaBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Invoke-DebugArenaBody {
    Start-Sleep -Seconds 2

    $start = Invoke-BossSnippet -Body @'
var settings = scope.Container.Resolve<DebugArenaSettings>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();
var config = scope.Container.Resolve<BossWaveConfig>();
var bosses = enemies.Enemies.Where(IsBossEnemy).ToList();
var others = enemies.Enemies.Count(e => !IsBossEnemy(e));
var flat = player.Position.Value; flat.y = 0f;
var target = config.PlayerPosition; target.y = 0f;
var pending = UnityEditor.EditorPrefs.HasKey(App.Common.Data.DebugConfig.DebugArenaRequestKey);
// 体力共有なので最大HPは2体ぶんの合計。期待値は今のウェーブの倍率で各メンバーの基礎体力から出す
var scaling = scope.Container.Resolve<IEnemyWaveScalingCalculatorDataStore>();
var expectedHp = 0f;
var baseHp = 0f;
foreach (var b in bosses)
{
    enemies.TryGetEnemyMasterData(b.EnemyMasterDataId, out var md);
    scaling.GetScaledStatus(md.Hp, md.Damage, out var scaledHp, out _);
    expectedHp += scaledHp;
    baseHp += md.Hp;
}
var maxHp = bosses.Count > 0 ? bosses[0].MaxHp : 0f;
return $"{{\"enabled\":{settings.IsEnabled.ToString().ToLower()},\"group\":\"{(settings.BossGroup != null ? settings.BossGroup.name : "")}\",\"pending\":{pending.ToString().ToLower()},\"selecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"pause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"wave\":{wave.CurrentWave.CurrentValue},\"bosses\":{bosses.Count},\"ids\":\"{string.Join(",", bosses.Select(b => b.Id))}\",\"others\":{others},\"isBossWave\":{bossWave.IsBossWave.ToString().ToLower()},\"maxHp\":{maxHp},\"expectedHp\":{expectedHp},\"baseHp\":{baseHp},\"alive\":{bossGroups.HasAliveGroup.ToString().ToLower()},\"playerOffset\":{Vector3.Distance(flat, target)}}}";
'@
    Assert-ProbeTrue -Name '予約からデバッグ対戦の設定が作られる' -Condition $start.enabled | Out-Null
    Assert-ProbeTrue -Name '予約したボスグループで始まる' -Condition ($start.group -eq 'BossGroup_TickTock') -Detail "$($start.group)" | Out-Null
    Assert-ProbeTrue -Name '予約は1回で消費される（EditorPrefs に残らない）' -Condition (-not $start.pending) | Out-Null
    Assert-ProbeTrue -Name 'セット選択を出さずに始まる' -Condition (-not $start.selecting -and -not $start.pause) -Detail "selecting=$($start.selecting) pause=$($start.pause)" | Out-Null
    Assert-ProbeValue -Name '指定したウェーブから始まる' -Actual $start.wave -Expected $Global:DebugArenaWave | Out-Null
    Assert-ProbeTrue -Name 'ボスウェーブの番号でもボスウェーブとしては扱わない' -Condition (-not $start.isBossWave) | Out-Null
    Assert-ProbeValue -Name 'ボスグループの2体が出る（ボスウェーブのボスが重ねて出ない）' -Actual $start.bosses -Expected 2 | Out-Null
    Assert-ProbeValue -Name 'ボスの体力は指定ウェーブの倍率' -Actual $start.maxHp -Expected $start.expectedHp -Tolerance 0.01 | Out-Null
    Write-Host "    体力: 基礎 $($start.baseHp) → ウェーブ$($Global:DebugArenaWave) $($start.maxHp)"
    Assert-ProbeTrue -Name 'ボスグループの台本が動いている' -Condition $start.alive | Out-Null
    # 二人組は出現直後にプレイヤーの周りへ瞬間移動するので、プレイヤーの位置だけを見る
    Assert-ProbeTrue -Name 'プレイヤーはボスウェーブのプレイヤー位置' -Condition ($start.playerOffset -lt 1.0) -Detail "ずれ $([math]::Round($start.playerOffset, 2))m" | Out-Null

    # 無敵: HP は減らないが、被弾の通知は流れる。記録の購読は後で Stop-BossProbeRecorder が破棄する
    $hit = Invoke-BossSnippet -Body @'
var damaged = new List<float>();
var subs = new CompositeDisposable();
player.OnDamaged.Subscribe(d => damaged.Add(d)).AddTo(subs);
AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
var before = player.Health.Value;
player.TakeDamage(player.MaxHealth.Value * 2f);
return $"{{\"before\":{before},\"after\":{player.Health.Value},\"notified\":{damaged.Count}}}";
'@
    Assert-ProbeValue -Name '無敵: 致死量の被弾でも HP が減らない' -Actual $hit.after -Expected $hit.before | Out-Null
    Assert-ProbeValue -Name '無敵: 被弾の通知は流れる' -Actual $hit.notified -Expected 1 | Out-Null

    # 制限時間を満たしても進まない。周期スポーン（コモンは4秒ごと）でも雑魚は湧かない
    Invoke-BossSnippet -Body @'
wave.AddElapsedTime(9999f);
return "{}";
'@ | Out-Null
    Start-Sleep -Seconds 5
    $state = Get-WaveState
    $others = (Invoke-BossSnippet -Body @'
return $"{{\"others\":{enemies.Enemies.Count(e => !IsBossEnemy(e))}}}";
'@).others
    Assert-ProbeTrue -Name '制限時間を過ぎてもウェーブが進まない・ショップが開かない' -Condition ($state.currentWave -eq $Global:DebugArenaWave -and -not $state.isWavePause) -Detail "wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null
    Assert-ProbeValue -Name '周期スポーンで雑魚が湧かない' -Actual $others -Expected 0 | Out-Null

    # 全員倒す（共有体力なので1体に当てれば2体とも消える）。出し直しの秒数の前後で数を見る
    Invoke-BossSnippet -Body @'
var boss = enemies.Enemies.First(IsBossEnemy);
enemies.Damage(new HitData(boss.Id, boss.Hp + 1f, default));
return "{}";
'@ | Out-Null
    Start-Sleep -Milliseconds 500
    $cleared = Invoke-BossSnippet -Body @'
return $"{{\"bosses\":{enemies.Enemies.Count(IsBossEnemy)},\"alive\":{bossGroups.HasAliveGroup.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '倒した直後はボスがいない（出し直しを待つ）' -Actual $cleared.bosses -Expected 0 | Out-Null

    Start-Sleep -Seconds ([math]::Ceiling($Global:DebugArenaRespawnDelay + 2))
    $respawn = Invoke-BossSnippet -Body @'
var bosses = enemies.Enemies.Where(IsBossEnemy).ToList();
return $"{{\"bosses\":{bosses.Count},\"ids\":\"{string.Join(",", bosses.Select(b => b.Id))}\",\"alive\":{bossGroups.HasAliveGroup.ToString().ToLower()},\"wave\":{wave.CurrentWave.CurrentValue},\"pause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()}}}";
'@
    $oldIds = @("$($start.ids)".Split(','))
    $newIds = @("$($respawn.ids)".Split(',') | Where-Object { $_ -ne '' })
    Assert-ProbeValue -Name '出し直しの秒数のあとで同じボスグループが出る' -Actual $respawn.bosses -Expected 2 | Out-Null
    Assert-ProbeTrue -Name '出し直したボスは新しい個体' -Condition (@($newIds | Where-Object { $oldIds -contains $_ }).Count -eq 0) -Detail "前 [$($start.ids)] 後 [$($respawn.ids)]" | Out-Null
    Assert-ProbeTrue -Name '出し直したボスの台本が動いている' -Condition $respawn.alive | Out-Null
    Assert-ProbeTrue -Name '撃破してもウェーブが進まない' -Condition ($respawn.wave -eq $Global:DebugArenaWave -and -not $respawn.pause) -Detail "wave=$($respawn.wave) pause=$($respawn.pause)" | Out-Null

    # リスタート（ゲームオーバーの「リスタート」と同じ RunResetUseCase）でも、指定したウェーブ・同じ相手で始まり直す
    Invoke-BossSnippet -Body @'
scope.Container.Resolve<App.Battle.UseCase.RunResetUseCase>().ResetRun();
return "{}";
'@ | Out-Null
    Start-Sleep -Seconds 2
    $restart = Invoke-BossSnippet -Body @'
var runStart = scope.Container.Resolve<IRunStartDataStore>();
return $"{{\"wave\":{wave.CurrentWave.CurrentValue},\"pause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"selecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"bosses\":{enemies.Enemies.Count(IsBossEnemy)}}}";
'@
    Assert-ProbeValue -Name 'リスタート後も指定したウェーブから始まる' -Actual $restart.wave -Expected $Global:DebugArenaWave | Out-Null
    Assert-ProbeTrue -Name 'リスタート後もセット選択を出さずに始まる' -Condition (-not $restart.selecting -and -not $restart.pause) -Detail "selecting=$($restart.selecting) pause=$($restart.pause)" | Out-Null
    Assert-ProbeValue -Name 'リスタート後も同じボスグループが出る' -Actual $restart.bosses -Expected 2 | Out-Null
}
