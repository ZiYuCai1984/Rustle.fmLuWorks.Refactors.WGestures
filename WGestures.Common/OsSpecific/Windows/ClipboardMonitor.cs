using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WGestures.Common.OsSpecific.Windows
{
    public class ClipboardMonitor : NativeWindow
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int WM_DESTROY = 0x0002;
        private const int WM_CLOSE = 0x0010;

        private bool _listenerAdded;

        public ClipboardMonitor()
        {
            var cp = new CreateParams();

            this.CreateHandle(cp);
        }

        public event Action<ClipbardUpdatedEventArgs> ClipboardUpdated;
        public event Action MonitorRegistered;

        protected virtual void OnMonitorRegistered()
        {
            var handler = this.MonitorRegistered;
            if (handler != null)
            {
                handler();
            }
        }

        protected virtual void OnClipboardUpdated(ClipbardUpdatedEventArgs args)
        {
            var handler = this.ClipboardUpdated;
            if (handler != null)
            {
                handler(args);
            }
        }

        public void StartMonitor()
        {
            var ok = AddClipboardFormatListener(this.Handle);
            if (!_listenerAdded && !ok)
            {
                throw new Exception("未能注册剪贴板监听器");
            }

            _listenerAdded = true;
            this.OnMonitorRegistered();
        }

        public void StopMonitor()
        {
            var ok = RemoveClipboardFormatListener(this.Handle);
            if (_listenerAdded && !ok)
            {
                throw new Exception("未能移除剪贴板监听器");
            }

            _listenerAdded = false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CLIPBOARDUPDATE:
                    var args = new ClipbardUpdatedEventArgs();
                    this.OnClipboardUpdated(args);
                    if (args.Handled)
                    {
                        m.Result = IntPtr.Zero;
                    }

                    return;
                case WM_DESTROY:
#if DEBUG
                    Console.WriteLine("ClipboardMonitor: WM_DESTROY");
#endif
                    this.StopMonitor();
                    break;
                case WM_CLOSE:
#if DEBUG
                    Console.WriteLine("ClipboardMonitor: WM_CLOSE");
#endif
                    this.StopMonitor();
                    break;
            }

            base.WndProc(ref m);
        }

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public class ClipbardUpdatedEventArgs : EventArgs
        {
            public bool Handled { get; set; }
        }
    }
}
