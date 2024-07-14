using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Interfaces;

namespace meteor.Views;

public partial class LeftSidebar : UserControl
{
    private readonly IEventAggregator _eventAggregator;

    static LeftSidebar()
    {
        BackgroundProperty.OverrideMetadata<LeftSidebar>(new StyledPropertyMetadata<IBrush?>());
        BorderBrushProperty.OverrideMetadata<LeftSidebar>(new StyledPropertyMetadata<IBrush?>());
        ForegroundProperty.OverrideMetadata<LeftSidebar>(new StyledPropertyMetadata<IBrush?>());
    }
    
    public LeftSidebar()
    {
        InitializeComponent();
        var fileExplorer = this.FindControl<FileExplorer>("FileExplorer");
        if (fileExplorer != null) fileExplorer.FileClicked += OnFileExplorerFileClicked;
        if (fileExplorer != null) fileExplorer.FileDoubleClicked += OnFileExplorerFileDoubleClicked;
    }

    public event EventHandler<string> FileClicked;
    public event EventHandler<string> FileDoubleClicked;

    private void OnFileExplorerFileClicked(object sender, string filePath)
    {
        FileClicked?.Invoke(this, filePath);
    }

    private void OnFileExplorerFileDoubleClicked(object sender, string filePath)
    {
        FileDoubleClicked?.Invoke(this, filePath);
    }
}