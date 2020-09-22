namespace WGestures.App
{
    internal static class Constants
    {
        public const string Identifier = "Rustle.fmLuWorks.Refactors.WGestures";
        public const string CheckForUpdateUrlAppSettingKey = "CheckForUpdateUrl";

        public const string ProductHomePageAppSettingKey = "ProductHomePage";

#if DEBUG
        public const int AutoCheckForUpdateInterval = 1000 * 3;
#else
        public const int AutoCheckForUpdateInterval = 1000 * 30;
#endif
    }
}
