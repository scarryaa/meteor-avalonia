using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.ViewModels;

public class TabViewModel : ITabViewModel
{
    private string _title;
    private bool _isModified;
    private bool _isActive;
    private string _content;
    private string _originalContent;
    private string _filePath;

    public ISolidColorBrush BorderBrush { get; set; }
    public ISolidColorBrush Background { get; set; }
    public ISolidColorBrush DirtyIndicatorBrush { get; set; }
    public ISolidColorBrush Foreground { get; set; }
    public ISolidColorBrush CloseButtonForeground { get; set; }
    public ISolidColorBrush CloseButtonBackground { get; set; }
    public ICommand CloseTabCommand { get; set; }

    public string FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
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
        get
        {
            if (IsTemporary)
                return $"* {Title}";
            return Title;
        }
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

    public TabViewModel(IEditorViewModel editorViewModel, string fileName, ITabViewModelConfig configuration)
    {
        EditorViewModel = editorViewModel;
        Title = fileName;
        FilePath = string.Empty;

        _content = string.Empty;
        _originalContent = string.Empty;

        // Ideally we would make this Avalonia independent
        BorderBrush = new SolidColorBrush(ConvertToColor(configuration.GetBorderBrush()));
        Background = new SolidColorBrush(ConvertToColor(configuration.GetBackground()));
        DirtyIndicatorBrush = new SolidColorBrush(ConvertToColor(configuration.GetDirtyIndicatorBrush()));
        Foreground = new SolidColorBrush(ConvertToColor(configuration.GetForeground()));
        CloseButtonForeground = new SolidColorBrush(ConvertToColor(configuration.GetCloseButtonForeground()));
        CloseButtonBackground = new SolidColorBrush(ConvertToColor(configuration.GetCloseButtonBackground()));

        IsModified = false;
        IsActive = false;
        CloseTabCommand = configuration.GetCloseTabCommand();

        EditorViewModel.ContentChanged += OnEditorContentChanged;
    }

    public void LoadContent(string content)
    {
        EditorViewModel.LoadContent(content);
        SetOriginalContent(content);
    }

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        Content = EditorViewModel.Content;
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

    private Color ConvertToColor(Core.Interfaces.Models.ISolidColorBrush brush)
    {
        return Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
    }
}