namespace App.Common.Interface
{
    /// <summary>
    /// XR の起動。起動シーン（Boot）が、XR の準備が終わってからメインメニューへ進むために参照する。
    /// </summary>
    public interface IXRInitUseCase
    {
        /// <summary>XR の起動処理が終わったか。XR を使わない（エディタの非VRモード）・起動に失敗した場合も true になる</summary>
        bool IsInitialized { get; }
    }
}
