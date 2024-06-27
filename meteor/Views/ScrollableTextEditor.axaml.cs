using System;
using Avalonia;
using Avalonia.Controls;
using meteor.ViewModels;

namespace meteor.Views;

public partial class ScrollableTextEditor : UserControl
{
    public ScrollableTextEditor()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DataContext = new ScrollableTextEditorViewModel();
    }

    private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ScrollableTextEditorViewModel viewModel)
        {
            viewModel.TextEditorViewModel.WindowWidth = Bounds.Width;
            viewModel.TextEditorViewModel.WindowHeight = Bounds.Height;
            this.GetObservable(BoundsProperty).Subscribe(bounds =>
            {
                viewModel.TextEditorViewModel.WindowWidth = bounds.Width;
                viewModel.TextEditorViewModel.WindowHeight = bounds.Height;
            });
        }
    }
}