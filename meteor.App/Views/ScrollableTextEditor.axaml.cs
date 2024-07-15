using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using meteor.ViewModels;

namespace meteor.App.Views;

public partial class ScrollableTextEditor : UserControl
{
    private readonly ScrollViewer _scrollViewer;

    public ScrollableTextEditor()
    {
        InitializeComponent();
        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");

        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            _scrollViewer.ScrollChanged += (sender, args) =>
            {
                viewModel.HorizontalOffset = ((ScrollViewer)sender).Offset.X;
                viewModel.VerticalOffset = ((ScrollViewer)sender).Offset.Y;
                UpdateViewportSize();
            };

            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ScrollableTextEditorViewModel.HorizontalOffset) ||
                    args.PropertyName == nameof(ScrollableTextEditorViewModel.VerticalOffset))
                    _scrollViewer.Offset = new Vector(viewModel.HorizontalOffset, viewModel.VerticalOffset);
                else if (args.PropertyName == nameof(ScrollableTextEditorViewModel.RequiredWidth))
                    _scrollViewer.Width = viewModel.RequiredWidth;
                else if (args.PropertyName == nameof(ScrollableTextEditorViewModel.RequiredHeight))
                    _scrollViewer.Height = viewModel.RequiredHeight;
            };

            UpdateViewportSize();
        }
    }

    private void UpdateViewportSize()
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            viewModel.ViewportWidth = _scrollViewer.Viewport.Width;
            viewModel.ViewportHeight = _scrollViewer.Viewport.Height;
            Console.WriteLine($"Updated viewport size: {viewModel.ViewportWidth}x{viewModel.ViewportHeight}");
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateViewportSize();
    }
}