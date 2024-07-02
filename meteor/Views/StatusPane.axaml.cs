using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace meteor.Views;

public partial class StatusPane : UserControl
{
    static StatusPane()
    {
        BackgroundProperty.OverrideMetadata<StatusPane>(new StyledPropertyMetadata<IBrush?>());
        BorderBrushProperty.OverrideMetadata<StatusPane>(new StyledPropertyMetadata<IBrush?>());
        ForegroundProperty.OverrideMetadata<StatusPane>(new StyledPropertyMetadata<IBrush?>());
    }

    public StatusPane()
    {
        InitializeComponent();
    }
}