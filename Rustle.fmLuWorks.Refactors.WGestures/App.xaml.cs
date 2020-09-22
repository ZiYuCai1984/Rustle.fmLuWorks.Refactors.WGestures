using System.Reflection;
using ReactiveUI;
using Rustle.fmLuWorks.Refactors.WGestures.ViewModels;
using Splat;

namespace Rustle.fmLuWorks.Refactors.WGestures
{
    internal partial class App
    {
        public App()
        {
            var currentMutable = Locator.CurrentMutable;

            currentMutable.InitializeSplat();
            currentMutable.InitializeReactiveUI();
            currentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());


            currentMutable.RegisterLazySingleton(() => new LayoutViewModel(), typeof(LayoutViewModel));


        }
    }
}
