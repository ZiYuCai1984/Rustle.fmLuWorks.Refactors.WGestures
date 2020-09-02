using System;
using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class GotoUrlCommandView : CommandViewUserControl
    {
        private GotoUrlCommand _command;

        public GotoUrlCommandView()
        {
            this.InitializeComponent();
        }

        public override AbstractCommand Command
        {
            get => _command;
            set
            {
                _command = (GotoUrlCommand) value;
                tb_url.Text = _command.Url ?? "";
            }
        }

        private void tb_url_TextChanged(object sender, EventArgs e)
        {
            _command.Url = tb_url.Text;
            this.OnCommandValueChanged();
        }
    }
}
