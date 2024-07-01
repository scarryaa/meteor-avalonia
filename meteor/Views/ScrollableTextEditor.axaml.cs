using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.ViewModels;

namespace meteor.Views;

public partial class ScrollableTextEditor : UserControl
{
    public ScrollableTextEditor()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, FontFamily>(
            nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, double>(nameof(FontSize), 13);

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
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
        if (sender is ButtonlessScrollViewer { } viewer && DataContext is ScrollableTextEditorViewModel viewModel)
        {
            viewModel.Viewport = viewer.Viewport;
            viewModel.HorizontalOffset = viewer.Offset.X;
            viewModel.VerticalOffset = viewer.Offset.Y;
        }
    }

    private void EditorScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is ButtonlessScrollViewer { } viewer && DataContext is ScrollableTextEditorViewModel viewModel)
            viewModel.Viewport = viewer.Viewport;
    }
}
