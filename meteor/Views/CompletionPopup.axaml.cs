using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using meteor.ViewModels;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace meteor.Views;

public partial class CompletionPopup : Window
{
    private readonly ListBox _suggestionListBox;
    private bool _isFirstFocus = true;
    private bool _shouldShowSuggestions;

    public CompletionPopup()
    {
        InitializeComponent();
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = null;

        Transitions = null;

        _suggestionListBox = this.FindControl<ListBox>("SuggestionListBox");
        _suggestionListBox.KeyDown += SuggestionListBox_KeyDown;
        DataContextChanged += CompletionPopup_DataContextChanged;
        _suggestionListBox.PointerPressed += SuggestionListBox_PointerPressed;
    }

    private void SuggestionListBox_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_suggestionListBox).Properties.IsLeftButtonPressed)
        {
            ApplySelectedSuggestion();
            e.Handled = true;
        }
    }

    private void CompletionPopup_DataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is CompletionPopupViewModel viewModel)
        {
            // Unsubscribe from the old event (if any)
            if (sender is CompletionPopupViewModel oldViewModel)
                oldViewModel.FocusRequested -= ViewModel_FocusRequested;

            // Subscribe to the new event
            viewModel.FocusRequested += ViewModel_FocusRequested;
        }
    }

    private async void ViewModel_FocusRequested(object sender, EventArgs e)
    {
        if (_isFirstFocus)
        {
            FocusListBox();
            _isFirstFocus = false;
        }

        _shouldShowSuggestions = true;
        if (_shouldShowSuggestions) ShowSuggestions();
    }

    private void ShowSuggestions()
    {
        _suggestionListBox.IsVisible = true;
        _suggestionListBox.IsHitTestVisible = true;
    }

    private void SuggestionListBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                SelectPreviousItem();
                e.Handled = true;
                break;
            case Key.Down:
                SelectNextItem();
                e.Handled = true;
                break;
            case Key.Enter:
                ApplySelectedSuggestion();
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }

    public void FocusListBox()
    {
        if (_suggestionListBox != null)
        {
            _suggestionListBox.Focus();
            if (_suggestionListBox.ItemCount > 0)
            {
                if (_suggestionListBox.SelectedIndex == -1) _suggestionListBox.SelectedIndex = 0;
                (_suggestionListBox.ContainerFromIndex(_suggestionListBox.SelectedIndex) as ListBoxItem)?.Focus();
            }
        }
    }

    public void SelectNextItem()
    {
        if (_suggestionListBox.SelectedIndex < _suggestionListBox.ItemCount - 1)
        {
            _suggestionListBox.SelectedIndex++;
            (_suggestionListBox.ContainerFromIndex(_suggestionListBox.SelectedIndex) as ListBoxItem)?.Focus();
        }
    }

    public void SelectPreviousItem()
    {
        if (_suggestionListBox.SelectedIndex > 0)
        {
            _suggestionListBox.SelectedIndex--;
            (_suggestionListBox.ContainerFromIndex(_suggestionListBox.SelectedIndex) as ListBoxItem)?.Focus();
        }
    }

    private void ApplySelectedSuggestion()
    {
        if (_suggestionListBox.SelectedItem is CompletionItem selectedItem)
        {
            (DataContext as CompletionPopupViewModel)?.ApplySelectedSuggestion(selectedItem);
            Hide();
        }
    }

    public void SetPosition(double left, double top)
    {
        Position = new PixelPoint((int)left, (int)top);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Opacity = 1;
        _isFirstFocus = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Opacity = 0;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _shouldShowSuggestions = false;
        Close();
    }

    private void SuggestionListBox_GotFocus(object sender, GotFocusEventArgs e)
    {
        if (_isFirstFocus)
        {
            _isFirstFocus = false;
            FocusListBox();
        }
    }
}