using System.Text.Json;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class SettingsService : ISettingsService
{
    private Dictionary<string, string> _settings = new();
    private const string SettingsFileName = "settings.json";

    public SettingsService()
    {
        LoadSettings();
    }

    public string GetSetting(string key, string defaultValue = "")
    {
        return _settings.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void SetSetting(string key, string value)
    {
        _settings[key] = value;
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(_settings, JsonContext.Default.DictionaryStringString);
        File.WriteAllText(SettingsFileName, json);
    }

    private void LoadSettings()
    {
        if (File.Exists(SettingsFileName))
        {
            var json = File.ReadAllText(SettingsFileName);
            _settings = JsonSerializer.Deserialize(json, JsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>();
        }
    }
}