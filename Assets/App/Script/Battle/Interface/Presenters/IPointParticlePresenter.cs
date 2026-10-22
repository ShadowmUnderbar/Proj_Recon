using System;
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

        /// <summary>漂っている粒子を全て消す（ポイントは加算しない）。ウェーブ切り替わり時に呼ばれる</summary>
        void AllRemove();

        /// <summary>位置が <paramref name="shouldRemove"/> を満たす粒子を、回収扱いにせず消す（床崩壊の波に合わせて消す用）</summary>
        void RemoveWhere(Func<Vector3, bool> shouldRemove);

        /// <summary>粒子をその場で止める（回収・吸い寄せもしない）。オーバークロック中に呼ばれる</summary>
        void SetPause(bool isPaused);
    }
}
