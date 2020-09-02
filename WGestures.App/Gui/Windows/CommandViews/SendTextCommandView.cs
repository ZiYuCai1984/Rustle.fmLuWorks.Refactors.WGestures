using System;
using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class SendTextCommandView : CommandViewUserControl
    {
        private SendTextCommand _command;

        public SendTextCommandView()
        {
            this.InitializeComponent();
        }

        public override AbstractCommand Command
        {
            get => _command;
            set
            {
                _command = (SendTextCommand) value;
                txt_text.Text = _command.Text;
            }
        }

        private void txt_text_TextChanged(object sender, EventArgs e)
        {
            _command.Text = txt_text.Text;
        }
    }
}
