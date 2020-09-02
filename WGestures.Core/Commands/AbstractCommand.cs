using System;
using WGestures.Common.Annotation;

namespace WGestures.Core.Commands
{
    [Serializable]
    public abstract class AbstractCommand
    {
        public abstract void Execute();

        public virtual string Description()
        {
            return NamedAttribute.GetNameOf(this.GetType());
        }
    }
}
