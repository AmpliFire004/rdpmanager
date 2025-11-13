using System.Windows.Forms;

namespace RdpManager
{
    partial class AddConnectionForm
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtName;
        private TextBox txtAddress;
        private NumericUpDown numPort;
        private CheckBox chkUsePort;
        private TextBox txtDomain;
        private TextBox txtUsername;
        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private CheckBox chkCustomRes;
        private Button btnOk;
        private Button btnCancel;
        private Label lblName;
        private Label lblAddress;
        private Label lblPort;
        private Label lblDomain;
        private Label lblUsername;
        private Label lblResolution;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.Text = "Add Connection";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 460;
            this.Height = 360;

            lblName = new Label { Left = 16, Top = 20, Width = 100, Text = "Name:" };
            txtName = new TextBox { Left = 120, Top = 16, Width = 260 };

            lblAddress = new Label { Left = 16, Top = 60, Width = 100, Text = "Address:" };
            txtAddress = new TextBox { Left = 120, Top = 56, Width = 260, PlaceholderText = "hostname or IP" };

            lblPort = new Label { Left = 16, Top = 100, Width = 100, Text = "Port (opt):" };
            numPort = new NumericUpDown { Left = 120, Top = 96, Width = 100, Minimum = 0, Maximum = 65535, Value = 3389 };
            chkUsePort = new CheckBox { Left = 230, Top = 98, Text = "Use custom port" };
            chkUsePort.CheckedChanged += (s, e) => numPort.Enabled = chkUsePort.Checked;
            numPort.Enabled = false;

            lblDomain = new Label { Left = 16, Top = 140, Width = 100, Text = "Domain (opt):" };
            txtDomain = new TextBox { Left = 120, Top = 136, Width = 120 };

            lblUsername = new Label { Left = 248, Top = 140, AutoSize = true, Text = "User (opt):" };
            txtUsername = new TextBox { Left = 320, Top = 136, Width = 120 };

            lblResolution = new Label { Left = 16, Top = 180, Width = 100, Text = "Resolution:" };
            chkCustomRes = new CheckBox { Left = 120, Top = 178, Width = 140, Text = "Use custom size" };
            numWidth = new NumericUpDown { Left = 260, Top = 176, Width = 80, Minimum = 640, Maximum = 10000, Value = 1920 };
            var lblX = new Label { Left = 345, Top = 180, Width = 10, Text = "x" };
            numHeight = new NumericUpDown { Left = 360, Top = 176, Width = 80, Minimum = 480, Maximum = 10000, Value = 1080 };
            chkCustomRes.CheckedChanged += (s, e) =>
            {
                numWidth.Enabled = chkCustomRes.Checked;
                numHeight.Enabled = chkCustomRes.Checked;
            };
            numWidth.Enabled = false;
            numHeight.Enabled = false;

            btnOk = new Button { Text = "OK", Left = 280, Width = 75, Top = 240, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Left = 365, Width = 75, Top = 240, DialogResult = DialogResult.Cancel };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblAddress);
            this.Controls.Add(txtAddress);
            this.Controls.Add(lblPort);
            this.Controls.Add(numPort);
            this.Controls.Add(chkUsePort);
            this.Controls.Add(lblDomain);
            this.Controls.Add(txtDomain);
            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblResolution);
            this.Controls.Add(chkCustomRes);
            this.Controls.Add(numWidth);
            this.Controls.Add(numHeight);
            this.Controls.Add(lblX);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
