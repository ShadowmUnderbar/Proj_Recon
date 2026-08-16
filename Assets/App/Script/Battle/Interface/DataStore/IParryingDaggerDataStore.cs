using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// パリングダガー（回避中に無効化した敵弾を、撃ってきた相手へ撃ち返す）の実行時状態。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public interface IParryingDaggerDataStore
    {
        /// <summary>パリングダガーを所持しているか</summary>
        bool IsActive { get; }

        /// <summary>
        /// パリィ弾の性能を返す。未所持なら false。
        /// 基礎性能は撃ってきた相手へのフォーカスショット（即着弾）で、
        /// レベルに応じてノーマル／ワルツ／マージのフォーム別強化を引き継ぐ。
        /// </summary>
        bool TryGetParryBulletData(out BulletData bulletData);
    }
}
