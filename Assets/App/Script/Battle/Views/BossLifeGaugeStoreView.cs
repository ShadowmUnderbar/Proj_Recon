using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// ボスごとの体力ゲージを生成・破棄する。ゲージ本体はプレイヤーの足元の半円ゲージ（PlayerLifeGaugeView）を流用し、
    /// バリアの弧は出さない。色と大きさは BossLifeGaugeConfig で決め、位置・割合は UseCase から渡す。
    /// </summary>
    public class BossLifeGaugeStoreView : MonoBehaviour, IBossLifeGaugeStoreView
    {
        [SerializeField, Tooltip("ゲージのプレハブ（プレイヤーのライフゲージと同じもの）")]
        private PlayerLifeGaugeView _gaugePrefab;

        private BossLifeGaugeConfig _config;

        private readonly Dictionary<int, PlayerLifeGaugeView> _gauges = new();

        [Inject]
        public void Construct(BossLifeGaugeConfig config)
        {
            _config = config;
        }

        public void Add(int enemyId)
        {
            if (_gauges.ContainsKey(enemyId))
            {
                return;
            }

            if (_gaugePrefab == null)
            {
                Debug.LogError($"[{nameof(BossLifeGaugeStoreView)}] ゲージのプレハブが未設定です", this);
                return;
            }

            var gauge = Instantiate(_gaugePrefab, transform);
            gauge.name = $"BossLifeGauge_{enemyId}";
            gauge.transform.localScale = _gaugePrefab.transform.localScale * _config.Scale;
            gauge.SetColors(_config.HealthColor, _config.LowHealthColor, _config.TrackColor);
            gauge.SetBarrierVisible(false);
            _gauges.Add(enemyId, gauge);
        }

        public void Remove(int enemyId)
        {
            if (!_gauges.Remove(enemyId, out var gauge))
            {
                return;
            }

            // シーン破棄で先に破棄されていることがある
            if (gauge != null)
            {
                Destroy(gauge.gameObject);
            }
        }

        public void SetPosition(int enemyId, Vector3 position)
        {
            if (_gauges.TryGetValue(enemyId, out var gauge))
            {
                gauge.SetPosition(position);
            }
        }

        public void SetHealthRatio(int enemyId, float ratio)
        {
            if (_gauges.TryGetValue(enemyId, out var gauge))
            {
                gauge.SetHealthRatio(ratio);
            }
        }

        private void OnDestroy()
        {
            _gauges.Clear();
        }
    }
}
