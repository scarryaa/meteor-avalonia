using System.Text.Json;

namespace meteor.Core.Interfaces.Services;

public interface ISettingsService
{
    T GetSetting<T>(string key, T defaultValue);
    void SetSetting<T>(string key, T value);
    void SaveSettings();
    void LoadSettings();
}