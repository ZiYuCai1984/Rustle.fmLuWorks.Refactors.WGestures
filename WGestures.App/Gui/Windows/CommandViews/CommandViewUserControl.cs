using System.Windows.Forms;
using WGestures.Core.Commands;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class CommandViewUserControl : UserControl, ICommandView
    {
        public delegate void CommandValueChangedEventHandler(AbstractCommand command);

        public CommandViewUserControl()
        {
            this.InitializeComponent();
        }

        public virtual AbstractCommand Command { get; set; }

        public event CommandValueChangedEventHandler CommandValueChanged;

        protected virtual void OnCommandValueChanged()
        {
            if (this.CommandValueChanged != null)
            {
                this.CommandValueChanged(this.Command);
            }
        }
    }
}
