using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.UI.Features.SourceControl.ViewModels;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Features.SourceControl.Controls;

public class SourceControlView : UserControl
{
    private const double _itemHeight = 24;
    private readonly IGitService _gitService;
    private readonly double _leftPadding = 10;
    private readonly double _rightPadding = 10;
    private readonly IThemeManager _themeManager;
    private Canvas _canvas;
    private ScrollViewer _scrollViewer;
    private SourceControlViewModel _viewModel;

    public SourceControlView(IThemeManager themeManager, IGitService gitService)
    {
        _themeManager = themeManager;
        _gitService = gitService;
        _viewModel = new SourceControlViewModel(gitService);
        DataContext = _viewModel;

        InitializeComponent();
        _viewModel.PropertyChanged += (_, __) => InvalidateVisual();
    }

    private void InitializeComponent()
    {
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        _canvas = new Canvas();
        _scrollViewer.Content = _canvas;

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*"),
            Children =
            {
                new Border
                {
                    Child = _scrollViewer,
                    [Grid.RowProperty] = 0
                }
            }
        };

        UpdateCanvasSize();
        LoadChanges();
    }

    private async Task LoadChanges()
    {
        await _viewModel.LoadChangesCommand.ExecuteAsync(null);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SourceControlViewModel vm)
        {
            _viewModel = vm;
            _viewModel.PropertyChanged += (_, __) => InvalidateVisual();
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsVisible)
            return;

        var viewportRect = new Rect(new Point(0, 0),
            new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height));
        context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)),
            viewportRect);

        RenderItems(context, _viewModel.Changes, -_scrollViewer.Offset.Y, viewportRect);
    }

    private void RenderItems(DrawingContext context, IEnumerable<FileChange> changes, double y, Rect viewport)
    {
        foreach (var change in changes)
        {
            if (y + _itemHeight > 0 && y < viewport.Height) RenderItem(context, change, y, viewport);

            y += _itemHeight;

            if (y > viewport.Height) break;
        }
    }

    private void RenderItem(DrawingContext context, FileChange change, double y, Rect viewport)
    {
        RenderItemIcon(context, change, y);
        RenderItemText(context, change, y);
    }

    private void RenderItemIcon(DrawingContext context, FileChange change, double y)
    {
        var iconSize = 16;
        var iconChar = GetChangeTypeIcon(change.ChangeType);
        var iconBrush = new SolidColorBrush(GetChangeTypeColor(change.ChangeType));
        var fontAwesomeSolid =
            new FontFamily(
                "avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var typeface = new Typeface(fontAwesomeSolid);

        var iconGeometry = CreateFormattedTextGeometry(iconChar, typeface, iconSize, iconBrush);

        var iconX = _leftPadding * 2.35 - _scrollViewer.Offset.X;
        var iconY = y + (_itemHeight - iconSize) / 2;

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(iconX + 1, iconY));
        context.DrawGeometry(iconBrush, null, iconGeometry);
    }

    private void RenderItemText(DrawingContext context, FileChange change, double y)
    {
        var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));
        var textSize = 13;
        var typeface = new Typeface("San Francisco");

        var formattedText = new FormattedText(
            change.FilePath,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            textSize,
            textBrush
        );

        var textX = _leftPadding * 2.65 + 20 - _scrollViewer.Offset.X;
        var textY = y + (_itemHeight - formattedText.Height) / 2;

        context.DrawText(formattedText, new Point(textX, textY));
    }

    private Geometry CreateFormattedTextGeometry(string text, Typeface typeface, double size, IBrush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush
        ).BuildGeometry(new Point(0, 0));
    }

    private string GetChangeTypeIcon(FileChangeType changeType)
    {
        return changeType switch
        {
            FileChangeType.Added => "\uf055", // plus-circle
            FileChangeType.Deleted => "\uf056", // minus-circle
            FileChangeType.Modified => "\uf044", // edit
            _ => "\uf128" // question
        };
    }

    private Color GetChangeTypeColor(FileChangeType changeType)
    {
        return changeType switch
        {
            FileChangeType.Added => Colors.Green,
            FileChangeType.Deleted => Colors.Red,
            FileChangeType.Modified => Colors.Orange,
            _ => Colors.Gray
        };
    }

    private void UpdateCanvasSize()
    {
        var totalHeight = Math.Max(_viewModel.Changes.Count * _itemHeight, _scrollViewer.Bounds.Height);
        var maxWidth = Math.Max(CalculateMaxWidth(_viewModel.Changes) + 20, _scrollViewer.Bounds.Width);
        _canvas.Width = maxWidth;
        _canvas.Height = totalHeight;
    }

    private double CalculateMaxWidth(IEnumerable<FileChange> changes)
    {
        var maxWidth = 0.0;
        foreach (var change in changes)
        {
            var itemWidth = _leftPadding + MeasureTextWidth(change.FilePath) + _rightPadding;
            maxWidth = Math.Max(maxWidth, itemWidth);
        }

        return maxWidth;
    }

    private double MeasureTextWidth(string text)
    {
        return new FormattedText(
                   text,
                   CultureInfo.CurrentCulture,
                   FlowDirection.LeftToRight,
                   new Typeface("San Francisco"),
                   13,
                   new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))).Width + _rightPadding +
               _leftPadding;
    }

    internal async Task UpdateChangesAsync()
    {
        await _viewModel.LoadChangesCommand.ExecuteAsync(null);
        UpdateCanvasSize();
        InvalidateVisual();
    }

    internal void UpdateBackground(Theme theme)
    {
        Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
        {
            UpdateCanvasSize();
            InvalidateVisual();
        }
    }
}