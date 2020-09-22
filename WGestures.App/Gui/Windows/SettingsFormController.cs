using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WGestures.App.Gui.Windows.CommandViews;
using WGestures.App.Migrate;
using WGestures.Common.Annotation;
using WGestures.Common.Config;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Core;
using WGestures.Core.Annotations;
using WGestures.Core.Commands.Impl;
using WGestures.Core.Impl.Windows;
using WGestures.Core.Persistence;
using WGestures.Core.Persistence.Impl;
using WGestures.View.Impl.Windows;

namespace WGestures.App.Gui.Windows
{
    internal class SettingsFormController : IDisposable, INotifyPropertyChanged
    {
        private readonly GlobalHotKeyManager _hotkeyMgr;
        private Form _form;
        private CanvasWindowGestureView _gestureView;
        private JsonGestureIntentStore _intentStore;
        private Win32MousePathTracker2 _pathTracker;

        public SettingsFormController(
            IConfig config, GestureParser parser,
            Win32MousePathTracker2 pathTracker, JsonGestureIntentStore intentStore,
            CanvasWindowGestureView gestureView, GlobalHotKeyManager hotkeyMgr)
        {
            this.Config = config;
            this.GestureParser = parser;
            _pathTracker = pathTracker;
            _intentStore = intentStore;
            _gestureView = gestureView;
            _hotkeyMgr = hotkeyMgr;

            #region 初始化支持的命令和命令视图

            //Add Command Types
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(DoNothingCommand)),
                typeof(DoNothingCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(HotKeyCommand)),
                typeof(HotKeyCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(WebSearchCommand)),
                typeof(WebSearchCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(WindowControlCommand)),
                typeof(WindowControlCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(TaskSwitcherCommand)),
                typeof(TaskSwitcherCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(OpenFileCommand)),
                typeof(OpenFileCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(SendTextCommand)),
                typeof(SendTextCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(GotoUrlCommand)),
                typeof(GotoUrlCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(CmdCommand)),
                typeof(CmdCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(ScriptCommand)),
                typeof(ScriptCommand));

            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(PauseWGesturesCommand)),
                typeof(PauseWGesturesCommand));
            this.SupportedCommands.Add(
                NamedAttribute.GetNameOf(typeof(ChangeAudioVolumeCommand)),
                typeof(ChangeAudioVolumeCommand));

            this.CommandViewFactory.Register<OpenFileCommand, OpenFileCommandView>();
            this.CommandViewFactory.Register<DoNothingCommand, GeneralNoParameterCommandView>();
            this.CommandViewFactory.Register<HotKeyCommand, HotKeyCommandView>();
            this.CommandViewFactory.Register<GotoUrlCommand, GotoUrlCommandView>();
            this.CommandViewFactory
                .Register<PauseWGesturesCommand, GeneralNoParameterCommandView>();
            this.CommandViewFactory.Register<WebSearchCommand, WebSearchCommandView>();
            this.CommandViewFactory.Register<WindowControlCommand, WindowControlCommandView>();
            this.CommandViewFactory.Register<CmdCommand, CmdCommandView>();
            this.CommandViewFactory.Register<SendTextCommand, SendTextCommandView>();
            this.CommandViewFactory.Register<TaskSwitcherCommand, TaskSwitcherCommandView>();
            this.CommandViewFactory.Register<ScriptCommand, ScriptCommandView>();
            this.CommandViewFactory
                .Register<ChangeAudioVolumeCommand, GeneralNoParameterCommandView>();

            #endregion

            #region Hotcorner

            this.SupportedHotCornerCommands.Add(
                NamedAttribute.GetNameOf(typeof(DoNothingCommand)),
                typeof(DoNothingCommand));
            this.SupportedHotCornerCommands.Add(
                NamedAttribute.GetNameOf(typeof(HotKeyCommand)),
                typeof(HotKeyCommand));
            this.SupportedHotCornerCommands.Add(
                NamedAttribute.GetNameOf(typeof(CmdCommand)),
                typeof(CmdCommand));

            this.HotCornerCommandViewFactory
                .Register<DoNothingCommand, GeneralNoParameterCommandView>();
            this.HotCornerCommandViewFactory.Register<HotKeyCommand, HotKeyCommandView>();
            this.HotCornerCommandViewFactory.Register<CmdCommand, CmdCommandView>();

            #endregion

            _form = new SettingsForm(this);
        }

        public bool IsDisposed { get; private set; }


        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            if (this.CommandViewFactory != null)
            {
                this.CommandViewFactory.Dispose();
            }

            this.Config = null;
            this.GestureParser = null;
            _gestureView = null;
            _intentStore = null;
            _pathTracker = null;

            this.SupportedCommands.Clear();
            this.SupportedCommands = null;
            this.CommandViewFactory = null;

            _form.Dispose();
            _form = null;

            //GC.Collect();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     以Modal方式呈现主设置窗口
        /// </summary>
        public void ShowDialog()
        {
            _form.ShowDialog();

            this.Config.Save();
            _intentStore.Save();

            using (var proc = Process.GetCurrentProcess())
            {
                Native.SetProcessWorkingSetSize(proc.Handle, -1, -1);
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

        public void BringToFront()
        {
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
            _form.BringToFront();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        #region tab1 Config Items

        public IConfig Config { get; private set; }

        public GlobalHotKeyManager.HotKey? PauseResumeHotkey
        {
            get => _hotkeyMgr.GetRegisteredHotKeyById(ConfigKeys.PauseResumeHotKey);

            set
            {
                if (value != null)
                {
                    if (this.PauseResumeHotkey != null
                        && value.Value.Equals(this.PauseResumeHotkey.Value))
                    {
                        return;
                    }

                    //use null action here cus we use _hotkeyMgr.HotKeyPreview event in Program class
                    _hotkeyMgr.RegisterHotKey(ConfigKeys.PauseResumeHotKey, value.Value, null);
                }
                else
                {
                    if (this.PauseResumeHotkey == null)
                    {
                        return;
                    }

                    _hotkeyMgr.UnRegisterHotKey(ConfigKeys.PauseResumeHotKey);
                }

                this.Config.Set(
                    ConfigKeys.PauseResumeHotKey,
                    value == null ? new byte[0] : value.Value.ToBytes());

                this.OnPropertyChanged("PauseResumeHotKey");
            }
        }

        /// <summary>
        ///     获取或设置是否开机自动运行
        /// </summary>
        public bool AutoStart
        {
            get
            {
                var config = this.Config.Get<bool>(ConfigKeys.AutoStart);
                return config;
            }
            set
            {
                if (value == this.AutoStart)
                {
                    return;
                }

                try
                {
                    if (value)
                    {
                        AutoStarter.Register(Constants.Identifier, Application.ExecutablePath);
                    }
                    else
                    {
                        AutoStarter.Unregister(Constants.Identifier);
                    }

                    this.Config.Set(ConfigKeys.AutoStart, value);
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }


                this.OnPropertyChanged("AutoStart");
            }
        }

        /// <summary>
        ///     获取或设置是否自动检查更新
        /// </summary>
        public bool AutoCheckForUpdate
        {
            get => this.Config.Get<bool>(ConfigKeys.AutoCheckForUpdate);
            set
            {
                if (value == this.AutoCheckForUpdate)
                {
                    return;
                }

                this.Config.Set(ConfigKeys.AutoCheckForUpdate, value);

                this.OnPropertyChanged("AutoCheckForUpdate");
            }
        }

        /// <summary>
        ///     获取或设置哪一个鼠标按键作为手势键
        /// </summary>
        public GestureTriggerButton PathTrackerTriggerButton
        {
            get => _pathTracker.TriggerButton;

            set
            {
                if (value == this.PathTrackerTriggerButton)
                {
                    return;
                }

                _pathTracker.TriggerButton = value;

                this.Config.Set(
                    ConfigKeys.PathTrackerTriggerButton,
                    (int) _pathTracker.TriggerButton);

                this.OnPropertyChanged("PathTrackerTriggerButton");
            }
        }

        public bool PathTrackerEnableWinKeyGesturing
        {
            get => _pathTracker.EnableWindowsKeyGesturing;
            set
            {
                Debug.WriteLine("Win: " + value);
                if (value == this.PathTrackerEnableWinKeyGesturing)
                {
                    return;
                }

                _pathTracker.EnableWindowsKeyGesturing = value;

                this.Config.Set(ConfigKeys.EnableWindowsKeyGesturing, value);
                this.OnPropertyChanged("PathTrackerEnableWinKeyGesturing");
            }
        }

        public bool GestureParserEnable8DirGesture
        {
            get => this.GestureParser.Enable8DirGesture;
            set
            {
                if (value == this.GestureParser.Enable8DirGesture)
                {
                    return;
                }

                this.GestureParser.Enable8DirGesture = value;
                this.Config.Set(ConfigKeys.GestureParserEnable8DirGesture, value);
                this.OnPropertyChanged("GestureParserEnable8DirGesture");
            }
        }

        /// <summary>
        ///     获取或设置出示有效移动距离
        /// </summary>
        public int PathTrackerInitialValidMove
        {
            get => _pathTracker.InitialValidMove;
            set
            {
                if (value == this.PathTrackerInitialValidMove)
                {
                    return;
                }

                _pathTracker.InitialValidMove = value;
                this.Config.Set(
                    ConfigKeys.PathTrackerInitialValidMove,
                    _pathTracker.InitialValidMove);

                this.OnPropertyChanged("PathTrackerInitialValidMove");
            }
        }

        public bool PathTrackerInitialStayTimeout
        {
            get => _pathTracker.InitialStayTimeout;
            set
            {
                if (value == this.PathTrackerInitialStayTimeout)
                {
                    return;
                }

                _pathTracker.InitialStayTimeout = value;
                this.Config.Set(ConfigKeys.PathTrackerInitialStayTimeout, value);

                this.OnPropertyChanged("PathTrackerInitialStayTimeout");
            }
        }

        public int PathTrackerInitalStayTimeoutMillis
        {
            get => _pathTracker.InitialStayTimeoutMillis;
            set
            {
                if (value == this.PathTrackerStayTimeoutMillis)
                {
                    return;
                }

                _pathTracker.InitialStayTimeoutMillis = value;
                this.Config.Set(ConfigKeys.PathTrackerInitialStayTimeoutMillis, value);

                this.OnPropertyChanged("PathTrackerInitalStayTimeoutMillis");
            }
        }

        public bool PathTrackerDisableInFullScreen
        {
            get => _pathTracker.DisableInFullscreen;
            set
            {
                if (value == this.PathTrackerDisableInFullScreen)
                {
                    return;
                }

                _pathTracker.DisableInFullscreen = value;
                this.Config.Set(ConfigKeys.PathTrackerDisableInFullScreen, value);

                this.OnPropertyChanged("PathTrackerDisableInFullScreen");
            }
        }

        /// <summary>
        ///     获取或设置鼠标是否允许停留超时
        /// </summary>
        public bool PathTrackerStayTimeout
        {
            get => _pathTracker.StayTimeout;
            set
            {
                if (value == this.PathTrackerStayTimeout)
                {
                    return;
                }

                _pathTracker.StayTimeout = value;
                this.Config.Set(ConfigKeys.PathTrackerStayTimeout, _pathTracker.StayTimeout);

                this.OnPropertyChanged("PathTrackerStayTimeout");
            }
        }

        /// <summary>
        ///     获取或设置停留超时的时间（毫秒）
        /// </summary>
        public int PathTrackerStayTimeoutMillis
        {
            get => _pathTracker.StayTimeoutMillis;
            set
            {
                if (value == this.PathTrackerStayTimeoutMillis)
                {
                    return;
                }

                _pathTracker.StayTimeoutMillis = value;
                this.Config.Set(
                    ConfigKeys.PathTrackerStayTimeoutMillis,
                    _pathTracker.StayTimeoutMillis);

                this.OnPropertyChanged("PathTrackerStayTimeoutMillis");
            }
        }

        public bool PathTrackerPreferCursorWindow
        {
            get => _pathTracker.PreferWindowUnderCursorAsTarget;
            set
            {
                if (value == this.PathTrackerPreferCursorWindow)
                {
                    return;
                }

                _pathTracker.PreferWindowUnderCursorAsTarget = value;
                this.Config.Set(ConfigKeys.PathTrackerPreferCursorWindow, value);

                this.OnPropertyChanged("PathTrackerPreferCursorWindow");
            }
        }

        /// <summary>
        ///     获取或设置是否显示手势路径
        /// </summary>
        public bool GestureViewShowPath
        {
            get => _gestureView.ShowPath;
            set
            {
                if (value == this.GestureViewShowPath)
                {
                    return;
                }

                _gestureView.ShowPath = value;
                this.Config.Set(ConfigKeys.GestureViewShowPath, value);

                this.OnPropertyChanged("GestureViewShowPath");
            }
        }

        /// <summary>
        ///     获取或设置是否显示手示意图名称
        /// </summary>
        public bool GestureViewShowCommandName
        {
            get => _gestureView.ShowCommandName;
            set
            {
                if (value == this.GestureViewShowCommandName)
                {
                    return;
                }

                _gestureView.ShowCommandName = value;
                this.Config.Set(ConfigKeys.GestureViewShowCommandName, value);

                this.OnPropertyChanged("GestureViewShowCommandName");
            }
        }

        /// <summary>
        ///     获取或设置是否在手势执行后视图淡出
        /// </summary>
        public bool GestureViewFadeOut
        {
            get => _gestureView.ViewFadeOut;
            set
            {
                if (value == this.GestureViewFadeOut)
                {
                    return;
                }

                _gestureView.ViewFadeOut = value;
                this.Config.Set(ConfigKeys.GestureViewFadeOut, value);

                this.OnPropertyChanged("GestureViewFadeOut");
            }
        }

        /// <summary>
        ///     获取或设置手势被识别时轨迹显示的颜色
        /// </summary>
        public Color GestureViewMainPathColor
        {
            get => _gestureView.PathMainColor;
            set
            {
                if (value == this.GestureViewMainPathColor)
                {
                    return;
                }

                _gestureView.PathMainColor = value;
                this.Config.Set(ConfigKeys.GestureViewMainPathColor, value.ToArgb());

                this.OnPropertyChanged("GestureViewMainPathColor");
            }
        }

        /// <summary>
        ///     获取或设置手势为被识别时轨迹的颜色
        /// </summary>
        public Color GestureViewAlternativePathColor
        {
            get => _gestureView.PathAlternativeColor;
            set
            {
                if (value == this.GestureViewAlternativePathColor)
                {
                    return;
                }

                _gestureView.PathAlternativeColor = value;
                this.Config.Set(ConfigKeys.GestureViewAlternativePathColor, value.ToArgb());

                this.OnPropertyChanged("GestureViewAlternativePathColor");
            }
        }

        public Color GestureViewMiddleBtnMainColor
        {
            get => _gestureView.PathMiddleBtnMainColor;
            set
            {
                if (value == this.GestureViewMiddleBtnMainColor)
                {
                    return;
                }

                _gestureView.PathMiddleBtnMainColor = value;
                this.Config.Set(ConfigKeys.GestureViewMiddleBtnMainColor, value.ToArgb());

                this.OnPropertyChanged("GestureViewMiddleBtnMainColor");
            }
        }

        public Color GestureVieXBtnMainColor
        {
            get => _gestureView.PathXBtnMainColor;
            set
            {
                if (value == this.GestureVieXBtnMainColor)
                {
                    return;
                }

                _gestureView.PathXBtnMainColor = value;
                this.Config.Set(ConfigKeys.GestureViewXBtnPathColor, value.ToArgb());

                this.OnPropertyChanged("GestureVieXBtnMainColor");
            }
        }

        #endregion

        #region migrate methods

        internal void ExportTo(string filePath)
        {
            MigrateService.ExportTo(filePath);
        }

        internal void RestoreDefaultGestures()
        {
            _intentStore.Import(
                MigrateService.Import(AppSettings.DefaultGesturesFilePath).GestureIntentStore,
                true);
            this.OnPropertyChanged("IntentStore");
        }

        internal void Import(
            ConfigAndGestures configAndGestures, bool importConfig, bool importGestures,
            bool mergeGestures)
        {
            //config 必然是合并
            if (importConfig)
            {
                this.Config.Import(configAndGestures.Config);
                this.ReApplyConfig();
            }

            if (importGestures)
            {
                _intentStore.Import(configAndGestures.GestureIntentStore, !mergeGestures);
                this.OnPropertyChanged("IntentStore");
            }
        }

        private void ReApplyConfig()
        {
            this.AutoStart = this.Config.Get(ConfigKeys.AutoStart, this.AutoStart);
            this.AutoCheckForUpdate = this.Config.Get(
                ConfigKeys.AutoCheckForUpdate,
                this.AutoCheckForUpdate);

            //PathTrackerGestureButton = _config.Get<int>(ConfigKeys.PathTrackerGestureButton);
            this.PathTrackerInitialValidMove = this.Config.Get(
                ConfigKeys.PathTrackerInitialValidMove,
                this.PathTrackerInitialValidMove);
            this.PathTrackerInitialStayTimeout = this.Config.Get(
                ConfigKeys.PathTrackerInitialStayTimeout,
                this.PathTrackerInitialStayTimeout);
            this.PathTrackerInitalStayTimeoutMillis = this.Config.Get(
                ConfigKeys.PathTrackerInitialStayTimeoutMillis,
                this.PathTrackerInitalStayTimeoutMillis);
            this.PathTrackerPreferCursorWindow = this.Config.Get(
                ConfigKeys.PathTrackerPreferCursorWindow,
                this.PathTrackerPreferCursorWindow);

            this.PathTrackerDisableInFullScreen = this.Config.Get(
                ConfigKeys.PathTrackerDisableInFullScreen,
                this.PathTrackerDisableInFullScreen);

            this.PathTrackerStayTimeout = this.Config.Get(
                ConfigKeys.PathTrackerStayTimeout,
                this.PathTrackerStayTimeout);
            this.PathTrackerStayTimeoutMillis = this.Config.Get(
                ConfigKeys.PathTrackerStayTimeoutMillis,
                this.PathTrackerStayTimeoutMillis);

            this.GestureViewShowPath = this.Config.Get(
                ConfigKeys.GestureViewShowPath,
                this.GestureViewShowPath);
            this.GestureViewShowCommandName = this.Config.Get(
                ConfigKeys.GestureViewShowCommandName,
                this.GestureViewShowCommandName);
            this.GestureViewFadeOut = this.Config.Get(
                ConfigKeys.GestureViewFadeOut,
                this.GestureViewFadeOut);
            this.GestureViewMainPathColor = Color.FromArgb(
                this.Config.Get(
                    ConfigKeys.GestureViewMainPathColor,
                    this.GestureViewMainPathColor.ToArgb()));
            this.GestureViewAlternativePathColor = Color.FromArgb(
                this.Config.Get(
                    ConfigKeys.GestureViewAlternativePathColor,
                    this.GestureViewAlternativePathColor.ToArgb()));
            this.GestureViewMiddleBtnMainColor = Color.FromArgb(
                this.Config.Get(
                    ConfigKeys.GestureViewMiddleBtnMainColor,
                    this.GestureViewMiddleBtnMainColor.ToArgb()));

            this.GestureParserEnableHotCorners = this.Config.Get(
                ConfigKeys.GestureParserEnableHotCorners,
                this.GestureParser.EnableHotCorners);
            this.GestureParserEnable8DirGesture = this.Config.Get(
                ConfigKeys.GestureParserEnable8DirGesture,
                this.GestureParser.Enable8DirGesture);
        }

        #endregion

        #region tab2 props

        /// <summary>
        ///     获取支持的命令类型字典
        /// </summary>
        public Dictionary<string, Type> SupportedCommands { get; private set; } =
            new Dictionary<string, Type>();

        /// <summary>
        ///     获取命令视图工厂
        /// </summary>
        public ICommandViewFactory<CommandViewUserControl> CommandViewFactory { get; private set; }
            = new CommandViewFactory<CommandViewUserControl>
                {EnableCaching = false};

        /// <summary>
        ///     获取内存中的手示意图存储结构
        /// </summary>
        public IGestureIntentStore IntentStore => _intentStore;

        /// <summary>
        ///     获取手势解析器
        /// </summary>
        public GestureParser GestureParser { get; private set; }

        #endregion

        #region hotcorners & edges

        public Dictionary<string, Type> SupportedHotCornerCommands { get; } =
            new Dictionary<string, Type>();

        public ICommandViewFactory<CommandViewUserControl> HotCornerCommandViewFactory { get; } =
            new CommandViewFactory<CommandViewUserControl>
                {EnableCaching = false};

        public bool GestureParserEnableHotCorners
        {
            get => this.GestureParser.EnableHotCorners;
            set
            {
                if (value == this.GestureParser.EnableHotCorners)
                {
                    return;
                }

                this.GestureParser.EnableHotCorners = value;
                this.Config.Set(ConfigKeys.GestureParserEnableHotCorners, value);
                this.OnPropertyChanged("GestureParserEnableHotCorners");
            }
        }

        public bool GestureParserEnableRubEdges
        {
            get => this.GestureParser.EnableRubEdge;
            set
            {
                if (value == this.GestureParser.EnableRubEdge)
                {
                    return;
                }

                this.GestureParser.EnableRubEdge = value;
                this.Config.Set(ConfigKeys.GestureParserEnableRubEdges, value);
                this.OnPropertyChanged("GestureParserEnableRubEdges");
            }
        }

        #endregion
    }
}
