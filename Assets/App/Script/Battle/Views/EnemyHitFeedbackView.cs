using System;
using System.Threading;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 敵の被弾フィードバック演出。
    /// 弾が当たった瞬間、撃たれた方向と逆向きにモデルを一瞬傾け、素早く元へ戻す。
    /// 敵ルートはAI/NavMeshが毎フレーム回転させるため、傾けるのはAIが触らない子モデル(VisualRoot)に限定する。
    /// </summary>
    public class EnemyHitFeedbackView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private EnemyHitFeedbackConfig _config;

        private Quaternion _restLocalRot = Quaternion.identity;
        private CancellationTokenSource _playCts;

        private void Awake()
        {
            if (_visualRoot != null)
            {
                _restLocalRot = _visualRoot.localRotation;
            }
        }

        /// <summary>
        /// 被弾方向(弾→敵の水平ベクトル)へモデルを傾ける。
        /// </summary>
        public void Play(Vector3 worldHitDir)
        {
            if (_visualRoot == null || _visualRoot.parent == null || _config == null)
            {
                return;
            }

            worldHitDir.y = 0f;
            if (worldHitDir.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            // 上端がworldHitDir側へ倒れる傾き軸
            var axisWorld = Vector3.Cross(Vector3.up, worldHitDir.normalized);
            if (axisWorld.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var axisLocal = _visualRoot.parent.InverseTransformDirection(axisWorld.normalized);
            var target = Quaternion.AngleAxis(_config.TiltAngle, axisLocal) * _restLocalRot;

            // 連射時は前の演出を打ち切って滑らかに再生
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            TiltAsync(target, _playCts.Token).Forget();
        }

        private async UniTaskVoid TiltAsync(Quaternion target, CancellationToken ct)
        {
            try
            {
                await TweenLocalRotation(_visualRoot.localRotation, target, _config.RiseDuration, ct);
                await TweenLocalRotation(_visualRoot.localRotation, _restLocalRot, _config.RecoverDuration, ct);
            }
            catch (OperationCanceledException)
            {
                // 後続の演出が走るため、ここでは復帰させない
            }
        }

        private async UniTask TweenLocalRotation(Quaternion from, Quaternion to, float duration, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                _visualRoot.localRotation = to;
                return;
            }

            var time = 0f;
            while (time < duration)
            {
                ct.ThrowIfCancellationRequested();
                var rate = time / duration;
                _visualRoot.localRotation = Quaternion.Slerp(from, to, rate);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                time += Time.deltaTime;
            }

            _visualRoot.localRotation = to;
        }

        private void OnDestroy()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
        }
    }
}
