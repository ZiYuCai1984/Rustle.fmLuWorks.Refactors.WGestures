using System;
using System.Windows.Forms;

namespace WGestures.Core.Commands.Impl
{
    public partial class SearchBox : Form
    {
        public SearchBox()
        {
            this.InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        private void SearchBox_Shown(object sender, EventArgs e)
        {
            this.Activate();
        }

        public void SetSearchText(string txt)
        {
            txt_content.Text = txt;
            txt_content.SelectAll();
        }

        private void SearchBox_Deactivate(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
