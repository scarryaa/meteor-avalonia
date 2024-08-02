using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.UI.Features.Tabs.ViewModels;

public class TabViewModel : ITabViewModel
{
    private string _content;
    private string _fileName;
    private string _filePath;
    private bool _isActive;
    private bool _isModified;
    private string _originalContent;
    private string _title;
    private readonly ITabViewModelConfig _configuration;
    private readonly IThemeManager _themeManager;

    public TabViewModel(IEditorViewModel editorViewModel, string filePath, string fileName,
        ITabViewModelConfig configuration, IThemeManager themeManager)
    {
        EditorViewModel = editorViewModel;
        Title = fileName;
        _content = string.Empty;
        _originalContent = string.Empty;
        _fileName = fileName;
        _filePath = filePath;
        _configuration = configuration;
        _themeManager = themeManager;

        UpdateThemeProperties();

        IsModified = false;
        IsActive = false;
        CloseTabCommand = configuration.GetCloseTabCommand();

        EditorViewModel.ContentChanged += OnEditorContentChanged;
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public Core.Models.Color BorderBrushColor { get; private set; }
    public Core.Models.Color BackgroundColor { get; private set; }
    public Core.Models.Color DirtyIndicatorColor { get; private set; }
    public Core.Models.Color ForegroundColor { get; private set; }
    public Core.Models.Color CloseButtonForegroundColor { get; private set; }
    public Core.Models.Color CloseButtonBackgroundColor { get; private set; }
    public ICommand CloseTabCommand { get; }

    public double ScrollPositionX { get; set; }
    public double ScrollPositionY { get; set; }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public bool IsTemporary { get; private set; }

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value)) IsModified = _content != _originalContent;
        }
    }

    public IEditorViewModel EditorViewModel { get; }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public void SaveScrollPosition(double x, double y)
    {
        ScrollPositionX = x;
        ScrollPositionY = y;
    }

    public void LoadContent(string content)
    {
        EditorViewModel.LoadContent(content);
        SetOriginalContent(content);
    }

    public void SetOriginalContent(string content)
    {
        _originalContent = content;
        Content = content;
        IsModified = false;
    }

    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
        Title = Path.GetFileName(filePath);
        IsTemporary = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        Content = EditorViewModel.Content;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void UpdateThemeProperties()
    {
        var theme = _themeManager.CurrentTheme;
        if (theme == null) return;

        BorderBrushColor = Color.FromHex(theme.BorderBrush);
        BackgroundColor = Color.FromHex(theme.TabBackgroundColor);
        DirtyIndicatorColor = Color.FromHex(theme.DirtyIndicatorBrush);
        ForegroundColor = Color.FromHex(theme.TabForegroundColor);
        CloseButtonForegroundColor = Color.FromHex(theme.CloseButtonForeground);
        CloseButtonBackgroundColor = Color.FromHex(theme.CloseButtonBackground);
    }

    private void OnThemeChanged(object sender, Theme newTheme)
    {
        UpdateThemeProperties();
    }
}