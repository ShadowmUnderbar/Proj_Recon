using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data.MasterData;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// アップグレード候補を3Dカードとして並べ、掴んで内容を確認し、トリガーで確定させるVR用のボード。
    ///
    /// カードはショップを開いた時点のカメラ（HMD）前方へ一度だけ配置し、以降はワールド固定にする。
    /// 追従させると掴む対象が動いてしまい狙いにくくなるため、あえて置きっぱなしにしている。
    ///
    /// 掴みは「手を重ねて掴む（近接）」と「レイで狙って掴む（遠隔）」の両方に対応する。
    /// 近接のほうが意図が明確なため、近接で掴めるカードがあればそちらを優先する。
    ///
    /// 非VR（PC/エディタ）では手が無いため掴みは使わず、マウスで狙ってクリックした時点で確定する。
    /// カードの見た目・配置はVRと共通なので、実機が無くてもPCで確認できる。
    ///
    /// このクラスはカードの生成・破棄と Inspector 値の保持だけを持ち、
    /// 配置は <see cref="UpgradeCardBoardLayout"/>、検索は <see cref="UpgradeCardFinder"/>、
    /// 掴みの状態遷移は <see cref="UpgradeCardHandInteraction"/> / <see cref="UpgradeCardPointerInteraction"/> が担う
    /// </summary>
    public class UpgradeCardBoardView : MonoBehaviour
    {
        [SerializeField, Tooltip("カード1枚のプレハブ")]
        private UpgradeCardView _cardPrefab;

        [Header("配置")]
        [SerializeField, Tooltip("カメラからカードまでの距離[m]")]
        private float _distance = 0.9f;

        [SerializeField, Range(-180f, 180f), Tooltip("カードを配置する俯角[deg]。0で視線の正面、正の値で下側")]
        private float _pitchAngle = 30f;

        [SerializeField, Tooltip("1行あたりのカード枚数")]
        private int _columnCount = 5;

        [SerializeField, Tooltip("カードの間隔[m]（X:横 Y:縦）")]
        private Vector2 _cardSpacing = new(0.18f, 0.26f);

        [SerializeField, Tooltip("並べたカードの向き[deg]。カードの表がプレイヤー側を向くように調整する")]
        private Vector3 _cardFacingRotation = new(0f, 180f, 0f);

        [Header("配置（非VR）")]
        [SerializeField, Tooltip("非VRでのカメラ相対のカード位置[m]。見下ろしカメラでも画面内に収まるようにする")]
        private Vector3 _pointerLocalPosition = new(0f, 0f, 1f);

        [Header("持ったときの見え方")]
        [SerializeField, Tooltip("持っているときのカードの拡大率")]
        private float _holdScale = 1.2f;

        [SerializeField, Range(1f, 5f),
         Tooltip("手首のひねりの増幅率。1で手首どおり、大きいほど少ないひねりでカードが回る（裏面の確認用）")]
        private float _wristRollMultiplier = 2f;

        [SerializeField, Tooltip("カードが手や定位置へ追いつく速さ。大きいほど速い")]
        private float _followSpeed = 18f;

        [Header("掴み判定")]
        [SerializeField, Tooltip("手を重ねて掴める距離[m]")]
        private float _directGrabDistance = 0.12f;

        [SerializeField, Tooltip("レイで掴める距離[m]")]
        private float _rayGrabDistance = 5f;

        [SerializeField, Tooltip("カードのコライダーが属するレイヤー")]
        private LayerMask _cardLayerMask = ~0;

        private readonly Subject<int> _onCardConfirmed = new();

        /// <summary>掴んだカードをトリガーで確定したときに、その候補インデックスを流す</summary>
        public Observable<int> OnCardConfirmed => _onCardConfirmed;

        private readonly List<UpgradeCardView> _cards = new();

        private readonly UpgradeCardHandInteraction _handInteraction = new();
        private readonly UpgradeCardPointerInteraction _pointerInteraction = new();

        /// <summary>カードの検索。_cards の参照は不変なので一度だけ作り、判定範囲は毎フレーム渡す</summary>
        private UpgradeCardFinder _finder;

        /// <summary>確定通知の委譲。毎フレームのラムダ生成を避けるため一度だけ作る</summary>
        private Action<int> _confirmCard;

        private Camera _targetCamera;

        /// <summary>マウスで選ぶモード（非VR）か。掴みは使わず、クリックした時点で確定する</summary>
        private bool _isPointerMode;

        private void Awake()
        {
            _finder = new UpgradeCardFinder(_cards, _handInteraction.IsHeld);
            _confirmCard = index => _onCardConfirmed.OnNext(index);
        }

        /// <summary>
        /// 候補ぶんのカードを生成し、カメラ前方に並べる。
        /// プレハブ未設定やカメラ未取得で並べられなかった場合は false を返し、呼び出し側でUIを出し分けられるようにする
        /// </summary>
        /// <param name="upgrades">並べるアップグレード候補</param>
        /// <param name="isPointerMode">非VRでマウス操作にするか。false なら手での掴み操作</param>
        public bool Open(IReadOnlyList<UpgradeMasterData> upgrades, bool isPointerMode)
        {
            Close();
            _isPointerMode = isPointerMode;

            if (_cardPrefab == null || upgrades == null || upgrades.Count == 0)
            {
                return false;
            }

            if (!TryGetTargetCamera(out var targetCamera))
            {
                return false;
            }

            var boardPose = isPointerMode
                ? UpgradeCardBoardLayout.CalcPointerBoardPose(targetCamera.transform, _pointerLocalPosition)
                : UpgradeCardBoardLayout.CalcBoardPose(targetCamera.transform, _distance, _pitchAngle);

            transform.SetPositionAndRotation(boardPose.position, boardPose.rotation);

            var facingRotation = Quaternion.Euler(_cardFacingRotation);

            for (var i = 0; i < upgrades.Count; i++)
            {
                var card = Instantiate(_cardPrefab, transform);
                var localPosition =
                    UpgradeCardBoardLayout.CalcCardLocalPosition(i, upgrades.Count, _columnCount, _cardSpacing);
                var homePose = new Pose(
                    transform.TransformPoint(localPosition),
                    transform.rotation * facingRotation);

                card.Setup(i, upgrades[i], homePose);
                _cards.Add(card);
            }

            return true;
        }

        /// <summary>指定インデックスのカードだけを取り除く（選択済みのカードを消す用）</summary>
        public void RemoveCard(int index)
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Index != index)
                {
                    continue;
                }

                _handInteraction.Release(_cards[i]);
                _pointerInteraction.Release(_cards[i]);
                Destroy(_cards[i].gameObject);
                _cards.RemoveAt(i);
                return;
            }
        }

        /// <summary>すべてのカードを破棄し、掴み状態を初期化する</summary>
        public void Close()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            _handInteraction.Reset();
            _pointerInteraction.Reset();
        }

        /// <summary>指定インデックスのカードに、所持ポイントで買えるかどうかを反映する</summary>
        public void SetPurchasable(int index, bool isPurchasable)
        {
            foreach (var card in _cards)
            {
                if (card.Index == index)
                {
                    card.SetPurchasable(isPurchasable);
                    return;
                }
            }
        }

        /// <summary>指定インデックスのカードに、ローカライズ済みの文言を反映する（購入済みで消えたカードは無視）</summary>
        public void SetText(int index, in UpgradeLocalizedText text)
        {
            foreach (var card in _cards)
            {
                if (card.Index == index)
                {
                    card.SetText(text);
                    return;
                }
            }
        }

        /// <summary>片手ぶんの入力を受け取る。判定・移動は LateUpdate でまとめて行う</summary>
        public void UpdateHandInput(in ShopHandInput input)
        {
            _handInteraction.SetInput(input);
        }

        /// <summary>非VRのポインタ入力を受け取る。判定は LateUpdate でまとめて行う</summary>
        public void UpdatePointerInput(in ShopPointerInput input)
        {
            _pointerInteraction.SetInput(input);
        }

        private void LateUpdate()
        {
            if (_cards.Count == 0)
            {
                return;
            }

            // Inspector の値は毎フレーム束ねて渡し、Play Mode 中の調整がそのまま効くようにする
            var grabSettings = new UpgradeCardGrabSettings(_directGrabDistance, _rayGrabDistance, _cardLayerMask);

            if (_isPointerMode)
            {
                if (TryGetTargetCamera(out var targetCamera))
                {
                    _pointerInteraction.Update(_finder, targetCamera, grabSettings, _confirmCard);
                }
            }
            else
            {
                var holdSettings = new UpgradeCardHoldSettings(_holdScale, _followSpeed, _wristRollMultiplier);
                _handInteraction.Update(_finder, grabSettings, holdSettings, _confirmCard);
            }

            // 掴まれていないカードは定位置へ戻す
            foreach (var card in _cards)
            {
                if (_handInteraction.IsHeld(card))
                {
                    continue;
                }

                card.MoveToHome(_followSpeed);
            }
        }

        private bool TryGetTargetCamera(out Camera targetCamera)
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            targetCamera = _targetCamera;
            return targetCamera != null;
        }

        private void OnDestroy()
        {
            _onCardConfirmed.Dispose();
        }
    }
}
