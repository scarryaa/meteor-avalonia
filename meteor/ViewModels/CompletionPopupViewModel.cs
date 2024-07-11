using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;

namespace meteor.ViewModels;

public class CompletionPopupViewModel : ViewModelBase
{
    private ObservableCollection<CompletionItem> _completionItems;
    private CompletionItem _selectedItem;
    private bool _isVisible;
    private bool _isFocused;

    public event EventHandler FocusRequested;
    public event EventHandler VisibilityChanged;
    public event EventHandler<CompletionItem> SuggestionApplied;

    public CompletionPopupViewModel(TextEditorViewModel textEditorViewModel)
    {
        TextEditorViewModel = textEditorViewModel;
        CompletionItems = new ObservableCollection<CompletionItem>();
        ApplyItemCommand = ReactiveCommand.Create<CompletionItem>(ApplyItem);
    }

    private void ApplyItem(CompletionItem item)
    {
        SelectedItem = item;
        ApplySelectedSuggestion(item);
    }

    public ICommand ApplyItemCommand { get; }

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

    public TextEditorViewModel TextEditorViewModel { get; }

    public bool IsFocused
    {
        get => _isFocused;
        set => this.RaiseAndSetIfChanged(ref _isFocused, value);
    }

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
        Console.WriteLine($"UpdateCompletionItems called with {items.Length} items");
        if (items == null || items.Length == 0)
        {
            Console.WriteLine("No items received, hiding popup");
            HidePopup();
            return;
        }

        CompletionItems = new ObservableCollection<CompletionItem>(items);
        SelectedItem = CompletionItems.FirstOrDefault();
        IsVisible = true;

        Console.WriteLine($"Completion popup updated with {CompletionItems.Count} items");
        Console.WriteLine("First 5 items:");
        foreach (var item in CompletionItems.Take(5)) Console.WriteLine($"  - {item.Label}");
    }

    public void HidePopup()
    {
        CompletionItems.Clear();
        SelectedItem = null;
        IsVisible = false;
        IsFocused = false;
    }

    public void SelectNextItem()
    {
        var currentIndex = CompletionItems.IndexOf(SelectedItem);
        if (currentIndex == -1) return;

        if (currentIndex < CompletionItems.Count - 1)
            SelectedItem = CompletionItems[currentIndex + 1];
        else
            SelectedItem = CompletionItems[0];
    }

    public void SelectPreviousItem()
    {
        var currentIndex = CompletionItems.IndexOf(SelectedItem);
        if (currentIndex == -1) return;

        if (currentIndex > 0)
            SelectedItem = CompletionItems[currentIndex - 1];
        else
            SelectedItem = CompletionItems[CompletionItems.Count - 1];
    }
    
    public void ApplySelectedSuggestion(CompletionItem item)
    {
        SuggestionApplied?.Invoke(this, item);
        HidePopup();
    }
}