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
        // TODO implement this
        throw new NotImplementedException("Resource loading without Avalonia needs to be implemented");
    }
}