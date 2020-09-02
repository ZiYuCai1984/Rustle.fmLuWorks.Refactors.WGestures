using System;
using System.Drawing;

namespace WGestures.Core
{
    public abstract class GestureContext // : MarshalByRefObject
    {
        public Point EndPoint;

        public GestureTriggerButton GestureButton;

        public uint ProcId;
        public Point StartPoint;
        public IntPtr WinId;

        public abstract void ActivateTargetWindow();
    }
}
