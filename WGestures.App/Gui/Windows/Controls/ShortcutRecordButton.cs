using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WindowsInput.Native;
using WGestures.Common.OsSpecific.Windows;

namespace WGestures.App.Gui.Windows.Controls
{
    internal class ShortcutRecordButton : Button
    {
        private static readonly VirtualKeyCode[] modifierKeys =
        {
            VirtualKeyCode.CONTROL, VirtualKeyCode.LCONTROL, VirtualKeyCode.RCONTROL,
            VirtualKeyCode.MENU, VirtualKeyCode.LMENU, VirtualKeyCode.RMENU,
            VirtualKeyCode.SHIFT, VirtualKeyCode.LSHIFT, VirtualKeyCode.RSHIFT,
            VirtualKeyCode.RWIN, VirtualKeyCode.LWIN
        };

        private readonly List<VirtualKeyCode> _keys = new List<VirtualKeyCode>();
        private readonly List<VirtualKeyCode> _modifiers = new List<VirtualKeyCode>();
        private readonly HashSet<VirtualKeyCode> _pressedKeys = new HashSet<VirtualKeyCode>();

        private bool _isRecording;

        //避免委托被回收
        private KeyEventHandler _keyboardHookOnKeyDown;
        private KeyEventHandler _keyboardhookOnKeyUp;

        private int _lastKey = -1;
        private string _oldText;

        private GlobalKeyboardHook hook = new GlobalKeyboardHook();


        public ShortcutRecordButton()
        {
            _keyboardHookOnKeyDown = this.KeyboardHookOnKeyDown;
            _keyboardhookOnKeyUp = this.KeyboardHookOnKeyUp;

            hook.KeyDown += _keyboardHookOnKeyDown;
            hook.KeyUp += _keyboardhookOnKeyUp;

            this.Click += this.btn_recordHotkey_Click;
        }

        public event EventHandler BeginRecord;
        public event EventHandler<ShortcutRecordEventArgs> EndRecord;

        private void btn_recordHotkey_Click(object sender, EventArgs e)
        {
            if (!_isRecording)
            {
                this._StartRecord();
            }
            else
            {
                this._EndRecord();
            }
        }

        private void KeyboardHookOnKeyDown(object sender, KeyEventArgs args)
        {
            args.Handled = true;

            if (!Enum.IsDefined(typeof(VirtualKeyCode), args.KeyValue))
            {
                return;
            }

            var key = (VirtualKeyCode) args.KeyValue;
            if (!_pressedKeys.Add(key))
            {
                return;
            }

            if (modifierKeys.Contains(key) && !_modifiers.Contains(key))
            {
                _modifiers.Add(key);
            }
            else if (!_keys.Contains(key))
            {
                _keys.Add(key);
            }
        }

        private void KeyboardHookOnKeyUp(object sender, KeyEventArgs args)
        {
            args.Handled = true;
            if (args.KeyValue == _lastKey)
            {
                _lastKey = -1;
            }

            if (Enum.IsDefined(typeof(VirtualKeyCode), args.KeyValue))
            {
                var key = (VirtualKeyCode) args.KeyValue;

                if (_modifiers.Contains(key) || _keys.Contains(key))
                {
                    _pressedKeys.Remove(key);
                }
            }

            if (_pressedKeys.Count == 0)
            {
                this.PerformClick();
            }
        }

        private void _StartRecord()
        {
            _isRecording = true;
            _lastKey = -1;
            _pressedKeys.Clear();

            _modifiers.Clear();
            _keys.Clear();

            this.BackColor = Color.OrangeRed;
            this.ForeColor = Color.White;
            _oldText = this.Text;
            this.Text = "...";

            hook.hook();

            if (this.BeginRecord != null)
            {
                this.BeginRecord(this, EventArgs.Empty);
            }
        }

        private void _EndRecord()
        {
            hook.unhook();

            this.BackColor = SystemColors.Control;
            this.ForeColor = Color.Black;
            this.Text = _oldText;

            _isRecording = false;

            if (this.EndRecord != null)
            {
                this.EndRecord(
                    this,
                    new ShortcutRecordEventArgs
                        {Keys = _keys.ToList(), Modifiers = _modifiers.ToList()});
            }
        }

        public static string HotKeyToString(
            IList<VirtualKeyCode> modifiers, IList<VirtualKeyCode> keys)
        {
            if (keys.Count != 0 || modifiers.Count != 0)
            {
                var sb = new StringBuilder(32);
                foreach (var k in modifiers)
                {
                    var str = "";
                    switch (k)
                    {
                        case VirtualKeyCode.MENU:
                        case VirtualKeyCode.RMENU:
                        case VirtualKeyCode.LMENU:
                            str = "Alt";
                            break;
                        case VirtualKeyCode.LCONTROL:
                        case VirtualKeyCode.RCONTROL:
                        case VirtualKeyCode.CONTROL:
                            str = "Ctrl";
                            break;
                        case VirtualKeyCode.RWIN:
                        case VirtualKeyCode.LWIN:
                            str = "Win";
                            break;
                        case VirtualKeyCode.SHIFT:
                        case VirtualKeyCode.LSHIFT:
                        case VirtualKeyCode.RSHIFT:
                            str = "Shift";
                            break;
                        default:
                            str = k.ToString();
                            break;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append('-');
                    }

                    sb.Append(str);
                }

                if (sb.Length > 0)
                {
                    sb.Append(" + ");
                }

                foreach (var k in keys)
                {
                    var str = k.ToString();
                    if (str.StartsWith("VK_"))
                    {
                        str = str.Substring(3);
                    }

                    sb.Append(str);
                    sb.Append(" + ");
                }


                sb.Remove(sb.Length - 3, 3);
                return sb.ToString();
            }

            return "";
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hook.Dispose();
                hook = null;

                _keyboardHookOnKeyDown = null;
                _keyboardhookOnKeyUp = null;
            }

            base.Dispose(disposing);
        }

        internal class ShortcutRecordEventArgs : EventArgs
        {
            public IList<VirtualKeyCode> Modifiers { get; set; }
            public IList<VirtualKeyCode> Keys { get; set; }
        }
    }
}
