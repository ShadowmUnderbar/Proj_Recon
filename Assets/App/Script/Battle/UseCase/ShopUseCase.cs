using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ウェーブ間ショップの制御（仮組み）
    /// ウェーブ突破（OnWaveAdvanced）でショップを開き、所持ポイントで買えるだけアップグレードを購入させ、
    /// 「次のウェーブへ」でショップを閉じてウェーブを再開する。
    /// 開くときにウェーブ中のチュートリアルを消してからショップのチュートリアルを出し、閉じるときにも出ているチュートリアルを消す。
    /// デバッグ対戦では出さない（既読にして、通常のプレイで出なくなるのを防ぐ）
    /// </summary>
    public class ShopUseCase : IRunResettable, IInitializable, ITickable, IDisposable
    {
        // ショップに並べるアップグレードの基本抽選数（目利きで加算される）
        private const int BaseUpgradeChoiceCount = 5;

        // 目利きによる加算後の上限。ShopView.prefab のボタン数（4列×3行のグリッド）と一致させること
        private const int MaxUpgradeChoiceCount = 12;

        // 所持アップグレードのタグ上位として表示する件数
        private const int OwnedTagRankingCount = 5;

        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IUpgradeLotteryDataStore _upgradeLotteryDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeSideEffectApplier _upgradeSideEffectApplier;
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IPointDataStore _pointDataStore;
        private readonly IShopPresenter _shopPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly IUpgradeLocalizationDataStore _upgradeLocalizationDataStore;
        private readonly ITagLocalizationDataStore _tagLocalizationDataStore;
        private readonly IUpgradeTagRankingDataStore _upgradeTagRankingDataStore;
        private readonly ITutorialMessageUseCase _tutorialMessageUseCase;
        private readonly DebugArenaSettings _debugArenaSettings;

        private readonly CompositeDisposable _disposable = new();

        // 今回のショップの候補。購入済みの枠は null にしてインデックス（＝ボタンの並び）を保つ
        private readonly List<UpgradeMasterData> _currentCandidates = new();

        // ショップ表示中か。表示中だけ所持ポイントの変化をUIへ反映し、手の入力をViewへ流す
        private bool _isShopOpen;

        [Inject]
        public ShopUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IUpgradeLotteryDataStore upgradeLotteryDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeSideEffectApplier upgradeSideEffectApplier,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IPointDataStore pointDataStore,
            IShopPresenter shopPresenter,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore,
            IUpgradeLocalizationDataStore upgradeLocalizationDataStore,
            ITagLocalizationDataStore tagLocalizationDataStore,
            IUpgradeTagRankingDataStore upgradeTagRankingDataStore,
            ITutorialMessageUseCase tutorialMessageUseCase,
            DebugArenaSettings debugArenaSettings
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _upgradeLotteryDataStore = upgradeLotteryDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeSideEffectApplier = upgradeSideEffectApplier;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _pointDataStore = pointDataStore;
            _shopPresenter = shopPresenter;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
            _upgradeLocalizationDataStore = upgradeLocalizationDataStore;
            _tagLocalizationDataStore = tagLocalizationDataStore;
            _upgradeTagRankingDataStore = upgradeTagRankingDataStore;
            _tutorialMessageUseCase = tutorialMessageUseCase;
            _debugArenaSettings = debugArenaSettings;
        }

        public void Initialize()
        {
            // ウェーブ突破時（この時点で IsWavePause=true・敵/弾クリア済み）にショップを開く
            _waveManagerDataStore.OnWaveAdvanced
                .Subscribe(_ => OpenShop())
                .AddTo(_disposable);

            _shopPresenter.OnUpgradeSelected
                .Subscribe(OnUpgradeSelected)
                .AddTo(_disposable);

            _shopPresenter.OnNextWavePressed
                .Subscribe(_ => StartNextWave())
                .AddTo(_disposable);

            // デバッグ: Shift+U でウェーブ突破を待たずにショップを開く（押した瞬間のみ。入力はエディタ限定）
            _gameInputDataStore.DebugOpenUpgradeShop
                .Where(isPressed => isPressed)
                .Subscribe(_ => OpenShopForDebug())
                .AddTo(_disposable);

            // ショップ表示中も粒子は吸い寄せられて回収されるため、所持ポイントの変化を表示と購入可否へ反映する
            _pointDataStore.CurrentPoint
                .Subscribe(_ => OnCurrentPointChanged())
                .AddTo(_disposable);

            // ローカライズ表の読込完了・ロケール切替で、表示中のカードの文言を差し替える
            _upgradeLocalizationDataStore.OnTableChanged
                .Subscribe(_ => OnLocalizationChanged())
                .AddTo(_disposable);

            // タグ名の表は別テーブルなので、読込完了・ロケール切替でカードのタグと所持タグ上位を差し替える
            _tagLocalizationDataStore.OnTableChanged
                .Subscribe(_ => OnTagLocalizationChanged())
                .AddTo(_disposable);

            // 購入で所持アップグレードが増えたら所持タグ上位を数え直す
            _upgradeSessionDataStore.OnChanged
                .Where(_ => _isShopOpen)
                .Subscribe(_ => RefreshOwnedTagRanking())
                .AddTo(_disposable);
        }

        private void OpenShop()
        {
            // 出現可能なアップグレードから抽選（候補ゼロならボタンはView側で全非表示になる）
            _currentCandidates.Clear();
            _currentCandidates.AddRange(_upgradeLotteryDataStore.DrawUpgrades(GetUpgradeChoiceCount()));
            _shopPresenter.Open(_currentCandidates);
            _isShopOpen = true;

            RefreshUpgradeTexts();
            RefreshUpgradeTags();
            RefreshOwnedTagRanking();

            // ショップ中はグラブ・トリガーをカード操作に使うため、フォーカスの切り替えは止める
            _gameInputDataStore.SetFocusInputEnable(false);

            RefreshPurchasable();

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);

            // ウェーブ中に出していたチュートリアル（ウェーブ開始時のものなど）をショップへ持ち越さない。
            // ショップの説明が既読で出ないときも、前のメッセージが残ったままにならないよう先に消す
            _tutorialMessageUseCase.Hide();

            if (!_debugArenaSettings.IsEnabled)
            {
                _tutorialMessageUseCase.ShowIfNeeded(TutorialType.Shop);
            }
        }

        /// <summary>
        /// デバッグ用にショップを開く。ウェーブ番号は進めず、ポーズして開くだけにする
        /// （「次のウェーブへ」でポーズ解除され、同じウェーブの続きから再開する）。
        /// ラン開始前のビルド選択中・ゲームオーバー中はポーズ中なので開かない
        /// </summary>
        private void OpenShopForDebug()
        {
            if (_isShopOpen || _waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            _waveManagerDataStore.SetWavePause(true);
            OpenShop();
        }

        private void OnLocalizationChanged()
        {
            // 閉じている間は反映先のカードが無い（次に開いたときに最新の文言で入る）
            if (!_isShopOpen)
            {
                return;
            }

            RefreshUpgradeTexts();
        }

        /// <summary>
        /// 候補ごとのローカライズ文言（名前・レベル・簡略説明・詳細説明）をViewへ反映する。
        /// テーブル読込前はキー文字列が入り、読込完了の通知で差し替わる
        /// </summary>
        private void RefreshUpgradeTexts()
        {
            for (var i = 0; i < _currentCandidates.Count; i++)
            {
                // 購入済み（null）の枠はカードごと消えているため触らない
                if (_currentCandidates[i] == null)
                {
                    continue;
                }

                _shopPresenter.SetUpgradeText(i, _upgradeLocalizationDataStore.GetText(_currentCandidates[i]));
            }
        }

        private void OnTagLocalizationChanged()
        {
            // 閉じている間は反映先が無い（次に開いたときに最新の名前で入る）
            if (!_isShopOpen)
            {
                return;
            }

            RefreshUpgradeTags();
            RefreshOwnedTagRanking();
        }

        /// <summary>
        /// 候補ごとのタグの表示名をViewへ反映する。並びはマスターデータ（UpgradeTagData シート）の順
        /// </summary>
        private void RefreshUpgradeTags()
        {
            for (var i = 0; i < _currentCandidates.Count; i++)
            {
                // 購入済み（null）の枠はカードごと消えているため触らない
                if (_currentCandidates[i] == null)
                {
                    continue;
                }

                var tags = _currentCandidates[i].Tags;
                var tagNames = new string[tags.Count];
                for (var t = 0; t < tags.Count; t++)
                {
                    tagNames[t] = _tagLocalizationDataStore.GetName(tags[t]);
                }

                _shopPresenter.SetUpgradeTags(i, tagNames);
            }
        }

        /// <summary>所持アップグレードのタグを件数の多い順に数え、上位を名前つきでViewへ反映する</summary>
        private void RefreshOwnedTagRanking()
        {
            var ranking = _upgradeTagRankingDataStore.GetTopTags(
                _upgradeSessionDataStore.AppliedUpgrades, OwnedTagRankingCount);

            var entries = new LocalizedUpgradeTagCount[ranking.Count];
            for (var i = 0; i < ranking.Count; i++)
            {
                entries[i] = new LocalizedUpgradeTagCount(
                    _tagLocalizationDataStore.GetName(ranking[i].Tag), ranking[i].Count);
            }

            _shopPresenter.SetOwnedTagRanking(entries);
        }

        private void OnCurrentPointChanged()
        {
            // 非表示のショップUIを触らない（ショップ外での回収はHUD側の担当）
            if (!_isShopOpen)
            {
                return;
            }

            RefreshPurchasable();
        }

        /// <summary>
        /// 所持ポイントの表示と、候補ごとの購入可否（ポイントが足りるか）をViewへ反映する
        /// </summary>
        private void RefreshPurchasable()
        {
            var currentPoint = _pointDataStore.CurrentPoint.CurrentValue;
            _shopPresenter.SetCurrentPoint(currentPoint);

            for (var i = 0; i < _currentCandidates.Count; i++)
            {
                // 購入済み（null）の枠はボタンごと消えているため触らない
                if (_currentCandidates[i] == null)
                {
                    continue;
                }

                _shopPresenter.SetPurchasable(i, _currentCandidates[i].Cost <= currentPoint);
            }
        }

        /// <summary>
        /// 今回のショップに並べる候補数。目利きは累積せず最高レベルのみ採用し、View のボタン数を超えないようにする
        /// </summary>
        private int GetUpgradeChoiceCount()
        {
            var extraCount = Mathf.RoundToInt(_upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.Appraisal));
            return Mathf.Min(BaseUpgradeChoiceCount + extraCount, MaxUpgradeChoiceCount);
        }

        private void OnUpgradeSelected(int index)
        {
            if (index < 0 || index >= _currentCandidates.Count)
            {
                return;
            }

            var selected = _currentCandidates[index];

            // 購入済みの枠（null）への押下は無視する
            if (selected == null)
            {
                return;
            }

            // ポイントが足りなければ購入させない（View側でもボタンを押せなくしているが、念のため弾く）
            if (!_pointDataStore.TrySpend(selected.Cost))
            {
                return;
            }

            _upgradeSessionDataStore.AddUpgrade(selected);

            // GrantBuff/バリア等の付与副作用を適用（読込フローと共通処理）
            _upgradeSideEffectApplier.Apply(selected);

            // 購入した候補だけを消し、残りはポイントが続く限り買えるままにする
            _currentCandidates[index] = null;
            _shopPresenter.HideUpgradeButton(index);

            RefreshPurchasable();
        }

        public void ResetRun()
        {
            // ショップを開いたままゲームオーバーになった場合、閉じないとビルド選択UIに重なって残り、
            // 生きている「次のウェーブへ」ボタンで選択中のままランが走り出してしまう
            if (_isShopOpen)
            {
                _shopPresenter.Close();
            }

            _isShopOpen = false;
            _currentCandidates.Clear();
        }

        /// <summary>
        /// カード操作用の入力をViewへ送る。VRは手の姿勢とグラブ・トリガー、非VRはマウスを送り、
        /// カードを並べられずCanvasへフォールバックした場合はView側で無視される
        /// </summary>
        public void Tick()
        {
            if (!_isShopOpen)
            {
                return;
            }

            if (!DebugConfig.IsVRMode)
            {
                // 非VRはマウスで狙ってクリック。左クリックは入力定義上 UseLeft（＝左トリガー）に割り当てられている
                _shopPresenter.UpdatePointerInput(new ShopPointerInput(
                    _gameInputDataStore.MouseInputPosition,
                    _gameInputDataStore.IsLeftTrigger.CurrentValue));

                return;
            }

            _shopPresenter.UpdateHandInput(new ShopHandInput(
                HandType.Left,
                _playerControlPresenter.LeftHandPose.Value,
                _gameInputDataStore.IsGrabLeft.Value,
                _gameInputDataStore.IsLeftTrigger.Value));

            _shopPresenter.UpdateHandInput(new ShopHandInput(
                HandType.Right,
                _playerControlPresenter.RightHandPose.Value,
                _gameInputDataStore.IsGrabRight.Value,
                _gameInputDataStore.IsRightTrigger.Value));
        }

        private void StartNextWave()
        {
            _isShopOpen = false;
            _currentCandidates.Clear();
            _gameInputDataStore.SetFocusInputEnable(true);
            _shopPresenter.Close();
            _playerControlPresenter.SetUiRayEnable(false);

            // ショップの説明などを次のウェーブへ持ち越さない。
            // ウェーブ再開より先に消すので、次のウェーブに割り当てたチュートリアルは TutorialWaveUseCase が出せる
            _tutorialMessageUseCase.Hide();

            // ポーズ解除で次ウェーブ再開（時間計測・スポーン・撃破カウントが再始動）
            _waveManagerDataStore.SetWavePause(false);
        }

        public void Dispose()
        {
            // ショップを開いたままシーンが終わってもフォーカス入力が止まりっぱなしにならないようにする
            _gameInputDataStore.SetFocusInputEnable(true);
            _disposable?.Dispose();
        }
    }
}
