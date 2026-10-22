using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>床崩壊の再生状態</summary>
    public enum FloorCollapsePhase
    {
        /// <summary>再生していない（床は元のまま）</summary>
        Idle = 0,

        /// <summary>崩壊中</summary>
        Collapsing = 1,

        /// <summary>崩壊しきって暗転した。次の演出（落下シーケンス）へ渡す待ち。床は戻していない</summary>
        Completed = 2,
    }

    /// <summary>
    /// ステージ遷移用の床崩壊の進行。見た目の変形だけを表し、コライダーや実際の床には触れない。
    /// 開始・進行・完了・初期状態への復帰を持ち、何度でも再生し直せる
    /// </summary>
    public interface IFloorCollapseDataStore
    {
        ReadOnlyReactiveProperty<FloorCollapsePhase> Phase { get; }

        /// <summary>全体の進行度（0〜1）。1で暗転しきる</summary>
        ReadOnlyReactiveProperty<float> Progress { get; }

        /// <summary>崩壊の中心（ワールド）</summary>
        Vector3 Origin { get; }

        /// <summary>今回の再生の見た目と動き</summary>
        FloorCollapseSettings Settings { get; }

        /// <summary>崩壊しきった瞬間に流れる。落下シーケンスなど次の演出はここから始める</summary>
        Observable<Unit> OnCompleted { get; }

        /// <summary><paramref name="origin"/> を中心に崩壊を始める。再生中に呼ぶと最初からやり直す</summary>
        void Begin(Vector3 origin, FloorCollapseSettings settings);

        /// <summary>時間を進める。崩壊中でなければ何もしない</summary>
        void Advance(float deltaTime);

        /// <summary>進行度を直接指定する（演出の確認やタイムライン制御用）。1にすると完了する</summary>
        void SetProgress(float progress);

        /// <summary>初期状態（床が元のまま）へ戻す</summary>
        void ResetCollapse();
    }
}
