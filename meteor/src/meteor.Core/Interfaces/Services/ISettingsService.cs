namespace meteor.Core.Interfaces.Services;

public interface ISettingsService
{
    T GetSetting<T>(string key, T defaultValue = default);
    void SetSetting<T>(string key, T value);
    void SaveSettings();
}