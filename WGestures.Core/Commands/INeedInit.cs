namespace WGestures.Core.Commands
{
    internal interface INeedInit
    {
        bool IsInitialized { get; }
        void Init();
    }
}
