using System;
using System.Collections.Generic;
using System.IO;

namespace WGestures.Common.Config.Impl
{
    public class PlistConfig : AbstractDictConfig
    {
        /// <summary>
        ///     创建一个空的Config
        /// </summary>
        public PlistConfig()
        {
        }

        /// <summary>
        ///     创建并指定要加载或保存的plist文件位置
        /// </summary>
        /// <param name="plistPath">Plist path.</param>
        public PlistConfig(string plistPath)
        {
            this.PlistPath = plistPath;
            this.Load();
        }

        public PlistConfig(Stream stream, bool closeStream)
        {
            this.Load(stream, closeStream);
        }

        public string FileVersion
        {
            get => this.Get<string>("$$FileVersion", null);
            set => this.Set("$$FileVersion", value);
        }

        public string PlistPath { get; set; }

        private void Load()
        {
            if (this.PlistPath == null)
            {
                throw new InvalidOperationException("未指定需要加载的plist文件路径");
            }

            if (!File.Exists(this.PlistPath))
            {
                return;
            }

            this.Dict = (Dictionary<string, object>) Plist.readPlist(this.PlistPath);
        }

        private void Load(Stream stream, bool closeStream = false)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("stream");
            }

            try
            {
                this.Dict = (Dictionary<string, object>) Plist.readPlist(stream, plistType.Auto);
            }
            catch (Exception)
            {
                if (closeStream && stream != null)
                {
                    stream.Close();
                }

                throw;
            }
        }

        public override void Save()
        {
            if (this.PlistPath == null)
            {
                throw new InvalidOperationException("未指定需要保存到的plist文件路径(PlistPath属性)");
            }

            Plist.writeXml(this.Dict, this.PlistPath);
        }
    }
}
