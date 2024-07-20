using System;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Services;
using meteor.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI.Factories;

public interface IEditorViewModelFactory
{
    IEditorViewModel Create();
    IEditorViewModel Create(ITextBufferService textBufferService);
}

public class EditorViewModelFactory : IEditorViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EditorViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEditorViewModel Create()
    {
        var textBufferService = new TextBufferService();
        return CreateEditorViewModel(textBufferService);
    }

    public IEditorViewModel Create(ITextBufferService textBufferService)
    {
        return CreateEditorViewModel(textBufferService);
    }

    private IEditorViewModel CreateEditorViewModel(ITextBufferService textBufferService)
    {
        return new EditorViewModel(
            textBufferService,
            _serviceProvider.GetRequiredService<ITabService>(),
            _serviceProvider.GetRequiredService<ISyntaxHighlighter>(),
            _serviceProvider.GetRequiredService<ISelectionService>(),
            _serviceProvider.GetRequiredService<IInputService>(),
            _serviceProvider.GetRequiredService<ICursorService>(),
            _serviceProvider.GetRequiredService<IEditorSizeCalculator>()
        );
    }
}