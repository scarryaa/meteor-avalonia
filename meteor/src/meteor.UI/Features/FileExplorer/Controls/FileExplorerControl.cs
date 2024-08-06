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
using System.Timers;
using Timer = System.Timers.Timer;
using meteor.UI.Features.FileExplorer.Services;

namespace meteor.UI.Features.FileExplorer.Controls;

public class FileExplorerControl : UserControl
{
    private const int MaxItemsPerDirectory = 1000;
    private const int LazyLoadThreshold = 100;
    private readonly double _indentWidth = 8;
    private readonly double _itemHeight = 24;
    private ObservableCollection<FileItem> _items = new ObservableCollection<FileItem>();
    private readonly double _leftPadding = 8;
    private readonly double _rightPadding = 8;
    private readonly IThemeManager _themeManager;
    private readonly IGitService _gitService;
    private FileSystemHelper _fileSystemHelper;
    private FileItemRenderer _fileItemRenderer;
    private Canvas _canvas;
    private Theme _currentTheme;
    private Grid _mainGrid;
    private ScrollViewer _scrollViewer;
    private FileItem _selectedItem;
    private Button _selectPathButton;
    private ConcurrentDictionary<string, Task> _populationTasks = new ConcurrentDictionary<string, Task>();
    private Dictionary<string, Geometry> _iconCache = new Dictionary<string, Geometry>();
    private Dictionary<string, FormattedText> _textCache = new Dictionary<string, FormattedText>();
    private FileSystemWatcher _fileWatcher;
    private CancellationTokenSource _updateCancellationTokenSource;
    private Timer _refreshTimer;
    private const int RefreshInterval = 5000; // 5 seconds
    private GitStatusManager _gitStatusManager;

    public FileExplorerControl(IThemeManager themeManager, IGitService gitService)
    {
        _themeManager = themeManager;
        _gitService = gitService;
        _currentTheme = _themeManager.CurrentTheme;
        _themeManager.ThemeChanged += OnThemeChanged;

        InitializeComponent();
        UpdateCanvasSize();
        Focus();

        InitializeRefreshTimer();

        _gitStatusManager = new GitStatusManager(_gitService);
    }

    public event EventHandler<string> FileSelected;
    public event EventHandler<string> DirectoryOpened;

    public void SetDirectory(string path)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _selectPathButton.IsVisible = false;
            _items = new ObservableCollection<FileItem>();
            _items.Add(new FileItem(path, true));
            _ = PopulateChildrenAsync(_items[0]);
            _items[0].IsExpanded = true;
            UpdateCanvasSize();
            InvalidateVisual();

            // Cache the changes
            var changes = _gitService.GetChanges().ToList();
            foreach (var change in changes)
            {
                _gitStatusManager.GetFileStatus(change.FilePath);
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

    private void InitializeRefreshTimer()
    {
        _refreshTimer = new Timer(RefreshInterval);
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.Start();
    }

    private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            RefreshAllFileStatuses();
        });
    }

    public void RefreshAllFileStatuses()
    {
        _gitStatusManager.UpdateAllItemStatuses(_items);
        InvalidateVisual();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnoreFileChange(e.FullPath))
        {
            return; // Ignore changes to Git files
        }

        _updateCancellationTokenSource?.Cancel();
        _updateCancellationTokenSource = new CancellationTokenSource();

        Task.Delay(500, _updateCancellationTokenSource.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateFileStatus(e.FullPath);
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        UpdateFileList(e.FullPath, true);
                        break;
                    case WatcherChangeTypes.Deleted:
                        UpdateFileList(e.FullPath, false);
                        break;
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.All:
                        UpdateFileStatus(e.FullPath);
                        break;
                }
            });
        }, TaskScheduler.Default);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldIgnoreFileChange(e.OldFullPath) || ShouldIgnoreFileChange(e.FullPath))
        {
            return; // Ignore changes to Git files
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateFileList(e.OldFullPath, false);
            UpdateFileList(e.FullPath, true);
            UpdateFileStatus(e.FullPath);
        });
    }

    private bool ShouldIgnoreFileChange(string filePath)
    {
        if (_fileSystemHelper.ShouldHideFile(filePath))
        {
            return true;
        }

        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath);

        // Ignore changes to specific Git files
        var gitFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HEAD",
            "config",
            "description",
            "index",
            "packed-refs",
            "COMMIT_EDITMSG"
        };

        // Ignore changes to files in .git directory or its subdirectories
        if (directoryName != null && directoryName.Split(Path.DirectorySeparatorChar).Contains(".git"))
        {
            return true;
        }

        return gitFiles.Contains(fileName);
    }

    private void UpdateFileStatus(string filePath)
    {
        var newStatus = _gitStatusManager.GetFileStatus(filePath);

        var item = FindItemByPath(_items, filePath);
        if (item != null)
        {
            item.GitStatus = newStatus;
            UpdateParentDirectories(item);
            InvalidateVisual();
        }
    }

    private void UpdateParentDirectories(FileItem item)
    {
        var parent = FindParentItem(_items, item);
        while (parent != null)
        {
            _gitStatusManager.UpdateDirectoryStatus(parent);
            parent = FindParentItem(_items, parent);
        }
    }

    private FileItem FindParentItem(IEnumerable<FileItem> items, FileItem target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target)) return item;
            if (item.IsDirectory && item.IsExpanded)
            {
                var result = FindParentItem(item.Children, target);
                if (result != null) return result;
            }
        }
        return null;
    }

    private void UpdateFileList(string filePath, bool add)
    {
        if (_fileSystemHelper.ShouldHideFile(filePath))
        {
            return; // Don't add hidden files to the list
        }

        if (add)
        {
            var newItem = new FileItem(filePath, Directory.Exists(filePath));
            var parentPath = Path.GetDirectoryName(filePath);
            var parentItem = FindItemByPath(_items, parentPath);
            if (parentItem != null)
            {
                InsertItemInOrder(new ObservableCollection<FileItem>(parentItem.Children), newItem);
                UpdateParentDirectories(newItem);
            }
            else
            {
                InsertItemInOrder(_items, newItem);
            }
        }
        else
        {
            var itemToRemove = FindItemByPath(_items, filePath);
            if (itemToRemove != null)
            {
                var parent = FindParentItem(_items, itemToRemove);
                if (parent != null)
                {
                    parent.Children.Remove(itemToRemove);
                    UpdateParentDirectories(parent);
                }
                else
                {
                    _items.Remove(itemToRemove);
                }
            }
        }
        UpdateCanvasSize();
        InvalidateVisual();
    }

    private void InsertItemInOrder(ObservableCollection<FileItem> collection, FileItem newItem)
    {
        int index = 0;
        while (index < collection.Count &&
               string.Compare(collection[index].Name, newItem.Name, StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }
        collection.Insert(index, newItem);
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

        _fileItemRenderer = new FileItemRenderer(_currentTheme, _themeManager, _gitService, _scrollViewer, _selectedItem);
        _fileSystemHelper = new FileSystemHelper(_gitService);
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
                new Setter(Button.TemplateProperty, CreateButtonTemplate())
            }
        };
    }

    private Style CreateButtonHoverStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.TemplateProperty, CreateButtonTemplate(true))
            }
        };
    }

    private Style CreateButtonPressedStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":pressed"))
        {
            Setters =
            {
                new Setter(Button.TemplateProperty, CreateButtonTemplate(isPressed: true))
            }
        };
    }

    private Style CreateButtonDisabledStyle()
    {
        return new Style(x => x.OfType<Button>().Class("noBg").Class(":disabled"))
        {
            Setters =
            {
                new Setter(Button.TemplateProperty, CreateButtonTemplate(isDisabled: true))
            }
        };
    }

    private IControlTemplate CreateButtonTemplate(bool isPointerOver = false, bool isPressed = false,
        bool isDisabled = false)
    {
        return new FuncControlTemplate<Button>((parent, scope) =>
        {
            var contentPresenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                Background = new SolidColorBrush(Color.Parse(_currentTheme.BackgroundColor)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                [!ContentPresenter.ContentProperty] = parent[!ContentControl.ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = parent[!ContentControl.ContentTemplateProperty]
            };

            contentPresenter.Foreground = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));

            return contentPresenter;
        });
    }

    private void UpdateSelectPathButtonVisibility()
    {
        if (_selectPathButton != null && _items != null)
        {
            _selectPathButton.IsVisible = _items.Count == 0;
        }
    }

    private async void OnSelectPathButtonClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

        if (result != null && result.Count > 0)
        {
            var selectedFolder = result[0];
            var folderPath = selectedFolder.Path.LocalPath;
            SetDirectory(folderPath);
            var rootDirectoryPath = Path.GetFullPath(folderPath);
            DirectoryOpened?.Invoke(this, rootDirectoryPath);
        }
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
        var directories = _fileSystemHelper.GetDirectories(item.FullPath);
        var files = _fileSystemHelper.GetFiles(item.FullPath);

        children.AddRange(directories.Take(MaxItemsPerDirectory / 2));
        children.AddRange(files.Take(MaxItemsPerDirectory / 2));

        if (children.Count > MaxItemsPerDirectory)
        {
            children = children.Take(MaxItemsPerDirectory).ToList();
            children.Add(new FileItem("... (More items not shown)", item.FullPath, false));
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            item.Children.Clear();
            foreach (var child in children)
            {
                item.Children.Add(child);
            }
            item.ChildrenPopulated = true;
            UpdateCanvasSize();
            InvalidateVisual();
        });
    }

    public void ClearFileStatusCache()
    {
        _gitStatusManager.ClearCache();
        _gitStatusManager.UpdateAllItemStatuses(_items);
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
        return items.Any() ? items.Sum(item => _itemHeight + (item.IsExpanded ? CalculateTotalHeight(item.Children) : 0)) : 0;
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
        _fileItemRenderer.RenderItems(context, Bounds.Size, _items, 0, -_scrollViewer.Offset.Y + buttonHeight, viewportRect);
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
        UpdateCanvasSize();
        InvalidateVisual();
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
            case Key.PageUp:
                MoveSelectionByPage(-1);
                break;
            case Key.PageDown:
                MoveSelectionByPage(1);
                break;
            case Key.Home:
                MoveSelectionToEnd(-1);
                break;
            case Key.End:
                MoveSelectionToEnd(1);
                break;
            case Key.Left:
                HandleLeftKey();
                break;
            case Key.Right:
                HandleRightKey();
                break;
            case Key.Enter:
            case Key.Space:
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

    private void MoveSelectionByPage(int direction)
    {
        var itemsPerPage = (int)(_scrollViewer.Viewport.Height / _itemHeight);
        MoveSelection(direction * itemsPerPage);
    }

    private void MoveSelectionToEnd(int direction)
    {
        var allItems = GetFlattenedItems(_items);
        _selectedItem = direction < 0 ? allItems[0] : allItems[allItems.Count - 1];
        ScrollToItem(_selectedItem);
        InvalidateVisual();
    }

    private void HandleLeftKey()
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

    private void HandleRightKey()
    {
        if (_selectedItem?.IsDirectory == true)
        {
            if (!_selectedItem.IsExpanded)
            {
                ToggleDirectoryExpansion(_selectedItem);
                UpdateCanvasSize();
            }
            else if (_selectedItem.Children.Any())
            {
                _selectedItem = _selectedItem.Children[0];
                ScrollToItem(_selectedItem);
            }
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
        else if (_selectedItem != null)
        {
            FileSelected?.Invoke(this, _selectedItem.FullPath);
        }
        InvalidateVisual();
    }

    private List<FileItem> GetFlattenedItems(IEnumerable<FileItem> items)
    {
        var result = new List<FileItem>();
        var stack = new Stack<IEnumerator<FileItem>>();
        stack.Push(items.GetEnumerator());

        while (stack.Count > 0)
        {
            var enumerator = stack.Peek();
            if (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                result.Add(item);
                if (item.IsExpanded && item.Children.Any())
                {
                    stack.Push(item.Children.GetEnumerator());
                }
            }
            else
            {
                stack.Pop();
                enumerator.Dispose();
            }
        }

        return result;
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