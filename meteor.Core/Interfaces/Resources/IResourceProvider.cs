namespace meteor.Core.Interfaces.Resources;

public interface IResourceProvider
{
    object GetResource(string resourceKey);
    bool TryGetResource(string resourceKey, out object resource);
}