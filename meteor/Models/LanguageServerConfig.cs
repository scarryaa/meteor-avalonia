using System;

namespace meteor.Models;

public class LanguageServerConfig
{
    public string Command { get; set; } = string.Empty;
    public string[] Args { get; set; } = Array.Empty<string>();
    public string InstallCommand { get; set; } = string.Empty;
}