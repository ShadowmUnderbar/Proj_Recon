using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 回避時跳ね返し攻撃の実行時状態。
    /// 回避中に接触した敵弾・敵を（同一対象は1回だけ）記録し、
    /// 回避終了時の攻撃対象・ダメージ・押し出し先を算出する。
    /// </summary>
    public interface IDodgeCounterAttackDataStore
    {
        /// <summary>回避中に接触した敵弾の数（同一の弾は1回のみ）</summary>
        int ContactedProjectileCount { get; }

        /// <summary>回避中に接触した敵の数（同一の敵は1回のみ）</summary>
        int ContactedEnemyCount { get; }

        /// <summary>回避中に接触した敵のId（回避終了時に押し出し、必ず攻撃対象に含める）</summary>
        IReadOnlyList<int> ContactedEnemyIds { get; }

        /// <summary>接触した敵弾を記録する（既に記録済みのIdは無視する）</summary>
        void RegisterProjectileContact(int projectileId);

        /// <summary>接触した敵を記録する（既に記録済みのIdは無視する）</summary>
        void RegisterEnemyContact(int enemyId);

        /// <summary>
        /// その敵が「接触」と見なせる距離にいるかを返す。
        /// 爆風は遠方の敵からでも当たるため、被弾を接触として数える前にこれで絞る。
        /// </summary>
        bool IsWithinContactRange(int enemyId, Vector3 playerPosition);

        /// <summary>次の回避に備えて接触記録を破棄する</summary>
        void ResetContacts();

        /// <summary>
        /// 跳ね返し攻撃のダメージを返す。
        /// ノーマル／マージ／ワルツの弾ダメージ（強化込み）の合計に、
        /// 接触数（敵弾＋敵）1件あたりの加算値を足したもの。
        /// </summary>
        float CalcDamage();

        /// <summary>
        /// 跳ね返し攻撃の対象となる敵のIdを返す。
        /// 通常対象は origin を頂点・direction を中心軸とした扇形範囲内の敵で、
        /// 回避中に接触した敵は範囲外でも必ず含める。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetTargetEnemyIds(Vector3 origin, Vector3 direction);

        /// <summary>
        /// レイ演出の太さ（ノーマル弾の当たり判定サイズ）を返す。
        /// </summary>
        float GetTracerWidth();

        /// <summary>
        /// 回避中に接触した敵の押し出し先を返す（回避終了地点から回避方向へ一定距離）。
        /// 複数体を同じ座標へ重ねないよう、index / count に応じて左右へ等間隔にずらす。
        /// </summary>
        /// <param name="index">押し出す敵の連番（0始まり）</param>
        /// <param name="count">同時に押し出す敵の総数</param>
        Vector3 GetPushPosition(Vector3 origin, Vector3 direction, int index, int count);
    }
}
