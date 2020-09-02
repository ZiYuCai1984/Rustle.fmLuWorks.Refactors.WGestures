using System;
using System.Diagnostics;
using System.IO;
using WGestures.Common.Annotation;

namespace WGestures.Core.Commands.Impl
{
    [Named("打开文件或应用程序")]
    [Serializable]
    public class OpenFileCommand : AbstractCommand
    {
        private string _filePath;

        public OpenFileCommand()
        {
            this.FilePath = "";
        }

        public string FilePath
        {
            get => _filePath;
            set => _filePath = value;
        }

        public override void Execute()
        {
            var info = new ProcessStartInfo(this.FilePath);
            info.UseShellExecute = true;

            var p = Process.Start(info);
            if (p != null)
            {
                p.Close();
            }
        }

        public override string Description()
        {
            if (File.Exists(this.FilePath))
            {
                return "打开 " + Path.GetFileName(this.FilePath);
            }

            return "";
        }
    }
}
