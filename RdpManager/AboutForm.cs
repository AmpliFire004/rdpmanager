using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;


namespace RdpManager
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About RdpManager";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Width = 420;
            Height = 180;

            // Icon (try embedded resource first)
            try
            {
                using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RdpManager.Assets.AppIcon.ico");
                if (iconStream != null)
                {
                    Icon = new Icon(iconStream);
                }
                else
                {
                    var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (appIcon != null) Icon = appIcon;
                }
            }
            catch { }

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount = 3;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            table.Padding = new Padding(12);

            var title = new Label();
            title.Text = "RdpManager";
            title.Font = new Font(title.Font.FontFamily, 12, FontStyle.Bold);
            title.AutoSize = true;

            var versionLabel = new Label();
            versionLabel.AutoSize = true;
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

                // Try assembly attributes first (InformationalVersion / FileVersion)
                var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                var fileAttrVer = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

                string? productVersion = null;
                string? fileVersion = null;

                // Try to locate the running executable path. For single-file published apps
                // `Assembly.Location` may be empty; use the process main module filename first.
                try
                {
                    string? exePath = null;
                    try
                    {
                        exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    }
                    catch { }

                    if (string.IsNullOrEmpty(exePath))
                    {
                        exePath = asm.Location;
                    }

                    if (string.IsNullOrEmpty(exePath))
                    {
                        exePath = AppContext.BaseDirectory; // last resort
                    }

                    if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(exePath);
                        productVersion = fvi?.ProductVersion;
                        fileVersion = fvi?.FileVersion;
                    }
                }
                catch { }

                // Prefer informational/product version from attributes if present
                productVersion = infoVer ?? productVersion ?? asm.GetName().Version?.ToString();
                fileVersion = fileAttrVer ?? fileVersion;

                if (string.IsNullOrEmpty(productVersion)) productVersion = "unknown";
                // Prefer the file version for a cleaner display; fall back to product/ informational version.
                var displayVersion = !string.IsNullOrEmpty(fileVersion) ? fileVersion : productVersion;
                versionLabel.Text = $"Version: {displayVersion}";
            }
            catch
            {
                versionLabel.Text = "Version: unknown";
            }

            var link = new LinkLabel();
            link.Text = "https://github.com/AmpliFire004/rdpmanager";
            link.AutoSize = true;
            link.LinkClicked += (s, e) =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "https://github.com/AmpliFire004/rdpmanager",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch { }
            };

            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Padding = new Padding(0);
            buttonPanel.AutoSize = true;

            var ok = new Button();
            ok.Text = "OK";
            ok.AutoSize = true;
            ok.Click += (s, e) => Close();
            buttonPanel.Controls.Add(ok);

            table.Controls.Add(title, 0, 0);
            table.Controls.Add(versionLabel, 0, 1);
            table.Controls.Add(link, 0, 2);

            Controls.Add(table);
            Controls.Add(buttonPanel);
        }
    }
}
