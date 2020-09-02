using WGestures.Common.Config.Impl;
using WGestures.Core.Persistence.Impl;

namespace WGestures.App.Migrate
{
    internal class ConfigAndGestures
    {
        public ConfigAndGestures(PlistConfig config, JsonGestureIntentStore gestures)
        {
            this.Config = config;
            this.GestureIntentStore = gestures;
        }

        public PlistConfig Config { get; }

        public JsonGestureIntentStore GestureIntentStore { get; }
    }
}
