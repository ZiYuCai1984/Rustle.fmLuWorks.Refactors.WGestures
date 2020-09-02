using System;
using System.Diagnostics;
using System.Windows.Forms;
using WGestures.App.Properties;

namespace WGestures.App
{
    public partial class Warn360 : Form
    {
        public Warn360()
        {
            this.InitializeComponent();
            this.Icon = Resources.icon;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://tieba.baidu.com/p/3275239932");
        }

        private void Warn360_Load(object sender, EventArgs e)
        {
            tb_wgPath.Text = Application.ExecutablePath;
            this.Activate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
