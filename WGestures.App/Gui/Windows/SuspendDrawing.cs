using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WGestures.App.Gui.Windows
{
    internal static class SuspendDrawingControl
    {
        private const int WM_SETREDRAW = 11;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        public static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        public static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }
    }
}
