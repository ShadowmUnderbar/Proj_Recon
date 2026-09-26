using System;
using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 非VR（PC/エディタ）のマウスでカードを選ぶ操作。
    /// カードは掴まず、狙っているカードを強調表示し、押した瞬間にそのカードを確定する。
    /// クリックの押下エッジを取るために前フレームの状態も保持する
    /// </summary>
    public class UpgradeCardPointerInteraction
    {
        private Vector2 _screenPosition;
        private bool _isPressed;
        private bool _previousPressed;
        private bool _hasInput;
        private UpgradeCardView _hoveredCard;

        /// <summary>
        /// 入力を差し替える。押下エッジの基準になる前フレームの状態もここで進める。
        ///
        /// 最初の1回だけは今の状態をそのまま前フレーム扱いにする。
        /// 非VRでは左クリックが射撃と確定を兼ねているため、撃ちっぱなしでウェーブが終わると
        /// ショップが開いた最初のフレームが押下エッジに見えて、カーソル下のカードを勝手に買ってしまう。
        /// 判定を行う <see cref="Update"/> 側で控えると、途中で抜けたフレームのぶん取りこぼす
        /// </summary>
        public void SetInput(in ShopPointerInput input)
        {
            _previousPressed = _hasInput ? _isPressed : input.IsPressed;
            _screenPosition = input.ScreenPosition;
            _isPressed = input.IsPressed;
            _hasInput = true;
        }

        /// <summary>破棄されるカードをホバー対象から外す</summary>
        public void Release(UpgradeCardView card)
        {
            if (_hoveredCard == card)
            {
                _hoveredCard = null;
            }
        }

        public void Reset()
        {
            _screenPosition = Vector2.zero;
            _isPressed = false;
            _previousPressed = false;
            _hasInput = false;
            _hoveredCard = null;
        }

        /// <summary>狙っているカードの強調表示と、クリックによる確定を処理する</summary>
        /// <param name="finder">カードの検索</param>
        /// <param name="targetCamera">スクリーン座標をレイに変換するカメラ</param>
        /// <param name="grabSettings">カードを探す判定範囲</param>
        /// <param name="onCardConfirmed">カードをクリックで確定したときに、その候補インデックスで呼ばれる</param>
        public void Update(
            UpgradeCardFinder finder,
            Camera targetCamera,
            in UpgradeCardGrabSettings grabSettings,
            Action<int> onCardConfirmed)
        {
            if (!_hasInput)
            {
                return;
            }

            // Canvasのボタン（次のウェーブへ）を押したクリックで、その裏のカードまで確定しないようにする
            var hovered = finder.IsPointerOverInteractableUi(_screenPosition)
                ? null
                : finder.FindCardByRay(targetCamera.ScreenPointToRay(_screenPosition), grabSettings);

            SetHovered(hovered);

            if (hovered != null && hovered.IsPurchasable && _isPressed && !_previousPressed)
            {
                onCardConfirmed(hovered.Index);
            }
        }

        private void SetHovered(UpgradeCardView card)
        {
            var previous = _hoveredCard;
            _hoveredCard = card;

            if (previous == card)
            {
                return;
            }

            if (previous != null)
            {
                previous.SetHighlight(CardHighlight.None);
            }

            if (card != null)
            {
                card.SetHighlight(CardHighlight.Hovered);
            }
        }
    }
}
