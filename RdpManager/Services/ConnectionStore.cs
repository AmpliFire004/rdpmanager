using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RdpManager.Models;

namespace RdpManager.Services
{
    public class ConnectionStore
    {
        private readonly string _filePath;

        public ConnectionStore()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RdpManager");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _filePath = Path.Combine(dir, "connections.json");
        }

        public List<Connection> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<Connection>();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Connection>>(json) ?? new List<Connection>();
            }
            catch
            {
                return new List<Connection>();
            }
        }

        public void Save(List<Connection> connections)
        {
            var json = JsonSerializer.Serialize(connections, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}

