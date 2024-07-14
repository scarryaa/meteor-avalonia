using System.ComponentModel;

namespace meteor.ViewModels;

public class ContentDialogViewModel : ViewModelBase
{
    private string _title;
    private object _content;
    private string _primaryButtonText;
    private string _secondaryButtonText;
    private string _closeButtonText;

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public object Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }

    public string PrimaryButtonText
    {
        get => _primaryButtonText;
        set
        {
            if (_primaryButtonText != value)
            {
                _primaryButtonText = value;
                OnPropertyChanged(nameof(PrimaryButtonText));
            }
        }
    }

    public string SecondaryButtonText
    {
        get => _secondaryButtonText;
        set
        {
            if (_secondaryButtonText != value)
            {
                _secondaryButtonText = value;
                OnPropertyChanged(nameof(SecondaryButtonText));
            }
        }
    }

    public string CloseButtonText
    {
        get => _closeButtonText;
        set
        {
            if (_closeButtonText != value)
            {
                _closeButtonText = value;
                OnPropertyChanged(nameof(CloseButtonText));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}