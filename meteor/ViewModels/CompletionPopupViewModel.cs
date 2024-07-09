using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;

namespace meteor.ViewModels;

public class CompletionPopupViewModel : ViewModelBase
{
    private ObservableCollection<CompletionItem> _completionItems;
    private CompletionItem _selectedItem;
    private bool _isVisible;

    public event EventHandler FocusRequested;
    public event EventHandler VisibilityChanged;

    public CompletionPopupViewModel()
    {
        CompletionItems = new ObservableCollection<CompletionItem>();
    }

    public ObservableCollection<CompletionItem> CompletionItems
    {
        get => _completionItems;
        set => this.RaiseAndSetIfChanged(ref _completionItems, value);
    }

    public CompletionItem SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public bool IsFocused { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _isVisible, value)) VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateCompletionItems(CompletionItem[] items)
    {
        if (items == null || items.Length == 0)
        {
            HidePopup();
            return;
        }

        var relevantItems = items.Where(IsRelevantCompletionItem).ToArray();
        if (relevantItems.Length == 0)
        {
            HidePopup();
            return;
        }

        CompletionItems = new ObservableCollection<CompletionItem>(relevantItems);
        SelectedItem = CompletionItems.FirstOrDefault();
        IsVisible = true;
    }

    private bool IsRelevantCompletionItem(CompletionItem item)
    {
        return true;
    }

    private void HidePopup()
    {
        CompletionItems.Clear();
        SelectedItem = null;
        IsVisible = false;
        IsFocused = false;
    }

    public void FocusPopup()
    {
        FocusRequested?.Invoke(this, EventArgs.Empty);
        IsFocused = true;
    }

    public void SelectNextItem()
    {
        var currentIndex = CompletionItems.IndexOf(SelectedItem);
        if (currentIndex < CompletionItems.Count - 1)
            SelectedItem = CompletionItems[currentIndex + 1];
    }

    public void SelectPreviousItem()
    {
        var currentIndex = CompletionItems.IndexOf(SelectedItem);
        if (currentIndex > 0)
            SelectedItem = CompletionItems[currentIndex - 1];
    }

    public void ApplySelectedSuggestion(CompletionItem item)
    {
        HidePopup();
    }
}