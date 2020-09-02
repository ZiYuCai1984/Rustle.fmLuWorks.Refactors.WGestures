using System;
using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class WindowControlCommandView : CommandViewUserControl
    {
        private WindowControlCommand _command;


        public WindowControlCommandView()
        {
            this.InitializeComponent();
        }


        public override AbstractCommand Command
        {
            get => _command;
            set
            {
                _command = (WindowControlCommand) value;
                combo_operation.SelectedIndex = (int) _command.ChangeWindowStateTo;
            }
        }

        private void combo_operation_SelectedIndexChanged(object sender, EventArgs e)
        {
            _command.ChangeWindowStateTo =
                (WindowControlCommand.WindowOperation) combo_operation.SelectedIndex;
            this.OnCommandValueChanged();
        }
    }
}
