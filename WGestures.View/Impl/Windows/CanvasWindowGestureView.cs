using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Timers;
using Microsoft.Win32;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Core;
using Timer = System.Timers.Timer;

namespace WGestures.View.Impl.Windows
{
    public class CanvasWindowGestureView : IDisposable
    {
        public CanvasWindowGestureView(GestureParser gestureParser)
        {
            _gestureParser = gestureParser;
            this.RegisterEventHandlers();
            var waitCanvasWindow = new AutoResetEvent(false);
            new Thread(
                () =>
                {
                    _canvasWindow = new CanvasWindow
                    {
                        //最初的时候放在屏幕以外
                        Visible = false,
                        IgnoreInput = true,
                        NoActivate = true,
                        TopMost = true
                    };
                    waitCanvasWindow.Set();
                    _canvasWindow.ShowDialog();
                },
                1) {Name = "CanvasWindow"}.Start();

            waitCanvasWindow.WaitOne();

            _canvasBuf = new DiBitmap(_screenBounds.Size);

            this.InitDefaultProperties();
            _fadeOuTimer.Elapsed += this.OnFadeOutTimerElapsed;
            SystemEvents.DisplaySettingsChanged += this.SystemDisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged += this.SystemEvents_UserPreferenceChanged;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            #region unregistor events

            _gestureParser.IntentRecognized -= this.HandleIntentRecognized;
            _gestureParser.IntentInvalid -= this.HandleIntentInvalid;
            _gestureParser.IntentOrPathCanceled -= this.HandleIntentOrPathCanceled;

            _gestureParser.PathTracker.PathStart -= this.HandlePathStart;
            _gestureParser.PathTracker.PathGrow -= this.HandlePathGrow;
            _gestureParser.PathTracker.PathTimeout -= this.HandlePathTimeout;
            _gestureParser.IntentReadyToExecute -= this.HandleIntentReadyToExecute;
            _gestureParser.IntentReadyToExecuteOnModifier -=
                this.HandleIntentReadyToExecuteOnModifier;

            _gestureParser.GestureCaptured -= this.HandleGestureRecorded;
            _gestureParser.CommandReportStatus -= this.HandleCommandReportStatus;

            #endregion

            #region dispose pens

            _mainPen.Dispose();
            _middleBtnPen.Dispose();
            _borderPen.Dispose();
            _alternativePen.Dispose();

            //_shadowPen.Dispose();

            //_shadowPen = null;


            _dirtyMarkerPen.Dispose();

            #endregion

            if (_canvasBuf != null)
            {
                _canvasBuf.Dispose();
                _canvasBuf = null;
            }

            if (_canvasWindow != null)
            {
                _canvasWindow.Dispose();
                _canvasWindow = null;
            }

            if (_gPath != null)
            {
                _gPath.Dispose();
                _gPath = null;
            }

            if (_gPathDirty != null)
            {
                _gPathDirty.Dispose();
                _gPathDirty = null;
            }

            if (_labelPath != null)
            {
                _labelPath.Dispose();
                _labelPath = null;
            }

            if (_labelFont != null)
            {
                _labelFont.Dispose();
                _labelFont = null;
            }

            if (_fadeOuTimer != null)
            {
                _fadeOuTimer.Elapsed -= this.OnFadeOutTimerElapsed;
                _fadeOuTimer.Dispose();
                _fadeOuTimer = null;
            }

            SystemEvents.DisplaySettingsChanged -= this.SystemDisplaySettingsChanged;


            Debug.WriteLine("Dispose");
            this.IsDisposed = true;
        }

        private void SystemEvents_UserPreferenceChanged(
            object sender, UserPreferenceChangedEventArgs e)
        {
            _systemColor = Native.GetWindowColorization();
        }

        private void SystemDisplaySettingsChanged(object sender, EventArgs e)
        {
            _screenBounds = Native.GetScreenBounds();
            _canvasBuf = new DiBitmap(_screenBounds.Size);
            _dpiFactor = Native.GetScreenDpi() / 96.0f;
        }

        private void InitDefaultProperties()
        {
            //defaults
            this.ShowCommandName = true;
            this.ShowPath = true;
            this.ViewFadeOut = true;

            this.PathMaxPointCount = (int) (512 * _dpiFactor);

            var widthBase = 2 * _dpiFactor;

            #region init pens

            _mainPen = new Pen(Color.FromArgb(255, 50, 200, 100), widthBase)
                {EndCap = LineCap.Round, StartCap = LineCap.Round};
            _middleBtnPen = new Pen(Color.FromArgb(255, 20, 150, 200), widthBase)
                {EndCap = LineCap.Round, StartCap = LineCap.Round};
            _xBtnPen = new Pen(Color.FromArgb(255, 20, 100, 200), widthBase)
                {EndCap = LineCap.Round, StartCap = LineCap.Round};
            _borderPen = new Pen(Color.FromArgb(255, 255, 255, 255), widthBase + 4)
                {EndCap = LineCap.Round, StartCap = LineCap.Round};
            _alternativePen = new Pen(Color.FromArgb(255, 255, 120, 20), widthBase)
                {EndCap = LineCap.Round, StartCap = LineCap.Round};


            //_shadowPen = new Pen(Color.FromArgb(25, Color.Black), widthBase * 3) { EndCap = LineCap.Round, StartCap = LineCap.Round };


            //_shadowPenWidth = _shadowPen.Width;
            //_dirtyMarkerPen = (Pen)_shadowPen.Clone();

            _dirtyMarkerPen = (Pen) _borderPen.Clone();
            _dirtyMarkerPen.Width *= 1.5f;

            _systemColor = Native.GetWindowColorization();

            #endregion
        }

        private void BeginView()
        {
            Debug.WriteLine("BeginView");
            this.StopFadeout();

            if (_canvasBuf.Size != _screenBounds.Size)
            {
                _canvasBuf = new DiBitmap(_screenBounds.Size);
            }

            _canvasOpacity = 255;
            _canvasWindow.Bounds = _screenBounds;

            _canvasWindow.TopMost = true;
            _canvasWindow.Visible = true;

            if (this.ShowPath)
            {
                _pathVisible = true;
                _pathPen = _alternativePen;
            }

            if (this.ShowCommandName)
            {
                _labelVisible = false;
            }
        }

        private void DrawAndUpdate()
        {
            this.Draw();

            #region 更新到窗口上

            var pathDirty = Rectangle.Ceiling(_gPathDirty.GetBounds());
            pathDirty.Offset(_screenBounds.X, _screenBounds.Y);
            pathDirty.Intersect(_screenBounds);
            pathDirty.Offset(-_screenBounds.X, -_screenBounds.Y); //挪回来变为基于窗口的坐标

            if (this.ShowPath)
            {
                _canvasWindow.SetDiBitmap(_canvasBuf, /*_pathDirtyRect*/pathDirty);
            }

            if (_labelChanged) //ShowCommandName)
            {
                var labelDirtyRect = _labelRect.Width > _lastLabelRect.Width
                    ? _labelRect
                    : _lastLabelRect;
                labelDirtyRect.Height = _labelRect.Height > _lastLabelRect.Height
                    ? _labelRect.Height
                    : _lastLabelRect.Height;
                _canvasWindow.SetDiBitmap(_canvasBuf, Rectangle.Ceiling(labelDirtyRect));
            }
            else if (_labelVisible)
            {
                var labelDirty = Rectangle.Ceiling(_labelRect);
                var intercected = pathDirty.IntersectsWith(labelDirty);

                if (intercected && !pathDirty.Contains(labelDirty))
                {
                    labelDirty.Intersect(pathDirty);
                    _canvasWindow.SetDiBitmap(_canvasBuf, labelDirty);
                }
            }

            #endregion
        }

        private void Draw()
        {
            var g = _canvasBuf.BeginDraw();

            //如果是识别与未识别之间转换，则使用region而非dirtyRect来重绘
            if (_recognizeStateChanged)
            {
                if (_gPath.PointCount > 0)
                {
                    _gPathDirty.Reset();
                    _gPathDirty.AddPath(_gPath, false);
                    _gPathDirty.Widen(_dirtyMarkerPen);
                }
            }

            g.SetClip(_gPathDirty);

            var labelAffected = false;
            //如果Label的内容改变，则整个重绘
            //不然，就判断哪些区域收到了path的影响，然只更新受影响的一小部分。
            if (_labelChanged)
            {
                var labelDirtyRect = _labelRect.Width > _lastLabelRect.Width
                    ? _labelRect
                    : _lastLabelRect;
                labelDirtyRect.Height = _labelRect.Height > _lastLabelRect.Height
                    ? _labelRect.Height
                    : _lastLabelRect.Height;

                g.SetClip(labelDirtyRect, CombineMode.Union);
                labelAffected = _labelVisible;
            }
            else if (_labelVisible)
            {
                var labelDirty = Rectangle.Ceiling(_labelRect);
                var pathDirty = Rectangle.Ceiling(_gPathDirty.GetBounds());
                labelAffected = pathDirty.IntersectsWith(labelDirty);

                if (labelAffected && !pathDirty.Contains(labelDirty))
                {
                    labelDirty.Intersect(pathDirty);
                    g.SetClip(labelDirty, CombineMode.Union);

                    Debug.WriteLine("LabelDirty=" + labelDirty);
                }
            }

            g.Clear(Color.Transparent);

            #region 1) 绘制路径

            if (this.ShowPath && _pathVisible)
            {
                //g.DrawPath(_shadowPen, _gPath);
                g.DrawPath(_borderPen, _gPath);
                g.DrawPath(_pathPen, _gPath);
            }

            #endregion

            #region 2) 绘制标签

            if (labelAffected) //ShowCommandName && _labelVisible)
            {
                Debug.WriteLine("Label Redraw");
                using (var pen = new Pen(Color.White, 1.5f * _dpiFactor))
                    //using (var shadow = new Pen(Color.FromArgb(40, 0, 0, 0), 3f * _dpiFactor))
                {
                    /*DrawRoundedRectangle(g, RectangleF.Inflate(_labelRect,
                        -1f * _dpiFactor, -1f * _dpiFactor),
                        (int)(12 * _dpiFactor), shadow, Color.Transparent);*/
                    this.DrawRoundedRectangle(
                        g,
                        RectangleF.Inflate(
                            _labelRect,
                            -2.6f * _dpiFactor,
                            -2.6f * _dpiFactor),
                        0,
                        pen,
                        _labelBgColor);

                    //if (_labelColor != Color.White)
                    //using (var stroke = new Pen(Color.Black, 1.5f * _dpiFactor))
                    //    g.DrawPath(stroke, _labelPath);

                    using (Brush brush = new SolidBrush(_labelColor))
                    {
                        g.FillPath(brush, _labelPath);
                    }
                }
            }

            #endregion

            _canvasBuf.EndDraw();
        }

        private void EndView()
        {
            Debug.WriteLine("EndView");
            if (this.ShowPath)
            {
                var g = _canvasBuf.BeginDraw();

                _gPath.Widen(_dirtyMarkerPen);
                g.SetClip(_gPath);
                g.Clear(Color.Transparent);
                _canvasBuf.EndDraw();

                var pathDirty = Rectangle.Ceiling(_gPath.GetBounds());
                pathDirty.Offset(_screenBounds.X, _screenBounds.Y);
                pathDirty.Intersect(_screenBounds);
                pathDirty.Offset(-_screenBounds.X, -_screenBounds.Y); //挪回来变为基于窗口的坐标

                _canvasWindow.SetDiBitmap(_canvasBuf, pathDirty);

                _gPath.Reset();
                _gPathDirty.Reset();
            }

            if (_labelVisible)
            {
                var g = _canvasBuf.BeginDraw();
                g.SetClip(_labelRect);
                g.Clear(Color.Transparent);
                _canvasBuf.EndDraw();
                _canvasWindow.SetDiBitmap(_canvasBuf, Rectangle.Ceiling(_labelRect));
                _labelPath.Reset();

                this.HideLabel();
                _labelChanged = false;
            }

            _canvasWindow.Visible = false;
            _labelRect = default(Rectangle); //todo: dirtyRect也是多余
        }

        private void HideLabel()
        {
            _labelText = null;
            _labelVisible = false;
            _labelChanged = true;
        }

        private void ShowLabel(Color color, string text, Color bgColor)
        {
            _labelVisible = true;
            _labelColor = color;
            _labelText = text;
            _labelBgColor = bgColor;
            _labelChanged = true;

            _lastLabelRect = _labelRect;

            _labelPath.Reset();
            var msgPos = new PointF(
                _screenBounds.Width / 2,
                _screenBounds.Height / 2 + _screenBounds.Width / 8);

            _labelPath.AddString(
                _labelText,
                _labelFont.FontFamily,
                0,
                _labelFont.Size * _dpiFactor,
                msgPos,
                StringFormat.GenericDefault);
            _labelRect = _labelPath.GetBounds();
            msgPos.X -= _labelRect.Width / 2;

            _labelPath.Reset();
            _labelPath.AddString(
                _labelText,
                _labelFont.FontFamily,
                0,
                _labelFont.Size * _dpiFactor,
                msgPos,
                StringFormat.GenericDefault);

            _labelRect = RectangleF.Inflate(
                _labelPath.GetBounds(),
                25 * _dpiFactor,
                15 * _dpiFactor);
        }

        private void RegisterEventHandlers()
        {
            _gestureParser.IntentRecognized += this.HandleIntentRecognized;
            _gestureParser.IntentInvalid += this.HandleIntentInvalid;
            _gestureParser.IntentOrPathCanceled += this.HandleIntentOrPathCanceled;
            _gestureParser.IntentReadyToExecute += this.HandleIntentReadyToExecute;
            _gestureParser.IntentReadyToExecuteOnModifier +=
                this.HandleIntentReadyToExecuteOnModifier;

            _gestureParser.PathTracker.PathStart += this.HandlePathStart;
            _gestureParser.PathTracker.PathGrow += this.HandlePathGrow;
            _gestureParser.PathTracker.PathTimeout += this.HandlePathTimeout;

            _gestureParser.GestureCaptured += this.HandleGestureRecorded;
            _gestureParser.CommandReportStatus += this.HandleCommandReportStatus;
        }


        #region Util

        /* private static void AddRoundedRectangle(GraphicsPath path, RectangleF bounds, int cornerRadius)
         {
             path.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180, 90);
             path.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270, 90);
             path.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
             path.AddArc(bounds.X, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
         }*/

        private void DrawRoundedRectangle(
            Graphics gfx, RectangleF Bounds, int CornerRadius, Pen DrawPen, Color FillColor)
        {
            var strokeOffset = Convert.ToInt32(Math.Ceiling(DrawPen.Width));

            var rect = Rectangle.Truncate(Bounds);

            rect.Inflate(-strokeOffset, -strokeOffset);

            using (var brush = new SolidBrush(FillColor))
            {
                //gfx.DrawRectangle(_shadowPen, Rectangle.Truncate(Bounds));

                gfx.FillRectangle(brush, rect);
                gfx.DrawRectangle(DrawPen, rect);
            }


            /*using (var gfxPath = new GraphicsPath())
            {
                gfxPath.AddArc(Bounds.X, Bounds.Y, CornerRadius, CornerRadius, 180, 90);
                gfxPath.AddArc(Bounds.X + Bounds.Width - CornerRadius, Bounds.Y, CornerRadius, CornerRadius, 270, 90);
                gfxPath.AddArc(Bounds.X + Bounds.Width - CornerRadius, Bounds.Y + Bounds.Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
                gfxPath.AddArc(Bounds.X, Bounds.Y + Bounds.Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
                gfxPath.CloseAllFigures();

                using (var sb = new SolidBrush(FillColor)) gfx.FillPath(sb, gfxPath);
                gfx.DrawPath(DrawPen, gfxPath);
            }*/
        }

        #endregion

        #region properties

        public Color PathMainColor
        {
            get => _mainPen.Color;
            set => _mainPen.Color = value;
        }

        public Color PathBorderColor
        {
            get => _borderPen.Color;
            set => _borderPen.Color = value;
        }

        public Color PathAlternativeColor
        {
            get => _alternativePen.Color;
            set => _alternativePen.Color = value;
        }

        public Color PathMiddleBtnMainColor
        {
            get => _middleBtnPen.Color;
            set => _middleBtnPen.Color = value;
        }

        public Color PathXBtnMainColor
        {
            get => _xBtnPen.Color;
            set => _xBtnPen.Color = value;
        }

        public float PathWidth
        {
            get => _mainPen.Width;
            set => _mainPen.Width = _alternativePen.Width = _middleBtnPen.Width = value;
        }

        public float PathBorderWidth
        {
            get => _borderPen.Width;
            set => _borderPen.Width = value;
        }

        //Pen.Width 内部使用了new float[] !, 反复调用Pen.Width导致alloc大量heap内存
        private float _shadowPenWidth;

        public float PathShadowWidth
        {
            get => _shadowPenWidth;
            set
            {
                /*_shadowPenWidth = _shadowPen.Width = value;*/
            } //TODO: ignore for temporarily
        }

        public int PathMaxPointCount { get; set; }

        public bool ShowPath { get; set; }
        public bool ShowCommandName { get; set; }
        public bool ViewFadeOut { get; set; }

        #endregion

        #region fields

        private Pen _tempMainPen;
        private Pen _middleBtnPen;
        private Pen _xBtnPen;
        private Pen _mainPen;
        private Pen _alternativePen;

        private Pen _borderPen;

        //Pen _shadowPen;
        private Pen _dirtyMarkerPen;
        private Point _prevPoint;
        private GraphicsPath _gPath = new GraphicsPath();
        private GraphicsPath _gPathDirty = new GraphicsPath();
        private Pen _pathPen;
        private bool _pathVisible;

        private readonly GestureParser _gestureParser;

        private Rectangle _screenBounds = Native.GetScreenBounds();
        private float _dpiFactor = Native.GetScreenDpi() / 96.0f;

        private CanvasWindow _canvasWindow;
        private DiBitmap _canvasBuf;

        private RectangleF _labelRect;
        private RectangleF _lastLabelRect;
        private bool _labelVisible;
        private string _labelText;
        private Color _labelColor;
        private Color _systemColor;
        private Color _labelBgColor;
        private bool _labelChanged;
        private GraphicsPath _labelPath = new GraphicsPath();
        private Font _labelFont = new Font("微软雅黑", 32);

        private bool _isCurrentRecognized;
        private bool _recognizeStateChanged;
        private short _pointCount;

        private Timer _fadeOuTimer = new Timer(55);
        private const byte FadeOutDelta = 60;
        private byte _canvasOpacity;

        #endregion

        #region event handlers

        private void HandlePathStart(PathEventArgs args)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("WhenPathStart");

            _screenBounds =
                Screen.ScreenBoundsFromPoint(args.Location)
                    .Value; //Screen.FromPoint(args.Location);

            _prevPoint = args.Location; //ToUpLeftCoord(args.Location);
            _pointCount = 1;

            //_tempMainPen = args.Button == GestureTriggerButton.Right ? _mainPen : _middleBtnPen;
            if ((args.Button & GestureTriggerButton.Right) != 0)
            {
                _tempMainPen = _mainPen;
            }
            else if ((args.Button & GestureTriggerButton.X) != 0)
            {
                _tempMainPen = _xBtnPen;
            }
            else
            {
                _tempMainPen = _middleBtnPen;
            }

            _isCurrentRecognized = false;
            _recognizeStateChanged = false;

            this.BeginView();
        }

        private void HandlePathGrow(PathEventArgs args)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            if (_pointCount > this.PathMaxPointCount)
            {
                return;
            }

            _pointCount++;

            if (_pointCount == this.PathMaxPointCount)
            {
                this.ShowLabel(Color.White, "您是有多无聊啊 :)", Color.FromArgb(150, 255, 0, 0));

                this.DrawAndUpdate();
                _labelChanged = false;
                _recognizeStateChanged = false;

                return;
            }

            var curPos = args.Location; //ToUpLeftCoord(args.Location);

            if (this.ShowPath)
            {
                //需要将点换算为基于窗口的坐标
                var pA = new Point(_prevPoint.X - _screenBounds.X, _prevPoint.Y - _screenBounds.Y);
                var pB = new Point(curPos.X - _screenBounds.X, curPos.Y - _screenBounds.Y);

                if (pA != pB)
                {
                    _gPath.AddLine(pA, pB);

                    _gPathDirty.Reset();
                    _gPathDirty.AddLine(pA, pB);
                    _gPathDirty.Widen(_dirtyMarkerPen);
                }

                curPos = new Point(pB.X + _screenBounds.X, pB.Y + _screenBounds.Y);
            }

            this.DrawAndUpdate();

            _recognizeStateChanged = false;

            _prevPoint = curPos; //args.Location;//ToUpLeftCoord(args.Location);
        }

        private void HandleIntentRecognized(GestureIntent intent)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("IntentRecognized");

            if (this.ShowCommandName)
            {
                var modifierText = intent.Gesture.Modifier.ToMnemonic();
                var newLabelText = intent.Name
                                   + (modifierText == string.Empty
                                       ? string.Empty
                                       : " " + modifierText);
                this.ShowLabel(Color.White, newLabelText, Color.FromArgb(120, 0, 0, 0));
            }

            if (!_isCurrentRecognized && this.ShowPath)
            {
                _isCurrentRecognized = true;
                _recognizeStateChanged = true;
                _pathPen = _tempMainPen;
                //ResetPathDirtyRect();
            }

            this.DrawAndUpdate();

            if (this.ShowCommandName)
            {
                _labelChanged = false;
            }

            _recognizeStateChanged = false;
        }

        //todo: 合并为IntentRecogChanged?
        private void HandleIntentInvalid(Gesture gesture)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("IntentInvalid");
            //pre
            if (this.ShowCommandName)
            {
                this.HideLabel();
            }

            if (_isCurrentRecognized && this.ShowPath)
            {
                _pathPen = _alternativePen;
                _isCurrentRecognized = false;
                _recognizeStateChanged = true;

                //ResetPathDirtyRect();
            }

            //draw
            this.DrawAndUpdate();

            //clear
            if (this.ShowCommandName)
            {
                _labelRect = default(Rectangle);
                _labelChanged = false;
            }
        }

        private void HandlePathTimeout(PathEventArgs args)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("PathTimeout");

            //if (ShowPath) ResetPathDirtyRect();
            this.EndView();
        }

        private void HandleIntentReadyToExecute(GestureIntent intent)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("WhenIntentReadyToExecute");
            if (this.ShowCommandName)
            {
                this.ShowLabel(
                    Color.White,
                    _labelText,
                    _systemColor); ////Color.FromArgb(120, 0, 80, 0));
            }

            //draw
            this.DrawAndUpdate();

            //clear
            if (this.ShowPath)
            {
                _pathVisible = false;
            }

            if (this.ShowCommandName)
            {
                _labelChanged = false;
            }

            if (this.ViewFadeOut)
            {
                this.FadeOut();
            }
            else
            {
                this.EndView();
            }
        }

        private void HandleIntentReadyToExecuteOnModifier(GestureModifier modifier)
        {
        }

        private void HandleIntentOrPathCanceled()
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("IntentOrPathCancled");
            this.EndView();
        }

        private void HandleGestureRecorded(Gesture g)
        {
            if (!this.ShowPath && !this.ShowCommandName)
            {
                return;
            }

            Debug.WriteLine("WhenGestureCaptured");
            this.EndView();
        }

        private void HandleCommandReportStatus(string status, GestureIntent intent)
        {
            if (this.ShowCommandName)
            {
                var modifierText = intent.Gesture.Modifier.ToMnemonic();
                var newLabelText =
                    (modifierText == string.Empty ? string.Empty : modifierText + " ")
                    + intent.Name
                    + status;
                if (newLabelText.Equals(_labelText))
                {
                    return;
                }

                _labelText = newLabelText;

                this.ShowLabel(Color.White, newLabelText, Color.FromArgb(120, 0, 0, 0));

                this.DrawAndUpdate();

                if (this.ShowCommandName)
                {
                    _labelChanged = false;
                }
            }
        }

        #endregion

        #region timer handlers

        private void FadeOut()
        {
            _fadeOuTimer.Enabled = true;
        }

        private void StopFadeout()
        {
            //终止fadeout
            if (_fadeOuTimer.Enabled)
            {
                lock (_fadeOuTimer)
                {
                    if (_fadeOuTimer.Enabled)
                    {
                        _fadeOuTimer.Enabled = false;
                        this.EndView();
                    }
                }
            }
        }

        private void OnFadeOutTimerElapsed(object o, ElapsedEventArgs e)
        {
            lock (_fadeOuTimer)
            {
                if (!_fadeOuTimer.Enabled)
                {
                    return;
                }

                Debug.Write("*");

                //利用溢出来判断是否小于_fadeOutTo了
                var before = _canvasOpacity;
                _canvasOpacity -= FadeOutDelta;

                if (before < _canvasOpacity)
                {
                    this.EndView();
                    _fadeOuTimer.Enabled = false;
                }
                else
                {
                    _canvasWindow.SetDiBitmap(
                        _canvasBuf,
                        Rectangle.Ceiling(_labelRect),
                        _canvasOpacity);
                    _canvasWindow.SetDiBitmap(
                        _canvasBuf,
                        Rectangle.Ceiling(_gPathDirty.GetBounds()),
                        _canvasOpacity);
                }
            }
        }

        #endregion
    }
}
