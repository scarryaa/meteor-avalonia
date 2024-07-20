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

    public EditorViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEditorViewModel Create()
    {
        return CreateEditorViewModel();
    }

    private IEditorViewModel CreateEditorViewModel()
    {
        return new EditorViewModel(
            _serviceProvider.GetRequiredService<EditorViewModelServiceContainer>(),
            _serviceProvider.GetRequiredService<ITextMeasurer>()
        );
    }
}