#
# 画面ごとのチュートリアル表示（ショップ・ゲームオーバー・セット選択）を検証するプローブ。
#
# - ショップを開くと Shop が出る
# - 次のウェーブが始まるとショップの説明は残らない（ウェーブに割り当てが無い・閲覧済みなら Hide）
# - セットを持ち込まずに始めたランのゲームオーバーでは GameOver が出る
# - リスタートでバトル内のセット選択に戻ると SelectSlot が出る（前のメッセージは消える）
# - スロットを選んで始めたランのゲームオーバーでは GameOver の代わりに OtherBuild が出る
# - メインメニューのセット選択でも SelectSlot が出て、タイトルへ戻ると消える
#
# 表示中の種類は TutorialMessageUseCase の _currentType を読んで判定する。
# 閲覧回数と再表示設定はセーブデータに永続化されるため、開始時に退避し、終了時に元へ戻して保存する。
# 保存済みスロットが1つも無いとセット選択が出ないため、その場合は検証NGとして理由を出す。
#

$Global:ProbeTutorialScreenRestartButtonPath = 'BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/RestartButton'

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

# 表示中のチュートリアルの種類・配置フェーズ・閲覧回数を読む。__SCOPE__ はシーンの LifetimeScope 型
$Global:TutorialScreenStateSnippet = @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<__SCOPE__>();
var message = scope.Container.Resolve<ITutorialMessageUseCase>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
var current = (TutorialType?)message.GetType()
    .GetField("_currentType", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(message);
var type = current.HasValue ? current.Value.ToString() : "None";
return $"{{\"type\":\"{type}\",\"phase\":\"{view.Phase}\",\"shop\":{progress.GetViewCount(TutorialType.Shop)},\"gameOver\":{progress.GetViewCount(TutorialType.GameOver)},\"selectSlot\":{progress.GetViewCount(TutorialType.SelectSlot)},\"otherBuild\":{progress.GetViewCount(TutorialType.OtherBuild)}}}";
'@

function Get-TutorialScreenState {
    param([string] $Scope = 'App.Battle.BattleLifetimeScope')
    return Invoke-UnityJson -Snippet ($Global:TutorialScreenStateSnippet.Replace('__SCOPE__', $Scope))
}

# HPを0にしてゲームオーバーを起こし、結果画面が出るまで待つ（GameOverRestart プローブと同じ経路）
function Invoke-TutorialScreenGameOver {
    Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();

// バリアは1発を丸ごと吸収する仕様なので、HPが0になるまで繰り返し当てる
for (var i = 0; i < 10 && player.Health.Value > 0f; i++)
{
    player.TakeDamage(99999f);
}

return "damaged";
'@ | Out-Null

    return (Wait-GameOverPanel)
}

function ProbeRun {
    # --- 0. セーブデータの退避と前提の準備 ---
    $saved = Invoke-UnityJson -Snippet @'
using System.Linq;
using VContainer;
using VContainer.Unity;
using App.Common;
using App.Common.Interface;

var scope = LifetimeScope.Find<CommonLifetimeScope>();
var save = scope.Container.Resolve<ISaveDataStore>();
var meta = scope.Container.Resolve<IMetaProgressionDataStore>();
var views = string.Join(";", save.SaveData.TutorialViews.Select(v => $"{(int)v.Type}:{v.ViewCount}"));
return $"{{\"views\":\"{views}\",\"replay\":{save.SaveData.IsTutorialReplayEnabled.ToString().ToLower()},\"hasSlot\":{meta.HasAnySavedSlot.ToString().ToLower()}}}";
'@
    Write-Host "セーブデータを退避しました（閲覧: [$($saved.views)] 再表示: $($saved.replay)）"

    Assert-ProbeTrue -Name '保存済みスロットがある（セット選択の検証に必要）' -Condition ([bool]$saved.hasSlot) `
        -Detail '(無ければ一度ゲームオーバーでスロットへ保存してから実行する)' | Out-Null
    if (-not [bool]$saved.hasSlot) { return }

    try {
        # 再表示設定を切り、今回の4種類を未閲覧に戻す（閲覧回数の記録も合わせて確かめる）
        Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var progress = scope.Container.Resolve<ITutorialProgressDataStore>();
scope.Container.Resolve<IPlayerSettingDataStore>().SetTutorialReplayEnabled(false);
progress.ResetProgress(TutorialType.Shop);
progress.ResetProgress(TutorialType.GameOver);
progress.ResetProgress(TutorialType.SelectSlot);
progress.ResetProgress(TutorialType.OtherBuild);
scope.Container.Resolve<ITutorialMessageUseCase>().Hide();
return "ready";
'@ | Out-Null

        # --- 1. ショップを開くと Shop が出る ---
        $shop = Invoke-UnityJson -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.UseCase;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var paused = wave.IsWavePause.CurrentValue;

// ウェーブ突破と同じ OpenShop を通す（デバッグ用の入口。ウェーブ番号は進めない）
var shop = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<ShopUseCase>().First();
typeof(ShopUseCase).GetMethod("OpenShopForDebug", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shop, null);

return $"{{\"pausedBefore\":{paused.ToString().ToLower()},\"pausedAfter\":{wave.IsWavePause.CurrentValue.ToString().ToLower()}}}";
'@
        $afterShop = Get-TutorialScreenState
        Assert-ProbeTrue -Name 'ショップを開くと Shop が出る' `
            -Condition (-not [bool]$shop.pausedBefore -and [bool]$shop.pausedAfter -and $afterShop.type -eq 'Shop' -and $afterShop.phase -eq 'HeadFollow') `
            -Detail "(種類: $($afterShop.type), phase: $($afterShop.phase), ポーズ: $($shop.pausedBefore)→$($shop.pausedAfter))" | Out-Null
        Assert-ProbeValue -Name 'Shop の閲覧回数が1になる' -Actual ([double]$afterShop.shop) -Expected 1 | Out-Null

        # --- 2. 次のウェーブが始まるとショップの説明は残らない ---
        Invoke-UnityCode -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.UseCase;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var shop = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<ShopUseCase>().First();
typeof(ShopUseCase).GetMethod("StartNextWave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shop, null);
return "next";
'@ | Out-Null
        $afterNext = Get-TutorialScreenState
        Assert-ProbeTrue -Name '次のウェーブ開始でショップの説明が消える（または差し替わる）' -Condition ($afterNext.type -ne 'Shop') `
            -Detail "(種類: $($afterNext.type), phase: $($afterNext.phase))" | Out-Null

        # --- 3. セットを持ち込まずに始めたランのゲームオーバーでは GameOver ---
        $shown = Invoke-TutorialScreenGameOver
        $afterGameOver = Get-TutorialScreenState
        Assert-ProbeTrue -Name '持ち込みなしのゲームオーバーで GameOver が出る' `
            -Condition ($shown -and $afterGameOver.type -eq 'GameOver' -and $afterGameOver.phase -eq 'HeadFollow') `
            -Detail "(画面: $shown, 種類: $($afterGameOver.type), phase: $($afterGameOver.phase))" | Out-Null
        Assert-ProbeValue -Name 'GameOver の閲覧回数が1になる' -Actual ([double]$afterGameOver.gameOver) -Expected 1 | Out-Null
        Assert-ProbeValue -Name '持ち込みなしでは OtherBuild を出さない' -Actual ([double]$afterGameOver.otherBuild) -Expected 0 | Out-Null

        # --- 4. リスタートでバトル内のセット選択に戻ると SelectSlot ---
        Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
            action = 'Click'; 'target-path' = $Global:ProbeTutorialScreenRestartButtonPath; 'bypass-raycast' = 'true'
        } | Out-Null
        Start-Sleep -Milliseconds 1000

        $afterRestart = Get-TutorialScreenState
        $selecting = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();
return $"{{\"selecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"loaded\":{runStart.HasLoadedBuild.ToString().ToLower()}}}";
'@
        Assert-ProbeTrue -Name 'リスタート後のセット選択で SelectSlot が出る' `
            -Condition ([bool]$selecting.selecting -and $afterRestart.type -eq 'SelectSlot' -and $afterRestart.phase -eq 'HeadFollow') `
            -Detail "(選択中: $($selecting.selecting), 種類: $($afterRestart.type), phase: $($afterRestart.phase))" | Out-Null
        Assert-ProbeValue -Name 'SelectSlot の閲覧回数が1になる' -Actual ([double]$afterRestart.selectSlot) -Expected 1 | Out-Null
        Assert-ProbeTrue -Name 'セット選択中は持ち込みなし扱いに戻っている' -Condition (-not [bool]$selecting.loaded) | Out-Null

        # --- 5. スロットを選んで始めたランのゲームオーバーでは OtherBuild ---
        $loaded = Invoke-UnityJson -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.UseCase;
using App.Battle.Interface.DataStore;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var meta = scope.Container.Resolve<IMetaProgressionDataStore>();
var slot = 0;
while (slot < meta.SlotCount && meta.IsSlotEmpty(slot)) slot++;

// RunStartView のスロットボタンと同じ経路（OnSlotSelected）で選ぶ
var runStartUseCase = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<RunStartUseCase>().First();
typeof(RunStartUseCase).GetMethod("OnSlotSelected", BindingFlags.NonPublic | BindingFlags.Instance)
    .Invoke(runStartUseCase, new object[] { slot });

var runStart = scope.Container.Resolve<IRunStartDataStore>();
return $"{{\"slot\":{slot},\"selecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"loaded\":{runStart.HasLoadedBuild.ToString().ToLower()}}}";
'@
        Assert-ProbeTrue -Name 'スロットを選ぶと持ち込みありで始まる' -Condition (-not [bool]$loaded.selecting -and [bool]$loaded.loaded) `
            -Detail "(スロット: $($loaded.slot), 選択中: $($loaded.selecting), 持ち込み: $($loaded.loaded))" | Out-Null

        $shown = Invoke-TutorialScreenGameOver
        $afterOther = Get-TutorialScreenState
        Assert-ProbeTrue -Name '持ち込みありのゲームオーバーで OtherBuild が出る' `
            -Condition ($shown -and $afterOther.type -eq 'OtherBuild' -and $afterOther.phase -eq 'HeadFollow') `
            -Detail "(画面: $shown, 種類: $($afterOther.type), phase: $($afterOther.phase))" | Out-Null
        Assert-ProbeValue -Name 'OtherBuild の閲覧回数が1になる' -Actual ([double]$afterOther.otherBuild) -Expected 1 | Out-Null
        Assert-ProbeValue -Name '持ち込みありでは GameOver を出さない（回数が増えない）' -Actual ([double]$afterOther.gameOver) -Expected 1 | Out-Null

        # --- 6. メインメニューのセット選択でも SelectSlot が出て、タイトルへ戻ると消える ---
        Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Common;
using App.Common.Interface;

var scope = LifetimeScope.Find<CommonLifetimeScope>();
scope.Container.Resolve<ISceneTransitionUseCase>().LoadMainMenu();
return "loading";
'@ | Out-Null

        $menuReady = $false
        for ($i = 0; $i -lt 50; $i++) {
            Start-Sleep -Milliseconds 200
            $ready = Invoke-UnityJson -Snippet @'
using VContainer.Unity;
using App.MainMenu;

var scope = LifetimeScope.Find<MainMenuLifetimeScope>();
var ready = scope != null && scope.Container != null;
return $"{{\"ready\":{ready.ToString().ToLower()}}}";
'@
            if ([bool]$ready.ready) { $menuReady = $true; break }
        }
        Assert-ProbeTrue -Name 'メインメニューへ移れた' -Condition $menuReady | Out-Null
        if (-not $menuReady) { return }

        $menu = Invoke-UnityJson -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using App.MainMenu;
using App.MainMenu.UseCase;

var scope = LifetimeScope.Find<MainMenuLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>();
var before = view.Phase.ToString();

// START ボタンと同じ経路（OnStart）でセット選択を出す
var mainMenu = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<MainMenuUseCase>().First();
typeof(MainMenuUseCase).GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(mainMenu, null);
return $"{{\"before\":\"{before}\"}}";
'@
        Start-Sleep -Milliseconds 300
        $menuShown = Get-TutorialScreenState -Scope 'App.MainMenu.MainMenuLifetimeScope'
        Assert-ProbeTrue -Name 'メインメニューのセット選択で SelectSlot が出る' `
            -Condition ($menu.before -eq 'Hidden' -and $menuShown.type -eq 'SelectSlot' -and $menuShown.phase -eq 'HeadFollow') `
            -Detail "(before: $($menu.before), 種類: $($menuShown.type), phase: $($menuShown.phase))" | Out-Null
        Assert-ProbeValue -Name 'メインメニューでも SelectSlot の閲覧回数が記録される' -Actual ([double]$menuShown.selectSlot) -Expected 2 | Out-Null

        $menuText = Invoke-UnityJson -Snippet @'
using TMPro;
using VContainer;
using VContainer.Unity;
using App.Common.Data;
using App.Common.Interface;
using App.Common.Views;
using App.MainMenu;

var scope = LifetimeScope.Find<MainMenuLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var text = view.GetComponentInChildren<TMP_Text>(true).text;
var expected = scope.Container.Resolve<ITutorialLocalizationDataStore>().GetText(TutorialType.SelectSlot);
return $"{{\"match\":{(text == expected && !string.IsNullOrEmpty(text)).ToString().ToLower()},\"length\":{text.Length}}}";
'@
        Assert-ProbeTrue -Name 'メインメニューのダイアログ本文が SelectSlot の文言' -Condition ([bool]$menuText.match) `
            -Detail "(文字数: $($menuText.length))" | Out-Null

        Invoke-UnityCode -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.MainMenu;
using App.MainMenu.UseCase;

var scope = LifetimeScope.Find<MainMenuLifetimeScope>();
var mainMenu = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<MainMenuUseCase>().First();
typeof(MainMenuUseCase).GetMethod("ShowTitle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(mainMenu, null);
return "title";
'@ | Out-Null
        $menuHidden = Get-TutorialScreenState -Scope 'App.MainMenu.MainMenuLifetimeScope'
        Assert-ProbeTrue -Name 'タイトルへ戻るとメッセージが消える' -Condition ($menuHidden.type -eq 'None' -and $menuHidden.phase -eq 'Hidden') `
            -Detail "(種類: $($menuHidden.type), phase: $($menuHidden.phase))" | Out-Null
    }
    finally {
        # --- 7. セーブデータを元に戻して保存する（シーンに依らず常駐スコープから触る） ---
        Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Common;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<CommonLifetimeScope>();
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
'@.Replace('__VIEWS__', $saved.views).Replace('__REPLAY__', $saved.replay.ToString().ToLower()) | Out-Null

        Write-Host "セーブデータを戻しました（閲覧: [$($saved.views)] 再表示: $($saved.replay)）"
    }
}
