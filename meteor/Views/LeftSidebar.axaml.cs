using System;
using Avalonia.Controls;
using meteor.Interfaces;

namespace meteor.Views;

public partial class LeftSidebar : UserControl
{
    private readonly IEventAggregator _eventAggregator;

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