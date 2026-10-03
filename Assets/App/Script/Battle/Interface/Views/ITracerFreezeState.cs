namespace App.Battle.Interface
{
    /// <summary>
    /// 即着弾のレイ演出（曳光弾・カウンターのレイ）をまとめて止めるための共有状態。
    /// レイは短命で次々生成されるため、個別に停止を伝える代わりに
    /// 各レイがこの状態を見て自分の進行を止める。
    /// フリーズとオーバークロックは別々に立て、どちらかが立っていればレイを止める
    /// （片方の解除でもう片方の停止を解かないため）。
    /// </summary>
    public interface ITracerFreezeState
    {
        /// <summary>レイの進行（保持時間・収縮）を止めているか（フリーズ中またはオーバークロック中）</summary>
        bool IsFreezing { get; }

        /// <summary>オーバークロック中か（発動中に生まれた弾が軌跡を残すかの判定に使う）</summary>
        bool IsOverclock { get; }

        /// <summary>フリーズによる停止を立てる／下ろす</summary>
        void SetFreezing(bool isFreezing);

        /// <summary>オーバークロックによる停止を立てる／下ろす</summary>
        void SetOverclock(bool isOverclock);
    }
}
