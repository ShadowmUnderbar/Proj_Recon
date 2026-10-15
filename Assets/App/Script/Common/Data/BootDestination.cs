namespace App.Common.Data
{
    /// <summary>
    /// 起動シーン（Boot）が初期化を終えたあとに遷移する先。
    /// 製品ビルドは常に MainMenu。エディタでは自動テスト・敵の確認用に Battle から始められる。
    /// </summary>
    public enum BootDestination
    {
        MainMenu,
        Battle,
    }
}
