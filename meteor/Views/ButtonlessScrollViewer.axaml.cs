using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace meteor.Views;

public partial class ButtonlessScrollViewer : ScrollViewer
{
    public static readonly DirectProperty<ButtonlessScrollViewer, Vector> CustomScrollOffsetProperty =
        AvaloniaProperty.RegisterDirect<ButtonlessScrollViewer, Vector>(
            nameof(CustomScrollOffset),
            o => o.CustomScrollOffset,
            (o, v) => o.CustomScrollOffset = v);

    private Vector _customScrollOffset;

    public ButtonlessScrollViewer()
    {
        InitializeComponent();
    }

    public Vector CustomScrollOffset
    {
        get => _customScrollOffset;
        set
        {
            SetAndRaise(CustomScrollOffsetProperty, ref _customScrollOffset, value);
            Offset = value;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OffsetProperty) CustomScrollOffset = Offset;
    }
}