using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.Views.Afterimage
{
    /// <summary>
    /// 残像の生成・寿命・描画を回す。残像は上限数ぶんだけ作って使い回し、焼き込み用のメッシュも作り直さない。
    /// 形状の取得は、アニメーションと IK が反映された後の LateUpdate で行う（Update 中に頼まれても同じフレームの最終ポーズを写す）。
    /// </summary>
    public class AfterimageStoreView : MonoBehaviour, IAfterimageStoreView
    {
        [SerializeField, Tooltip("水平線カーブで沈む分のカリング余裕を読む設定")]
        private CurvedWorldConfig _curvedWorldConfig;

        private AfterimageConfig _config;
        private AfterimageDrawer _drawer;

        private readonly List<AfterimageInstance> _instances = new();
        private readonly List<Renderer> _rendererBuffer = new();

        // 同じフレームに頼まれた分はまとめて1体にする（同じポーズを重ねて描いても見えない）
        private IAfterimageSource _pendingSource;
        private AfterimageStyle _pendingStyle;

        private uint _nextSeed;

        [Inject]
        public void Construct(AfterimageConfig config)
        {
            _config = config;
            if (config.Material == null)
            {
                Debug.LogWarning("[AfterimageStoreView] AfterimageConfig にマテリアルが設定されていないため、残像は描かれません");
            }

            var margin = _curvedWorldConfig != null ? _curvedWorldConfig.CullingDropMargin : 0f;
            _drawer = new AfterimageDrawer(config.Material, gameObject.layer, margin);
        }

        public void Spawn(IAfterimageSource source, AfterimageStyle style)
        {
            _pendingSource = source;
            _pendingStyle = style;
        }

        private void LateUpdate()
        {
            if (_config == null)
            {
                return;
            }

            CapturePending();

            var deltaTime = Time.deltaTime;
            foreach (var instance in _instances)
            {
                if (!instance.Tick(deltaTime))
                {
                    continue;
                }

                var lifeRatio = instance.LifeRatio;
                var style = instance.Style;
                var opacity = style.Opacity * _config.EvaluateOpacity(lifeRatio);
                var dissolve = Mathf.Lerp(style.Dissolve, 1f, _config.EvaluateDissolve(lifeRatio));
                _drawer.Draw(instance, opacity, dissolve);
            }
        }

        private void CapturePending()
        {
            if (_pendingSource == null)
            {
                return;
            }

            var source = _pendingSource;
            _pendingSource = null;

            _rendererBuffer.Clear();
            source.CollectRenderers(_rendererBuffer);
            if (_rendererBuffer.Count == 0)
            {
                return;
            }

            var instance = Rent();
            instance.Snapshot.Capture(_rendererBuffer);
            instance.Begin(_pendingStyle, _config.Lifetime, _nextSeed++);
            _rendererBuffer.Clear();
        }

        /// <summary>
        /// 空いている残像を返す。表示中の数が上限に達していれば一番古いものを消して使い回す。
        /// 実行中に上限を下げた場合も、表示中の数で判定するので新しい上限を超えて増えない
        /// </summary>
        private AfterimageInstance Rent()
        {
            var maxCount = Mathf.Max(1, _config.MaxCount);
            AfterimageInstance oldest = null;
            AfterimageInstance free = null;
            var aliveCount = 0;
            foreach (var instance in _instances)
            {
                if (!instance.IsAlive)
                {
                    free ??= instance;
                    continue;
                }

                aliveCount++;
                if (oldest == null || instance.Age > oldest.Age)
                {
                    oldest = instance;
                }
            }

            if (aliveCount >= maxCount)
            {
                return oldest;
            }

            if (free != null)
            {
                return free;
            }

            var created = new AfterimageInstance();
            _instances.Add(created);
            return created;
        }

        private void OnDestroy()
        {
            foreach (var instance in _instances)
            {
                instance.Dispose();
            }

            _instances.Clear();
        }
    }
}
