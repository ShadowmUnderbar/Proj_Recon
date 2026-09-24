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

        [SerializeField, Tooltip("取得判定のコライダー（見た目より大きめに広げる）")]
        private SphereCollider _hitCollider;

        private PointParticleConfig _config;

        // 漂いの中心。吸い寄せが始まるまではこの位置を基準に上下・水平へ揺らす
        private Vector3 _driftCenter;

        // 粒子ごとに揺れをずらすための位相と水平方向
        private float _bobPhase;
        private Vector3 _driftDirection;

        // 吸い寄せ中の速度（近づくほど加速させる）
        private float _magnetSpeed;

        // 取得判定の半径（水平距離で判定する）。見た目の大きさと最小判定サイズの大きい方
        private float _collectRadius;

        // 弾が当たってプレイヤーへ吸い込まれている最中の経過秒数と、吸い込み開始地点
        private float _pullElapsed;
        private Vector3 _pullStartPosition;

        // 色をインスタンス化せずに差し替えるためのブロック（粒子ごとにマテリアルを増やさない）
        private static MaterialPropertyBlock _propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // プレハブのスフィアコライダーの既定半径（スケール1のときのワールド半径）
        private const float DefaultColliderRadius = 0.5f;

        // 吸い込み時間の下限（0以下を設定されても即座に消えないようにする）
        private const float MinPullDuration = 0.01f;

        public bool IsCollected { get; private set; }

        /// <summary>弾が当たってプレイヤーへ吸い込まれている最中か</summary>
        public bool IsPulling { get; private set; }

        /// <summary>この粒子を回収したときに得られるポイント</summary>
        public int Value { get; private set; }

        public void Init(PointUnitData unit, Vector3 driftCenter, PointParticleConfig config)
        {
            _config = config;
            Value = unit.Value;
            _driftCenter = driftCenter;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);

            var driftAngle = Random.Range(0f, Mathf.PI * 2f);
            _driftDirection = new Vector3(Mathf.Cos(driftAngle), 0f, Mathf.Sin(driftAngle));

            transform.localScale = Vector3.one * unit.Scale;

            ApplyHitSize(unit.Scale);
            ApplyColor(unit.Color);
        }

        /// <summary>
        /// 取得判定を見た目より大きく広げる。小さい単位の粒子でも取りこぼさないようにするため、
        /// 弾の通過判定（コライダー）とプレイヤーの接触判定に同じ最小サイズを効かせる
        /// </summary>
        private void ApplyHitSize(float scale)
        {
            // 見た目の半径（スケール適用後のワールド半径）と、最小判定サイズの半径の大きい方
            var visualRadius = DefaultColliderRadius * scale;
            _collectRadius = Mathf.Max(visualRadius, _config.MinHitSize * 0.5f);

            if (_hitCollider == null)
            {
                return;
            }

            // コライダーはスケールの影響を受けるため、ワールド半径が _collectRadius になるよう割り戻す
            _hitCollider.radius = scale > 0f ? _collectRadius / scale : DefaultColliderRadius;
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

            // 弾で撃たれた粒子は、近づいたとき同様プレイヤーへ吸い込まれてから回収される
            if (IsPulling)
            {
                PullToPlayer(deltaTime, playerCenter);
                return;
            }

            var toPlayer = playerCenter - transform.position;

            // 取得判定は高さを無視する（粒子はプレイヤーの高さへ寄っていく途中でも取れるようにする）
            var horizontalDistance = new Vector2(toPlayer.x, toPlayer.z).sqrMagnitude;
            var collectDistance = Mathf.Max(_config.CollectDistance, _collectRadius);

            if (horizontalDistance <= collectDistance * collectDistance)
            {
                Collect();
                return;
            }

            if (toPlayer.sqrMagnitude <= _config.MagnetDistance * _config.MagnetDistance)
            {
                MoveToPlayer(deltaTime, toPlayer);
                return;
            }

            Drift(deltaTime, playerCenter.y);
        }

        public void Collect()
        {
            IsCollected = true;
        }

        /// <summary>
        /// 弾が当たったときの吸い込みを開始する。回収は吸い込みが終わってから
        /// </summary>
        public void StartPull()
        {
            if (IsCollected || IsPulling)
            {
                return;
            }

            IsPulling = true;
            _pullElapsed = 0f;
            _pullStartPosition = transform.position;
        }

        /// <summary>
        /// 吸い込みの更新。撃った距離に関わらず BulletPullDuration 秒で必ず回収されるよう、
        /// 開始地点からプレイヤーまでを時間で補間する（プレイヤーが動いても追従する）
        /// </summary>
        private void PullToPlayer(float deltaTime, Vector3 playerCenter)
        {
            var duration = Mathf.Max(_config.BulletPullDuration, MinPullDuration);
            _pullElapsed += deltaTime;

            var progress = Mathf.Clamp01(_pullElapsed / duration);

            // 近づくほど速くなる見た目にする（近接の吸い寄せと同じ加速感）
            transform.position = Vector3.Lerp(_pullStartPosition, playerCenter, progress * progress);

            if (progress >= 1f)
            {
                Collect();
            }
        }

        private void MoveToPlayer(float deltaTime, Vector3 toPlayer)
        {
            _magnetSpeed = Mathf.Min(_magnetSpeed + _config.MagnetAcceleration * deltaTime, _config.MagnetSpeed);
            transform.position += toPlayer.normalized * (_magnetSpeed * deltaTime);

            // プレイヤーが離れて漂いへ戻ったときに元の位置へ瞬間移動しないよう、漂いの中心も連れてくる
            _driftCenter = transform.position;
        }

        private void Drift(float deltaTime, float targetHeight)
        {
            // 吸い寄せ圏外へ戻った場合に備えて速度を戻す（再度ゆっくり加速し直す）
            _magnetSpeed = 0f;

            _driftCenter += _driftDirection * (_config.DriftSpeed * deltaTime);

            // 撃破地点の高さからプレイヤーの高さへ徐々に移動する（弾を通して取りやすい高さへ揃える）
            _driftCenter.y = Mathf.MoveTowards(_driftCenter.y, targetHeight, _config.HeightFollowSpeed * deltaTime);

            var bobOffset = Mathf.Sin((Time.time + _bobPhase) * _config.BobFrequency * Mathf.PI * 2f)
                            * _config.BobAmplitude;

            var position = _driftCenter;
            position.y = _driftCenter.y + bobOffset;
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
