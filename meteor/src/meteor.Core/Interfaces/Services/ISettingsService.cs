namespace meteor.Core.Interfaces.Services;

public interface ISettingsService
{
    string GetSetting(string key, string defaultValue = "");
    void SetSetting(string key, string value);
    void SaveSettings();
}