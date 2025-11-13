using System;
using System.IO;
using System.Text.Json;
using RdpManager.Models;

namespace RdpManager.Services
{
    public class SettingsStore
    {
        private readonly string _filePath;

        public SettingsStore()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RdpManager");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _filePath = Path.Combine(dir, "settings.json");
        }

        public UserSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new UserSettings();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public void Save(UserSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}

