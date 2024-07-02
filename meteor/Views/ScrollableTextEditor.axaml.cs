using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.ViewModels;

namespace meteor.Views;

public partial class ScrollableTextEditor : UserControl
{
    public new static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, FontFamily>(
            nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public new static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, double>(nameof(FontSize), 13);

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, IBrush>(nameof(BackgroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> CursorBrushProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, IBrush>(nameof(CursorBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, IBrush>(nameof(SelectionBrush),
            new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)));

    public static readonly StyledProperty<IBrush> LineHighlightBrushProperty =
        AvaloniaProperty.Register<ScrollableTextEditor, IBrush>(nameof(LineHighlightBrush),
            new SolidColorBrush(Color.Parse("#ededed")));

    public new FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public new double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush CursorBrush
    {
        get => GetValue(CursorBrushProperty);
        set => SetValue(CursorBrushProperty, value);
    }

    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public IBrush LineHighlightBrush
    {
        get => GetValue(LineHighlightBrushProperty);
        set => SetValue(LineHighlightBrushProperty, value);
    }

    public ScrollableTextEditor()
    {
        InitializeComponent();
        DataContext = this;
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