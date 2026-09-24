using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 撃破地点付近に漂うポイント粒子。
    /// 毎フレームの更新は PointParticleStoreView からまとめて呼ばれる（粒子ごとのUpdateを持たない）。
    /// </summary>
    public class PointParticleView : MonoBehaviour, IPointParticleView
    {
        [SerializeField] private Renderer _renderer;

        private PointParticleConfig _config;

        // 漂いの中心。吸い寄せが始まるまではこの位置を基準に上下・水平へ揺らす
        private Vector3 _driftCenter;

        // 粒子ごとに揺れをずらすための位相と水平方向
        private float _bobPhase;
        private Vector3 _driftDirection;

        // 吸い寄せ中の速度（近づくほど加速させる）
        private float _magnetSpeed;

        // 漂う高さの基準（撃破地点の足元）。高さ制限を絶対座標で持つと段差のある地形で破綻するため相対で持つ
        private float _groundHeight;

        // 色をインスタンス化せずに差し替えるためのブロック（粒子ごとにマテリアルを増やさない）
        private static MaterialPropertyBlock _propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsCollected { get; private set; }

        /// <summary>この粒子を回収したときに得られるポイント</summary>
        public int Value { get; private set; }

        public void Init(PointUnitData unit, Vector3 driftCenter, float groundHeight, PointParticleConfig config)
        {
            _config = config;
            Value = unit.Value;
            _driftCenter = driftCenter;
            _groundHeight = groundHeight;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);

            var driftAngle = Random.Range(0f, Mathf.PI * 2f);
            _driftDirection = new Vector3(Mathf.Cos(driftAngle), 0f, Mathf.Sin(driftAngle));

            transform.localScale = Vector3.one * unit.Scale;

            ApplyColor(unit.Color);
        }

        /// <summary>
        /// 漂い・吸い寄せ・接触回収の更新。playerCenter はプレイヤーの胴体あたりの座標
        /// </summary>
        public void Tick(float deltaTime, Vector3 playerCenter)
        {
            if (IsCollected)
            {
                return;
            }

            var toPlayer = playerCenter - transform.position;
            var sqrDistance = toPlayer.sqrMagnitude;

            if (sqrDistance <= _config.CollectDistance * _config.CollectDistance)
            {
                Collect();
                return;
            }

            if (sqrDistance <= _config.MagnetDistance * _config.MagnetDistance)
            {
                MoveToPlayer(deltaTime, toPlayer);
                return;
            }

            Drift(deltaTime);
        }

        public void Collect()
        {
            IsCollected = true;
        }

        private void MoveToPlayer(float deltaTime, Vector3 toPlayer)
        {
            _magnetSpeed = Mathf.Min(_magnetSpeed + _config.MagnetAcceleration * deltaTime, _config.MagnetSpeed);
            transform.position += toPlayer.normalized * (_magnetSpeed * deltaTime);

            // プレイヤーが離れて漂いへ戻ったときに元の位置へ瞬間移動しないよう、漂いの中心も連れてくる
            _driftCenter = transform.position;
        }

        private void Drift(float deltaTime)
        {
            // 吸い寄せ圏外へ戻った場合に備えて速度を戻す（再度ゆっくり加速し直す）
            _magnetSpeed = 0f;

            _driftCenter += _driftDirection * (_config.DriftSpeed * deltaTime);

            var bobOffset = Mathf.Sin((Time.time + _bobPhase) * _config.BobFrequency * Mathf.PI * 2f)
                            * _config.BobAmplitude;

            var position = _driftCenter;
            position.y = Mathf.Clamp(
                _driftCenter.y + bobOffset,
                _groundHeight + _config.MinHeight,
                _groundHeight + _config.MaxHeight);
            transform.position = position;
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            // URP（_BaseColor）とBuilt-in（_Color）のどちらでも色が乗るよう両方へ書き込む
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
