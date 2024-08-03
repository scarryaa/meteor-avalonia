using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Controls.Presenters;
using Avalonia.Media.TextFormatting;
using meteor.Core.Interfaces.Services;
using Avalonia.Platform.Storage;
using meteor.UI.Features.Tabs.ViewModels;
using meteor.UI.Features.Editor.ViewModels;
using meteor.Core.Services;
using meteor.UI.Services;
using meteor.Core.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using System.IO;

namespace meteor.UI.Features.CommandPalette.Controls;

public class CommandPalette : UserControl
{
    private readonly IThemeManager _themeManager;
    private readonly IFileService _fileService;
    private readonly ITabService _tabService;
    private readonly ITabViewModelFactory _tabViewModelFactory;
    private readonly ITextMeasurer _textMeasurer;
    private readonly ICompletionProvider _completionProvider;
    private readonly ObservableCollection<CommandItem> _commands;
    private ListBox _resultsList;
    private TextBox _searchBox;
    private readonly UndoRedoManager _undoRedoManager;

    public CommandPalette(IThemeManager themeManager, IFileService fileService, ITabService tabService, ITabViewModelFactory tabViewModelFactory, ITextMeasurer textMeasurer, UndoRedoManager undoRedoManager)
    {
        _themeManager = themeManager;
        _fileService = fileService;
        _tabService = tabService;
        _tabViewModelFactory = tabViewModelFactory;
        _textMeasurer = textMeasurer;
        _undoRedoManager = undoRedoManager;
        _commands = new ObservableCollection<CommandItem>
        {
            new CommandItem("Switch Theme", () => _themeManager.ApplyTheme(_themeManager.CurrentTheme.Name == "Light" ? "Dark" : "Light")),
            new CommandItem("Open File", async () => await OpenFile()),
            new CommandItem("Save File", SaveFile, false),
            new CommandItem("Close Tab", CloseTab, false)
        };
        InitializeComponent();
        ApplyTheme();
        _themeManager.ThemeChanged += (_, _) => ApplyTheme();
        _tabService.ActiveTabChanged += (_, _) => UpdateCommandVisibility();
    }

    private void InitializeComponent()
    {
        _searchBox = new TextBox
        {
            Watermark = "Search commands...",
            Margin = new Thickness(10, 10, 10, 5),
            Classes = { "search-box" }
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

        _resultsList = new ListBox
        {
            ItemsSource = _commands.Where(c => c.IsVisible),
            Margin = new Thickness(5, 0, 5, 5),
            Classes = { "results-list" }
        };

        _resultsList.ItemTemplate = new FuncDataTemplate<CommandItem>((item, _) =>
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var icon = new TextBlock
            {
                Text = GetIconForCommand(item?.Name),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                FontFamily = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free")
            };

            var text = new TextBlock
            {
                Text = item?.Name ?? "Unknown Command",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };

            panel.Children.Add(icon);
            panel.Children.Add(text);

            return panel;
        });

        Content = new Border
        {
            MinWidth = 400,
            MaxWidth = 600,
            MinHeight = 50,
            MaxHeight = 400,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(10, 0, 10, 10),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 2,
                Blur = 4,
                Spread = 0,
                Color = Color.FromArgb(40, 0, 0, 0)
            }),
            Child = new StackPanel
            {
                Children =
                {
                    _searchBox,
                    _resultsList
                }
            }
        };

        KeyDown += CommandPalette_KeyDown;
        PropertyChanged += (sender, args) =>
        {
            if (args.Property.Name == nameof(IsVisible))
            {
                CommandPalette_IsVisibleChanged(sender, args);
            }
        };
    }

    private string GetIconForCommand(string commandName)
    {
        return commandName switch
        {
            "Switch Theme" => "\uf53f", // fa-palette
            "Open File" => "\uf07c",    // fa-folder-open
            "Save File" => "\uf0c7",    // fa-save
            "Close Tab" => "\uf00d",    // fa-times
            _ => "\uf002"               // fa-search
        };
    }

    private void ApplyTheme()
    {
        var theme = _themeManager.CurrentTheme;
        Background = Brushes.Transparent;

        var styles = new Styles
        {
            new Style(x => x.OfType<TextBox>().Class("search-box"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteBackgroundColor))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteForegroundColor))),
                    new Setter(BorderThicknessProperty, new Thickness(0, 0, 0, 0)),
                    new Setter(BorderBrushProperty, new SolidColorBrush(Colors.Transparent)),
                    new Setter(PaddingProperty, new Thickness(10)),
                    new Setter(FontSizeProperty, 14d),
                    new Setter(FontWeightProperty, FontWeight.Regular),
                    new Setter(TextBox.CaretBrushProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteForegroundColor))),
                    new Setter(TextBox.SelectionBrushProperty, new SolidColorBrush(Color.Parse(theme.SelectionColor))),
                }
            },
            new Style(x => x.OfType<TextBox>().Class("search-box").Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteBackgroundColor))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteForegroundColor))),
                    new Setter(BorderBrushProperty, new SolidColorBrush(Colors.Transparent)),
                    new Setter(BorderThicknessProperty, new Thickness(0))
                }
            },
            new Style(x => x.OfType<ListBox>().Class("results-list"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, Brushes.Transparent),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(theme.CommandPaletteForegroundColor))),
                    new Setter(BorderThicknessProperty, new Thickness(0)),
                    new Setter(PaddingProperty, new Thickness(5))
                }
            },
            new Style(x => x.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(BackgroundProperty, Brushes.Transparent),
                    new Setter(PaddingProperty, new Thickness(5)),
                    new Setter(MarginProperty, new Thickness(0, 2)),
                    new Setter(CornerRadiusProperty, new CornerRadius(2))
                }
            },
            new Style(x => x.OfType<ListBoxItem>().Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.Parse(theme.HighlightBrush)))
                }
            },
            new Style(x => x.OfType<ListBoxItem>().Class(":selected"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.Parse(theme.AccentBrush))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Colors.White))
                }
            }
        };

        Styles.Clear();
        foreach (var style in styles)
        {
            Styles.Add(style);
        }

        if (Content is Border border)
        {
            border.Background = new SolidColorBrush(Color.Parse(theme.CommandPaletteBackgroundColor));
            border.BorderBrush = new SolidColorBrush(Color.Parse(theme.BorderBrush));
            border.BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 2,
                Blur = 8,
                Spread = 0,
                Color = Color.FromArgb(30, 0, 0, 0)
            });
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = _searchBox.Text.ToLower();
        var filteredCommands = _commands
            .Where(c => c.Name.ToLower().Contains(searchText) && c.IsVisible)
            .ToList();
        _resultsList.ItemsSource = filteredCommands;
    }

    private void CommandPalette_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                break;
            case Key.Up:
                if (_resultsList.SelectedIndex > 0) _resultsList.SelectedIndex--;
                e.Handled = true;
                break;
            case Key.Down:
                if (_resultsList.SelectedIndex < _resultsList.ItemCount - 1) _resultsList.SelectedIndex++;
                e.Handled = true;
                break;
            case Key.Enter:
                ExecuteSelectedCommand();
                e.Handled = true;
                break;
        }
    }

    private void ExecuteSelectedCommand()
    {
        if (_resultsList.SelectedItem is CommandItem selectedCommand)
        {
            selectedCommand.Execute();
            Hide();
        }
    }

    private void CommandPalette_IsVisibleChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            Dispatcher.UIThread.InvokeAsync(() => _searchBox.Focus());
        }
        else
        {
            _searchBox.Text = string.Empty;
        }
    }

    public void Show()
    {
        IsVisible = true;
        _searchBox.Text = string.Empty;
        _resultsList.SelectedIndex = -1;
        UpdateCommandVisibility();
        _resultsList.ItemsSource = _commands.Where(c => c.IsVisible).ToList();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private async Task OpenFile()
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider != null)
        {
            var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                try
                {
                    var content = await _fileService.OpenFileAsync(filePath);
                    if (content != null)
                    {
                        var fileName = Path.GetFileName(filePath);
                        var tabConfig = new TabConfig(_tabService, _themeManager);
                        var editorViewModel = CreateEditorViewModel();
                        if (editorViewModel != null)
                        {
                            editorViewModel.LoadContent(content);
                            var tabViewModel = _tabViewModelFactory.Create(editorViewModel, tabConfig, filePath, fileName, _themeManager);
                            if (tabViewModel != null)
                            {
                                _tabService.AddTab(editorViewModel, tabConfig, fileName, filePath, content);
                            }
                            else
                            {
                                Console.WriteLine("Error: Failed to create tab view model.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error: Failed to create editor view model.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: File content is null.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening file: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        else
        {
            Console.WriteLine("Error: Storage provider is null.");
        }
    }

    private EditorViewModel CreateEditorViewModel()
    {
        var textBuffer = new TextBuffer();
        var editorConfig = new EditorConfig();
        var textBufferService = new TextBufferService(textBuffer, _textMeasurer, editorConfig);
        var cursorManager = new CursorManager(textBufferService, editorConfig);
        var clipboardManager = new ClipboardManager { TopLevelRef = TopLevel.GetTopLevel(this) };
        var selectionManager = new SelectionManager(textBufferService);
        var textAnalysisService = new TextAnalysisService();
        var inputManager = new InputManager(textBufferService, cursorManager, clipboardManager, selectionManager,
            textAnalysisService,
            new ScrollManager(editorConfig, _textMeasurer),
            _undoRedoManager);
        var editorViewModel = new EditorViewModel(
            textBufferService,
            cursorManager,
            inputManager,
            selectionManager,
            editorConfig,
            _textMeasurer,
            new CompletionProvider(textBufferService),
            _undoRedoManager);
        inputManager.SetViewModel(editorViewModel);

        return editorViewModel;
    }

    private void SaveFile()
    {
        if (_tabService.ActiveTab?.FilePath != null)
            _fileService.SaveFileAsync(_tabService.ActiveTab.FilePath, _tabService.ActiveTab.Content);
    }

    private void CloseTab()
    {
        _tabService.RemoveTab(_tabService.ActiveTab);
    }

    private void UpdateCommandVisibility()
    {
        var hasActiveTab = _tabService.ActiveTab != null;
        _commands.First(c => c.Name == "Save File").IsVisible = hasActiveTab;
        _commands.First(c => c.Name == "Close Tab").IsVisible = hasActiveTab;
        _resultsList.ItemsSource = _commands.Where(c => c.IsVisible).ToList();
    }
}

public class CommandItem
{
    public string Name { get; }
    public Action Execute { get; }
    public bool IsVisible { get; set; }

    public CommandItem(string name, Action execute, bool isVisible = true)
    {
        Name = name;
        Execute = execute;
        IsVisible = isVisible;
    }
}