namespace meteor.Core.Interfaces.Resources;

public interface IApplicationResourceProvider
{
    IResourceProvider Resources { get; set; }
    IResourceProvider LoadResource(string source);
}