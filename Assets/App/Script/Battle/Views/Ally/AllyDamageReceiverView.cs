using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// 召喚した味方の被弾判定。敵の近接（Rush）・敵弾（BaseBulletView）は IHitBoxView を探して OnHit を呼ぶため、
    /// 味方のコライダーにこれを載せることで被弾を受け取れる。
    /// コライダーは Player レイヤーに置く（プレイヤーの弾は撃ち手と同じレイヤーをすり抜けるため、味方に当たらない）
    /// </summary>
    public class AllyDamageReceiverView : MonoBehaviour, IHitBoxView
    {
        private readonly Subject<HitData> _onHitObservable = new();
        private readonly Subject<float> _onDamaged = new();

        /// <summary>受けたダメージ量</summary>
        public Observable<float> OnDamaged => _onDamaged;

        public Observable<HitData> OnHitObservable => _onHitObservable;

        public int Id { get; private set; }
        public HitDirectionType ResistanceDirectionType { get; private set; }
        public HitBoxType HitBoxType { get; private set; } = HitBoxType.Ally;

        public void Init(int id, HitBoxType hitBoxType, HitDirectionType resistanceDirectionType)
        {
            Id = id;
            HitBoxType = hitBoxType;
            ResistanceDirectionType = resistanceDirectionType;
        }

        public void OnHit(float damage, int attackerId, Vector3 attackCenter, out bool canPenetrable,
            int penetrationIndex = 1, ShotType? shotType = null, AimFocusType focusType = AimFocusType.NotFocus,
            bool isFocusTarget = false, bool isProjectile = false, int projectileId = 0)
        {
            _onDamaged.OnNext(damage);
            _onHitObservable.OnNext(new HitData(Id, damage, HitDirectionType.None));

            // 敵弾は味方を貫通させず、この地点で止める
            canPenetrable = false;
        }

        private void OnDestroy()
        {
            _onHitObservable.Dispose();
            _onDamaged.Dispose();
        }
    }
}
