using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using meteor.ViewModels;

namespace meteor.Views;

public partial class TabView : UserControl
{
    public TabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is TabViewModel viewModel)
        {
            UpdateToolTip(viewModel.FilePath);
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.FilePath)) UpdateToolTip(viewModel.FilePath);
            };
            viewModel.InvalidateRequired += OnInvalidateRequired;
        }
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void UpdateToolTip(string? filePath)
    {
        var mainBorder = this.FindControl<Border>("MainBorder");
        if (!string.IsNullOrEmpty(filePath))
        {
            var tooltip = new ToolTip
            {
                Content = new TextBlock
                {
                    FontFamily = new FontFamily("SanFrancisco"),
                    FontSize = 13,
                    Text = filePath
                }
            };

            ToolTip.SetTip(mainBorder, tooltip);
        }
        else
        {
            mainBorder.ClearValue(ToolTip.TipProperty);
        }
    }
}