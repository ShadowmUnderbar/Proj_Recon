using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ボスの足元の体力ゲージの見た目。ゲージ本体はプレイヤーのライフゲージ（App/PlayerLifeGauge のマテリアル）を流用し、
    /// 色と大きさだけをボスごとのゲージに上書きする（プレイヤーのゲージと見分けるため）。
    /// </summary>
    [CreateAssetMenu(fileName = "BossLifeGaugeConfig", menuName = "Config/BossLifeGaugeConfig")]
    public class BossLifeGaugeConfig : ScriptableObject
    {
        [SerializeField, Tooltip("体力の色")]
        private Color _healthColor = new(0.65f, 0.3f, 1f, 0.9f);

        [SerializeField, Tooltip("体力が少ないときの色（プレイヤーの低HP色と区別できる色にする）")]
        private Color _lowHealthColor = new(1f, 0.35f, 0.8f, 0.95f);

        [SerializeField, Tooltip("空き部分（減った体力）の色")]
        private Color _trackColor = new(0f, 0f, 0f, 0.4f);

        [SerializeField, Min(0.1f), Tooltip("プレイヤー用に対するゲージの大きさの倍率（ボスは体が大きいため広げる）")]
        private float _scale = 1.5f;

        public Color HealthColor => _healthColor;
        public Color LowHealthColor => _lowHealthColor;
        public Color TrackColor => _trackColor;
        public float Scale => _scale;
    }
}
