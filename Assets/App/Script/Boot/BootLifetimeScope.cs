using VContainer;
using VContainer.Unity;

namespace App.Boot
{
    /// <summary>
    /// 起動シーン（Boot）のスコープ。親は常駐スコープ（CommonLifetimeScope）。
    /// このスコープが組み上がる時点で常駐スコープも生成され、セーブ読み込み・XR 起動などが始まる。
    /// 初期化の完了を待ってメインメニューへ遷移するだけのシーンなので、持ち物は <see cref="BootUseCase"/> のみ。
    /// </summary>
    public class BootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootUseCase>();
        }
    }
}
