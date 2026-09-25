using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// プレイヤーの足元に床置きする半円のライフゲージ。
    /// 弧の形・色・低HP時の点滅はシェーダ（App/PlayerLifeGauge）が描き、
    /// このViewは表示値を目標値へなめらかに寄せてマテリアルへ渡すだけにする。
    /// 向きはワールドに固定し、プレイヤーが振り向いてもゲージは回らない。
    /// </summary>
    public class PlayerLifeGaugeView : MonoBehaviour, IPlayerLifeGaugeView
    {
        private static readonly int HealthFillId = Shader.PropertyToID("_HealthFill");
        private static readonly int BarrierFillId = Shader.PropertyToID("_BarrierFill");
        private static readonly int BarrierVisibleId = Shader.PropertyToID("_BarrierVisible");

        [SerializeField, Tooltip("ゲージを描くRenderer（App/PlayerLifeGauge のマテリアル）")]
        private Renderer _renderer;

        [SerializeField, Tooltip("足元からの浮かせ量[m]。地面とのZファイティングを防ぐ")]
        private float _heightOffset = 0.02f;

        [SerializeField, Tooltip("ゲージのワールドでの向き[度]。0で半円がワールド-Z側（画面手前）に来る")]
        private float _worldYaw;

        [SerializeField, Tooltip("表示値が目標値へ追いつく速さ[割合/秒]")]
        private float _fillSpeed = 1.5f;

        private MaterialPropertyBlock _propertyBlock;

        private float _targetHealth = 1f;
        private float _displayHealth = 1f;
        private float _targetBarrier;
        private float _displayBarrier;
        private bool _barrierVisible;
        private bool _isDirty = true;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            transform.rotation = Quaternion.Euler(0f, _worldYaw, 0f);
        }

        private void Update()
        {
            // 目標値に追いついている間は何もしない（毎フレームのプロパティ更新を避ける）
            if (!Mathf.Approximately(_displayHealth, _targetHealth))
            {
                _displayHealth = Mathf.MoveTowards(_displayHealth, _targetHealth, _fillSpeed * Time.deltaTime);
                _isDirty = true;
            }

            if (!Mathf.Approximately(_displayBarrier, _targetBarrier))
            {
                _displayBarrier = Mathf.MoveTowards(_displayBarrier, _targetBarrier, _fillSpeed * Time.deltaTime);
                _isDirty = true;
            }

            if (_isDirty)
            {
                ApplyProperties();
            }
        }

        public void SetPosition(Vector3 position)
        {
            // 本体（PlayerMoveView.Move）と同じくXZだけを使い、高さは地面に固定する。
            // 回避で壁に当たると論理位置のYが浮くことがあり、そのまま使うとゲージだけ宙に浮く
            transform.position = new Vector3(position.x, _heightOffset, position.z);
        }

        public void SetHealthRatio(float ratio)
        {
            _targetHealth = Mathf.Clamp01(ratio);
        }

        public void SetBarrierRatio(float ratio)
        {
            _targetBarrier = Mathf.Clamp01(ratio);
        }

        public void SetBarrierVisible(bool visible)
        {
            _barrierVisible = visible;
            _isDirty = true;
        }

        private void ApplyProperties()
        {
            _isDirty = false;

            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(HealthFillId, _displayHealth);
            _propertyBlock.SetFloat(BarrierFillId, _displayBarrier);
            _propertyBlock.SetFloat(BarrierVisibleId, _barrierVisible ? 1f : 0f);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
