using System.Collections.Generic;
using R3;
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

        /// <summary>
        /// 回避中に敵弾・敵を1つ以上巻き込んだか。
        /// これが false の回避では跳ね返し攻撃（扇形・直線とも）を発生させない。
        /// </summary>
        bool HasContact { get; }

        /// <summary>接触した敵弾を記録する（既に記録済みのIdは無視する）</summary>
        void RegisterProjectileContact(int projectileId);

        /// <summary>
        /// 回避中に新しく敵へ接触した瞬間に、その敵のIdを流す。
        /// スタン付与と吹き飛ばしの起動に使う（同じ敵では1回しか流れない）。
        /// </summary>
        Observable<int> OnEnemyContacted { get; }

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

        /// <summary>直線判定（SphereCast）の射程（m）</summary>
        float LineAttackDistance { get; }

        /// <summary>
        /// 直線判定（SphereCast）の半径を返す。
        /// 基礎半径にノーマル／マージ／ワルツの当たり判定サイズ合計を足した値。
        /// </summary>
        float GetLineAttackRadius();

        /// <summary>
        /// 直線判定で当たった敵のうち、扇形範囲の対象と重複しないものだけを返す。
        /// 撃破済みの敵と、同じ敵の重複も除外する。
        /// <see cref="GetTargetEnemyIds"/> の後に呼ぶこと。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> GetLineTargetEnemyIds(IReadOnlyList<int> lineHitEnemyIds);

        /// <summary>
        /// 接触した敵のスタン時間（秒）を返す。
        /// 吹き飛ばしの移動時間とフリーズ時間の合計で、回避終了とフリーズ明けまで固まる長さになる。
        /// </summary>
        /// <param name="knockBackDuration">その敵の吹き飛ばしにかける時間（秒）</param>
        float GetContactStunDuration(float knockBackDuration);

        /// <summary>スタン中として時間を管理する敵を登録する（同じ敵は長い方の残り時間を採用）</summary>
        void RegisterStun(int enemyId, float duration);

        /// <summary>
        /// スタンの残り時間を deltaTime 分進め、切れた敵のIdを返す。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<int> UpdateStunTimers(float deltaTime);

        /// <summary>
        /// 直近に接触した敵の吹き飛ばし先を返す。
        /// 回避先（dodgeTargetPosition）からさらに回避方向へ一定距離進んだ地点で、
        /// 複数体が重ならないよう接触順に応じて左右へずらす。
        /// </summary>
        Vector3 GetKnockBackPosition(Vector3 dodgeTargetPosition, Vector3 direction);
    }
}
