using UnityEngine;

namespace App.Battle.Interface
{
    /// <summary>ボスの体力ゲージの表示</summary>
    public interface IBossLifeGaugePresenter
    {
        void Add(int enemyId);
        void Remove(int enemyId);
        void SetPosition(int enemyId, Vector3 position);
        void SetHealthRatio(int enemyId, float ratio);
    }
}
