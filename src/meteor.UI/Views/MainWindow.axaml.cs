using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        var editorView = serviceProvider.GetService<EditorView>();
        Content = editorView;
    }
}