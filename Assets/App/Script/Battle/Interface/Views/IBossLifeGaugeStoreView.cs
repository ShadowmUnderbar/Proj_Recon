using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>
    /// ボスの足元に出す体力ゲージ（プレイヤーのライフゲージと同じ半円）を、ボスごとに出し入れする。
    /// </summary>
    public interface IBossLifeGaugeStoreView
    {
        /// <summary>指定の敵のゲージを出す（出していれば何もしない）</summary>
        void Add(int enemyId);

        /// <summary>指定の敵のゲージを消す</summary>
        void Remove(int enemyId);

        /// <summary>ゲージを置く足元のワールド座標を設定する</summary>
        void SetPosition(int enemyId, Vector3 position);

        /// <summary>体力の割合（0〜1）を設定する</summary>
        void SetHealthRatio(int enemyId, float ratio);
    }
}
