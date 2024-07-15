using meteor.Core.Interfaces.Resources;

namespace meteor.Core.Models.Resources;

public class ResourceDictionary : IResourceProvider
{
    public List<IResourceProvider> MergedDictionaries { get; } = new();
    private readonly Dictionary<string, object> _resources = new();

    public object GetResource(string resourceKey)
    {
        if (TryGetResource(resourceKey, out var resource)) return resource;

        throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");
    }

    public bool TryGetResource(string key, out object resource)
    {
        if (_resources.TryGetValue(key, out resource)) return true;

        foreach (var dict in MergedDictionaries)
            if (dict.TryGetResource(key, out resource))
                return true;

        resource = null;
        return false;
    }

    public void AddResource(string key, object value)
    {
        _resources[key] = value;
    }
}