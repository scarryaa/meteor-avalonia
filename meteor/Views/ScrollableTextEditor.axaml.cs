using System;
using Avalonia;
using Avalonia.Controls;
using meteor.ViewModels;

namespace meteor.Views;

public partial class ScrollableTextEditor : UserControl
{
    public ScrollableTextEditor()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            viewModel.TextEditorViewModel.WindowWidth = Bounds.Width;
            viewModel.TextEditorViewModel.WindowHeight = Bounds.Height;
            this.GetObservable(BoundsProperty).Subscribe(bounds =>
            {
                viewModel.TextEditorViewModel.WindowWidth = bounds.Width;
                viewModel.TextEditorViewModel.WindowHeight = bounds.Height;
            });
        }
    }

    private void EditorScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer { } viewer && DataContext is ScrollableTextEditorViewModel viewModel)
        {
            viewModel.Viewport = viewer.Viewport;
            viewModel.HorizontalOffset = viewer.Offset.X;
            viewModel.VerticalOffset = viewer.Offset.Y;
        }
    }

    private void EditorScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer { } viewer && DataContext is ScrollableTextEditorViewModel viewModel)
            viewModel.Viewport = viewer.Viewport;
    }
}