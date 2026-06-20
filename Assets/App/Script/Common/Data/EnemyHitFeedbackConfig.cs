using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// 敵の被弾フィードバック（傾き演出）パラメータ。
    /// 単一アセットを全敵プレハブの EnemyHitFeedbackView から参照し、値を一元管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyHitFeedbackConfig", menuName = "Config/EnemyHitFeedbackConfig")]
    public class EnemyHitFeedbackConfig : ScriptableObject
    {
        [Header("傾き演出パラメータ")]
        [SerializeField, Tooltip("傾ける角度（度）")]
        private float _tiltAngle = 30f;

        [SerializeField, Tooltip("傾くまでの時間（秒）")]
        private float _riseDuration = 0.05f;

        [SerializeField, Tooltip("元へ戻るまでの時間（秒）")]
        private float _recoverDuration = 0.12f;

        public float TiltAngle => _tiltAngle;
        public float RiseDuration => _riseDuration;
        public float RecoverDuration => _recoverDuration;
    }
}
