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

    public ISolidColorBrush BorderBrush { get; set; }
    public ISolidColorBrush Background { get; set; }
    public ISolidColorBrush DirtyIndicatorBrush { get; set; }
    public ISolidColorBrush Foreground { get; set; }
    public ISolidColorBrush CloseButtonForeground { get; set; }
    public ISolidColorBrush CloseButtonBackground { get; set; }
    public ICommand CloseTabCommand { get; set; }
    public string FilePath { get; set; }
    public bool IsDirty { get; set; }
    public bool IsTemporary { get; set; }

    public IEditorViewModel EditorViewModel { get; }

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
        FilePath = fileName;

        // Ideally we would make this Avalonia independent
        BorderBrush = new SolidColorBrush(ConvertToColor(configuration.GetBorderBrush()));
        Background = new SolidColorBrush(ConvertToColor(configuration.GetBackground()));
        DirtyIndicatorBrush = new SolidColorBrush(ConvertToColor(configuration.GetDirtyIndicatorBrush()));
        Foreground = new SolidColorBrush(ConvertToColor(configuration.GetForeground()));
        CloseButtonForeground = new SolidColorBrush(ConvertToColor(configuration.GetCloseButtonForeground()));
        CloseButtonBackground = new SolidColorBrush(ConvertToColor(configuration.GetCloseButtonBackground()));

        IsModified = false;
        IsActive = false;
        IsDirty = false;
        IsTemporary = false;
        CloseTabCommand = configuration.GetCloseTabCommand();
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