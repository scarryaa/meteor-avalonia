using System;
using Avalonia.Controls;
using meteor.ViewModels;

namespace meteor.Views;

public partial class TabView : UserControl
{
    public TabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is TabViewModel viewModel) viewModel.InvalidateRequired += OnInvalidateRequired;
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}