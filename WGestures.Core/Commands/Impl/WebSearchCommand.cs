using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using WindowsInput.Native;
using Microsoft.Win32;
using WGestures.Common.Annotation;
using WGestures.Common.OsSpecific.Windows;
using Timer = System.Windows.Forms.Timer;

namespace WGestures.Core.Commands.Impl
{
    [Named("Web搜索")]
    [Serializable]
    public class WebSearchCommand : AbstractCommand, IGestureContextAware
    {
        public WebSearchCommand()
        {
            this.SearchEngingName = "Google";
            this.SearchEngineUrl = "https://www.google.com/search?q={0}";
        }

        public string SearchEngineUrl { get; set; }
        public string SearchEngingName { get; set; }
        public string UseBrowser { get; set; }

        public GestureContext Context { set; private get; }

        public override void Execute()
        {
            if (this.SearchEngineUrl == null
                || !Uri.IsWellFormedUriString(
                    this.PopulateSearchEngingUrl("Nothing"),
                    UriKind.Absolute))
            {
                return;
            }

            var t = new Thread(
                () =>
                {
                    var clipboardMonitor = new ClipboardMonitor();

                    clipboardMonitor.MonitorRegistered += () =>
                    {
                        var timer = new Timer
                            {Interval = 2000};
                        timer.Tick += (sender, args) =>
                        {
                            Debug.WriteLine("WebSearchCommand Timeout!");
                            timer.Enabled = false;

                            clipboardMonitor.StopMonitor();
                            clipboardMonitor.DestroyHandle();
                            Application.ExitThread();

                            Debug.WriteLine("超时结束 WebSearchCommand Runloop");
                        };

                        timer.Enabled = true;

                        try
                        {
                            Sim.KeyDown(VirtualKeyCode.CONTROL);
                            Sim.KeyDown(VirtualKeyCode.VK_C);

                            Sim.KeyUp(VirtualKeyCode.VK_C);
                            Sim.KeyUp(VirtualKeyCode.CONTROL);
                            //_sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, new[] { VirtualKeyCode.VK_C });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("发送按键失败:" + ex);
                            Native.TryResetKeys(
                                new[] {VirtualKeyCode.CONTROL, VirtualKeyCode.VK_C});
#if DEBUG
                        //throw;
#endif
                        }
                    };

                    clipboardMonitor.ClipboardUpdated += args =>
                    {
                        Debug.WriteLine("ClipboardChanged");
                        args.Handled = true;

                        var text = "";
                        if (Clipboard.ContainsText()
                            && (text = Clipboard.GetText().Trim()).Length > 0)
                        {
                            var browser = GetDefaultBrowserPath();
                            if (this.UseBrowser != null
                                && File.Exists(this.UseBrowser.Replace("\"", "")))
                            {
                                browser = this.UseBrowser;
                            }

                            string urlToOpen;
                            //如果是URL则打开，否则搜索
                            if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                            {
                                urlToOpen = text;
                            }
                            else
                            {
                                if (text.Length > 100)
                                {
                                    text = text.Substring(0, 100);
                                }

                                urlToOpen = this.PopulateSearchEngingUrl(text);
                            }

                            //M$ Edge Hack
                            if (browser.Contains("LaunchWinApp.exe"))
                            {
                                urlToOpen = "microsoft-edge:" + urlToOpen;
                                Process.Start(urlToOpen);
                            }
                            else
                            {
                                var startInfo = new ProcessStartInfo(
                                    browser,
                                    "\"" + urlToOpen + "\"");
                                using (Process.Start(startInfo))
                                {
                                    ;
                                }
                            }
                        }

                        clipboardMonitor.StopMonitor();
                        clipboardMonitor.DestroyHandle();

                        Application.ExitThread();
                    };

                    clipboardMonitor.StartMonitor();

                    Application.Run();
                    Debug.WriteLine("Thread End?");
                }) {Name = "WebSearchCommand Runloop(Bug！我不应该存在！)"};

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        private string PopulateSearchEngingUrl(string param)
        {
            return HttpUtility.UrlPathEncode(string.Format(this.SearchEngineUrl, param));
        }

        public override string Description()
        {
            return (this.SearchEngingName ?? "Web") + "搜索";
        }

        private static string GetDefaultBrowserPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\http\\UserChoice")
            )
            {
                var progId = key.GetValue("Progid", null);

                if (progId != null)
                {
                    const string exeSuffix = ".exe";

                    using (var pathKey =
                        Registry.ClassesRoot.OpenSubKey(progId + @"\shell\open\command"))
                    {
                        if (pathKey != null)
                        {
                            // Trim parameters.
                            try
                            {
                                var path = pathKey.GetValue(null)
                                    .ToString()
                                    .ToLower()
                                    .Replace("\"", "");
                                if (!path.EndsWith(exeSuffix))
                                {
                                    path = path.Substring(
                                        0,
                                        path.LastIndexOf(exeSuffix, StringComparison.Ordinal)
                                        + exeSuffix.Length);

                                    return path;
                                }
                            }
                            catch
                            {
                                // Assume the registry value is set incorrectly, or some funky browser is used which currently is unknown.
                            }
                        }
                    }
                }
            }

            return "explorer.exe";
        }
    }
}
