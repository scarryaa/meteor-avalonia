using System.Text.Json;
using meteor.Core.Interfaces.Resources;

namespace meteor.Core.Models.Resources;

public class ApplicationResourceProvider : IApplicationResourceProvider
{
    public IResourceProvider Resources { get; set; }

    public ApplicationResourceProvider()
    {
        Resources = new ResourceDictionary();
    }

    public IResourceProvider LoadResource(string source)
    {
        Console.WriteLine($"Loading resource from: {source}");
        if (File.Exists(source))
        {
            var resourceDictionary = new ResourceDictionary();
            var jsonString = File.ReadAllText(source);
            var resources = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(jsonString);

            if (resources != null && resources.TryGetValue("Resources", out var resourceValues))
                foreach (var kvp in resourceValues)
                {
                    Console.WriteLine($"Adding resource: {kvp.Key} = {kvp.Value}");
                    resourceDictionary.AddResource(kvp.Key, kvp.Value);
                }

            return resourceDictionary;
        }

        throw new FileNotFoundException($"Resource file not found: {source}");
    }

    public void LoadTheme(string theme)
    {
        var resourceFilePath = theme.ToLower() switch
        {
            "light" => "Resources/light-theme.json",
            "dark" => "Resources/dark-theme.json",
            _ => throw new ArgumentException("Invalid theme specified", nameof(theme))
        };

        Resources = LoadResource(resourceFilePath);
    }
}