using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ラン開始時のセット選択中かどうかを保持する。
    /// 選択中はゲームを停止（IsWavePause=true）し、選択完了でランを開始する。
    /// </summary>
    public interface IRunStartDataStore
    {
        /// <summary>ラン開始のセット選択中か</summary>
        ReadOnlyReactiveProperty<bool> IsSelecting { get; }

        void SetSelecting(bool isSelecting);

        /// <summary>
        /// 今のランを保存済みのセット（スロット）を装備して始めたか。
        /// メインメニューでの選択・バトル内のセット選択のどちらでも、空でないセットを装備したら true。
        /// 「使わずに開始」やスロットが無いまま始めたときは false
        /// </summary>
        bool HasLoadedBuild { get; }

        void SetLoadedBuild(bool hasLoadedBuild);
    }
}
