using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// プレイヤーの視点（頭と両手の親。Player 直下の Camera）を、本体の移動に追従させずその場に留める。
    /// HMD の追従（TrackedPoseDriver）は子のローカル姿勢だけを書くので、留めている間も首振りと手の動きは効く。
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public class PlayerCameraPinView : MonoBehaviour
    {
        /// <summary>視点の姿勢を読む他の LateUpdate（注視判定・頭に追従するUI）より先に留め直したいので、既定より前で回す</summary>
        private const int ExecutionOrder = -100;

        private bool _isPinned;

        // 留めた時点のワールド位置
        private Vector3 _pinnedPosition;

        // 留める前の本体からの相対位置（解除時にここへ戻す）
        private Vector3 _defaultLocalPosition;

        public void SetPinned(bool isPinned)
        {
            if (_isPinned == isPinned)
            {
                return;
            }

            _isPinned = isPinned;

            if (isPinned)
            {
                _defaultLocalPosition = transform.localPosition;
                _pinnedPosition = transform.position;
                return;
            }

            transform.localPosition = _defaultLocalPosition;
        }

        private void LateUpdate()
        {
            // 本体の移動は Update 中に反映されるため、描画前の LateUpdate で留めた位置へ戻す
            if (!_isPinned)
            {
                return;
            }

            transform.position = _pinnedPosition;
        }
    }
}
