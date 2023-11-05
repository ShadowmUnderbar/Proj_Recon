using VContainer;
using VContainer.Unity;
using App.Common.UseCase;
using App.Common.Interface.UseCase;

namespace App.Common
{
    public class CommonLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<CommonLifetimeScope>(Lifetime.Singleton);


            builder.RegisterEntryPoint<GameInputUsecase>().As<IGameInputUsecase>();
#if OCULUS
            builder.RegisterEntryPoint<OVRInputUseCase>().As<IOVRInputUseCase>();
#endif
        }
    }
}