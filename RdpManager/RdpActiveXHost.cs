using System;
using System.Windows.Forms;
using RdpManager.Models;

namespace RdpManager
{
    internal sealed class RdpActiveXHost : AxHost
    {
        private const string MsTscAxClsid = "{8B918B82-7985-4C24-89DF-C33AD2BBFBCD}";
        private int _lastWidth;
        private int _lastHeight;

        public RdpActiveXHost() : base(MsTscAxClsid)
        {
        }

        public void Configure(Connection connection, int windowWidth, int windowHeight)
        {
            dynamic ocx = GetOcx();
            ocx.Server = connection.Address;

            if (connection.Port.HasValue && connection.Port.Value > 0)
            {
                ocx.AdvancedSettings9.RDPPort = connection.Port.Value;
            }

            if (!string.IsNullOrWhiteSpace(connection.Username))
            {
                ocx.UserName = connection.Username;
            }

            if (!string.IsNullOrWhiteSpace(connection.Domain))
            {
                ocx.Domain = connection.Domain;
            }

            ApplyDesktopSize(windowWidth, windowHeight);

            try
            {
                ocx.FullScreen = false;
            }
            catch
            {
            }

            try
            {
                ocx.AdvancedSettings9.DisplayConnectionBar = true;
                ocx.AdvancedSettings9.GrabFocusOnConnect = true;
                ocx.AdvancedSettings9.NegotiateSecurityLayer = true;
                ocx.AdvancedSettings9.EnableCredSspSupport = true;
                ocx.AdvancedSettings9.SmartSizing = true;
            }
            catch
            {
            }
        }

        public void ConnectSession()
        {
            dynamic ocx = GetOcx();
            ocx.Connect();
        }

        public void UpdateDesktopSize(int width, int height, bool reconnect)
        {
            ApplyDesktopSize(width, height);

            try
            {
                dynamic ocx = GetOcx();
                if ((short)ocx.Connected != 0)
                {
                    if (reconnect)
                    {
                        ocx.Reconnect((uint)width, (uint)height);
                    }
                    else
                    {
                        ocx.SyncSessionDisplaySettings();
                    }
                }
            }
            catch
            {
            }
        }

        public void DisconnectSession()
        {
            try
            {
                dynamic ocx = GetOcx();
                ocx.Disconnect();
            }
            catch
            {
            }
        }

        private void ApplyDesktopSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (_lastWidth == width && _lastHeight == height)
            {
                return;
            }

            _lastWidth = width;
            _lastHeight = height;

            try
            {
                dynamic ocx = GetOcx();
                ocx.DesktopWidth = width;
                ocx.DesktopHeight = height;
            }
            catch
            {
            }
        }
    }
}
