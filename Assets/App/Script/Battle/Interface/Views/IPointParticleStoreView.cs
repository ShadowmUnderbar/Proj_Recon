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
        /// 「特定タイミングで空間上の物を一括で消す」ための口で、現時点では呼び出し元を繋いでいない
        /// </summary>
        void AllRemove();
    }
}
