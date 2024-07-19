using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.ViewModels;

public sealed class TabItemViewModel : ITabItemViewModel
{
    private bool _isSelected;
    private bool _isDirty;
    private bool _isTemporary;
    private string _title;
    private IEditorViewModel _editorViewModel;

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public IEditorViewModel EditorViewModel
    {
        get => _editorViewModel;
        set
        {
            if (_editorViewModel != value)
            {
                _editorViewModel = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsTemporary
    {
        get => _isTemporary;
        set
        {
            if (_isTemporary != value)
            {
                _isTemporary = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public TabItemViewModel(string title, IEditorViewModel editorViewModel)
    {
        Title = title;
        EditorViewModel = editorViewModel;
    }

    public void Dispose()
    {
        // TODO dispose editor view model?
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}