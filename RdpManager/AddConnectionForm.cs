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
                chkUsePort.Checked = true; // enables numPort via handler
                try { numPort.Value = Math.Min(Math.Max(existing.Port.Value, (int)numPort.Minimum), (int)numPort.Maximum); } catch { }
            }

            txtDomain.Text = existing.Domain ?? string.Empty;
            txtUsername.Text = existing.Username ?? string.Empty;

            if (existing.ScreenWidth.HasValue && existing.ScreenHeight.HasValue)
            {
                chkCustomRes.Checked = true; // enables width/height via handler
                try { numWidth.Value = Math.Min(Math.Max(existing.ScreenWidth.Value, (int)numWidth.Minimum), (int)numWidth.Maximum); } catch { }
                try { numHeight.Value = Math.Min(Math.Max(existing.ScreenHeight.Value, (int)numHeight.Minimum), (int)numHeight.Maximum); } catch { }
            }
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

            int? port = numPort.Enabled ? (int?)Convert.ToInt32(numPort.Value) : null;
            var domain = string.IsNullOrWhiteSpace(txtDomain.Text) ? null : txtDomain.Text.Trim();
            var username = string.IsNullOrWhiteSpace(txtUsername.Text) ? null : txtUsername.Text.Trim();
            int? width = (chkCustomRes.Checked && numWidth.Enabled) ? (int?)Convert.ToInt32(numWidth.Value) : null;
            int? height = (chkCustomRes.Checked && numHeight.Enabled) ? (int?)Convert.ToInt32(numHeight.Value) : null;

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
