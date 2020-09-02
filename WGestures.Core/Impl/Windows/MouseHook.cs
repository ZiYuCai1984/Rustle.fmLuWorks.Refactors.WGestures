using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Common.OsSpecific.Windows.Win32;

namespace WGestures.Core.Impl.Windows
{
    internal class MouseKeyboardHook : IDisposable
    {
        public delegate void KeyboardHookEventHandler(KeyboardHookEventArgs e);

        public delegate void MouseHookEventHandler(MouseHookEventArgs e);

        private const int WM_HOOK_TIMEOUT = (int) User32.WM.WM_USER + 1;
        private readonly Native.LowLevelkeyboardHookProc _kbdHookProc;

        private readonly Native.LowLevelMouseHookProc _mouseHookProc;
        private IntPtr _hookId;
        private Thread _hookThread;
        private uint _hookThreadNativeId;
        private IntPtr _kbdHookId;


        public MouseKeyboardHook()
        {
            _mouseHookProc = this.MouseHookProc;
            _kbdHookProc = this.KeyboardHookProc;
        }

        public bool IsDisposed { get; private set; }

        public event MouseHookEventHandler MouseHookEvent;
        public event KeyboardHookEventHandler KeyboardHookEvent;
        public event Func<Native.MSG, bool> GotMessage;

        private void _install()
        {
            _hookId = Native.SetMouseHook(_mouseHookProc);
            _kbdHookId = Native.SetKeyboardHook(_kbdHookProc);

            if (_hookId == IntPtr.Zero || _kbdHookId == IntPtr.Zero)
            {
                throw new Win32Exception("Fail to install mouse hook:" + Native.GetLastError());
            }
        }

        private void _uinstall()
        {
            var hookId = _hookId;
            var kbdHookId = _kbdHookId;
            _hookId = IntPtr.Zero;
            _kbdHookId = IntPtr.Zero;


            if (Native.UnhookWindowsHookEx(hookId) && Native.UnhookWindowsHookEx(kbdHookId))
            {
                Debug.WriteLine("钩子已卸载");
            }
            else
            {
                throw new Win32Exception("Fail to uinstall mouse hook: " + Native.GetLastError());
            }
        }

        public void Install()
        {
            if (_hookThread != null)
            {
                throw new InvalidOperationException("钩子已经安装了");
            }

            _hookThread = new Thread(
                () =>
                {
                    this._install();
                    Debug.WriteLine("钩子安装成功");

                    _hookThreadNativeId = Native.GetCurrentThreadId();

                    try
                    {
                        var @continue = true;
                        do
                        {
                            Native.MSG msg;
                            if (Native.GetMessage(out msg, IntPtr.Zero, 0, 0) <= 0)
                            {
                                break;
                            }

                            switch (msg.message)
                            {
                                case WM_HOOK_TIMEOUT:
                                    Debug.WriteLine("Reinstalling Mouse Hook");
                                    try
                                    {
                                        this._uinstall();
                                    }
                                    catch (Win32Exception e)
                                    {
                                        Debug.WriteLine(e); //ignore
                                    }

                                    this._install();
                                    break;

                                case (uint) User32.WM.WM_CLOSE:
                                    @continue = false;
                                    this._uinstall();
                                    _hookThreadNativeId = 0;
                                    break;
                            }

                            if (this.GotMessage != null)
                            {
                                @continue = this.GotMessage(msg);
                            }
                            else
                            {
                                @continue = true;
                            }
                        } while (@continue);
                    }
                    finally
                    {
                        if (_hookId != IntPtr.Zero)
                        {
                            Native.UnhookWindowsHookEx(_hookId);
                        }

                        if (_kbdHookId != IntPtr.Zero)
                        {
                            Native.UnhookWindowsHookEx(_kbdHookId);
                        }
                    }

                    Debug.WriteLine("钩子线程结束");

                    //GC.KeepAlive(hookProc);
                },
                1)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "MouseHook钩子线程"
            };

            _hookThread.Start();
        }

        public void Uninstall()
        {
            if (_hookId == IntPtr.Zero || _kbdHookId == IntPtr.Zero || _hookThreadNativeId == 0)
            {
                return;
            }

            //发送一个消息给钩子线程,使其GetMessage退出
            if (_hookThread != null && _hookThread.IsAlive)
            {
                Native.PostThreadMessage(
                    _hookThreadNativeId,
                    (uint) User32.WM.WM_CLOSE,
                    UIntPtr.Zero,
                    IntPtr.Zero);

                if (!_hookThread.Join(1000 * 3))
                {
                    throw new TimeoutException("等待钩子线程结束超时");
                }

                _hookThread = null;
            }
        }

        protected virtual IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                Debug.WriteLine("nCode < 0 ??");
                return Native.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            //注意：用这个API来过的鼠标位置，不会出现在迅雷上坐标值变为一半的问题。
            Native.POINT curPos;
            Native.GetCursorPos(out curPos);
            //Debug.WriteLine(wParam);
            var args = new MouseHookEventArgs(
                (MouseMsg) wParam,
                curPos.x,
                curPos.y,
                wParam,
                lParam);

            try
            {
                if (this.MouseHookEvent != null)
                {
                    var timeBefore = DateTime.UtcNow;

                    this.MouseHookEvent(args);

                    var timeElapsed = DateTime.UtcNow - timeBefore;
                    //Debug.WriteLine("MouseHookEvent used time: " + timeElapsed.TotalMilliseconds);

                    //如果用了太长时间，则假定卡住了，重新安装
                    if (timeElapsed.TotalMilliseconds > 1000)
                    {
                        Debug.WriteLine("MouseHookEvent消耗了太多时间，假定hook已失效；重新安装ing...");
                        Native.PostThreadMessage(
                            _hookThreadNativeId,
                            WM_HOOK_TIMEOUT,
                            UIntPtr.Zero,
                            IntPtr.Zero);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("MouseHookEvent中发生了未处理的异常，并且冒泡到了MouseHookProc。这是不应该出现的。" + e);
            }

            return args.Handled
                ? new IntPtr(-1)
                : Native.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        protected virtual int KeyboardHookProc(
            int code, int wParam, ref Native.keyboardHookStruct lParam)
        {
            if (code >= 0 && this.KeyboardHookEvent != null)
            {
                var key = (Keys) lParam.vkCode;
                KeyboardEventType type;

                if (wParam == (int) User32.WM.WM_KEYDOWN || wParam == (int) User32.WM.WM_SYSKEYDOWN)
                {
                    type = KeyboardEventType.KeyDown;
                }
                else if (wParam == (int) User32.WM.WM_KEYUP
                         || wParam == (int) User32.WM.WM_SYSKEYUP)
                {
                    type = KeyboardEventType.KeyUp;
                }
                else
                {
                    return Native.CallNextHookEx(_hookId, code, wParam, ref lParam);
                }

                var args = new KeyboardHookEventArgs(type, key, wParam, lParam);
                this.KeyboardHookEvent(args);

                if (args.Handled)
                {
                    return 1;
                }
            }

            return Native.CallNextHookEx(_hookId, code, wParam, ref lParam);
        }

        public class MouseHookEventArgs : EventArgs
        {
            public IntPtr lParam;

            public IntPtr wParam;

            public MouseHookEventArgs(MouseMsg msg, int x, int y, IntPtr wParam, IntPtr lParam)
            {
                this.Msg = msg;
                this.X = x;
                this.Y = y;

                this.wParam = wParam;
                this.lParam = lParam;
            }

            public MouseMsg Msg { get; }
            public int X { get; }
            public int Y { get; }

            public Point Pos => new Point
                {X = this.X, Y = this.Y};

            public bool Handled { get; set; }
        }

        public class KeyboardHookEventArgs : EventArgs
        {
            public bool Handled;
            public Keys key;
            public Native.keyboardHookStruct lParam;
            public KeyboardEventType Type;
            public int wParam;

            public KeyboardHookEventArgs(
                KeyboardEventType type, Keys key, int wParam, Native.keyboardHookStruct lParam)
            {
                Type = type;
                this.wParam = wParam;
                this.lParam = lParam;
                this.key = key;
            }
        }

        #region dispose

        //If the method is invoked from the finalizer (disposing is false), 
        //other objects should not be accessed. 
        //The reason is that objects are finalized in an unpredictable order and so they,
        //or any of their dependencies, might already have been finalized.
        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                this.Uninstall();
            }
            else
            {
                this.Uninstall();
            }

            this.IsDisposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MouseKeyboardHook()
        {
            this.Dispose(false);
        }

        #endregion
    }

    public enum MouseMsg
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,

        WM_MOUSEWHEEL = 0x020A,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0X0208,

        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,

        WM_XBUTTONDOWN = 0x020B,
        WM_XBUTTONUP = 0x020C
    }

    public enum KeyboardEventType
    {
        KeyDown,
        KeyUp
    }

    public enum XButtonNumber
    {
        One = 1,
        Two = 2
    }
}
