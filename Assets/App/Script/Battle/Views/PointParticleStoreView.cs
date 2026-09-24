using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// 漂っているポイント粒子の生成・更新・破棄をまとめて受け持つ。
    /// 粒子は数が増えるため、粒子ごとのUpdateを持たせずここから一括で更新する。
    /// </summary>
    public class PointParticleStoreView : MonoBehaviour, IPointParticleStoreView
    {
        [SerializeField] private PointParticleView _pointParticlePrefab;

        private PointParticleConfig _config;
        private IBattlePlayerView _playerView;

        private readonly List<PointParticleView> _particles = new();

        private readonly Subject<int> _onCollected = new();
        public Observable<int> OnCollected => _onCollected;

        [Inject]
        public void Construct(PointParticleConfig config, IBattlePlayerView playerView)
        {
            _config = config;
            _playerView = playerView;
        }

        public void Spawn(Vector3 position, IReadOnlyList<PointUnitData> units)
        {
            if (_pointParticlePrefab == null)
            {
                return;
            }

            for (var i = 0; i < units.Count; i++)
            {
                var offset = Random.insideUnitSphere * _config.SpawnRadius;
                var driftCenter = position + offset;
                // 高さは撃破地点からの相対で決める（段差のある地形でも足元へ埋まらない・浮きすぎない）
                driftCenter.y = position.y + Mathf.Clamp(offset.y, _config.MinHeight, _config.MaxHeight);

                // 生成後に座標を動かすと、同じフレームの物理クエリ（即着弾の弾など）に位置が反映されないため
                // 最初から漂いの中心へ生成する
                var particle = Instantiate(_pointParticlePrefab, driftCenter, Quaternion.identity, transform);
                particle.Init(units[i], driftCenter, position.y, _config);
                _particles.Add(particle);
            }
        }

        public void AllRemove()
        {
            for (var i = 0; i < _particles.Count; i++)
            {
                if (_particles[i] != null)
                {
                    Destroy(_particles[i].gameObject);
                }
            }

            _particles.Clear();
        }

        private void Update()
        {
            if (_particles.Count == 0)
            {
                return;
            }

            var playerCenter = _playerView.PlayerTransform.position + Vector3.up * _config.PlayerCenterHeight;
            var deltaTime = Time.deltaTime;

            // 回収済みの粒子を取り除きながら走査するため末尾から見る
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];

                if (particle == null)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                particle.Tick(deltaTime, playerCenter);

                // 弾の通過による回収もここで拾う（Collect はフラグを立てるだけ）
                if (!particle.IsCollected)
                {
                    continue;
                }

                _onCollected.OnNext(particle.Value);
                Destroy(particle.gameObject);
                _particles.RemoveAt(i);
            }
        }

        private void OnDestroy()
        {
            _onCollected.Dispose();
        }
    }
}
