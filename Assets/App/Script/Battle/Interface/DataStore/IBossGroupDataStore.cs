using System.Collections.Generic;
using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 複数の個体で構成するボス（ボスグループ）の出現と行動台本の進行を管理する。
    /// 個体への命令の配送は <c>BossGroupUseCase</c> が担う。
    /// </summary>
    public interface IBossGroupDataStore
    {
        /// <summary>メンバーが1体でも残っているボスグループがあるか</summary>
        bool HasAliveGroup { get; }

        /// <summary>
        /// ボスグループを出現させる。各メンバーを敵として登録し、台本の進行を始める。
        /// </summary>
        /// <param name="origin">出現位置の基準。メンバーの位置はこの向きに対するローカルのずれで決まる</param>
        /// <returns>メンバーの敵Id（メンバー番号順）。出現できなかった場合は空</returns>
        IReadOnlyList<int> SpawnGroup(BossGroupConfig config, Pose origin);

        /// <summary>個体の状態を反映する（ボスグループに属さない敵なら何もしない）</summary>
        void UpdateMemberStatus(int enemyId, BossMemberStatus status);

        /// <summary>個体を撃破・消去済みにする（ボスグループに属さない敵なら何もしない）</summary>
        void RemoveMember(int enemyId);

        /// <summary>
        /// 全グループの台本を進め、出す命令を output に追加する。全員いなくなったグループは破棄する。
        /// フリーズ・ウェーブ間ポーズ中は呼ばないこと。
        /// </summary>
        void Tick(float deltaTime, List<BossDirectorCommand> output);
    }
}
