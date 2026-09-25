using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤーの基礎パラメータ（体力・移動・射撃フォーム倍率・回避）。
    /// 旧 BasePlayerParameter（静的クラス）を ScriptableObject 化したもので、
    /// BattleLifetimeScope から RegisterInstance され、各 DataStore にコンストラクタ注入される。
    /// プレイヤーID など「バランス調整ではない定数」は PlayerConstants に置く。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseParameterConfig", menuName = "Config/PlayerBaseParameterConfig")]
    public class PlayerBaseParameterConfig : ScriptableObject
    {
        [Header("基礎")]
        [SerializeField, Min(1f), Tooltip("最大体力の基礎値。HP強化はこの値に倍率を掛ける")]
        private float _health = 100f;

        [SerializeField, Min(0f), Tooltip("移動速度（1フレームあたりの移動量）。スティックの倒し量にこの値を掛ける")]
        private float _moveSpeed = 0.05f;

        [SerializeField, Min(0f), Tooltip("弾ダメージの基礎値")]
        private float _baseDamage = 4f;

        [SerializeField, Min(0.01f), Tooltip("射撃クールダウンの基礎値（秒）。各種倍率はこの値に乗算する。0にすると毎フレーム発射になる")]
        private float _baseFireRate = 0.3f;

        [SerializeField, Min(0), Tooltip("貫通数の基礎値")]
        private int _basePenetration = 0;

        [Header("フォーカス")]
        [SerializeField, Min(0f), Tooltip("フォーカス時の射撃クールダウン倍率")]
        private float _focusFireRateMagnification = 0.9f;

        [SerializeField, Min(0f), Tooltip("フォーカス時の弾ダメージ倍率")]
        private float _focusDamageMagnification = 3f;

        [Header("ワルツ")]
        // 片手1ストリーム基準で、両手発射のノーマルショット（26.7DPS）の約0.83倍（22.2DPS）になる倍率。
        // 左右で別方向を狙う仕様のため、単体火力は最下位だが左右合計の面制圧力で差別化する
        [SerializeField, Min(0f), Tooltip("ワルツの弾ダメージ倍率")]
        private float _waltzDamageMagnification = 0.5f;

        [SerializeField, Min(0f), Tooltip("ワルツの射撃クールダウン倍率")]
        private float _waltzFireRateMagnification = 0.3f;

        [Header("マージ")]
        // 利き手のみの1ストリーム基準で、弾ダメージ(8.0)＋爆風ダメージ(8.0)の合計16.0により、
        // 両手発射のノーマルショット（26.7DPS）の約1.33倍（35.6DPS）になる。
        // 両手を寄せる必要がありアキンボを捨てる代償として、単体火力を最上位に置く
        [SerializeField, Min(0f), Tooltip("マージの弾ダメージ倍率")]
        private float _mergeDamageMagnification = 2f;

        [SerializeField, Min(0f), Tooltip("マージの射撃クールダウン倍率")]
        private float _mergeFireRateMagnification = 1.5f;

        [SerializeField, Min(0f), Tooltip("マージ弾の飛翔速度（m/s）。他フォームは即着弾")]
        private float _mergeBulletSpeed = 40f;

        [SerializeField, Min(0f), Tooltip("マージ弾の爆風半径の基礎値")]
        private float _mergeExplosiveScale = 2f;

        [SerializeField, Min(0f), Tooltip("爆風ダメージの弾ダメージに対する割合。弾ダメージ側の強化・バフがそのまま爆風にも反映される")]
        private float _mergeExplosiveDamageRate = 1f;

        [Header("回避")]
        [SerializeField, Min(0f), Tooltip("回避の移動距離（m）")]
        private float _dodgeRange = 10f;

        [SerializeField, Min(0f), Tooltip("回避で敵に接触したときのダメージ")]
        private float _dodgeDamage = 10f;

        [SerializeField, Min(1), Tooltip("回避の最大回数")]
        private int _dodgeCount = 2;

        [SerializeField, Min(0f), Tooltip("回避回数が1回復するまでの時間（秒）")]
        private float _dodgeCooldown = 3f;

        [SerializeField, Min(0f), Tooltip("回避の移動にかける時間（秒）。DodgeRange をこの時間で直線移動する。0以下で瞬間移動")]
        private float _dodgeDuration = 0.15f;

        public float Health => _health;
        public float MoveSpeed => _moveSpeed;
        public float BaseDamage => _baseDamage;
        public float BaseFireRate => _baseFireRate;
        public int BasePenetration => _basePenetration;

        public float FocusFireRateMagnification => _focusFireRateMagnification;
        public float FocusDamageMagnification => _focusDamageMagnification;

        public float WaltzDamageMagnification => _waltzDamageMagnification;
        public float WaltzFireRateMagnification => _waltzFireRateMagnification;

        public float MergeDamageMagnification => _mergeDamageMagnification;
        public float MergeFireRateMagnification => _mergeFireRateMagnification;
        public float MergeBulletSpeed => _mergeBulletSpeed;
        public float MergeExplosiveScale => _mergeExplosiveScale;
        public float MergeExplosiveDamageRate => _mergeExplosiveDamageRate;

        public float DodgeRange => _dodgeRange;
        public float DodgeDamage => _dodgeDamage;
        public int DodgeCount => _dodgeCount;
        public float DodgeCooldown => _dodgeCooldown;
        public float DodgeDuration => _dodgeDuration;
    }
}
