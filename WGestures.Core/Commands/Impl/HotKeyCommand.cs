using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using WindowsInput.Native;
using WGestures.Common.Annotation;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Common.OsSpecific.Windows.Win32;

namespace WGestures.Core.Commands.Impl
{
    [Named("执行快捷键")]
    [Serializable]
    public class HotKeyCommand : AbstractCommand, IGestureContextAware
    {
        public HotKeyCommand()
        {
            this.Modifiers = new List<VirtualKeyCode>();
            this.Keys = new List<VirtualKeyCode>();
        }

        public List<VirtualKeyCode> Modifiers { get; set; }

        public List<VirtualKeyCode> Keys { get; set; }


        public GestureContext Context { set; private get; }


        public override void Execute()
        {
            if (this.Keys.Count + this.Modifiers.Count == 0)
            {
                return;
            }

            if (this.Keys.Count == 1
                && this.Keys[0] == VirtualKeyCode.VK_L
                && this.Modifiers.Count == 1
                && (this.Modifiers[0] == VirtualKeyCode.LWIN
                    || this.Modifiers[0] == VirtualKeyCode.RWIN))
            {
                User32.LockWorkStation();
                return;
            }


            //活动进程 未必 是活动root窗口进程, 就像clover
            var fgWindow = Native.GetForegroundWindow();
            var rootWindow = IntPtr.Zero;

            Debug.WriteLine(string.Format("FGWindow: {0:X}", fgWindow.ToInt64()));

            //如果没有前台窗口，或者前台窗口是任务栏，则使用鼠标指针下方的窗口？
            /*var useCursorWindow = false;
            if (fgWindow != IntPtr.Zero)
            {
                var className = new StringBuilder(32);
                Native.GetClassName(fgWindow, className, className.Capacity);

                //如果是任务栏 或者 窗口处于最小化状态
                if (className.ToString() == "Shell_TrayWnd")
                {
                    useCursorWindow = true;
                } //如果活动窗口与鼠标指针不在同一个屏幕
                else if (!IsCursorAndWindowSameScreen(fgWindow))
                {
                    useCursorWindow = true;
                }
                else
                {
                    rootWindow = Native.GetAncestor(fgWindow, Native.GetAncestorFlags.GetRoot);
                    if (IsWindowMinimized(rootWindow))
                    {
                        Debug.WriteLine("Use Cursor Window Cuz rootWindow is Minimized.");
                        useCursorWindow = true;
                    }
                }
            }
            else
            {
                useCursorWindow = true;
            }*/


            //if (useCursorWindow)
            {
                //Debug.WriteLine("* * Why Is fgWindow NULL?");

                if (this.Context != null) //触发角将不会注入此字段
                {
                    fgWindow = Native.WindowFromPoint(
                        new Native.POINT
                            {x = this.Context.StartPoint.X, y = this.Context.StartPoint.Y});
                    Debug.WriteLine(string.Format("WinforFromPoint={0:x}", fgWindow.ToInt64()));
                    if (fgWindow == IntPtr.Zero)
                    {
                        return;
                    }
                }
            }

            if (rootWindow == IntPtr.Zero)
            {
                rootWindow = Native.GetAncestor(fgWindow, Native.GetAncestorFlags.GetRoot);
            }

            //User32.SetForegroundWindow(fgWindow);
            //ForceWindowIntoForeground(fgWindow);
            uint pid;
            var fgThread = Native.GetWindowThreadProcessId(fgWindow, out pid);
            Debug.WriteLine("pid=" + pid);

            //失败可能原因之一：被杀毒软件或系统拦截

            try
            {
                foreach (var k in this.Modifiers)
                {
                    Debug.Write(k);
                    this.PerformKey(pid, fgThread, k);
                }

                foreach (var k in this.Keys)
                {
                    Debug.Write(k);
                    this.PerformKey(pid, fgThread, k);
                }


                foreach (var k in this.Keys)
                {
                    Debug.Write(k + " Up:");

                    this.PerformKey(pid, fgThread, k, true);
                }

                foreach (var k in this.Modifiers)
                {
                    Debug.Write(k + " Up:");

                    this.PerformKey(pid, fgThread, k, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发送按键的时候发生异常： " + ex);
                Native.TryResetKeys(this.Keys, this.Modifiers);
#if TEST
                throw;
#endif
            }

            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        private static bool IsWindowMinimized(IntPtr hwnd)
        {
            var style = User32.GetWindowLong(hwnd, User32.GWL.GWL_STYLE);

            return (int) User32.WS.WS_MINIMIZE == (style & (int) User32.WS.WS_MINIMIZE);
        }

        private void PerformKey(uint pid, uint tid, VirtualKeyCode key, bool isUp = false)
        {
            //Native.WaitForInputIdle(pid, tid, 100);
            Thread.Sleep(10);
            if (!isUp)
            {
                Sim.KeyDown(key);
            }
            else
            {
                Sim.KeyUp(key);
            }

            //Native.WaitForInputIdle(pid, tid, 20);
        }


        private static bool IsCursorAndWindowSameScreen(IntPtr win)
        {
            Native.POINT pt;
            Native.GetCursorPos(out pt);

            var fgWinScreen = Screen.FromHandle(win);
            var cursorScreen = Screen.FromPoint(pt.ToPoint());

            return fgWinScreen.Equals(cursorScreen);
        }

        public override string Description()
        {
            return HotKeyToString(this.Modifiers, this.Keys);
        }

        public static void ForceWindowIntoForeground(IntPtr window)
        {
            const uint LSFW_LOCK = 1;
            const uint LSFW_UNLOCK = 2;
            const int ASFW_ANY = -1; // by MSDN

            var currentThread = Native.GetCurrentThreadId();

            var activeWindow = User32.GetForegroundWindow();
            //uint activeProcess;
            var activeThread = User32.GetWindowThreadProcessId(activeWindow, IntPtr.Zero);

            uint windowProcess;
            var windowThread = User32.GetWindowThreadProcessId(window, IntPtr.Zero);

            if (currentThread != activeThread)
            {
                User32.AttachThreadInput(currentThread, activeThread, true);
            }

            if (windowThread != currentThread)
            {
                User32.AttachThreadInput(windowThread, currentThread, true);
            }

            uint oldTimeout = 0, newTimeout = 0;
            User32.SystemParametersInfo(User32.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref oldTimeout, 0);
            User32.SystemParametersInfo(User32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref newTimeout, 0);
            User32.LockSetForegroundWindow(LSFW_UNLOCK);
            User32.AllowSetForegroundWindow(ASFW_ANY);

            User32.SetForegroundWindow(window);
            User32.ShowWindow(window, User32.SW.SW_RESTORE);
            User32.SetFocus(window);

            User32.SystemParametersInfo(User32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref oldTimeout, 0);

            if (currentThread != activeThread)
            {
                User32.AttachThreadInput(currentThread, activeThread, false);
            }

            if (windowThread != currentThread)
            {
                User32.AttachThreadInput(windowThread, currentThread, false);
            }
        }

        public static string HotKeyToString(
            ICollection<VirtualKeyCode> modifiers, ICollection<VirtualKeyCode> keys)
        {
            if (keys.Count != 0 || modifiers.Count != 0)
            {
                var sb = new StringBuilder(32);
                foreach (var k in modifiers)
                {
                    var str = "";
                    switch (k)
                    {
                        case VirtualKeyCode.MENU:
                        case VirtualKeyCode.RMENU:
                        case VirtualKeyCode.LMENU:
                            str = "Alt";
                            break;
                        case VirtualKeyCode.LCONTROL:
                        case VirtualKeyCode.RCONTROL:
                        case VirtualKeyCode.CONTROL:
                            str = "Ctrl";
                            break;
                        case VirtualKeyCode.RWIN:
                        case VirtualKeyCode.LWIN:
                            str = "Win";
                            break;
                        case VirtualKeyCode.SHIFT:
                        case VirtualKeyCode.LSHIFT:
                        case VirtualKeyCode.RSHIFT:
                            str = "Shift";
                            break;
                        default:
                            str = k.ToString();
                            break;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append('-');
                    }

                    sb.Append(str);
                }

                if (sb.Length > 0)
                {
                    sb.Append(" + ");
                }

                foreach (var k in keys)
                {
                    var str = k.ToString();
                    if (str.StartsWith("VK_"))
                    {
                        str = str.Substring(3);
                    }

                    sb.Append(str);
                    sb.Append(" + ");
                }


                sb.Remove(sb.Length - 3, 3);
                return sb.ToString();
            }

            return "";
        }
    }
}
