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
    }
}
