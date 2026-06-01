using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RdpManager.Models
{
    public class UserSettings
    {
        public string SortField { get; set; } = "Name"; // or "Host"
        public bool SortAsc { get; set; } = true;
        public List<string> QuickConnectHistory { get; set; } = new();
        // Quick connect default settings persisted for user convenience
        public string? QuickConnectUsername { get; set; }
        // Resolution defaults are runtime-only; do not persist in settings.
        [JsonIgnore]
        public int? QuickConnectScreenWidth { get; set; }
        [JsonIgnore]
        public int? QuickConnectScreenHeight { get; set; }
        // Tab management
        public List<string> Tabs { get; set; } = new() { "General" };
        public string SelectedTab { get; set; } = "General";
        // Persisted column widths and layout preference
        public int? ColumnWidthName { get; set; }
        public int? ColumnWidthHost { get; set; }
        public int? ColumnWidthDescription { get; set; }
        public bool AutoLayoutColumns { get; set; } = true;
    }
}
