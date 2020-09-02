using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WGestures.Core
{
    /// <summary>
    ///     表示一个手势实例
    /// </summary>
    [Serializable]
    public class Gesture
    {
        public enum GestureDir
        {
            Up = 0,
            RightUp,
            Right,
            RightDown,
            Down,
            LeftDown,
            Left,
            LeftUp
        }

        private static readonly char[] dirs = {'↑', '↗', '→', '↘', '↓', '↙', '←', '↖'};


        public Gesture(
            GestureTriggerButton gestureBtn = GestureTriggerButton.Right, int defaultCapacity = 16)
        {
            this.GestureButton = gestureBtn;
            this.Dirs = new List<GestureDir>(defaultCapacity);
            this.Modifier = GestureModifier.None;
        }

        public GestureTriggerButton GestureButton { get; set; }

        public List<GestureDir> Dirs { get; set; }
        public GestureModifier Modifier { get; set; }

        public void Add(params GestureDir[] newDirs)
        {
            foreach (var d in newDirs)
            {
                this.Dirs.Add(d);
            }
        }

        public int Count()
        {
            return this.Dirs.Count;
        }

        internal GestureDir? Last()
        {
            return this.Dirs.Count == 0 ? (GestureDir?) null : this.Dirs.Last();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(this.Count() + 4);

            sb.Append(this.GestureButton.ToMnemonic());

            foreach (var d in this.Dirs)
            {
                sb.Append(dirs[(byte) d]);
            }

            sb.Append(this.Modifier.ToMnemonic());


            return sb.ToString();
        }

        public override int GetHashCode()
        {
            var hash = 19;
            hash += hash * 31 + this.GestureButton.GetHashCode();
            foreach (var d in this.Dirs)
            {
                hash = hash * 31 + (int) d;
            }

            hash = hash * 31 + (int) this.Modifier;


            return hash;
        }

        public override bool Equals(object obj)
        {
            var o = obj as Gesture;
            return o != null
                   && o.GestureButton == this.GestureButton
                   && this.Dirs.SequenceEqual(o.Dirs)
                   && this.Modifier == o.Modifier;
        }
    }
}
