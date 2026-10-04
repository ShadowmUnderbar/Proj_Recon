using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ボスの帯状の攻撃（発狂フェイズ）の設定。ボスからプレイヤーの方向へ伸びる帯を予兆として出し、
    /// 予兆が明けた瞬間に帯の中にいるプレイヤーへダメージを与える。
    /// </summary>
    [CreateAssetMenu(fileName = "BossLineStrikeConfig", menuName = "Config/BossLineStrikeConfig")]
    public class BossLineStrikeConfig : ScriptableObject
    {
        [Header("範囲")]
        [SerializeField, Min(0.1f), Tooltip("帯の幅（m）")]
        private float _width = 4f;

        [SerializeField, Min(1f), Tooltip("帯の長さ（m）。ボスの位置から伸びる。プレイヤーの向こう側まで届く長さにする")]
        private float _length = 40f;

        [Header("時間")]
        [SerializeField, Min(0f), Tooltip("予兆を出している秒数（この間に避ける）")]
        private float _telegraphSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("攻撃の表示を残す秒数（判定は攻撃の瞬間の1回だけ）")]
        private float _strikeSeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("攻撃後の硬直の秒数")]
        private float _recoverySeconds = 0.5f;

        [Header("連続攻撃（×字の配置で使う行動）")]
        [SerializeField, Min(1), Tooltip("予兆1回のあと続けて当てる回数")]
        private int _repeatCount = 3;

        [SerializeField, Min(0.02f), Tooltip("連続攻撃の間隔（秒）。2回目以降は予兆なしでこの間隔で当てる")]
        private float _repeatInterval = 0.25f;

        [Header("ダメージ")]
        [SerializeField, Min(0f), Tooltip("ボスの攻撃力（ウェーブ強化後）に掛ける倍率")]
        private float _damageMultiplier = 1f;

        [Header("見た目")]
        [SerializeField, Tooltip("予兆の色")]
        private Color _telegraphColor = new(1f, 0.1f, 0.05f, 0.35f);

        [SerializeField, Tooltip("攻撃の瞬間の色")]
        private Color _strikeColor = new(1f, 0.15f, 0.05f, 0.9f);

        public float Width => _width;
        public float Length => _length;
        public float TelegraphSeconds => _telegraphSeconds;
        public float StrikeSeconds => _strikeSeconds;
        public float RecoverySeconds => _recoverySeconds;
        public float DamageMultiplier => _damageMultiplier;
        public int RepeatCount => _repeatCount;
        public float RepeatInterval => _repeatInterval;
        public Color TelegraphColor => _telegraphColor;
        public Color StrikeColor => _strikeColor;
    }
}
