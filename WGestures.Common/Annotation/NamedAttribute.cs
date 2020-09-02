using System;
using System.Linq;

namespace WGestures.Common.Annotation
{
    /// <summary>
    ///     用于给一个类命名
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NamedAttribute : Attribute
    {
        public NamedAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public static string GetNameOf(Type t)
        {
            if (t.GetCustomAttributes(typeof(NamedAttribute), false).FirstOrDefault() is
                NamedAttribute attr)
            {
                return attr.Name;
            }

            return t.Name;
        }
    }
}
