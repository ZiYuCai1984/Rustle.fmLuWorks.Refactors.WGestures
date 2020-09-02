using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace WGestures.Common.Product
{
    public class VersionChecker : IDisposable
    {
        private readonly string _url;
        private TimeOutWebClient _client;

        public VersionChecker(string url, int timeOutSeconds = 15)
        {
            _url = url;
            _client = new TimeOutWebClient
                {TimeOutSecs = timeOutSeconds};
            _client.Proxy = null;
            _client.Encoding = Encoding.UTF8;

            _client.DownloadStringCompleted += (sender, args) =>
            {
                if (args.Cancelled)
                {
                    this.OnCanceled();
                    return;
                }

                if (args.Error != null)
                {
                    this.OnErrorHappened(args.Error);
                    return;
                }

                VersionInfo versionInfo = null;

                //{"Version":"1.5.2.0","WhatsNew":"Abcd\"efg"}
                try
                {
                    versionInfo = JsonConvert.DeserializeObject<VersionInfo>(args.Result);

                    /*const string versionPattern = "\"Version\"\\s*:\\s*\"\\d.\\d.\\d.\\d\"";
                    const string whatsNewPattern = "\"WhatsNew\"\\s*:\\s*((?<![\\\\])['\"])((?:.(?!(?<![\\\\])\\1))*.?)\\1";

                    var versionMatch = Regex.Match(args.Result, versionPattern);//"^\\{\"Version\":\"\\d.\\d.\\d.\\d\",\"WhatsNew\":\"[^\"]*\"\\}$");
                    var whatsNewMatch = Regex.Match(args.Result, whatsNewPattern);
                    if (versionMatch.Success && whatsNewMatch.Success)
                    {
                        var versionStr = Regex.Match(versionMatch.Value,"\\d.\\d.\\d.\\d").Value;
                        var whatsNewStr = whatsNewMatch.Value.Split(':')[1];
                        whatsNewStr = whatsNewStr.Substring(1,whatsNewStr.Length -2).Replace("\\r\\n","\r\n").Replace("\\\"","\"");

                        versionInfo = new VersionInfo(){Version = versionStr, WhatsNew = whatsNewStr};
                    }
                    else throw new Exception();*/
                }
                catch (Exception e)
                {
                    this.OnErrorHappened(e);
                    return;
                }

                this.OnFinished(versionInfo);
            };
        }

        public bool IsBusy => _client.IsBusy;

        public void Dispose()
        {
            if (_client.IsBusy)
            {
                _client.CancelAsync();
            }

            _client.Dispose();
            _client = null;
        }

        public event Action<VersionInfo> Finished;
        public event Action Canceled;
        public event Action<Exception> ErrorHappened;


        protected virtual void OnFinished(VersionInfo obj)
        {
            var handler = this.Finished;
            if (handler != null)
            {
                handler(obj);
            }
        }

        protected virtual void OnCanceled()
        {
            var handler = this.Canceled;
            if (handler != null)
            {
                handler();
            }
        }

        protected virtual void OnErrorHappened(Exception e)
        {
            var handler = this.ErrorHappened;
            if (handler != null)
            {
                handler(e);
            }
        }

        public void CheckAsync()
        {
            var now = DateTime.Now;
            _client.DownloadStringAsync(new Uri(_url));
            var then = DateTime.Now;

            Debug.WriteLine("CheckAsync Time used: " + (then - now).TotalSeconds);
        }

        public void Cancel()
        {
            _client.CancelAsync();
        }
    }

    internal class TimeOutWebClient : WebClient
    {
        public TimeOutWebClient()
        {
            this.TimeOutSecs = 30;
        }

        public int TimeOutSecs { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var w = base.GetWebRequest(address);

            w.Timeout = this.TimeOutSecs * 1000;
            return w;
        }
    }
}
