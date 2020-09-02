using System;
using System.IO;
using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class CmdCommandView : CommandViewUserControl
    {
        private static readonly string explorerPath =
            Environment.GetEnvironmentVariable("windir")
            + Path.DirectorySeparatorChar
            + "explorer.exe";

        private CmdCommand _command;


        public CmdCommandView()
        {
            this.InitializeComponent();
        }

        public override AbstractCommand Command
        {
            get => _command;
            set
            {
                _command = (CmdCommand) value;
                check_ShowWindow.Checked = _command.ShowWindow;
                check_setWorkingDir.Checked = _command.AutoSetWorkingDir;
                txt_CmdLine.Text = _command.Code ?? "echo Hello";
            }
        }

        private void check_ShowWindow_CheckedChanged(object sender, EventArgs e)
        {
            _command.ShowWindow = check_ShowWindow.Checked;
        }

        private void txt_CmdLine_TextChanged(object sender, EventArgs e)
        {
            _command.Code = txt_CmdLine.Text;

            this.OnCommandValueChanged();
        }


        private void check_setWorkingDir_CheckedChanged(object sender, EventArgs e)
        {
            _command.AutoSetWorkingDir = check_setWorkingDir.Checked;
        }
    }
}
