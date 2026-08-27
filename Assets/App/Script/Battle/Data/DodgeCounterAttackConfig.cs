using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 回避時跳ね返し攻撃の調整パラメータ。
    /// 攻撃範囲・ダメージ加算・押し出し・接触判定をまとめて外部化する。
    /// </summary>
    [CreateAssetMenu(fileName = "DodgeCounterAttackConfig", menuName = "Config/DodgeCounterAttackConfig")]
    public class DodgeCounterAttackConfig : ScriptableObject
    {
        [Header("攻撃範囲")]
        [SerializeField, Tooltip("通常対象の最大射程（m）")]
        private float _attackRange = 15f;

        [SerializeField, Tooltip("発射方向を中心とした左右の許容角度（度）。10なら左右合計20度の扇形になる")]
        private float _attackHalfAngle = 10f;

        [SerializeField, Tooltip("遮蔽判定を行う高さ（m）。回避終了地点・敵Poseは足元基準のため胴体あたりで見通しを見る")]
        private float _sightHeight = 1f;

        [Header("直線判定（SphereCast）")]
        [SerializeField, Tooltip("直線判定の射程（m）")]
        private float _lineAttackDistance = 50f;

        [SerializeField, Tooltip("直線判定の基礎半径（m）。実際の半径はこれに3フォームの当たり判定サイズ合計を足した値")]
        private float _lineAttackBaseRadius = 1f;

        [Header("ダメージ")]
        [SerializeField, Tooltip("接触1件（敵弾・敵の区別なし）あたりのダメージ加算値")]
        private float _damagePerContact = 0.1f;

        [Header("押し出し")]
        [SerializeField, Tooltip("接触した敵を回避終了地点から回避方向へ押し出す距離（m）")]
        private float _pushDistance = 2f;

        [SerializeField, Tooltip("複数の敵を押し出すときに重ならないよう左右へずらす間隔（m）")]
        private float _pushSpacing = 1f;

        [Header("接触判定")]
        [SerializeField, Tooltip("弾以外の被弾を接触と見なすプレイヤーからの距離（m）。遠方から届く爆風を接触扱いしないための上限")]
        private float _contactRange = 3f;

        [Header("演出")]
        [SerializeField, Tooltip("攻撃時に敵・プレイヤー・弾を止める秒数（0以下なら止めない）")]
        private float _freezeDuration = 0.2f;

        [SerializeField, Tooltip("レイ演出の高さ（m）。回避終了地点・敵Poseの足元基準に対して胴体あたりを結ぶ")]
        private float _tracerHeight = 1f;

        public float AttackRange => _attackRange;
        public float AttackHalfAngle => _attackHalfAngle;
        public float SightHeight => _sightHeight;
        public float LineAttackDistance => _lineAttackDistance;
        public float LineAttackBaseRadius => _lineAttackBaseRadius;
        public float DamagePerContact => _damagePerContact;
        public float PushDistance => _pushDistance;
        public float PushSpacing => _pushSpacing;
        public float ContactRange => _contactRange;
        public float TracerHeight => _tracerHeight;
        public float FreezeDuration => _freezeDuration;
    }
}
