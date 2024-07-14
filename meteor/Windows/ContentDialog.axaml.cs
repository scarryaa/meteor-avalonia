using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using meteor.Enums;
using meteor.ViewModels;

namespace meteor.Windows;

public partial class ContentDialog : Window
{
    private TaskCompletionSource<ContentDialogResult> _resultCompletionSource;
    private ContentDialogViewModel ViewModel => (ContentDialogViewModel)DataContext;

    public ContentDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var buttonStack = this.FindControl<StackPanel>("ButtonStack");

        if (!string.IsNullOrEmpty(ViewModel.PrimaryButtonText))
        {
            var primaryButton = new Button
                { Content = ViewModel.PrimaryButtonText, Foreground = Brushes.Black };
            primaryButton.Click += (_, __) => Close(ContentDialogResult.Primary);
            buttonStack.Children.Add(primaryButton);
        }

        if (!string.IsNullOrEmpty(ViewModel.SecondaryButtonText))
        {
            var secondaryButton = new Button
                { Content = ViewModel.SecondaryButtonText, Foreground = Brushes.Black };
            secondaryButton.Click += (_, __) => Close(ContentDialogResult.Secondary);
            buttonStack.Children.Add(secondaryButton);
        }

        if (!string.IsNullOrEmpty(ViewModel.CloseButtonText))
        {
            var closeButton = new Button
                { Content = ViewModel.CloseButtonText, Foreground = Brushes.Black };
            closeButton.Click += (_, __) => Close(ContentDialogResult.None);
            buttonStack.Children.Add(closeButton);
        }
    }

    public new Task<ContentDialogResult> ShowDialog(Window owner)
    {
        _resultCompletionSource = new TaskCompletionSource<ContentDialogResult>();
        base.ShowDialog(owner);
        return _resultCompletionSource.Task;
    }

    private void Close(ContentDialogResult result)
    {
        _resultCompletionSource.TrySetResult(result);
        base.Close();
    }
}