using System.Configuration;
using System.Windows.Forms;

namespace WGestures.App
{
    internal static class AppSettings
    {
        public static string CheckForUpdateUrl
        {
            get
            {
#if DEBUG
                return ConfigurationManager.AppSettings.Get(Constants.CheckForUpdateUrlAppSettingKey);// "http://localhost:1226/projects/latestVersion?product=WGestures";

#else
                return ConfigurationManager.AppSettings.Get(
                    Constants.CheckForUpdateUrlAppSettingKey);

#endif
            }
        }

        public static string ProductHomePage
            => ConfigurationManager.AppSettings.Get(Constants.ProductHomePageAppSettingKey);

        public static string UserDataDirectory => Application.LocalUserAppDataPath;


        public static string ConfigFilePath => UserDataDirectory + @"\config.plist";

        public static string GesturesFilePath => UserDataDirectory + @"\gestures.wg2";

        public static string DefaultGesturesFilePath
            => Application.StartupPath + @"\defaults\gestures.wg2";


        public static string ConfigFileVersion => "1";

        public static string GesturesFileVersion => "3";
    }
}
