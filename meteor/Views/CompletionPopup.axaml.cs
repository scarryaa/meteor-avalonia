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
        DataContextChanged += CompletionPopup_DataContextChanged;
        _suggestionListBox.PointerPressed += SuggestionListBox_PointerPressed;
        _suggestionListBox.AttachedToVisualTree += SuggestionListBox_AttachedToVisualTree;
        Deactivated += OnDeactivated;
        PointerPressed += OnPointerPressed;
    }

    private void SuggestionListBox_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        for (var i = 0; i < _suggestionListBox.ItemCount; i++)
            if (_suggestionListBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem listBoxItem)
            {
                listBoxItem.PointerEntered += (s, ev) => ev.Handled = true;
                listBoxItem.PointerExited += (s, ev) => ev.Handled = true;
            }
    }

    private void SuggestionListBox_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual visual)
        {
            var point = e.GetPosition(_suggestionListBox);
            var item = _suggestionListBox.InputHitTest(point);

            if (item != null && item is CompletionItem completionItem)
            {
                _suggestionListBox.SelectedItem = completionItem;
                ApplySelectedSuggestion();
                e.Handled = true;
            }
        }
    }

    private void SuggestionItem_DoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is CompletionItem item)
        {
            _suggestionListBox.SelectedItem = item;
            ApplySelectedSuggestion();
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!SuggestionListBox.IsPointerOver) Hide();
    }

    private void CompletionPopup_DataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is CompletionPopupViewModel viewModel)
        {
            if (sender is CompletionPopupViewModel oldViewModel)
                oldViewModel.FocusRequested -= ViewModel_FocusRequested;
            
            viewModel.FocusRequested += ViewModel_FocusRequested;
        }
    }

    private void ViewModel_FocusRequested(object sender, EventArgs e)
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is CompletionPopupViewModel viewModel)
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
                case Key.Space:
                    Hide();
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                    viewModel.TextEditorViewModel.HandleKeyPress();
                    break;
                default:
                    viewModel.TextEditorViewModel.HandleKeyPress();
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
        if (_suggestionListBox.ItemCount == 0) return;

        _suggestionListBox.SelectedIndex = (_suggestionListBox.SelectedIndex + 1) % _suggestionListBox.ItemCount;
        (_suggestionListBox.ContainerFromIndex(_suggestionListBox.SelectedIndex) as ListBoxItem)?.Focus();
    }

    public void SelectPreviousItem()
    {
        if (_suggestionListBox.ItemCount == 0) return;

        _suggestionListBox.SelectedIndex = (_suggestionListBox.SelectedIndex - 1 + _suggestionListBox.ItemCount) %
                                           _suggestionListBox.ItemCount;
        (_suggestionListBox.ContainerFromIndex(_suggestionListBox.SelectedIndex) as ListBoxItem)?.Focus();
    }

    private void ApplySelectedSuggestion()
    {
        if (DataContext is CompletionPopupViewModel viewModel && viewModel.SelectedItem != null)
        {
            viewModel.ApplySelectedSuggestion(viewModel.SelectedItem);
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
