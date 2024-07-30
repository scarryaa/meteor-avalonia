using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Models;
using meteor.Core.Interfaces.Services;
using meteor.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI.Factories;

public class EditorInstanceFactory : IEditorInstanceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EditorInstanceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEditorInstance Create()
    {
        return new EditorInstance(
            _serviceProvider.GetRequiredService<IEditorConfig>(),
            _serviceProvider.GetRequiredService<ITextMeasurer>(),
            _serviceProvider.GetRequiredService<IClipboardManager>(),
            _serviceProvider.GetRequiredService<ITextAnalysisService>(),
            _serviceProvider.GetRequiredService<IScrollManager>()
        );
    }
}