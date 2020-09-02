using System.Collections.Generic;
using WGestures.Core.Commands;

namespace WGestures.Core.Persistence
{
    public interface IGestureIntentStore : IEnumerable<ExeApp>
    {
        GlobalApp GlobalApp { get; }
        AbstractCommand[] HotCornerCommands { get; set; }
        bool TryGetExeApp(string key, out ExeApp found);
        ExeApp GetExeApp(string key);

        void Remove(string app);
        void Remove(ExeApp app);
        void Add(ExeApp app);
        void Save();
    }
}
