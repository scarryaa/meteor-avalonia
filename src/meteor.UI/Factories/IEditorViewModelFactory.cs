using System;
using meteor.Core.Interfaces.ViewModels;
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
        return _serviceProvider.GetRequiredService<IEditorViewModel>();
    }
}