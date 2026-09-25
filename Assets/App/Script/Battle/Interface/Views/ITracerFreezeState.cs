namespace App.Battle.Interface
{
    /// <summary>
    /// 即着弾のレイ演出（曳光弾・カウンターのレイ）をまとめて止めるための共有状態。
    /// レイは短命で次々生成されるため、個別に停止を伝える代わりに
    /// 各レイがこの状態を見て自分の進行を止める。
    /// </summary>
    public interface ITracerFreezeState
    {
        /// <summary>レイの進行（保持時間・収縮）を止めているか</summary>
        bool IsFreezing { get; }

        /// <summary>レイの進行を止める／再開する</summary>
        void SetFreezing(bool isFreezing);
    }
}
