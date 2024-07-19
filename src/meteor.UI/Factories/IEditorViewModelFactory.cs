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
}

public class EditorViewModelFactory : IEditorViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITabService _tabService;

    public EditorViewModelFactory(IServiceProvider serviceProvider, ITabService tabService)
    {
        _serviceProvider = serviceProvider;
        _tabService = tabService;
    }

    public IEditorViewModel Create()
    {
        // Create new instances of services for each view model
        var textBufferService = new TextBufferService();
        var editorViewModel = new EditorViewModel(
            textBufferService,
            _tabService,
            _serviceProvider.GetRequiredService<ISyntaxHighlighter>(),
            _serviceProvider.GetRequiredService<ISelectionService>(),
            _serviceProvider.GetRequiredService<IInputService>(),
            _serviceProvider.GetRequiredService<ICursorService>(),
            _serviceProvider.GetRequiredService<IEditorSizeCalculator>()
        );

        // Register the new text buffer service with the tab service
        var tabIndex = _tabService.GetNextAvailableTabIndex();
        _tabService.RegisterTab(tabIndex, textBufferService);

        return editorViewModel;
    }
}