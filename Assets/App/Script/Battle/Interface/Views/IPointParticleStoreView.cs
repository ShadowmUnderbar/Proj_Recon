using System.Collections.Generic;
using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPointParticleStoreView
    {
        /// <summary>粒子が回収されたときに、その粒子のポイント値を流す</summary>
        Observable<int> OnCollected { get; }

        /// <summary>撃破地点を中心に、分割済みの単位ぶんの粒子を生成する</summary>
        void Spawn(Vector3 position, IReadOnlyList<PointUnitData> units);

        /// <summary>
        /// 漂っている粒子を全て消す（ポイントは加算しない）。
        /// ウェーブ切り替わり時に弾の一括消去と合わせて呼ばれる
        /// </summary>
        void AllRemove();
    }
}
