#
# ウェーブ開始時のチュートリアル表示（TutorialWaveUseCase / TutorialWaveConfig）を検証するプローブ。
#
# ランナーがセット選択のゲートを解除した時点でウェーブ1が始まり、TutorialWaveConfig の割り当て
# （既定: ウェーブ1 → Wave1）に従ってメッセージが出て閲覧回数が記録されること、
# 閲覧済み（規定回数）なら出ないこと、再表示設定が有効なら閲覧済みでも出ること、
# 割り当てのないウェーブでは前のメッセージが消えることを確認する。
#
# 閲覧回数と再表示設定はセーブデータに永続化されるため、開始時に退避し、終了時に元へ戻して保存する。
#

function ProbePrepare {
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
}

function ProbeCleanup {
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
}

# セーブデータのチュートリアル関連を退避／復元する共通スニペット
$Global:TutorialWaveSaveSnippet = @'
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var save = scope.Container.Resolve<ISaveDataStore>();
var views = string.Join(";", save.SaveData.TutorialViews.Select(v => $"{(int)v.Type}:{v.ViewCount}"));
return $"{{\"views\":\"{views}\",\"replay\":{save.SaveData.IsTutorialReplayEnabled.ToString().ToLower()}}}";
'@

function ProbeRun {
    # --- 0. セーブデータの退避と、割り当ての確認 ---
    $saved = Invoke-UnityJson -Snippet $Global:TutorialWaveSaveSnippet
    Write-Host "セーブデータを退避しました（閲覧: [$($saved.views)] 再表示: $($saved.replay)）"

    $config = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Common.Data;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var config = scope.Container.Resolve<TutorialWaveConfig>();
var has = config.TryGetTutorial(1, out var type);
return $"{{\"hasWave1\":{has.ToString().ToLower()},\"type\":\"{type}\"}}";
'@

    Assert-ProbeTrue -Name 'ウェーブ1に割り当てがある' -Condition ([bool]$config.hasWave1) -Detail "(種類: $($config.type))" | Out-Null

    try {
        # --- 1. ゲート解除でウェーブ1が始まった直後。ランナーが解除済みなので、既に表示されているはず ---
        # ただし閲覧済みなら出ないため、まず状態を読んで判定する
        $afterStart = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var type = (TutorialType)System.Enum.Parse(typeof(TutorialType), "__TYPE__");
return $"{{\"phase\":\"{view.Phase}\",\"count\":{progress.GetViewCount(type)},\"completed\":{progress.IsCompleted(type).ToString().ToLower()},\"wave\":{wave.CurrentWave.CurrentValue},\"paused\":{wave.IsWavePause.CurrentValue.ToString().ToLower()}}}";
'@.Replace('__TYPE__', $config.type)

        Assert-ProbeTrue -Name 'ウェーブ1が始まっている' -Condition ([int]$afterStart.wave -eq 1 -and -not [bool]$afterStart.paused) `
            -Detail "(wave: $($afterStart.wave), paused: $($afterStart.paused))" | Out-Null

        # --- 2. 閲覧記録を消してから、再度ウェーブ開始（ポーズ→解除）を起こし、表示と記録を見る ---
        $fresh = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var message = scope.Container.Resolve<ITutorialMessageUseCase>();
var setting = scope.Container.Resolve<IPlayerSettingDataStore>();
var type = (TutorialType)System.Enum.Parse(typeof(TutorialType), "__TYPE__");

setting.SetTutorialReplayEnabled(false);
progress.ResetProgress(type);
message.Hide();
var before = view.Phase.ToString();

wave.SetWavePause(true);
wave.SetWavePause(false);

return $"{{\"before\":\"{before}\",\"phase\":\"{view.Phase}\",\"count\":{progress.GetViewCount(type)}}}";
'@.Replace('__TYPE__', $config.type)

        Assert-ProbeTrue -Name '未閲覧ならウェーブ開始で表示される' -Condition ($fresh.before -eq 'Hidden' -and $fresh.phase -eq 'HeadFollow') `
            -Detail "(before: $($fresh.before) → after: $($fresh.phase))" | Out-Null
        Assert-ProbeValue -Name '表示で閲覧回数が1になる' -Actual ([double]$fresh.count) -Expected 1 | Out-Null

        # --- 2b. 購読時点で既にポーズが解けていれば即座に出る（メインメニュー経由・スロット全空の即開始と同じ状況） ---
        $immediate = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.UseCase;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var message = scope.Container.Resolve<ITutorialMessageUseCase>();
var type = (TutorialType)System.Enum.Parse(typeof(TutorialType), "__TYPE__");

progress.ResetProgress(type);
message.Hide();
var before = view.Phase.ToString();
var pausedBefore = wave.IsWavePause.CurrentValue;

// ウェーブ1が既に始まっている状態で、後から初期化される UseCase を作って購読させる
using (var late = new TutorialWaveUseCase(
    scope.Container.Resolve<TutorialWaveConfig>(), wave, progress, message))
{
    late.Initialize();
}

return $"{{\"before\":\"{before}\",\"pausedBefore\":{pausedBefore.ToString().ToLower()},\"phase\":\"{view.Phase}\",\"count\":{progress.GetViewCount(type)}}}";
'@.Replace('__TYPE__', $config.type)

        Assert-ProbeTrue -Name '既に始まっているウェーブ1に後から購読しても即座に出る' `
            -Condition ($immediate.before -eq 'Hidden' -and -not [bool]$immediate.pausedBefore -and $immediate.phase -eq 'HeadFollow') `
            -Detail "(before: $($immediate.before), pausedBefore: $($immediate.pausedBefore), after: $($immediate.phase))" | Out-Null

        # --- 3. 規定回数まで閲覧済みにすると出ない ---
        $completed = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.DataStore;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var message = scope.Container.Resolve<ITutorialMessageUseCase>();
var type = (TutorialType)System.Enum.Parse(typeof(TutorialType), "__TYPE__");

for (var i = 0; i < TutorialProgressDataStore.RequiredViewCount; i++) progress.MarkViewed(type);
message.Hide();

wave.SetWavePause(true);
wave.SetWavePause(false);

return $"{{\"completed\":{progress.IsCompleted(type).ToString().ToLower()},\"phase\":\"{view.Phase}\",\"count\":{progress.GetViewCount(type)}}}";
'@.Replace('__TYPE__', $config.type)

        Assert-ProbeTrue -Name '規定回数閲覧すると閲覧済みになる' -Condition ([bool]$completed.completed) `
            -Detail "(回数: $($completed.count))" | Out-Null
        Assert-ProbeTrue -Name '閲覧済みならウェーブ開始でも出ない' -Condition ($completed.phase -eq 'Hidden') `
            -Detail "(phase: $($completed.phase))" | Out-Null

        # --- 4. 再表示設定が有効なら閲覧済みでも出る ---
        $replay = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var setting = scope.Container.Resolve<IPlayerSettingDataStore>();

setting.SetTutorialReplayEnabled(true);
wave.SetWavePause(true);
wave.SetWavePause(false);

return $"{{\"phase\":\"{view.Phase}\"}}";
'@

        Assert-ProbeTrue -Name '再表示設定が有効なら閲覧済みでも出る' -Condition ($replay.phase -eq 'HeadFollow') `
            -Detail "(phase: $($replay.phase))" | Out-Null

        # --- 5. 割り当てのないウェーブでは前のメッセージが消える ---
        $noEntry = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var config = scope.Container.Resolve<TutorialWaveConfig>();

// 割り当てのないウェーブ番号を探す
var target = 2;
while (config.TryGetTutorial(target, out _)) target++;

// 該当ウェーブまで進める（AdvanceWave は番号を1つ進めて OnWaveAdvanced を流す）
while (wave.CurrentWave.CurrentValue < target) wave.AdvanceWave();
var before = view.Phase.ToString();
wave.SetWavePause(true);
wave.SetWavePause(false);

return $"{{\"target\":{target},\"wave\":{wave.CurrentWave.CurrentValue},\"before\":\"{before}\",\"phase\":\"{view.Phase}\"}}";
'@

        Assert-ProbeTrue -Name '割り当てのないウェーブでは前のメッセージが消える' `
            -Condition ([int]$noEntry.wave -eq [int]$noEntry.target -and $noEntry.before -ne 'Hidden' -and $noEntry.phase -eq 'Hidden') `
            -Detail "(wave: $($noEntry.wave), before: $($noEntry.before), after: $($noEntry.phase))" | Out-Null
    }
    finally {
        # --- 6. セーブデータを元に戻して保存する ---
        $restore = Invoke-UnityCode -Snippet @'
using System.Linq;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var save = scope.Container.Resolve<ISaveDataStore>();
var setting = scope.Container.Resolve<IPlayerSettingDataStore>();

save.SaveData.TutorialViews.Clear();
foreach (var pair in "__VIEWS__".Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
{
    var parts = pair.Split(':');
    save.SaveData.TutorialViews.Add(new TutorialViewRecord { Type = (TutorialType)int.Parse(parts[0]), ViewCount = int.Parse(parts[1]) });
}
// 設定は DataStore 経由で戻す（ReactiveProperty も同期させる）
setting.SetTutorialReplayEnabled(__REPLAY__);
save.Save();
return "restored";
'@.Replace('__VIEWS__', $saved.views).Replace('__REPLAY__', $saved.replay.ToString().ToLower())

        Write-Host "セーブデータを戻しました（閲覧: [$($saved.views)] 再表示: $($saved.replay)）"
    }
}
