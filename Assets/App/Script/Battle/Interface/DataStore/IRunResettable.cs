namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ラン（1回のプレイ）限りの実行時状態を持つDataStore・UseCaseが実装する。
    /// リスタート時に <see cref="ResetRun"/> がまとめて呼ばれ、ラン開始直後と同じ状態へ戻す。
    /// VContainerへの登録（Register / RegisterEntryPoint のいずれも AsImplementedInterfaces）から
    /// 対象を集めるため、実装するだけでよく個別の呼び出しは不要。
    /// 呼ばれる順序は登録順なので、他の実装のリセット順に依存しない初期化を書くこと。
    /// </summary>
    public interface IRunResettable
    {
        /// <summary>ラン限りの状態を初期化する（セーブデータ・マスターデータには触れない）</summary>
        void ResetRun();
    }
}
