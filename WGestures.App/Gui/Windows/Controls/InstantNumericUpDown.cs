using System;
using System.Windows.Forms;

namespace WGestures.App.Gui.Windows.Controls
{
    internal class InstantNumericUpDown : NumericUpDown
    {
        protected override void OnTextBoxTextChanged(object source, EventArgs e)
        {
            base.OnTextBoxTextChanged(source, e);
            this.ParseEditText();
        }
    }
}
