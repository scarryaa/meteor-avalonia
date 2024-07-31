using System.Text.Json;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, JsonElement> _settings;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        _settings = new Dictionary<string, JsonElement>();
        
        if (!File.Exists(_settingsFilePath))
        {
            File.WriteAllText(_settingsFilePath, "{}");
        }
        
        LoadSettings();
    }

    public T GetSetting<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            return value.Deserialize<T>() ?? defaultValue;
        }
        return defaultValue;
    }

    public void SetSetting<T>(string key, T value)
    {
        _settings[key] = JsonSerializer.SerializeToElement(value);
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    public void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            var json = File.ReadAllText(_settingsFilePath);
            _settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
        }
        else
        {
            _settings = new Dictionary<string, JsonElement>();
        }
    }
}