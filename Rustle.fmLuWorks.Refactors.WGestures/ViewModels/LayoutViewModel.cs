using ReactiveUI;

namespace Rustle.fmLuWorks.Refactors.WGestures.ViewModels
{
    internal class LayoutViewModel : ReactiveObject
    {
        //TODO-fmLu Temp title
        private string? _title = "MainWindowViewModel";

        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
    }
}
