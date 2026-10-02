#
# ボス撃破でのクリア（WaveManagerUseCase → GameStateDataStore.IsCleared → GameClearUseCase → RunResultUseCase）を
# 実プレイで検証するプローブ。
#
# ボスウェーブ（BossWaveConfig、既定: 5）まで進めてボスを全員倒し、
#   - クリアになり、ウェーブは進まず（ショップも開かず）ポーズのままになること
#   - まず見出し（STAGE CLEAR）だけが出て、保存・リスタート・メインメニューのボタンは隠れていること
#   - クリア表示中にHPが0になってもゲームオーバーにならないこと
#   - GameClearConfig の秒数のあとでボタンが出ること
#   - 結果画面に今回のランで獲得したアップグレードの一覧が獲得順に出ること
#   - スロット保存でボスウェーブの番号が記録されること
#   - リスタートでクリアが解除され、ウェーブ1・ビルド選択へ戻ること
# を確かめる。スロット保存はセーブファイルを書き換えるため、実行前に退避して終了後に戻す。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:GameClearPanelPath = 'BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel'
$Global:GameClearSaveDataPath = Join-Path $PSScriptRoot '../../../Assets/DLHN/SaveData.json'
$Global:GameClearSaveDataBackup = Join-Path ([System.IO.Path]::GetTempPath()) 'probe-gameclear-savedata.json'

function ProbePrepare {
    Enter-BossProbeScene
    $Global:GameClearHadSaveData = Test-Path $Global:GameClearSaveDataPath
    if ($Global:GameClearHadSaveData) {
        Copy-Item -LiteralPath $Global:GameClearSaveDataPath -Destination $Global:GameClearSaveDataBackup -Force
        Write-Host "セーブデータを退避しました: $Global:GameClearSaveDataBackup"
    }
}

function ProbeCleanup {
    if ($Global:GameClearHadSaveData) {
        Copy-Item -LiteralPath $Global:GameClearSaveDataBackup -Destination $Global:GameClearSaveDataPath -Force
        Write-Host "セーブデータを戻しました"
    }
    Exit-BossProbeScene
}

# 結果画面の表示状態と、クリア・ゲームオーバーの状態
$Global:GameClearPanelSnippet = @'
var gameState = scope.Container.Resolve<IGameStateDataStore>();
bool Shown(string path) { var go = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel" + path); return go != null && go.activeInHierarchy; }
var headline = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/Headline")?.GetComponent<UnityEngine.UI.Text>()?.text ?? "";
var status = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/Status")?.GetComponent<UnityEngine.UI.Text>()?.text ?? "";
var list = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/UpgradeList")?.GetComponent<UnityEngine.UI.Text>()?.text ?? "";
var shopPanel = GameObject.Find("BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel");
string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "/");
return $"{{\"cleared\":{gameState.IsCleared.CurrentValue.ToString().ToLower()},\"gameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"wave\":{wave.CurrentWave.CurrentValue},\"pause\":{wave.IsWavePause.Value.ToString().ToLower()},\"panel\":{Shown("").ToString().ToLower()},\"restart\":{Shown("/RestartButton").ToString().ToLower()},\"slot0\":{Shown("/SlotButtons/SlotButton0").ToString().ToLower()},\"menu\":{Shown("/ReturnToMainMenuButton").ToString().ToLower()},\"shop\":{(shopPanel != null && shopPanel.activeInHierarchy).ToString().ToLower()},\"headline\":\"{Esc(headline)}\",\"status\":\"{Esc(status)}\",\"listShown\":{Shown("/UpgradeList").ToString().ToLower()},\"list\":\"{Esc(list)}\"}}";
'@

function Get-GameClearPanel {
    return Invoke-BossSnippet -Body $Global:GameClearPanelSnippet
}

function ProbeRun {
    $bossWaveNumber = Move-ToBossWaveShop
    $duration = [double](Invoke-BossSnippet -Body 'return $"{{\"d\":{scope.Container.Resolve<GameClearConfig>().HeadlineOnlyDuration}}}";').d
    Write-Host "クリア表示の秒数: $duration"

    Skip-BossProbeShop
    Start-Sleep -Seconds 1

    # --- ボスを全員倒す（体力共有なら1体に当てれば全員消える） ---
    $kill = Invoke-BossSnippet -Body @'
var bossIds = enemies.Enemies.Where(IsBossEnemy).Select(e => e.Id).ToList();
foreach (var id in bossIds)
{
    if (enemies.TryGetEnemyData(id, out var e) && !e.IsDead) enemies.Damage(new HitData(id, 99999f, App.Common.Data.HitDirectionType.None));
}
return $"{{\"bosses\":{bossIds.Count}}}";
'@
    Assert-ProbeTrue -Name 'ボスウェーブでボスが出ている' -Condition ([int]$kill.bosses -gt 0) -Detail "ボス $($kill.bosses) 体" | Out-Null
    # 結果画面の一覧を確かめるため、獲得アップグレードを2件仕込む（同じものの2回目は UpgradeSessionDataStore が弾く）。
    # 撃破後に足すのでボスの挙動には影響しない。付与の副作用は通さない（GameOverRestart と同じ）
    $added = Invoke-BossSnippet -Body @'
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var db = scope.Container.Resolve<App.Common.Data.Database.UpgradeDatabase>();
var a = db.UpgradeMasterData[0];
var b = db.UpgradeMasterData[1];
session.AddUpgrade(a);
session.AddUpgrade(b);
session.AddUpgrade(b);
var la = scope.Container.Resolve<IUpgradeLocalizationDataStore>().GetText(a);
var lb = scope.Container.Resolve<IUpgradeLocalizationDataStore>().GetText(b);
AppDomain.CurrentDomain.SetData("gameClearProbe.expected", "・" + la.Title + la.LevelLabel + "    ・" + lb.Title + lb.LevelLabel);
return $"{{\"newly\":{session.NewlyAcquiredUpgrades.Count}}}";
'@
    Start-Sleep -Milliseconds 500

    $p = Get-GameClearPanel
    Assert-ProbeTrue -Name 'クリア表示の間は獲得アップグレードの一覧も隠れている' -Condition (-not $p.listShown) | Out-Null
    Assert-ProbeTrue -Name 'ボスを全員倒すとクリアになる' -Condition $p.cleared | Out-Null
    Assert-ProbeTrue -Name 'クリアでウェーブは進まない' -Condition ($p.wave -eq $bossWaveNumber) -Detail "wave=$($p.wave)" | Out-Null
    Assert-ProbeTrue -Name 'クリアで敵・ウェーブが止まる（ポーズ）' -Condition $p.pause | Out-Null
    Assert-ProbeTrue -Name 'クリアでショップは開かない' -Condition (-not $p.shop) | Out-Null
    Assert-ProbeTrue -Name 'クリア表示が出る' -Condition ($p.panel -and $p.headline -like 'STAGE CLEAR*') -Detail "headline=[$($p.headline)]" | Out-Null
    Assert-ProbeTrue -Name 'クリア表示の間はボタンが隠れている' -Condition (-not $p.restart -and -not $p.slot0 -and -not $p.menu) -Detail "restart=$($p.restart) slot0=$($p.slot0) menu=$($p.menu)" | Out-Null

    # --- クリア表示中に倒れてもゲームオーバーにしない ---
    Invoke-BossSnippet -Body 'for (var i = 0; i < 10 && player.Health.Value > 0f; i++) player.TakeDamage(99999f); return "{}";' | Out-Null
    Start-Sleep -Milliseconds 300
    $p = Get-GameClearPanel
    Assert-ProbeTrue -Name 'クリア後にHPが0になってもゲームオーバーにならない' -Condition ($p.cleared -and -not $p.gameOver) -Detail "cleared=$($p.cleared) gameOver=$($p.gameOver)" | Out-Null

    # --- 秒数のあとでボタンが出る ---
    Start-Sleep -Milliseconds ([int](($duration + 0.5) * 1000))
    $p = Get-GameClearPanel
    Assert-ProbeTrue -Name '結果画面のボタンが出る（保存・リスタート・メインメニュー）' -Condition ($p.panel -and $p.restart -and $p.slot0 -and $p.menu) -Detail "restart=$($p.restart) slot0=$($p.slot0) menu=$($p.menu)" | Out-Null
    Assert-ProbeTrue -Name '結果画面の見出しはクリア' -Condition ($p.headline -like 'STAGE CLEAR*') -Detail "headline=[$($p.headline)]" | Out-Null
    # 項目は半角スペース4つで区切られ、表示側で折り返す
    $lines = @($p.list -split '    ' | Where-Object { $_ -ne '' })
    $expected = (Invoke-BossSnippet -Body 'return $"{{\"e\":\"{AppDomain.CurrentDomain.GetData("gameClearProbe.expected")}\"}}";').e
    Assert-ProbeTrue -Name '獲得アップグレードの一覧が出る' -Condition ($p.listShown -and [int]$added.newly -eq 2) -Detail "newly=$($added.newly) list=[$($p.list)]" | Out-Null
    Assert-ProbeValue -Name '一覧は1件ずつ並ぶ（同じものの再獲得で増えない）' -Actual $lines.Count -Expected 2 | Out-Null
    Assert-ProbeTrue -Name '一覧は獲得順に名前とレベルが並ぶ' -Condition ($p.list -eq $expected) -Detail "list=[$($p.list)] expected=[$expected]" | Out-Null
    Assert-ProbeTrue -Name '一覧はローカライズ済みの名前（キーのままではない）' -Condition (-not ($p.list -match '\$')) -Detail "list=[$($p.list)]" | Out-Null

    # --- スロット保存 ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestGameOverSlotButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 500
    $saved = Invoke-BossSnippet -Body @'
var meta = scope.Container.Resolve<App.Common.Interface.IMetaProgressionDataStore>();
return $"{{\"empty\":{meta.IsSlotEmpty(0).ToString().ToLower()},\"wave\":{meta.GetSlotClearedWave(0)}}}";
'@
    $p = Get-GameClearPanel
    Assert-ProbeTrue -Name 'クリア後にスロット保存できる（ボスウェーブの番号が残る）' -Condition ([int]$saved.wave -eq $bossWaveNumber) -Detail "slotWave=$($saved.wave) status=[$($p.status)]" | Out-Null
    Assert-ProbeTrue -Name '保存しても結果画面は開いたまま' -Condition ($p.panel -and $p.restart) | Out-Null

    # --- リスタート ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = "$Global:GameClearPanelPath/RestartButton"; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 1000
    $p = Get-GameClearPanel
    $state = Get-WaveState
    Assert-ProbeTrue -Name 'リスタートでクリアが解除される' -Condition (-not $p.cleared -and -not $p.gameOver) | Out-Null
    Assert-ProbeTrue -Name 'リスタートで結果画面が閉じる' -Condition (-not $p.panel) | Out-Null
    # 保存済みスロットが無ければビルド選択を飛ばして即開始する（0個の保存は空スロット扱い）ので、どちらでもよい
    Assert-ProbeTrue -Name 'リスタートでウェーブ1から（ビルド選択または即開始）' -Condition ($state.currentWave -eq 1 -and ($state.isSelectingRunStart -or -not $state.isWavePause)) -Detail "wave=$($state.currentWave) selecting=$($state.isSelectingRunStart) pause=$($state.isWavePause)" | Out-Null
    Assert-ProbeTrue -Name 'リスタートでHPが戻る' -Condition ([double]$state.playerHealth -gt 0) -Detail "hp=$($state.playerHealth)" | Out-Null
}
