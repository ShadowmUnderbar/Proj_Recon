using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// プレイヤーの被弾判定。敵の近接（Rush）・敵弾（BaseBulletView）はいずれも
    /// IHitBoxView を探して OnHit を呼ぶため、プレイヤーのコライダーにこれを載せることで被弾を受け取れる。
    /// 敵のHitBoxViewと違い HitBoxStoreView には登録せず、受けたダメージ量を OnDamaged として直接公開する。
    /// </summary>
    public class PlayerDamageReceiverView : MonoBehaviour, IHitBoxView
    {
        private readonly Subject<HitData> _onHitObservable = new();
        private readonly Subject<PlayerDamagedData> _onDamaged = new();

        /// <summary>被弾内容を流す（レイジ等の被弾条件バフ・HP減少・回避中のパリィに使う）</summary>
        public Observable<PlayerDamagedData> OnDamaged => _onDamaged;

        public Observable<HitData> OnHitObservable => _onHitObservable;

        public int Id { get; private set; } = BasePlayerParameter.PlayerId;
        public HitDirectionType ResistanceDirectionType { get; private set; }
        public HitBoxType HitBoxType { get; private set; }

        public void Init(int id, HitBoxType hitBoxType, HitDirectionType resistanceDirectionType)
        {
            Id = id;
            HitBoxType = hitBoxType;
            ResistanceDirectionType = resistanceDirectionType;
        }

        public void OnHit(float damage, int attackerId, Vector3 attackCenter, out bool canPenetrable,
            int penetrationIndex = 1, ShotType? shotType = null, AimFocusType focusType = AimFocusType.NotFocus,
            bool isFocusTarget = false, bool isProjectile = false)
        {
            _onDamaged.OnNext(new PlayerDamagedData(damage, attackerId, isProjectile));

            // 演出等での購読余地を残すためHitDataも流す（方向は使わないのでデフォルト）
            _onHitObservable.OnNext(new HitData(Id, damage, HitDirectionType.None));

            // 敵弾はプレイヤーを貫通させず、この地点で止める
            canPenetrable = false;
        }

        private void OnDestroy()
        {
            _onHitObservable.Dispose();
            _onDamaged.Dispose();
        }
    }
}
