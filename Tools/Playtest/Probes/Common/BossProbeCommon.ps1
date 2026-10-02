#
# ボス系プローブ（BossWave / BossAxisPair）の共通処理。各プローブの先頭で dot-source する。
#   - 再生開始シーンを Battle にする／戻す。あわせて開始時アップグレード（デバッグ設定）を空にする／戻す
#   - BossWaveConfig のボスグループを一時的に差し替える／戻す（ボスウェーブに出るボスを検証対象に合わせる）
#   - デバッグ対戦（任意のボスグループ・敵とだけ戦う）を予約する（Request-DebugArena）
#   - C# スニペットの共通の前置き（コンテナ解決・台本の進行のリフレクション）
#   - ボスウェーブ直前のショップまでウェーブを進める
#

$Global:BossProbeWaveConfigPath = 'Assets/App/MasterData/Boss/BossWaveConfig.asset'

function Enter-BossProbeScene {
    # 開いているシーンを切り替えず、再生開始時だけ Battle シーンを使う（TutorialMessage プローブと同じ）
    $Global:ProbeSavedPlayModeStartScene = Invoke-UnityCode -Snippet @'
using UnityEditor;
using UnityEditor.SceneManagement;

var saved = EditorSceneManager.playModeStartScene != null
    ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
    : string.Empty;
EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Battle.unity");

return saved;
'@
    Write-Host "再生開始シーンを Battle に設定しました（退避: [$Global:ProbeSavedPlayModeStartScene]）"

    # 開始時アップグレード（デバッグ設定）にメデューサ（Boss ランクをスタン）やスネークアイズ（減速）が入っていると、
    # 注視の向きしだいでボスの行動が打ち切られ・遅れ、判定が揺れる。実行中だけ空にする
    $Global:ProbeSavedStartUpgradeIds = Invoke-UnityCode -Snippet @'
using UnityEditor;

var saved = EditorPrefs.GetString("StartUpgradeIds", string.Empty);
EditorPrefs.SetString("StartUpgradeIds", string.Empty);
return saved;
'@
    Write-Host "開始時アップグレードを空にしました（退避: [$Global:ProbeSavedStartUpgradeIds]）"
}

function Exit-BossProbeScene {
    $saved = $Global:ProbeSavedPlayModeStartScene
    $escaped = $saved -replace '\\', '\\' -replace '"', '\"'

    Invoke-UnityCode -Snippet @"
using UnityEditor;
using UnityEditor.SceneManagement;

var path = "$escaped";
EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
    ? null
    : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

return "restored";
"@ | Out-Null

    Write-Host "再生開始シーンを戻しました: [$saved]"

    $savedUpgrades = $Global:ProbeSavedStartUpgradeIds
    Invoke-UnityCode -Snippet @"
using UnityEditor;

EditorPrefs.SetString("StartUpgradeIds", "$savedUpgrades");
return "restored";
"@ | Out-Null
    Write-Host "開始時アップグレードを戻しました: [$savedUpgrades]"
}

function Request-DebugArena {
    # デバッグ対戦（App/デバッグ: 敵と対戦）を予約し、再生開始シーンを Battle にする。再生前（エディット時）に呼ぶこと。
    # 予約は次の再生の組み立てで1回だけ使われ、再生の終了時に DebugArenaLauncher が再生開始シーンと残った予約を片付ける。
    # Enter-BossProbeScene のあとに呼ぶ（そちらが元の再生開始シーンを退避・復元する）
    param(
        [string]$BossGroupPath = '',
        [string]$EnemyCode = '',
        [int]$EnemyCount = 1,
        [bool]$AutoRespawn = $true,
        [double]$RespawnDelaySeconds = 3,
        [bool]$Invincible = $true
    )
    $ci = [System.Globalization.CultureInfo]::InvariantCulture
    $result = Invoke-UnityCode -Snippet @"
using App.Common.Data;

var request = new DebugArenaRequest
{
    BossGroupAssetPath = "$BossGroupPath",
    EnemyCode = "$EnemyCode",
    EnemyCount = $EnemyCount,
    AutoRespawn = $($AutoRespawn.ToString().ToLower()),
    RespawnDelaySeconds = $($RespawnDelaySeconds.ToString($ci))f,
    Invincible = $($Invincible.ToString().ToLower())
};
return App.Editor.DebugArenaLauncher.Prepare(request) ? "ok" : "failed";
"@
    if ($result -ne 'ok') {
        throw "デバッグ対戦の予約に失敗しました（$result）"
    }
    Write-Host "デバッグ対戦を予約しました（ボスグループ: [$BossGroupPath] 敵: [$EnemyCode]x$EnemyCount 出し直し: $AutoRespawn/$RespawnDelaySeconds 秒 無敵: $Invincible）"
}

function Set-BossWaveGroup {
    # BossWaveConfig のボスグループを差し替え、元のアセットパスを返す（空文字は未設定）。
    # 再生前（エディット時）に呼ぶこと。終了時は Restore-BossWaveGroup で必ず戻す
    param([Parameter(Mandatory)] [string]$GroupPath)
    $result = Invoke-UnityCode -Snippet @"
using UnityEditor;
using App.Battle.Data;

var config = AssetDatabase.LoadAssetAtPath<BossWaveConfig>("$Global:BossProbeWaveConfigPath");
var so = new SerializedObject(config);
var prop = so.FindProperty("_bossGroup");
var saved = prop.objectReferenceValue != null ? AssetDatabase.GetAssetPath(prop.objectReferenceValue) : string.Empty;
prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossGroupConfig>("$GroupPath");
so.ApplyModifiedPropertiesWithoutUndo();
return saved;
"@
    Write-Host "ボスウェーブのボスグループを $GroupPath に差し替えました（退避: [$result]）"
    return $result
}

function Restore-BossWaveGroup {
    param([string]$GroupPath)
    Invoke-UnityCode -Snippet @"
using UnityEditor;
using App.Battle.Data;

var config = AssetDatabase.LoadAssetAtPath<BossWaveConfig>("$Global:BossProbeWaveConfigPath");
var so = new SerializedObject(config);
so.FindProperty("_bossGroup").objectReferenceValue = string.IsNullOrEmpty("$GroupPath") ? null : AssetDatabase.LoadAssetAtPath<BossGroupConfig>("$GroupPath");
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();
return "restored";
"@ | Out-Null
    Write-Host "ボスウェーブのボスグループを戻しました: [$GroupPath]"
}

# 各スニペット共通の using と解決処理
$Global:BossProbePrelude = @'
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using R3;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views.Enemy.AI.Boss;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var enemies = scope.Container.Resolve<IEnemyDataStore>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var bossGroups = scope.Container.Resolve<IBossGroupDataStore>();
var bossWave = scope.Container.Resolve<IBossWaveDataStore>();
var presenter = scope.Container.Resolve<IEnemyPresenter>();
var freeze = scope.Container.Resolve<IFreezeDataStore>();

bool IsBossEnemy(EnemyData e) =>
    enemies.TryGetEnemyMasterData(e.EnemyMasterDataId, out var md) && md.EnemyRankType == App.Common.Data.EnemyRankType.Boss;

// 台本の進行（メンバー番号順の敵Id・ステップ番号）は検証のためリフレクションで読む
var groupsField = bossGroups.GetType().GetField("_groups", BindingFlags.NonPublic | BindingFlags.Instance);
var holdField = typeof(BossAIBase).GetField("_isHold", BindingFlags.NonPublic | BindingFlags.Instance);
var phaseMachineField = typeof(BossAIBase).GetField("_actionPhase", BindingFlags.NonPublic | BindingFlags.Instance);
var elapsedField = typeof(BossActionPhaseMachine).GetField("_elapsed", BindingFlags.NonPublic | BindingFlags.Instance);
var formationProperty = typeof(BossAIBase).GetProperty("FormationSlot", BindingFlags.NonPublic | BindingFlags.Instance);
App.Battle.DataStore.BossPatternRunner GetRunner()
{
    var list = (System.Collections.IList)groupsField.GetValue(bossGroups);
    return list.Count > 0 ? (App.Battle.DataStore.BossPatternRunner)list[0] : null;
}
BossAIBase GetBoss(int slot)
{
    var runner = GetRunner();
    if (runner == null) return null;
    var id = runner.MemberIds[slot];
    return UnityEngine.Object.FindObjectsOfType<BossAIBase>().FirstOrDefault(b => b.EnemyId == id);
}
string Describe(BossAIBase b)
{
    if (b == null) return "-";
    var s = b.Status.CurrentValue;
    return $"{s.Phase}|{(bool)holdField.GetValue(b)}|{s.IsStun}|{s.IsDead}";
}
BossFormationSlot GetFormation(BossAIBase b) => b == null ? BossFormationSlot.None : (BossFormationSlot)formationProperty.GetValue(b);
'@

function Invoke-BossSnippet {
    param([Parameter(Mandatory)] [string]$Body)
    return Invoke-UnityJson -Snippet ($Global:BossProbePrelude + "`n" + $Body)
}

function Skip-BossProbeShop {
    # ショップで何も買わずに次のウェーブへ進める。Resolve-ShopIfOpen はランダムに1枚買うため、
    # メデューサ（注視した Boss ランクをスタン）やスネークアイズ（減速）を引くとボスの行動が変わり、判定が揺れる
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
            action = 'Click'; 'target-path' = $Global:PlaytestShopNextWaveButtonPath; 'bypass-raycast' = 'true'
        } | Out-Null
        Start-Sleep -Milliseconds 500
        if (-not (Get-WaveState).isWavePause) { return }
    }
    throw "ショップから次ウェーブへ遷移できませんでした（NextWaveButton押下後もisWavePause=trueのまま）"
}

function Move-ToBossWaveShop {
    # 制限時間を満たして通常の進行経路（AdvanceWaveInternal）を通し、ボスウェーブ開始前のショップまで進める。
    # ボスウェーブの番号を返す
    $bossWaveNumber = (Invoke-BossSnippet -Body @'
var config = scope.Container.Resolve<BossWaveConfig>();
var n = Enumerable.Range(1, 99).First(w => config.IsBossWave(w));
return $"{{\"n\":{n}}}";
'@).n
    Write-Host "ボスウェーブ: $bossWaveNumber"

    for ($i = 0; $i -lt 30; $i++) {
        $state = Get-WaveState
        if ($state.currentWave -eq $bossWaveNumber -and $state.isWavePause) { break }
        if ($state.isWavePause) {
            Skip-BossProbeShop
            continue
        }
        Invoke-BossSnippet -Body @'
player.Health.Value = player.MaxHealth.Value;
wave.AddElapsedTime(9999f);
return "{}";
'@ | Out-Null
        Start-Sleep -Milliseconds 700
    }

    $state = Get-WaveState
    Assert-ProbeTrue -Name 'ボスウェーブ前のショップに到達' -Condition ($state.currentWave -eq $bossWaveNumber -and $state.isWavePause) -Detail "wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null
    return $bossWaveNumber
}

function Stop-BossProbeRecorder {
    # 毎フレームの記録（HP全快を含む）を止める。途中で検証を打ち切っても呼ぶこと
    Invoke-BossSnippet -Body @'
(AppDomain.CurrentDomain.GetData("bossProbe.sub") as IDisposable)?.Dispose();
AppDomain.CurrentDomain.SetData("bossProbe.sub", null);
return "{}";
'@ | Out-Null
}
