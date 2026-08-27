using App.Battle.Data;
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

        /// <summary>今回の回避の到達地点（回避中に接触した敵を運ぶ基準に使う）</summary>
        Vector3 DodgeTargetPosition { get; }

        /// <summary>今回の回避の方向（水平・正規化済み）</summary>
        Vector3 DodgeDirection { get; }

        /// <summary>回避が成立した瞬間に発火する（AfterDodge条件バフの起動に使う）</summary>
        Observable<Unit> OnDodge { get; }

        /// <summary>
        /// 回避中に攻撃を受け、そのダメージを無効化した瞬間に発火する。
        /// 流れる値は無効化しなければ受けていた攻撃の内容（ダメージ量・攻撃者・弾か否か・弾Id）。
        /// 回避時跳ね返し攻撃の接触カウントやジャスト回避演出をここに繋げる。
        /// </summary>
        Observable<PlayerDamagedData> OnDamagedDuringDodge { get; }

        /// <summary>
        /// 回避の移動が終わった瞬間に発火する（回避終了地点と回避方向が流れる）。
        /// 回避時跳ね返し攻撃の起点をここから取る。
        /// </summary>
        Observable<DodgeEndData> OnDodgeEnd { get; }

        void SetCoolDownTime();

        /// <summary>回避の直線移動を開始する（start から target へ DodgeDuration 秒で移動）</summary>
        void StartDodge(Vector3 start, Vector3 target);

        /// <summary>
        /// 回避移動を deltaTime 分進め、現在フレームの座標を返す。
        /// 移動中でない場合は false を返す。
        /// </summary>
        /// <param name="isFinished">このフレームで回避終了地点へ到達したか</param>
        bool TryAdvanceDodge(float deltaTime, out Vector3 position, out bool isFinished);

        /// <summary>回避中の被弾を無効化したことを通知する（OnDamagedDuringDodge を発火）</summary>
        void NotifyDamageBlocked(PlayerDamagedData damagedData);

        /// <summary>
        /// 回避終了を通知する（OnDodgeEnd を発火）。
        /// 到達フレームの接触判定を取りこぼさないよう、呼び出し側が
        /// 座標確定と接触記録を終えた後に呼ぶ。
        /// </summary>
        void NotifyDodgeEnd();
    }
}
