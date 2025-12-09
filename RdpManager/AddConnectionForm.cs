using System;
using System.Windows.Forms;
using RdpManager.Models;

namespace RdpManager
{
    public partial class AddConnectionForm : Form
    {
        public Connection? NewConnection { get; private set; }

        public AddConnectionForm()
        {
            InitializeComponent();
            try { numPort.Value = 3389; } catch { }
        }

        public AddConnectionForm(Connection existing)
        {
            InitializeComponent();
            this.Text = "Edit Connection";

            // Prefill fields from existing connection
            txtName.Text = existing.Name;
            txtAddress.Text = existing.Address;

            if (existing.Port.HasValue)
            {
                try { numPort.Value = Math.Min(Math.Max(existing.Port.Value, (int)numPort.Minimum), (int)numPort.Maximum); } catch { }
            }

            txtDomain.Text = existing.Domain ?? string.Empty;
            txtUsername.Text = existing.Username ?? string.Empty;

            if (existing.ScreenWidth.HasValue && existing.ScreenHeight.HasValue)
            {
                // Try to select a matching resolution preset, otherwise add it
                var sel = $"{existing.ScreenWidth.Value}x{existing.ScreenHeight.Value}";
                var idx = cbResolution.Items.IndexOf(sel);
                if (idx >= 0) cbResolution.SelectedIndex = idx;
                else
                {
                    // Insert custom resolution after Fullscreen
                    cbResolution.Items.Insert(1, sel);
                    cbResolution.SelectedIndex = 1;
                }
            }
            else
            {
                // keep default Fullscreen
                try { cbResolution.SelectedIndex = 0; } catch { }
            }

            // no SSH key field in RDP-only build
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            var name = txtName.Text.Trim();
            var address = txtAddress.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show(this, "Address is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int? port = (int?)Convert.ToInt32(numPort.Value);
            var domain = string.IsNullOrWhiteSpace(txtDomain.Text) ? null : txtDomain.Text.Trim();
            var username = string.IsNullOrWhiteSpace(txtUsername.Text) ? null : txtUsername.Text.Trim();
            int? width = null;
            int? height = null;
            try
            {
                // Use selected item or the typed text so custom resolutions are supported
                var sel = cbResolution.SelectedItem?.ToString() ?? cbResolution.Text ?? "Fullscreen";
                if (!string.Equals(sel, "Fullscreen", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = sel.Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                    {
                        width = w;
                        height = h;
                    }
                }
            }
            catch { }
            NewConnection = new Connection
            {
                Name = name,
                Address = address,
                Port = port,
                Domain = domain,
                Username = username,
                ScreenWidth = width,
                ScreenHeight = height
                
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
