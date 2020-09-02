using System;
using System.Windows.Forms;
using WGestures.App.Migrate;
using WGestures.App.Properties;

namespace WGestures.App.Gui.Windows
{
    public partial class ImportForm : Form
    {
        private ConfigAndGestures _configAndGestures;
        private ImportOption _configImportOption = ImportOption.None;

        private string _filePath;

        private ImportOption _gesturesImportOption = ImportOption.None;

        public ImportForm()
        {
            this.InitializeComponent();
            this.Icon = Resources.icon;

            combo_importGesturesOption.SelectedIndex = 0;
        }

        internal event EventHandler<ImportEventArgs> Import;

        private void btn_selectWgb_Click(object sender, EventArgs e)
        {
            var result = openFile_wgb.ShowDialog();
            if (result == DialogResult.OK)
            {
                _filePath = openFile_wgb.FileName;
                this.HideError();


                try
                {
                    _configAndGestures = MigrateService.Import(_filePath);
                }
                catch (MigrateException ex)
                {
                    this.ShowError(ex.Message);
                    return;
                }

                var containsGestures = _configAndGestures.GestureIntentStore != null;
                var containsConfig = _configAndGestures.Config != null;

                check_importGestures.Checked = containsGestures;
                check_importGestures.Enabled = containsGestures;

                check_importConfig.Checked = containsConfig;
                check_importConfig.Enabled = containsConfig;

                txt_filePath.Text = _filePath;
                group_importOptions.Visible = true;
            }
        }

        private void ImportForm_Load(object sender, EventArgs e)
        {
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void check_importGestures_CheckedChanged(object sender, EventArgs e)
        {
            combo_importGesturesOption.Enabled = check_importGestures.Checked;
            if (!check_importGestures.Checked)
            {
                _gesturesImportOption = ImportOption.None;
            }
            else
            {
                _gesturesImportOption = combo_importGesturesOption.SelectedIndex == 0
                    ? ImportOption.Merge
                    : ImportOption.Replace;
            }

            this.Validate();
        }

        private void check_importConfig_CheckedChanged(object sender, EventArgs e)
        {
            if (!check_importConfig.Checked)
            {
                _configImportOption = ImportOption.None;
            }
            else
            {
                _configImportOption = ImportOption.Merge;
            }

            this.Validate();
        }


        private void combo_importGesturesOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            _gesturesImportOption = combo_importGesturesOption.SelectedIndex == 0
                ? ImportOption.Merge
                : ImportOption.Replace;
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            this.HideError();
            this.OnImport();
        }

        private void OnImport()
        {
            if (this.Import != null)
            {
                var args = new ImportEventArgs(
                    _configAndGestures,
                    _gesturesImportOption,
                    _configImportOption);
                this.Import(this, args);

                if (args.Success)
                {
                    MessageBox.Show(
                        "导入成功！",
                        "导入完成",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    if (args.ErrorMessage != null)
                    {
                        this.ShowError(args.ErrorMessage);
                    }
                }
            }
        }


        private void ShowError(string msg)
        {
            lb_errMsg.Text = msg;
            flowAlert.Visible = true;
        }

        private void HideError()
        {
            flowAlert.Visible = false;
        }

        private new void Validate()
        {
            if (!(check_importConfig.Checked || check_importGestures.Checked))
            {
                btnOk.Enabled = false;
            }
            else
            {
                btnOk.Enabled = true;
            }
        }

        internal class ImportEventArgs : EventArgs
        {
            public ImportEventArgs(
                ConfigAndGestures confAndGest, ImportOption gestImpOpt, ImportOption confImpOpt)
            {
                this.Success = true;

                this.ConfigAndGestures = confAndGest;
                this.GesturesImportOption = gestImpOpt;
                this.ConfigImportOption = confImpOpt;
            }

            public ConfigAndGestures ConfigAndGestures { get; }
            public ImportOption GesturesImportOption { get; }
            public ImportOption ConfigImportOption { get; }

            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        internal enum ImportOption
        {
            None,
            Replace,
            Merge
        }
    }
}
