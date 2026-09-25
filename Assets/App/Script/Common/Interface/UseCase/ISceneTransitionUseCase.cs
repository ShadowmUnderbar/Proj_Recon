namespace App.Common.Interface
{
    /// <summary>
    /// シーン遷移。遷移先はメソッドで表し、シーン名を呼び出し側へ漏らさない。
    /// </summary>
    public interface ISceneTransitionUseCase
    {
        /// <summary>遷移中かどうか。UIの二重操作を防ぎたい側が参照する</summary>
        bool IsTransitioning { get; }

        /// <summary>メインメニューシーンへ遷移する。遷移を開始できたらtrue</summary>
        bool LoadMainMenu();

        /// <summary>バトルシーンへ遷移する。遷移を開始できたらtrue</summary>
        bool LoadBattle();
    }
}
