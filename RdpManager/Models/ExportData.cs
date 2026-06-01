using System;
using System.Collections.Generic;

namespace RdpManager.Models
{
    public class ExportData
    {
        public List<Connection> Connections { get; set; } = new();
        public List<string> Tabs { get; set; } = new() { "General" };
        public string SelectedTab { get; set; } = "General";
    }
}