using System.Diagnostics;
using System.Reactive.Disposables;
using ReactiveUI;
using Rustle.fmLuWorks.Refactors.WGestures.ViewModels;
using Splat;

namespace Rustle.fmLuWorks.Refactors.WGestures.Views
{
    [SingleInstanceView]
    internal partial class LayoutView
    {
        public LayoutView()
        {
            this.InitializeComponent();

            this.ViewModel = Locator.Current.GetService<LayoutViewModel>();
            if (this.ViewModel == null)
            {
                Debugger.Break();
            }

            this.WhenActivated(
                disposableRegistration =>
                {
                    this.OneWayBind(
                            this.ViewModel,
                            viewModel => viewModel.Title,
                            view => view.TextBlock.Text)
                        .DisposeWith(disposableRegistration);
                });
        }
    }
}
