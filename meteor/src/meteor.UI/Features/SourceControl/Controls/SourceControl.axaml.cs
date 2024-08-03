using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.UI.Features.SourceControl.ViewModels;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Vector = Avalonia.Vector;
using System.IO;

namespace meteor.UI.Features.SourceControl.Controls;

public partial class SourceControlView : UserControl, IDisposable
{
    private const double ItemHeight = 24;
    private const double LeftPadding = 10;
    private const double IconSize = 16;
    private const double ItemSpacing = 0;
    private const double HeaderHeight = 24;
    private const double ChevronLeftMargin = 10;

    private readonly IThemeManager _themeManager;
    private readonly IGitService _gitService;
    private SourceControlViewModel _viewModel;
    private Canvas _canvas;
    private ScrollViewer _scrollViewer;
    private bool _isChangesExpanded = true;
    private bool _isHeaderHovered = false;
    private bool _isHeaderSelected = false;
    private FileSystemWatcher _watcher;
    private System.Timers.Timer _debounceTimer;
    private string _currentRepositoryPath;

    public SourceControlView(IThemeManager themeManager, IGitService gitService)
    {
        _themeManager = themeManager;
        _gitService = gitService;
        _viewModel = new SourceControlViewModel(gitService);
        DataContext = _viewModel;

        InitializeComponent();
        SetupEventHandlers();
        InitializeFileWatcher();

        _gitService.RepositoryPathChanged += OnRepositoryPathChanged;
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
            Children = { new Border { Child = _scrollViewer, [Grid.RowProperty] = 0 } }
        };

        UpdateCanvasSize();
    }

    private void SetupEventHandlers()
    {
        _scrollViewer.ScrollChanged += (_, _) => InvalidateVisual();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerExited += OnPointerExited;
        KeyDown += OnKeyDown;
        _viewModel.PropertyChanged += (_, _) => InvalidateVisual();
        _themeManager.ThemeChanged += OnThemeChanged;

        // Add focus handling for keyboard navigation
        GotFocus += (_, _) => Focus();
        LostFocus += (_, _) => InvalidateVisual();

        // Make the control focusable
        Focusable = true;
    }

    private void InitializeFileWatcher()
    {
        UpdateFileWatcher(_gitService.GetRepositoryPath());
    }

    private void UpdateFileWatcher(string newRepositoryPath)
    {
        if (_watcher != null)
        {
            _watcher.Dispose();
            _watcher = null;
        }

        if (string.IsNullOrEmpty(newRepositoryPath) || !Directory.Exists(newRepositoryPath))
        {
            return;
        }

        _currentRepositoryPath = newRepositoryPath;
        _watcher = new FileSystemWatcher(_currentRepositoryPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnFileSystemChanged;
        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Renamed += OnFileSystemChanged;

        if (_debounceTimer == null)
        {
            _debounceTimer = new System.Timers.Timer(500);
            _debounceTimer.Elapsed += async (sender, e) =>
            {
                _debounceTimer.Stop();
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await UpdateChangesAsync(CancellationToken.None);
                });
            };
        }
    }

    private void OnRepositoryPathChanged(object sender, string newPath)
    {
        UpdateFileWatcher(newPath);
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await UpdateChangesAsync(CancellationToken.None);
        });
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnThemeChanged(object sender, Theme newTheme)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!IsVisible) return;

        var viewportRect = new Rect(new Point(0, 0), _scrollViewer.Viewport);
        context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)), viewportRect);

        RenderHeader(context, viewportRect);

        if (_isChangesExpanded && _viewModel.Changes != null && _viewModel.Changes.Any())
        {
            RenderItems(context, _viewModel.Changes, -_scrollViewer.Offset.Y + HeaderHeight, viewportRect);
        }
        else if (!_isChangesExpanded)
        {
            // Render nothing when collapsed
        }
    }

    private void RenderHeader(DrawingContext context, Rect viewport)
    {
        var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

        // Draw hover rectangle for header if hovered or selected
        if (_isHeaderHovered || _isHeaderSelected)
        {
            var hoverRect = new Rect(0, 0, viewport.Width, HeaderHeight);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)), hoverRect);
        }

        // Draw collapse/expand icon
        var iconChar = _isChangesExpanded ? "\uf078" : "\uf054"; // chevron-down : chevron-right
        var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var iconGeometry = CreateFormattedTextGeometry(iconChar, new Typeface(fontAwesomeSolid), 12, textBrush);

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(ChevronLeftMargin, (HeaderHeight - 12) / 2));
        context.DrawGeometry(textBrush, null, iconGeometry);

        var headerText = "Changes";
        var formattedText = new FormattedText(headerText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal), 14, textBrush);

        context.DrawText(formattedText, new Point(ChevronLeftMargin + 20, (HeaderHeight - formattedText.Height) / 2));
    }

    private void RenderItems(DrawingContext context, IEnumerable<FileChange> changedFiles, double startY, Rect viewport)
    {
        double y = startY;
        foreach (var file in changedFiles)
        {
            if (y + ItemHeight > HeaderHeight && y < viewport.Height)
            {
                RenderItem(context, file, y, viewport.Width);
            }
            y += ItemHeight + ItemSpacing;
            if (y > viewport.Height) break;
        }
    }

    private void RenderItem(DrawingContext context, FileChange file, double y, double viewportWidth)
    {
        // Draw selection/hover effect
        if (file == _viewModel.SelectedFile || file == _viewModel.HoveredItem)
        {
            var rect = new Rect(0, y, viewportWidth, ItemHeight);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)), rect);
        }

        RenderItemIcon(context, file, y);
        RenderItemText(context, file, y, viewportWidth);
    }

    private void RenderItemIcon(DrawingContext context, FileChange file, double y)
    {
        var iconChar = "\uf15b"; // Default file icon
        var iconBrush = new SolidColorBrush(Colors.Gray);
        var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var iconGeometry = CreateFormattedTextGeometry(iconChar, new Typeface(fontAwesomeSolid), IconSize, iconBrush);

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(LeftPadding - _scrollViewer.Offset.X, y + (ItemHeight - IconSize) / 2));
        context.DrawGeometry(iconBrush, null, iconGeometry);
    }

    private void RenderItemText(DrawingContext context, FileChange file, double y, double viewportWidth)
    {
        var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));
        var maxTextWidth = viewportWidth - LeftPadding - IconSize - 10;
        var truncatedText = TruncateText(Path.GetFileName(file.FilePath), maxTextWidth);
        var formattedText = new FormattedText(truncatedText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("San Francisco"), 13, textBrush);
        context.DrawText(formattedText, new Point(LeftPadding + IconSize + 10 - _scrollViewer.Offset.X, y + (ItemHeight - formattedText.Height) / 2));
    }

    private string TruncateText(string text, double maxWidth)
    {
        var ellipsis = "...";
        var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("San Francisco"), 13, Brushes.Black);

        if (formattedText.Width <= maxWidth)
            return text;

        while (formattedText.Width > maxWidth && text.Length > 1)
        {
            text = text.Substring(0, text.Length - 1);
            formattedText = new FormattedText(text + ellipsis, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("San Francisco"), 13, Brushes.Black);
        }

        return text + ellipsis;
    }

    private Geometry CreateFormattedTextGeometry(string text, Typeface typeface, double size, IBrush brush) =>
        new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush).BuildGeometry(new Point(0, 0));

    private void UpdateCanvasSize()
    {
        _canvas.Height = CalculateTotalHeight(_viewModel.Changes);
    }

    private double CalculateTotalHeight(IEnumerable<FileChange> changedFiles)
    {
        if (changedFiles == null || !changedFiles.Any() || !_isChangesExpanded)
        {
            return HeaderHeight;
        }

        return Math.Max(HeaderHeight + changedFiles.Count() * (ItemHeight + ItemSpacing), _scrollViewer.Bounds.Height);
    }

    private double MeasureTextWidth(string text, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("San Francisco"),
            fontSize,
            new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
        );
        return formattedText.Width;
    }
    internal async Task UpdateChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await _viewModel.LoadChangesCommand.ExecuteAsync(cancellationToken);
                UpdateCanvasSize();
                InvalidateVisual();
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("UpdateChanges operation was canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating changes: {ex.Message}");
        }
    }

    internal void UpdateBackground(Theme theme)
    {
        Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
        InvalidateVisual();
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

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);

        // Check if header was clicked
        if (point.Y < HeaderHeight)
        {
            _isChangesExpanded = !_isChangesExpanded;
            UpdateCanvasSize();
            InvalidateVisual();
            return;
        }

        if (!_isChangesExpanded) return;

        double y = HeaderHeight - _scrollViewer.Offset.Y;

        if (_viewModel.Changes == null) return;

        foreach (var file in _viewModel.Changes)
        {
            if (point.Y >= y && point.Y < y + ItemHeight)
            {
                _viewModel.SelectedFile = file;
                _isHeaderSelected = false;
                InvalidateVisual();
                // TODO: Implement action for selected file
                return;
            }
            y += ItemHeight + ItemSpacing;
        }

        Focus();
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        var point = e.GetPosition(this);

        bool wasHeaderHovered = _isHeaderHovered;
        _isHeaderHovered = point.Y < HeaderHeight;

        if (wasHeaderHovered != _isHeaderHovered)
        {
            InvalidateVisual();
        }

        if (!_isChangesExpanded)
        {
            if (_viewModel.HoveredItem != null)
            {
                _viewModel.HoveredItem = null;
                InvalidateVisual();
            }
            return;
        }

        double y = HeaderHeight - _scrollViewer.Offset.Y;

        if (_viewModel.Changes == null) return;

        FileChange newHoveredItem = null;

        foreach (var file in _viewModel.Changes)
        {
            if (point.Y >= y && point.Y < y + ItemHeight)
            {
                newHoveredItem = file;
                break;
            }
            y += ItemHeight + ItemSpacing;
        }

        if (_viewModel.HoveredItem != newHoveredItem)
        {
            _viewModel.HoveredItem = newHoveredItem;
            InvalidateVisual();
        }

        if (newHoveredItem != null)
        {
            ToolTip.SetTip(this, newHoveredItem.FilePath);
        }
        else
        {
            ToolTip.SetTip(this, null);
        }
    }

    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        _isHeaderHovered = false;
        if (_viewModel.HoveredItem != null)
        {
            _viewModel.HoveredItem = null;
            InvalidateVisual();
        }
        ToolTip.SetTip(this, null);
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                MoveSelectionUp();
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelectionDown();
                e.Handled = true;
                break;
            case Key.Enter:
                if (_isHeaderSelected)
                {
                    ToggleExpansion();
                }
                else if (_viewModel.SelectedFile != null)
                {
                    // TODO: Implement action for selected file
                }
                e.Handled = true;
                break;
            case Key.Space:
                ToggleExpansion();
                e.Handled = true;
                break;
        }
    }

    private void MoveSelectionDown()
    {
        if (_isHeaderSelected)
        {
            if (_viewModel.Changes != null && _viewModel.Changes.Any())
            {
                _isHeaderSelected = false;
                _viewModel.SelectedFile = _viewModel.Changes.First();
            }
        }
        else if (_viewModel.SelectedFile != null)
        {
            int currentIndex = _viewModel.Changes.IndexOf(_viewModel.SelectedFile);
            if (currentIndex < _viewModel.Changes.Count - 1)
            {
                _viewModel.SelectedFile = _viewModel.Changes[currentIndex + 1];
            }
            else
            {
                _isHeaderSelected = true;
                _viewModel.SelectedFile = null;
            }
        }
        else
        {
            _isHeaderSelected = true;
        }

        EnsureSelectedItemVisible();
        InvalidateVisual();
    }

    private void ToggleExpansion()
    {
        _isChangesExpanded = !_isChangesExpanded;
        UpdateCanvasSize();
        InvalidateVisual();
    }

    private void MoveSelectionUp()
    {
        if (_isHeaderSelected)
        {
            if (_viewModel.Changes != null && _viewModel.Changes.Any())
            {
                _isHeaderSelected = false;
                _viewModel.SelectedFile = _viewModel.Changes.Last();
            }
        }
        else if (_viewModel.SelectedFile != null)
        {
            int currentIndex = _viewModel.Changes.IndexOf(_viewModel.SelectedFile);
            if (currentIndex > 0)
            {
                _viewModel.SelectedFile = _viewModel.Changes[currentIndex - 1];
            }
            else
            {
                _isHeaderSelected = true;
                _viewModel.SelectedFile = null;
            }
        }
        else
        {
            _isHeaderSelected = true;
        }

        EnsureSelectedItemVisible();
        InvalidateVisual();
    }

    private void EnsureSelectedItemVisible()
    {
        if (_viewModel.SelectedFile == null) return;

        int index = _viewModel.Changes.IndexOf(_viewModel.SelectedFile);
        if (index == -1) return;

        double y = HeaderHeight + index * (ItemHeight + ItemSpacing);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}