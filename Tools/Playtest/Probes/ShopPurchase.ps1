#
# ショップの通貨制（ShopUseCase / ShopView / UpgradeCardBoardView）を数値で検証するプローブ。
#
# 候補は3Dカードで並ぶため、実際にカードをマウスでクリックして購入し、
# 所持ポイント・取得済みアップグレード数・残ったカードの実測で確認する。
#
# カードのクリック判定は非VRのポインタ操作なので、ProbePrepareでVRモードを一時的に切り、
# ProbeCleanupで元へ戻す（VRモードのままだとカードは掴み操作でしか選べず、クリックが空振りする）。
#
# 候補のコストは固定値を仮定せず、カード上のコスト表記から読む。
# デバッグ用の開始アップグレードが載っているとLv2以上（＝コストが異なる）の候補が並ぶため。
#

function ProbePrepare {
    # VRモードはDebugConfigがstatic readonlyで持つため、Playに入る前（＝ドメインリロード前）に切り替える
    $Global:ShopProbePreviousVrMode = Invoke-UnityCode -Snippet @'
var before = UnityEditor.EditorPrefs.GetBool("VRMode", false);
UnityEditor.EditorPrefs.SetBool("VRMode", false);
return before.ToString().ToLower();
'@
    Write-Host "VRモードを一時的に無効化しました（元の値: $Global:ShopProbePreviousVrMode）"
}

function ProbeCleanup {
    if ($null -eq $Global:ShopProbePreviousVrMode) { return }

    $snippet = @'
UnityEditor.EditorPrefs.SetBool("VRMode", __PREVIOUS__);
return UnityEditor.EditorPrefs.GetBool("VRMode", false).ToString().ToLower();
'@.Replace('__PREVIOUS__', $Global:ShopProbePreviousVrMode)

    Invoke-UnityCode -Snippet $snippet | Out-Null
    Write-Host "VRモードを元に戻しました（$Global:ShopProbePreviousVrMode）"
    $Global:ShopProbePreviousVrMode = $null
}

# 所持ポイントと取得済みアップグレード数のスナップショット
function Get-ShopProbeState {
    return Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var sessionDataStore = scope.Container.Resolve<IUpgradeSessionDataStore>();

return $"{{\"point\":{pointDataStore.CurrentPoint.CurrentValue},\"owned\":{sessionDataStore.AppliedUpgrades.Count}}}";
'@
}

function ProbeRun {
    # --- 1. ポイント0ではどの候補も買えない ---
    Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var waveDataStore = scope.Container.Resolve<IWaveManagerDataStore>();

// ウェーブ突破と同じ手順でショップを開く
waveDataStore.SetWavePause(true);
waveDataStore.AdvanceWave();

return "opened";
'@ | Out-Null

    Start-Sleep -Milliseconds 500

    $noPointState = Get-ShopProbeState
    $noPointCards = @(Get-ShopCards)

    Assert-ProbeValue -Name 'ショップを開いた時点の所持ポイント' -Actual ([double]$noPointState.point) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '候補のカードが1枚以上並ぶ' -Condition ($noPointCards.Count -ge 1) `
        -Detail "(実測: $($noPointCards.Count)枚)" | Out-Null
    Assert-ProbeTrue -Name '並んだカードがすべて画面内に収まる' `
        -Condition (@($noPointCards | Where-Object { -not $_.onScreen }).Count -eq 0) | Out-Null
    Assert-ProbeTrue -Name 'カードにコストが表示される' `
        -Condition (@($noPointCards | Where-Object { $_.cost -lt 1 }).Count -eq 0) `
        -Detail "(コスト: $(($noPointCards | ForEach-Object { $_.cost }) -join ', '))" | Out-Null
    Assert-ProbeValue -Name 'ポイント0のとき買えるカードの数' `
        -Actual ([double]@($noPointCards | Where-Object { $_.purchasable }).Count) -Expected 0 | Out-Null

    # Assert-*は記録するだけで止まらないため、カードが無いまま進むと後段が意味不明なエラーで落ちる。
    # ここで打ち切って、上の検証結果がそのままレポートに残るようにする
    if ($noPointCards.Count -eq 0) {
        Write-Host "カードが1枚も並ばなかったため、以降の検証を打ち切ります"
        return
    }

    # --- 2. 買えないカードはクリックしても購入されない ---
    Invoke-ShopCardClick -Card $noPointCards[0]
    $afterDeadClick = Get-ShopProbeState
    $afterDeadClickCards = @(Get-ShopCards)

    Assert-ProbeValue -Name 'ポイント不足のクリックでポイントが減らない' `
        -Actual ([double]$afterDeadClick.point) -Expected ([double]$noPointState.point) | Out-Null
    Assert-ProbeValue -Name 'ポイント不足のクリックで取得数が増えない' `
        -Actual ([double]$afterDeadClick.owned) -Expected ([double]$noPointState.owned) | Out-Null
    Assert-ProbeValue -Name 'ポイント不足のクリックでカードが消えない' `
        -Actual ([double]$afterDeadClickCards.Count) -Expected ([double]$noPointCards.Count) | Out-Null

    # --- 3. ショップ表示中にポイントが入ると購入可否がその場で更新される ---
    #     （粒子はショップ表示中も吸い寄せられて回収されるため、開き直さずに反映される必要がある）
    Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();

// ショップは開いたまま、粒子を回収したのと同じようにポイントを足す
scope.Container.Resolve<IPointDataStore>().Add(500);

return "funded";
'@ | Out-Null

    $fundedState = Get-ShopProbeState
    $fundedCards = @(Get-ShopCards)

    Assert-ProbeValue -Name '付与後の所持ポイント' -Actual ([double]$fundedState.point) -Expected 500 | Out-Null
    Assert-ProbeTrue -Name '表示中にポイントが入るとカードを買えるようになる' `
        -Condition (@($fundedCards | Where-Object { $_.purchasable }).Count -ge 2) `
        -Detail "(買えるカード: $(@($fundedCards | Where-Object { $_.purchasable }).Count)枚)" | Out-Null

    # --- 4. カードのクリックで購入でき、同じショップで続けて買える ---
    # 開始アップグレードが載っているとLv2以上＝高コストの候補が混ざるため、必ず買えるカードを選ぶ
    $affordableCards = @($fundedCards | Where-Object { $_.purchasable -and $_.onScreen })

    if ($affordableCards.Count -eq 0) {
        Write-Host "買えるカードが無かったため、以降の検証を打ち切ります"
        return
    }

    $firstCard = $affordableCards[0]
    Invoke-ShopCardClick -Card $firstCard
    $afterFirst = Get-ShopProbeState
    $afterFirstCards = @(Get-ShopCards)

    Assert-ProbeValue -Name '1件目の購入で減ったポイント' `
        -Actual ([double]$fundedState.point - [double]$afterFirst.point) -Expected ([double]$firstCard.cost) | Out-Null
    Assert-ProbeValue -Name '1件目の購入で増えた取得数' `
        -Actual ([double]$afterFirst.owned - [double]$fundedState.owned) -Expected 1 | Out-Null
    Assert-ProbeValue -Name '購入したカードが消える' `
        -Actual ([double]$afterFirstCards.Count) -Expected ([double]$fundedCards.Count - 1) | Out-Null
    Assert-ProbeValue -Name '購入したカードだけが消える' `
        -Actual ([double]@($afterFirstCards | Where-Object { $_.index -eq $firstCard.index }).Count) -Expected 0 | Out-Null

    $secondCandidates = @($afterFirstCards | Where-Object { $_.purchasable -and $_.onScreen })

    if ($secondCandidates.Count -eq 0) {
        Write-Host "2件目に買えるカードが無かったため、以降の検証を打ち切ります"
        return
    }

    $secondCard = $secondCandidates[0]
    Invoke-ShopCardClick -Card $secondCard
    $afterSecond = Get-ShopProbeState

    Assert-ProbeValue -Name '2件目の購入で減ったポイント' `
        -Actual ([double]$afterFirst.point - [double]$afterSecond.point) -Expected ([double]$secondCard.cost) | Out-Null
    Assert-ProbeValue -Name '同じショップで2件購入できる' `
        -Actual ([double]$afterSecond.owned - [double]$fundedState.owned) -Expected 2 | Out-Null

    # --- 5. 買えるだけ買うと、残ったカードは買えないものだけになる ---
    for ($i = 0; $i -lt 12; $i++) {
        $affordable = @(Get-ShopCards | Where-Object { $_.purchasable })
        if ($affordable.Count -eq 0) { break }
        Invoke-ShopCardClick -Card $affordable[0]
    }

    $spentOutState = Get-ShopProbeState
    $spentOutCards = @(Get-ShopCards)

    Assert-ProbeValue -Name '買えるカードが残っていない' `
        -Actual ([double]@($spentOutCards | Where-Object { $_.purchasable }).Count) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '残ったカードに所持ポイントで買えるものは無い' `
        -Actual ([double]@($spentOutCards | Where-Object { $_.cost -le $spentOutState.point }).Count) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'まとめ買いで複数取得できている' `
        -Condition ([int]$spentOutState.owned - [int]$fundedState.owned -ge 2) `
        -Detail "(このショップでの取得数: $([int]$spentOutState.owned - [int]$fundedState.owned)件)" | Out-Null
}
