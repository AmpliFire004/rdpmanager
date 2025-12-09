using System;
using System.Windows.Forms;

namespace RdpManager
{
    public class QuickConnectSettingsForm : Form
    {
        private TextBox txtUsername = null!;
        private ComboBox cbResolution = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;

        public string? Username { get; private set; }
        public int? ScreenWidth { get; private set; }
        public int? ScreenHeight { get; private set; }

        public QuickConnectSettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Quick Connect Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 360;
            this.Height = 180;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblUser = new Label { Text = "Username:", Left = 12, Top = 16, AutoSize = true };
            txtUsername = new TextBox { Left = 100, Top = 12, Width = 220 };

            var lblRes = new Label { Text = "Resolution:", Left = 12, Top = 52, AutoSize = true };
            cbResolution = new ComboBox { Left = 100, Top = 48, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };

            // Common resolution presets (10 options)
            var resolutions = new string[]
            {
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
            cbResolution.Items.Insert(0, "Fullscreen");
            cbResolution.SelectedIndex = 0;

            btnOk = new Button { Text = "OK", Left = 160, Width = 80, Top = 96, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Left = 248, Width = 80, Top = 96, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.Add(lblUser);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblRes);
            this.Controls.Add(cbResolution);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            Username = string.IsNullOrWhiteSpace(txtUsername.Text) ? null : txtUsername.Text.Trim();
            if (cbResolution.SelectedIndex <= 0)
            {
                ScreenWidth = null;
                ScreenHeight = null;
            }
            else
            {
                var sel = cbResolution.SelectedItem?.ToString() ?? string.Empty;
                var parts = sel.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                {
                    ScreenWidth = w;
                    ScreenHeight = h;
                }
                else
                {
                    ScreenWidth = null;
                    ScreenHeight = null;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public void LoadSettings(string? username, int? screenWidth, int? screenHeight)
        {
            txtUsername.Text = username ?? string.Empty;
            if (screenWidth.HasValue && screenHeight.HasValue)
            {
                var s = $"{screenWidth.Value}x{screenHeight.Value}";
                var idx = cbResolution.Items.IndexOf(s);
                if (idx >= 0) cbResolution.SelectedIndex = idx;
                else cbResolution.SelectedIndex = 0;
            }
            else
            {
                cbResolution.SelectedIndex = 0;
            }
        }
    }
}
