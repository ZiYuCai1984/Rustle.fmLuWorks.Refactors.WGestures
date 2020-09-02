using System;
using System.Diagnostics;
using System.Windows.Forms;
using WGestures.Common.Product;

namespace WGestures.App.Gui.Windows
{
    public partial class UpdateInfoForm : Form
    {
        private readonly string _gotoUrl;

        public UpdateInfoForm(string gotoUrl, VersionInfo versionInfo)
        {
            this.InitializeComponent();

            _gotoUrl = gotoUrl;
            lb_newVersion.Text = versionInfo.Version;
            tb_whatsNew.Text = versionInfo.WhatsNew;

            lb_currentVersion.Text = Application.ProductVersion;
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnk_gotoUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var startInfo = new ProcessStartInfo("explorer.exe", _gotoUrl);
            using (Process.Start(startInfo))
            {
            }

            ;

            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Keys.W == (keyData & Keys.W) && Keys.Control == (keyData & Keys.Control))
            {
                this.Close();

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
