using System;
using System.Drawing;

namespace WGestures.Core
{
    public class PathEventArgs : EventArgs
    {
        public GestureTriggerButton Button;

        public PathEventArgs(Point location, GestureContext context)
        {
            this.Location = location;
            this.Context = context;
        }

        public PathEventArgs()
        {
        }

        public Point Location { get; set; }
        public GestureModifier Modifier { get; set; }
        public GestureContext Context { get; set; }
    }

    public class BeforePathStartEventArgs
    {
        public BeforePathStartEventArgs(PathEventArgs pathEventArgs)
        {
            this.PathEventArgs = pathEventArgs;
            this.Context = pathEventArgs.Context;
            this.ShouldPathStart = true;
        }

        public PathEventArgs PathEventArgs { get; }
        public bool ShouldPathStart { get; set; }
        public GestureContext Context { get; }
    }


    public delegate void PathTrackEventHandler(PathEventArgs args);

    public delegate void BeforePathStartEventHandler(BeforePathStartEventArgs args);

    public interface IPathTracker : IDisposable
    {
        bool Paused { get; set; }
        bool IsSuspended { get; }
        void Start();
        void Stop();

        void SuspendTemprarily(GestureModifier filteredModifiers);

        event BeforePathStartEventHandler BeforePathStart;

        event PathTrackEventHandler PathStart;
        event PathTrackEventHandler PathGrow;
        event PathTrackEventHandler EffectivePathGrow;
        event PathTrackEventHandler PathEnd;
        event PathTrackEventHandler PathTimeout;
        event PathTrackEventHandler PathModifier;

        //TODO: 按照接口隔离原则重构
        event Action<ScreenCorner> HotCornerTriggered;
        event Action<ScreenEdge> EdgeRubbed;
    }
}
