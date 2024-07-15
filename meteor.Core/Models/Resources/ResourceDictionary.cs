using meteor.Core.Interfaces.Resources;

namespace meteor.Core.Models.Resources;

public class ResourceDictionary : IResourceProvider
{
    public List<IResourceProvider> MergedDictionaries { get; } = new();

    public object GetResource(string resourceKey)
    {
        throw new NotImplementedException();
    }

    public bool TryGetResource(string key, out object resource)
    {
        foreach (var dict in MergedDictionaries)
            if (dict.TryGetResource(key, out resource))
                return true;

        resource = null;
        return false;
    }
}