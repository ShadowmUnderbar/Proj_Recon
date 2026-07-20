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
    }
}
