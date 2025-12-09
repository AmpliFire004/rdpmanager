using System.Drawing;
using System.Windows.Forms;

namespace RdpManager
{
    partial class AddConnectionForm
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtName;
        private TextBox txtAddress;
        private NumericUpDown numPort;
        private TextBox txtDomain;
        private TextBox txtUsername;
        private ComboBox cbResolution;
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
            var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            lblName = new Label { Left = 16, Top = 16, Width = 100, Text = "Name:" };
            txtName = new TextBox { Left = 120, Top = 12, Width = 320, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            lblAddress = new Label { Left = 16, Top = 56, Width = 100, Text = "Address:" };
            txtAddress = new TextBox { Left = 120, Top = 52, Width = 320, PlaceholderText = "hostname or IP", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            lblPort = new Label { Left = 16, Top = 216, Width = 100, Text = "Port:" };
            numPort = new NumericUpDown { Left = 120, Top = 212, Width = 100, Minimum = 1, Maximum = 65535, Value = 3389 };

            lblDomain = new Label { Left = 16, Top = 96, Width = 100, Text = "Domain:" };
            txtDomain = new TextBox { Left = 120, Top = 92, Width = 180, Anchor = AnchorStyles.Top | AnchorStyles.Left };

            // Make the Domain and User fields shorter so they fit nicely.
            // Place the User field under the Domain field to improve layout.
            lblUsername = new Label { Left = 16, Top = 136, Width = 100, Text = "User:" };
            txtUsername = new TextBox { Left = 120, Top = 132, Width = 180, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Move resolution down below the user row so the form feels balanced.
            lblResolution = new Label { Left = 16, Top = 176, Width = 100, Text = "Resolution:" };
            // Allow typing custom resolutions
            cbResolution = new ComboBox { Left = 120, Top = 172, Width = 320, DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var resolutions = new string[]
            {
                "Fullscreen",
                "800x600",
                "1024x768",
                "1280x720",
                "1280x800",
                "1366x768",
                "1440x900",
                "1600x900",
                "1920x1080",
                "2560x1440",
                "3840x2160"
            };
            foreach (var r in resolutions) cbResolution.Items.Add(r);
            cbResolution.SelectedIndex = 0;

            // No protocol selection; only RDP supported here.

            // Add a bit more padding from the right/bottom edges by moving the buttons left and anchoring.
            // Keep buttons anchored to the bottom-right and positioned at the bottom of the form
            btnOk = new Button { Text = "OK", Left = 260, Width = 75, Top = 292, DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnCancel = new Button { Text = "Cancel", Left = 345, Width = 75, Top = 292, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblAddress);
            this.Controls.Add(txtAddress);
            this.Controls.Add(lblPort);
            this.Controls.Add(numPort);
            this.Controls.Add(lblDomain);
            this.Controls.Add(txtDomain);
            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblResolution);
            this.Controls.Add(cbResolution);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
