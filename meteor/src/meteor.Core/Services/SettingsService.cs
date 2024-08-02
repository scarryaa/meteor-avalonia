using System.Text.Json;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;
    private Dictionary<string, object> _settings = new();

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "meteor",
            SettingsFileName);
        LoadSettings();
    }

    public T GetSetting<T>(string key, T defaultValue = default) =>
        _settings.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

    public void SetSetting<T>(string key, T value) => _settings[key] = value;

    public void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
        File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(_settings));
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var deserializedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
                _settings = deserializedSettings?.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonElement(kvp.Value))
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing settings: {ex.Message}");
                _settings = new Dictionary<string, object>();
            }
        }
        else
        {
            _settings = new Dictionary<string, object>
            {
                { "LastOpenedDirectory", "" },
                { "WindowWidth", 500 },
                { "WindowHeight", 500 }
            };
        }
    }

    private object ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt32(out int intValue) ? intValue : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };
}