using System;
using System.Windows.Forms;

namespace WGestures.App.Gui.Windows
{
    public partial class ErrorForm : Form
    {
        public ErrorForm()
        {
            this.InitializeComponent();


            /*tb_Detail.MouseEnter += (sender, args) =>
            {
                tb_Detail.Focus();
                tb_Detail.SelectAll();
            };

            tb_mail.MouseEnter += (sender, args) =>
            {
                tb_mail.Focus();
                tb_mail.SelectAll();
            };*/
        }

        public string ErrorText
        {
            get => tb_Detail.Text;
            set => tb_Detail.Text = GetProductInfo() + value;
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            this.Close();
            Environment.Exit(1);
        }

        private static string GetProductInfo()
        {
            return "WGestures Version:"
                   + Application.ProductVersion
                   + "\r\nOS:"
                   + Environment.OSVersion
                   + "\r\nAppPath:"
                   + Application.ExecutablePath
                   + "\r\n=======================\r\n";
        }
    }
}
