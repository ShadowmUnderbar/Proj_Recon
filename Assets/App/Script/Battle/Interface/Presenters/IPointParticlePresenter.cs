using System.Collections.Generic;
using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPointParticlePresenter
    {
        /// <summary>粒子が回収されたときに、その粒子のポイント値を流す</summary>
        Observable<int> OnCollected { get; }

        void Spawn(Vector3 position, IReadOnlyList<PointUnitData> units);

        /// <summary>漂っている粒子を全て消す（ポイントは加算しない）</summary>
        void AllRemove();
    }
}
