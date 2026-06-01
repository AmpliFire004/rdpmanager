using System;
using System.Text.Json.Serialization;

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
        // Resolution is runtime-only; do not persist in connection storage.
        [JsonIgnore]
        public int? ScreenWidth { get; set; }
        [JsonIgnore]
        public int? ScreenHeight { get; set; }
        public string TabName { get; set; } = "General"; // Default tab name
        public string? Description { get; set; }
        // (No protocol-specific fields here; only RDP supported)
    }
}
