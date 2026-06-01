using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RdpManager.Models;

namespace RdpManager
{
    internal sealed class RdpSessionForm : Form
    {
        private readonly RdpActiveXHost _rdpHost;
        private readonly Connection _connection;
        private readonly Action? _fallbackLaunch;
        private readonly Panel _hostPanel;

        public RdpSessionForm(Connection connection, Action? fallbackLaunch)
        {
            _connection = connection;
            _fallbackLaunch = fallbackLaunch;

            Text = $"rdpmanager - connecting to {connection.Address}";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;

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
                    if (appIcon != null)
                    {
                        Icon = appIcon;
                    }
                }
            }
            catch
            {
            }

            _hostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            _rdpHost = new RdpActiveXHost
            {
                Dock = DockStyle.Fill
            };

            _hostPanel.Controls.Add(_rdpHost);
            Controls.Add(_hostPanel);

            Shown += RdpSessionForm_Shown;
            FormClosed += RdpSessionForm_FormClosed;
            _hostPanel.SizeChanged += HostPanel_SizeChanged;
            ResizeEnd += RdpSessionForm_ResizeEnd;
        }

        private void RdpSessionForm_Shown(object? sender, EventArgs e)
        {
            try
            {
                _rdpHost.Configure(_connection, _hostPanel.ClientSize.Width, _hostPanel.ClientSize.Height);
                _rdpHost.ConnectSession();
                Text = $"rdpmanager - connected to {_connection.Address}";
            }
            catch (Exception ex)
            {
                Text = $"rdpmanager - connection failed to {_connection.Address}";
                MessageBox.Show(this, ex.Message, "Embedded RDP launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                _fallbackLaunch?.Invoke();
            }
        }

        private void HostPanel_SizeChanged(object? sender, EventArgs e)
        {
            try
            {
                _rdpHost.UpdateDesktopSize(_hostPanel.ClientSize.Width, _hostPanel.ClientSize.Height, reconnect: false);
            }
            catch
            {
            }
        }

        private void RdpSessionForm_ResizeEnd(object? sender, EventArgs e)
        {
            try
            {
                _rdpHost.UpdateDesktopSize(_hostPanel.ClientSize.Width, _hostPanel.ClientSize.Height, reconnect: true);
            }
            catch
            {
            }
        }

        private void RdpSessionForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                _rdpHost.DisconnectSession();
            }
            catch
            {
            }
        }
    }
}
