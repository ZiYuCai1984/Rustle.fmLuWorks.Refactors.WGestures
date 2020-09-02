using WGestures.Core;

namespace WGestures.App.Gui
{
    /// <summary>
    ///     用于注入“作用于”的应用程序
    /// </summary>
    internal interface ITargetAppAware
    {
        AbstractApp TargetApp { set; }
    }
}
