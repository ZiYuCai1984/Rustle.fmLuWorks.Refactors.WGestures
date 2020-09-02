using System;
using System.IO;
using WGestures.Core;

namespace WGestures.App.Gui.Model
{
    [Serializable]
    internal class OrderableExeApp : ExeApp, IComparable<OrderableExeApp>
    {
        public OrderableExeApp()
        {
        }

        public OrderableExeApp(ExeApp from)
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

        //显示顺序
        public int Order { get; set; }

        public bool Exists => File.Exists(this.ExecutablePath);


        public int CompareTo(OrderableExeApp other)
        {
            if (other.Order == this.Order)
            {
                return 0;
            }

            if (other.Order > this.Order)
            {
                return -1;
            }

            return 1;
        }
    }
}
