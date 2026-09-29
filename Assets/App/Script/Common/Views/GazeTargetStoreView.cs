using System.Collections.Generic;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 有効な <see cref="GazeTargetView"/> を束ね、LateUpdate で決まった数ずつ順番に注視判定する。
    /// 全員を毎フレーム判定しないのは、対象が増えても1フレームの負担を一定に保つため。
    /// 頭の姿勢はメインカメラ（VRではHMD）から取る。LateUpdate ならそのフレームのトラッキング更新後の姿勢になる。
    /// 常駐スコープ（CommonLifetimeScope）に置き、メインメニュー・バトルの双方で使う
    /// </summary>
    public class GazeTargetStoreView : MonoBehaviour, IGazeTargetStoreView
    {
        [SerializeField, Min(1), Tooltip("1フレームに判定する対象の数。対象が増えて反応が遅く感じたら増やす")]
        private int _targetsPerFrame = 1;

        private readonly List<GazeTargetView> _targets = new();

        /// <summary>次に判定する対象の位置</summary>
        private int _nextIndex;

        private Camera _headCamera;

        public void Register(GazeTargetView target)
        {
            if (_targets.Contains(target))
            {
                return;
            }

            _targets.Add(target);
        }

        public void Unregister(GazeTargetView target)
        {
            var index = _targets.IndexOf(target);
            if (index < 0)
            {
                return;
            }

            _targets.RemoveAt(index);

            // 手前が抜けたぶん詰めて、順番を飛ばさないようにする
            if (index < _nextIndex)
            {
                _nextIndex--;
            }
        }

        private void LateUpdate()
        {
            if (_targets.Count == 0)
            {
                return;
            }

            if (!TryGetHeadPose(out var headPose))
            {
                return;
            }

            var time = Time.unscaledTime;
            var count = Mathf.Min(_targetsPerFrame, _targets.Count);
            for (var i = 0; i < count; i++)
            {
                // 判定結果の購読者が対象を無効化すると、ループ中に登録が外れて数が減ることがある
                if (_targets.Count == 0)
                {
                    return;
                }

                if (_nextIndex >= _targets.Count)
                {
                    _nextIndex = 0;
                }

                // 判定より先に進めておく。判定中に自分が外れても Unregister の詰め直しで次を飛ばさない
                var target = _targets[_nextIndex];
                _nextIndex++;
                target.Evaluate(headPose, time);
            }
        }

        private bool TryGetHeadPose(out Pose pose)
        {
            // 常駐するためシーン切り替えでカメラが破棄される。破棄されたら取り直す
            if (_headCamera == null)
            {
                _headCamera = Camera.main;
            }

            if (_headCamera == null)
            {
                pose = default;
                return false;
            }

            var cameraTransform = _headCamera.transform;
            pose = new Pose(cameraTransform.position, cameraTransform.rotation);
            return true;
        }
    }
}
