using System;
using System.Diagnostics;
using WGestures.Common.Annotation;

namespace WGestures.Core.Commands.Impl
{
    [Named("打开网址")]
    [Serializable]
    public class GotoUrlCommand : AbstractCommand
    {
        private string _url;

        public GotoUrlCommand()
        {
            this.Url = "";
        }

        public string Url
        {
            get => _url;
            set => _url = value;
        }

        public override void Execute()
        {
            if (this.Url != null)
            {
                if (!this.Url.Contains("://"))
                {
                    this.Url = "http://" + this.Url;
                }
            }

            using (Process.Start(this.Url))
            {
                ;
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }
    }
}
