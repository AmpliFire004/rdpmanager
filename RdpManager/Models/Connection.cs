using System;

namespace RdpManager.Models
{
    public class Connection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty; // hostname or IP
        public int? Port { get; set; } // optional
        public string? Domain { get; set; }
        public string? Username { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        // (No protocol-specific fields here; only RDP supported)
    }
}
