namespace App.Common.Views
{
    /// <summary>
    /// 注視対象の登録先。<see cref="GazeTargetView"/> が有効な間だけ登録され、Store が順番に判定する。
    /// 引数が View の具象型のため、Interface アセンブリ（Views を参照できない）ではなく Views に置く
    /// </summary>
    public interface IGazeTargetStoreView
    {
        void Register(GazeTargetView target);

        void Unregister(GazeTargetView target);
    }
}
