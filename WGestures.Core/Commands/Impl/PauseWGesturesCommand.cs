using System;
using WGestures.Common.Annotation;

namespace WGestures.Core.Commands.Impl
{
    [Named("暂停WGestures")]
    [Serializable]
    public class PauseWGesturesCommand : AbstractCommand, IGestureParserAware
    {
        public GestureParser Parser { set; private get; }

        public override void Execute()
        {
            this.Parser.Pause();
        }
    }
}
