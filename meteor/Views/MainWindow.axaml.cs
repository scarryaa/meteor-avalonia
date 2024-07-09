using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();

        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(bounds =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.WindowWidth = bounds.Width;
                viewModel.WindowHeight = bounds.Height;
            }
        }));

        var leftSidebar = this.FindControl<LeftSidebar>("LeftSidebar");
        if (leftSidebar != null) leftSidebar.FileClicked += OnFileClicked;
        if (leftSidebar != null) leftSidebar.FileDoubleClicked += OnFileDoubleClicked;
    }

    private void OnBottomPaneSplitterDragDelta(object sender, VectorEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Adjust the height based on the drag delta
            var newHeight = viewModel.BottomPaneViewModel.BottomPaneHeight + e.Vector.Y;

            newHeight = Math.Max(100, Math.Min(newHeight, Bounds.Height / 2));

            viewModel.BottomPaneViewModel.BottomPaneHeight = newHeight;
        }
    }
    
    private void OnFileClicked(object sender, string filePath)
    {
        if (DataContext is MainWindowViewModel viewModel) viewModel.OnFileClicked(filePath);
    }

    private void OnFileDoubleClicked(object sender, string filePath)
    {
        if (DataContext is MainWindowViewModel viewModel) viewModel.OnFileDoubleClicked(filePath);
    }

    private void OnOverlayPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel) viewModel.CommandPaletteViewModel.Close();
    }

}