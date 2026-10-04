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

        /// <summary>
        /// 粒子をその場で止める。止まっている間は漂い・吸い寄せ・回収のどれも起きず、弾が当たっても吸い込まない。
        /// 吸い込み途中の粒子は止まった位置から再開する。オーバークロック中に呼ばれる
        /// </summary>
        void SetPause(bool isPaused);
    }
}
