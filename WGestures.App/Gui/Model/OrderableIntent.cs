using System;
using WGestures.Core;

namespace WGestures.App.Gui.Model
{
    [Serializable]
    internal class OrderableIntent : GestureIntent
    {
        public OrderableIntent()
        {
        }


        public OrderableIntent(GestureIntent from)
        {
            var t = from.GetType();
            foreach (var fieldInf in t.GetFields())
            {
                fieldInf.SetValue(this, fieldInf.GetValue(from));
            }

            foreach (var propInf in t.GetProperties())
            {
                propInf.SetValue(this, propInf.GetValue(from, null), null);
            }
        }

        public int Order { get; set; }
    }
}
