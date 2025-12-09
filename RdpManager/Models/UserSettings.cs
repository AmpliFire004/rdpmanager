using System;
using System.Collections.Generic;

namespace RdpManager.Models
{
    public class UserSettings
    {
        public bool IsListView { get; set; } = true;
        public string SortField { get; set; } = "Name"; // or "Host"
        public bool SortAsc { get; set; } = true;
        public List<string> QuickConnectHistory { get; set; } = new();
        // Quick connect default settings persisted for user convenience
        public string? QuickConnectUsername { get; set; }
        public int? QuickConnectScreenWidth { get; set; }
        public int? QuickConnectScreenHeight { get; set; }
    }
}
