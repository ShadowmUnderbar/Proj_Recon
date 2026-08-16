using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerDodgeParameterDataStore
    {
        public ReactiveProperty<float> DodgeCount { get; }
        public ReactiveProperty<float> MaxDodgeCount { get; }
        float DodgeDamage { get; }
        bool CanDodge { get; }
        float DodgeRange { get; }

        /// <summary>回避の移動にかける時間（秒）</summary>
        float DodgeDuration { get; }

        /// <summary>回避で移動している最中かどうか（trueの間は被弾を無効化する）</summary>
        ReadOnlyReactiveProperty<bool> IsDodging { get; }

        /// <summary>回避が成立した瞬間に発火する（AfterDodge条件バフの起動に使う）</summary>
        Observable<Unit> OnDodge { get; }

        /// <summary>
        /// 回避中に攻撃を受け、そのダメージを無効化した瞬間に発火する。
        /// 流れる値は「無効化しなければ受けていたダメージ量」。
        /// ジャスト回避演出やカウンター等をここに繋げる。
        /// </summary>
        Observable<float> OnDamagedDuringDodge { get; }

        void SetCoolDownTime();

        /// <summary>回避の直線移動を開始する（start から target へ DodgeDuration 秒で移動）</summary>
        void StartDodge(Vector3 start, Vector3 target);

        /// <summary>
        /// 回避移動を deltaTime 分進め、現在フレームの座標を返す。
        /// 移動中でない場合は false を返す。
        /// </summary>
        bool TryAdvanceDodge(float deltaTime, out Vector3 position);

        /// <summary>回避中の被弾を無効化したことを通知する（OnDamagedDuringDodge を発火）</summary>
        void NotifyDamageBlocked(float damage);
    }
}
