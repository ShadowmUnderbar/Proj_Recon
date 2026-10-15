using App.Common.Data;
using VContainer;
using VContainer.Unity;

namespace App.Boot
{
    /// <summary>
    /// 起動シーン（Boot）のスコープ。親は常駐スコープ（CommonLifetimeScope）。
    /// このスコープが組み上がる時点で常駐スコープも生成され、セーブ読み込み・XR 起動などが始まる。
    /// 初期化の完了を待って次のシーンへ遷移するだけのシーンなので、持ち物は <see cref="BootUseCase"/> のみ。
    ///
    /// 遷移先は通常メインメニュー。エディタで「Battle から再生」（自動テスト・デバッグ対戦）したときだけ Battle になる。
    /// 予約は組み立て時に1回だけ取り出す（バトルから戻ったあとの通常の流れに持ち越さない）。
    /// </summary>
    public class BootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootUseCase>()
                .WithParameter(DebugConfig.ConsumeBootDestination());
        }
    }
}
