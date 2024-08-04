using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using System.Collections.Concurrent;
using Avalonia.Platform.Storage;
using meteor.Core.Interfaces.Services;
using System.IO;

namespace meteor.UI.Features.FileExplorer.Controls;

public class FileExplorerControl : UserControl
{
    private const int MaxItemsPerDirectory = 1000;
    private const int LazyLoadThreshold = 100;
    private readonly double _indentWidth = 8;
    private readonly double _itemHeight = 24;
    private ObservableCollection<FileItem> _items;
    private readonly double _leftPadding = 8;
    private readonly double _rightPadding = 8;
    private readonly IThemeManager _themeManager;
    private readonly IGitService _gitService;
    private Canvas _canvas;
    private Theme _currentTheme;
    private Grid _mainGrid;
    private ScrollViewer _scrollViewer;
    private FileItem _selectedItem;
    private Button _selectPathButton;
    private ConcurrentDictionary<string, Task> _populationTasks = new ConcurrentDictionary<string, Task>();
    private Dictionary<string, Geometry> _iconCache = new Dictionary<string, Geometry>();
    private Dictionary<string, FormattedText> _textCache = new Dictionary<string, FormattedText>();
    private Dictionary<string, FileChangeType?> _fileStatusCache = new Dictionary<string, FileChangeType?>();
    private FileSystemWatcher _fileWatcher;
    private CancellationTokenSource _updateCancellationTokenSource;

    public FileExplorerControl(IThemeManager themeManager, IGitService gitService)
    {
        _themeManager = themeManager;
        _gitService = gitService;
        _currentTheme = _themeManager.CurrentTheme;
        _themeManager.ThemeChanged += OnThemeChanged;

        _items = [];
        InitializeComponent();
        UpdateCanvasSize();
        Focus();
    }

    public event EventHandler<string> FileSelected;
    public event EventHandler<string> DirectoryOpened;

    public void SetDirectory(string path)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _selectPathButton.IsVisible = false;
            _items.Clear();
            _items.Add(new FileItem(path, true));
            _ = PopulateChildrenAsync(_items[0]);
            _items[0].IsExpanded = true;
            UpdateCanvasSize();
            InvalidateVisual();

            // Cache the changes
            var changes = _gitService.GetChanges().ToList();
            foreach (var change in changes)
            {
                _fileStatusCache[change.FilePath] = change.ChangeType;
            }

            // Dispose of the previous file watcher if it exists
            _fileWatcher?.Dispose();

            // Initialize file watcher
            _fileWatcher = new FileSystemWatcher(path);
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.IncludeSubdirectories = true;
            _fileWatcher.EnableRaisingEvents = true;
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _updateCancellationTokenSource?.Cancel();
        _updateCancellationTokenSource = new CancellationTokenSource();

        Task.Delay(500, _updateCancellationTokenSource.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        UpdateFileList(e.FullPath, true);
                        break;
                    case WatcherChangeTypes.Deleted:
                        UpdateFileList(e.FullPath, false);
                        break;
                    case WatcherChangeTypes.Changed:
                        break;
                }
            });
        }, TaskScheduler.Default);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateFileList(e.OldFullPath, false);
            UpdateFileList(e.FullPath, true);
        });
    }

    private void UpdateFileList(string filePath, bool add)
    {
        if (add)
        {
            var newItem = new FileItem(filePath, Directory.Exists(filePath));
            var parentPath = Path.GetDirectoryName(filePath);
            var parentItem = FindItemByPath(_items, parentPath);
            if (parentItem != null)
            {
                parentItem.Children.Add(newItem);
                parentItem.Children.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                _items.Add(newItem);
                _items = new ObservableCollection<FileItem>(_items.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase));
            }
        }
        else
        {
            var itemToRemove = FindItemByPath(_items, filePath);
            if (itemToRemove != null)
            {
                var parent = FindParentItem(_items, itemToRemove);
                if (parent != null)
                    parent.Children.Remove(itemToRemove);
                else
                    _items.Remove(itemToRemove);
            }
        }
        UpdateCanvasSize();
        InvalidateVisual();
    }

    private FileItem FindItemByPath(IEnumerable<FileItem> items, string path)
    {
        foreach (var item in items)
        {
            if (item.FullPath == path)
                return item;
            if (item.IsDirectory && item.IsExpanded)
            {
                var result = FindItemByPath(item.Children, path);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    private void InitializeComponent()
    {
        CreateMainGrid();
        CreateSelectPathButton();
        CreateScrollViewer();
        SetupEventHandlers();
        SetupLayout();
        UpdateSelectPathButtonVisibility();
    }

    private void CreateMainGrid()
    {
        _mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };
    }

    private void CreateSelectPathButton()
    {
        _selectPathButton = new Button
        {
            Content = "Select Folder",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10),
            Cursor = new Cursor(StandardCursorType.Hand),
            Classes = { "noBg" }
        };

        _selectPathButton.Styles.Add(CreateButtonStyles());
        _selectPathButton.Click += OnSelectPathButtonClick;

        Grid.SetRow(_selectPathButton, 0);
        _mainGrid.Children.Add(_selectPathButton);
    }

    private void CreateScrollViewer()
    {
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        _canvas = new Canvas();
        _scrollViewer.Content = _canvas;
        Grid.SetRow(_scrollViewer, 1);
        _mainGrid.Children.Add(_scrollViewer);
    }

    private void SetupEventHandlers()
    {
        _scrollViewer.ScrollChanged += OnScrollChanged;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
    }

    private void SetupLayout()
    {
        Content = _mainGrid;
        Focusable = true;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void OnThemeChanged(object sender, Theme newTheme)
    {
        _currentTheme = newTheme;
        UpdateButtonStyles();
        ClearCaches();
        InvalidateVisual();
    }

    private void ClearCaches()
    {
        _iconCache.Clear();
        _textCache.Clear();
    }

    private void UpdateButtonStyles()
    {
        _selectPathButton.Styles.Clear();
        _selectPathButton.Styles.Add(CreateButtonStyles());
    }

    private Styles CreateButtonStyles()
    {
        return new Styles
        {
            CreateButtonStyle(),
            CreateButtonHoverStyle(),
            CreateButtonPressedStyle(),
            CreateButtonDisabledStyle()
        };
    }

    private Style CreateButtonStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg"))
        {
            Setters =
            {
                new Setter(TemplateProperty, CreateButtonTemplate())
            }
        };
    }

    private Style CreateButtonHoverStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplateProperty, CreateButtonTemplate(true))
            }
        };
    }

    private Style CreateButtonPressedStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":pressed"))
        {
            Setters =
            {
                new Setter(TemplateProperty, CreateButtonTemplate(isPressed: true))
            }
        };
    }

    private Style CreateButtonDisabledStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":disabled"))
        {
            Setters =
            {
                new Setter(TemplateProperty, CreateButtonTemplate(isDisabled: true))
            }
        };
    }

    private IControlTemplate CreateButtonTemplate(bool isPointerOver = false, bool isPressed = false,
        bool isDisabled = false)
    {
        return new FuncControlTemplate((parent, scope) =>
        {
            var contentPresenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                Background = new SolidColorBrush(Color.Parse(_currentTheme.BackgroundColor)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                [!ContentPresenter.ContentProperty] = parent[!ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = parent[!ContentTemplateProperty]
            };

            contentPresenter.Foreground = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));

            return contentPresenter;
        });
    }

    private void UpdateSelectPathButtonVisibility()
    {
        _selectPathButton.IsVisible = _items.Count == 0;
    }

    private async void OnSelectPathButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = TopLevel.GetTopLevel(this);
        var window = GetMainWindow();
        var result = await dialog.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

        if (result != null && result.Count > 0)
        {
            var selectedFolder = result[0];
            var folderPath = selectedFolder.Path.LocalPath;
            SetDirectory(folderPath);
            var rootDirectoryPath = Path.GetFullPath(folderPath);
            DirectoryOpened?.Invoke(this, rootDirectoryPath);
        }
    }

    private Window GetMainWindow()
    {
        return (Window)VisualRoot;
    }

    private async Task PopulateChildrenAsync(FileItem item)
    {
        if (item.ChildrenPopulated) return;

        try
        {
            var task = _populationTasks.GetOrAdd(item.FullPath, _ => Task.Run(() => PopulateChildrenInternal(item)));
            await task;
            _populationTasks.TryRemove(item.FullPath, out _);
        }
        catch (Exception ex)
        {
            item.Children.Add(new FileItem($"Error: {ex.Message}", item.FullPath, false));
        }
    }

    private void PopulateChildrenInternal(FileItem item)
    {
        var children = new List<FileItem>();
        var directories = GetDirectories(item.FullPath);
        var files = GetFiles(item.FullPath);

        children.AddRange(directories.Take(MaxItemsPerDirectory / 2));
        children.AddRange(files.Take(MaxItemsPerDirectory / 2));

        if (children.Count > MaxItemsPerDirectory)
        {
            children = children.Take(MaxItemsPerDirectory).ToList();
            children.Add(new FileItem("... (More items not shown)", item.FullPath, false));
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            item.Children.AddRange(children);
            item.ChildrenPopulated = true;
            UpdateCanvasSize();
            InvalidateVisual();
        });
    }

    private IEnumerable<FileItem> GetDirectories(string path)
    {
        return Directory.EnumerateDirectories(path)
            .Where(dir => !Path.GetFileName(dir).StartsWith("."))
            .Select(dir => new FileItem(dir, true) { GitStatus = GetFileStatus(dir) })
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<FileItem> GetFiles(string path)
    {
        return Directory.EnumerateFiles(path)
            .Where(file => !ShouldHideFile(file))
            .Select(file => new FileItem(file, false) { GitStatus = GetFileStatus(file) })
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
    }

    private FileChangeType? GetFileStatus(string path)
    {
        if (_fileStatusCache.TryGetValue(path, out var cachedStatus))
        {
            return cachedStatus;
        }

        var status = _gitService.GetFileStatus(path) as FileChangeType?;
        _fileStatusCache[path] = status;
        return status;
    }

    private bool ShouldHideFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        var hiddenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".DS_Store",
            "Thumbs.db",
            "desktop.ini",
            "index.lock"
        };

        return fileName.StartsWith(".") || hiddenFiles.Contains(fileName);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        var totalHeight = Math.Max(CalculateTotalHeight(_items) + _itemHeight, _scrollViewer.Bounds.Height);
        var maxWidth = Math.Max(CalculateMaxWidth(_items) + 20, _scrollViewer.Bounds.Width);
        _canvas.Width = maxWidth;
        _canvas.Height = totalHeight;
    }

    private double CalculateMaxWidth(IEnumerable<FileItem> items, int depth = 0)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0) return 0;

        return itemsList.Max(item =>
        {
            var itemWidth = depth * _indentWidth + _leftPadding + MeasureTextWidth(item.Name) + _rightPadding;
            if (item.IsDirectory && item.IsExpanded)
                itemWidth = Math.Max(itemWidth, CalculateMaxWidth(item.Children, depth + 1));
            return itemWidth;
        });
    }

    private double MeasureTextWidth(string text)
    {
        if (!_textCache.TryGetValue(text, out var formattedText))
        {
            formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco"),
                13,
                new SolidColorBrush(Color.Parse(_currentTheme.TextColor)));
            _textCache[text] = formattedText;
        }
        return formattedText.Width + _rightPadding + _leftPadding;
    }

    private double CalculateTotalHeight(IEnumerable<FileItem> items)
    {
        return items.Sum(item => _itemHeight + (item.IsExpanded ? CalculateTotalHeight(item.Children) : 0));
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        InvalidateVisual();
        LazyLoadVisibleItems();
    }

    private void LazyLoadVisibleItems()
    {
        var visibleItems = GetVisibleItems(_items, -_scrollViewer.Offset.Y, _scrollViewer.Viewport.Height);
        foreach (var item in visibleItems)
        {
            if (item.IsDirectory && item.IsExpanded && !item.ChildrenPopulated)
            {
                _ = PopulateChildrenAsync(item);
            }
        }
    }

    private List<FileItem> GetVisibleItems(IEnumerable<FileItem> items, double startY, double viewportHeight)
    {
        var visibleItems = new List<FileItem>();
        foreach (var item in items)
        {
            if (startY + _itemHeight > 0 && startY < viewportHeight)
                visibleItems.Add(item);

            startY += _itemHeight;

            if (item.IsExpanded)
            {
                visibleItems.AddRange(GetVisibleItems(item.Children, startY, viewportHeight));
                startY += CalculateTotalHeight(item.Children);
            }

            if (startY > viewportHeight) break;
        }
        return visibleItems;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsVisible)
            return;

        var viewportRect = new Rect(new Point(0, 0),
            new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height + 50));
        context.FillRectangle(new SolidColorBrush(Color.Parse(_currentTheme.BackgroundColor)), viewportRect);

        var buttonHeight = _selectPathButton.IsVisible ? _selectPathButton.Bounds.Height : 0;
        RenderItems(context, _items, 0, -_scrollViewer.Offset.Y + buttonHeight, viewportRect);
    }

    private double RenderItems(DrawingContext context, IEnumerable<FileItem> items, int indentLevel, double y, Rect viewport)
    {
        foreach (var item in items)
        {
            if (y + _itemHeight > 0 && y < viewport.Height)
                RenderItem(context, item, indentLevel, y, viewport);

            y += _itemHeight;

            if (item.IsExpanded)
                y = RenderItems(context, item.Children, indentLevel + 1, y, viewport);

            if (y > viewport.Height)
                break;
        }

        return y;
    }

    private void RenderItem(DrawingContext context, FileItem item, int indentLevel, double y, Rect viewport)
    {
        RenderItemBackground(context, item, y, viewport);
        RenderItemChevron(context, item, indentLevel, y);
        RenderItemIcon(context, item, indentLevel, y);
        RenderItemText(context, item, indentLevel, y);
        RenderItemGitStatus(context, item, indentLevel, y, viewport);
    }

    private void RenderItemBackground(DrawingContext context, FileItem item, double y, Rect viewport)
    {
        if (item == _selectedItem)
        {
            var backgroundBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerSelectedItemBackgroundColor));
            context.FillRectangle(backgroundBrush, new Rect(0, y, viewport.Width, _itemHeight));
        }
    }

    private void RenderItemIcon(DrawingContext context, FileItem item, int indentLevel, double y)
    {
        var iconSize = 16;
        var iconChar = item.IsDirectory ? "\uf07b" : "\uf15b"; // folder icon : file icon
        var iconBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerFileIconColor));
        var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var typeface = new Typeface(fontAwesomeSolid);

        var iconGeometry = CreateFormattedTextGeometry(iconChar, typeface, iconSize, iconBrush);

        var iconX = _leftPadding + indentLevel * _indentWidth + 20 - _scrollViewer.Offset.X;
        var iconY = y + (_itemHeight - iconSize) / 2;

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(iconX, iconY));
        context.DrawGeometry(iconBrush, null, iconGeometry);
    }

    private void RenderItemChevron(DrawingContext context, FileItem item, int indentLevel, double y)
    {
        if (!item.IsDirectory) return;

        var chevronBrush = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));
        var chevronSize = 10;
        var chevronChar = item.IsExpanded ? "\uf078" : "\uf054"; // chevron-down : chevron-right
        var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var typeface = new Typeface(fontAwesomeSolid);

        var chevronGeometry = CreateFormattedTextGeometry(chevronChar, typeface, chevronSize, chevronBrush);

        var chevronX = _leftPadding + indentLevel * _indentWidth - _scrollViewer.Offset.X;
        var chevronY = y + (_itemHeight - chevronSize) / 2;

        chevronGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(chevronX, chevronY));
        context.DrawGeometry(chevronBrush, null, chevronGeometry);
    }

    private void RenderItemText(DrawingContext context, FileItem item, int indentLevel, double y)
    {
        var textSize = 13;
        var typeface = new Typeface("San Francisco");

        var textBrush = GetTextBrushAccordingToGitStatus(item);

        var maxTextWidth = Math.Max(0, Bounds.Width - _leftPadding - indentLevel * _indentWidth - 65 - _scrollViewer.Offset.X);

        var formattedText = new FormattedText(
            item.Name,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            textSize,
            textBrush
        )
        {
            Trimming = TextTrimming.CharacterEllipsis,
            MaxLineCount = 1,
            MaxTextWidth = maxTextWidth
        };

        var textX = _leftPadding + indentLevel * _indentWidth + 40 - _scrollViewer.Offset.X;
        var textY = y + (_itemHeight - formattedText.Height) / 2 + 1;

        if (item.IsDirectory)
        {
            textX += 2;
        }

        context.DrawText(formattedText, new Point(textX, textY));
    }

    private SolidColorBrush GetTextBrushAccordingToGitStatus(FileItem item)
    {
        var gitStatus = _gitService.GetFileStatus(item.FullPath);
        switch (gitStatus)
        {
            case FileChangeType.Added:
                return new SolidColorBrush(Color.Parse("#4CAF50")); // Green
            case FileChangeType.Modified:
                return new SolidColorBrush(Color.Parse("#FFD700")); // Gold
            case FileChangeType.Deleted:
                return new SolidColorBrush(Color.Parse("#F44336")); // Red  
            case FileChangeType.Renamed:
                return new SolidColorBrush(Color.Parse("#007ACC")); // Blue
            default:
                return new SolidColorBrush(Color.Parse(_currentTheme.TextColor));
        }
    }

    private void RenderItemGitStatus(DrawingContext context, FileItem item, int indentLevel, double y, Rect viewport)
    {
        if (!_gitService.IsValidGitRepository(_gitService.GetRepositoryPath()))
            return;

        var gitStatus = _gitService.GetFileStatus(item.FullPath);
        if (gitStatus == null)
            return;

        var statusColor = GetStatusColor((FileChangeType)gitStatus, !item.IsDirectory);
        if (statusColor == Colors.Transparent)
            return;

        var statusBrush = new SolidColorBrush(statusColor);
        var circleSize = 8;
        var charSize = 12;
        var statusY = y + (_itemHeight - circleSize) / 2;
        var statusX = viewport.Width - _rightPadding - circleSize - _scrollViewer.Offset.X;

        if (item.IsDirectory)
        {
            var circleGeometry = new EllipseGeometry(new Rect(statusX, statusY, circleSize, circleSize));
            context.DrawGeometry(statusBrush, null, circleGeometry);
        }
        else
        {
            var statusChar = gitStatus switch
            {
                FileChangeType.Added => "A",
                FileChangeType.Modified => "M",
                FileChangeType.Deleted => "D",
                FileChangeType.Renamed => "R",
                _ => "U",
            };

            var typeface = new Typeface("Arial");

            var statusGeometry = CreateFormattedTextGeometry(statusChar, typeface, charSize, statusBrush);

            statusGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(statusX, statusY + (circleSize - charSize) / 2));
            context.DrawGeometry(statusBrush, null, statusGeometry);
        }
    }

    private Color GetStatusColor(FileChangeType status, bool isChar = false)
    {
        return status switch
        {
            FileChangeType.Added => isChar ? Color.Parse("#4CAF50") : Color.Parse("#704CAF50"),
            FileChangeType.Modified => isChar ? Color.Parse("#FFD700") : Color.Parse("#70FFD700"),
            FileChangeType.Deleted => isChar ? Color.Parse("#F44336") : Color.Parse("#70F44336"),
            FileChangeType.Renamed => isChar ? Color.Parse("#007ACC") : Color.Parse("#70007ACC"),
            _ => Colors.Transparent
        };
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

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var itemClicked = FindClickedItem(_items, point.Y, -_scrollViewer.Offset.Y);

        if (itemClicked != null)
        {
            _selectedItem = itemClicked;
            if (itemClicked.IsDirectory)
                ToggleDirectoryExpansion(itemClicked);
            else
                FileSelected?.Invoke(this, itemClicked.FullPath);

            UpdateCanvasSize();
            InvalidateVisual();
        }
    }

    private void ToggleDirectoryExpansion(FileItem directory)
    {
        directory.IsExpanded = !directory.IsExpanded;
        if (directory.IsExpanded && !directory.ChildrenPopulated) _ = PopulateChildrenAsync(directory);
    }

    private FileItem FindClickedItem(IEnumerable<FileItem> items, double clickY, double startY)
    {
        foreach (var item in items)
        {
            if (IsClickWithinItemBounds(clickY, startY))
                return item;

            startY += _itemHeight;

            if (item.IsExpanded)
            {
                var childResult = FindClickedItem(item.Children, clickY, startY);
                if (childResult != null) return childResult;
                startY += CalculateTotalHeight(item.Children);
            }

            if (startY > clickY) break;
        }

        return null;
    }

    private bool IsClickWithinItemBounds(double clickY, double startY)
    {
        return clickY >= startY && clickY < startY + _itemHeight;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                MoveSelection(-1);
                break;
            case Key.Down:
                MoveSelection(1);
                break;
            case Key.Left:
                CollapseOrMoveUp();
                break;
            case Key.Right:
                ExpandOrMoveDown();
                break;
            case Key.Enter:
                ActivateSelectedItem();
                break;
        }

        e.Handled = true;
    }

    private void MoveSelection(int direction)
    {
        var allItems = GetFlattenedItems(_items);
        var currentIndex = allItems.IndexOf(_selectedItem);
        var newIndex = (currentIndex + direction + allItems.Count) % allItems.Count;
        _selectedItem = allItems[newIndex];
        ScrollToItem(_selectedItem);
        InvalidateVisual();
    }

    private void CollapseOrMoveUp()
    {
        if (_selectedItem?.IsDirectory == true && _selectedItem.IsExpanded)
        {
            _selectedItem.IsExpanded = false;
            UpdateCanvasSize();
        }
        else
        {
            var parent = FindParentItem(_items, _selectedItem);
            if (parent != null)
            {
                _selectedItem = parent;
                ScrollToItem(_selectedItem);
            }
        }

        InvalidateVisual();
    }

    private void ExpandOrMoveDown()
    {
        if (_selectedItem?.IsDirectory == true && !_selectedItem.IsExpanded)
        {
            ToggleDirectoryExpansion(_selectedItem);
            UpdateCanvasSize();
        }
        else if (_selectedItem?.IsDirectory == true && _selectedItem.IsExpanded && _selectedItem.Children.Any())
        {
            _selectedItem = _selectedItem.Children.First();
            ScrollToItem(_selectedItem);
        }

        InvalidateVisual();
    }

    private void ActivateSelectedItem()
    {
        if (_selectedItem?.IsDirectory == true)
        {
            ToggleDirectoryExpansion(_selectedItem);
            UpdateCanvasSize();
        }

        InvalidateVisual();
    }

    private List<FileItem> GetFlattenedItems(IEnumerable<FileItem> items)
    {
        var result = new List<FileItem>();
        foreach (var item in items)
        {
            result.Add(item);
            if (item.IsExpanded) result.AddRange(GetFlattenedItems(item.Children));
        }

        return result;
    }

    private FileItem FindParentItem(IEnumerable<FileItem> items, FileItem target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target)) return item;
            if (item.IsExpanded)
            {
                var result = FindParentItem(item.Children, target);
                if (result != null) return result;
            }
        }

        return null;
    }

    private void ScrollToItem(FileItem item)
    {
        var allItems = GetFlattenedItems(_items);
        var index = allItems.IndexOf(item);
        var itemTop = index * _itemHeight;
        var itemBottom = itemTop + _itemHeight;

        var viewportTop = _scrollViewer.Offset.Y;
        var viewportBottom = viewportTop + _scrollViewer.Viewport.Height;

        var desiredOffset = CalculateDesiredOffset(itemTop, itemBottom, viewportTop, viewportBottom);

        if (desiredOffset != _scrollViewer.Offset.Y) UpdateScrollViewerOffset(desiredOffset);
    }

    private double CalculateDesiredOffset(double itemTop, double itemBottom, double viewportTop, double viewportBottom)
    {
        if (itemTop < viewportTop)
            return itemTop;
        if (itemBottom > viewportBottom)
            return itemBottom - _scrollViewer.Viewport.Height;
        return _scrollViewer.Offset.Y;
    }

    private void UpdateScrollViewerOffset(double desiredOffset)
    {
        var maxScrollOffset = Math.Max(0, _canvas.Bounds.Height - _scrollViewer.Viewport.Height);
        desiredOffset = Math.Max(0, Math.Min(desiredOffset, maxScrollOffset));
        _scrollViewer.Offset = new Point(0, desiredOffset);
    }
}