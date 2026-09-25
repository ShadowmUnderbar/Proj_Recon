using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤー死亡演出の調整パラメータ。
    /// HPが0になってからゲームオーバー画面を出すまでの「ヒットストップ → 死亡アニメ → 余韻」を外部化する。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDeathConfig", menuName = "Config/PlayerDeathConfig")]
    public class PlayerDeathConfig : ScriptableObject
    {
        [Header("ヒットストップ")]
        [SerializeField, Tooltip("HPが0になった瞬間に敵・弾を止める秒数（0以下なら止めずに即座に倒れる）")]
        private float _hitStopDuration = 0.2f;

        [Header("死亡アニメーション")]
        [SerializeField, Tooltip("アニメの終了を待つ上限秒数。クリップ差し替えやAnimator未設定で待ち続けないための保険")]
        private float _animationTimeout = 5f;

        [Header("余韻")]
        [SerializeField, Tooltip("アニメ終了からゲームオーバー画面を出すまでの間（秒）。0なら終了直後に出す")]
        private float _postAnimationDelay = 0.5f;

        /// <summary>HPが0になった瞬間に敵・弾を止める秒数</summary>
        public float HitStopDuration => _hitStopDuration;

        /// <summary>死亡アニメの終了を待つ上限秒数</summary>
        public float AnimationTimeout => _animationTimeout;

        /// <summary>アニメ終了からゲームオーバー画面を出すまでの間（秒）</summary>
        public float PostAnimationDelay => _postAnimationDelay;
    }
}
