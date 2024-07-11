using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using meteor.ViewModels;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace meteor.Views.Services;

public class CompletionService
{
    private readonly TextEditorViewModel _textEditorViewModel;
    private CompletionPopup _popupWindow;
    private CancellationTokenSource _completionCancellationTokenSource;
    private const int DefaultCompletionDebounceTime = 1;

    public CompletionService(TextEditorViewModel textEditorViewModel)
    {
        _textEditorViewModel = textEditorViewModel;
        _popupWindow = new CompletionPopup();
        _popupWindow.DataContext = _textEditorViewModel.CompletionPopupViewModel;
    }

    public void ShowSuggestions(object? sender, RoutedEventArgs e)
    {
        if (!_textEditorViewModel.CompletionPopupViewModel.IsVisible) UpdatePopupPosition(sender, e);
    }

    public void UpdatePopupPosition(object sender, RoutedEventArgs e)
    {
        if (e is TextInputEventArgs || (e is KeyEventArgs ke && ke.Key != Key.Tab && ke.Key != Key.Escape))
        {
            var wordStartPosition = _textEditorViewModel.GetWordStartPosition(_textEditorViewModel.CursorPosition);
            var caretPosition = _textEditorViewModel.GetCaretScreenPosition(sender as Control, wordStartPosition);

            _textEditorViewModel.PopupLeft = caretPosition.X;
            _textEditorViewModel.PopupTop = caretPosition.Y + _textEditorViewModel.LineHeight;

            if (_textEditorViewModel.CompletionPopupViewModel.IsVisible)
                ShowCompletionSuggestions(_textEditorViewModel.CompletionPopupViewModel.CompletionItems.ToArray());
        }
    }

    public void ShowCompletionSuggestions(CompletionItem[] items)
    {
        if (items.Length == 0) return;
        _textEditorViewModel.CompletionPopupViewModel.UpdateCompletionItems(items);
        _textEditorViewModel.CompletionPopupViewModel.IsVisible = true;
        ShowPopup(_textEditorViewModel.PopupLeft, _textEditorViewModel.PopupTop);
    }

    private void ShowPopup(double left, double top)
    {
        if (_popupWindow == null || !_popupWindow.IsVisible)
        {
            _popupWindow = new CompletionPopup();
            _popupWindow.DataContext = _textEditorViewModel.CompletionPopupViewModel;
        }

        _popupWindow.SetPosition(left, top);
        _popupWindow.Show();
        _textEditorViewModel.CompletionPopupViewModel.IsFocused = true;
    }

    public void HideCompletionSuggestions()
    {
        _textEditorViewModel.CompletionPopupViewModel.IsVisible = false;
        _textEditorViewModel.CompletionPopupViewModel.IsFocused = false;
        _popupWindow?.Hide();
    }

    public void ApplySelectedSuggestion()
    {
        var selectedItem = _textEditorViewModel.CompletionPopupViewModel.SelectedItem;
        if (selectedItem != null) _textEditorViewModel.ApplySelectedSuggestion(selectedItem);
        HideCompletionSuggestions();
    }

    public async Task RequestCompletionAsync(long position, char? lastTypedChar = null)
    {
        if (!_textEditorViewModel.HasUserStartedTyping) return;

        try
        {
            if (!_textEditorViewModel.IsLspReady()) return;

            var wordBeforeCursor = _textEditorViewModel.GetWordBeforeCursor();

            // If the word before cursor is empty and it's not a trigger character, don't show suggestions
            if (string.IsNullOrEmpty(wordBeforeCursor) && !IsCompletionTriggerCharacter(lastTypedChar))
            {
                HideCompletionSuggestions();
                return;
            }

            var lspPosition = _textEditorViewModel.GetLspPosition(position);
            var lineText = _textEditorViewModel.TextBuffer.GetLineText(
                _textEditorViewModel.TextBuffer.GetLineIndexFromPosition(position));
            var triggerCharacter = lastTypedChar?.ToString() ??
                                   (position > 0 ? _textEditorViewModel.TextBuffer.GetText(position - 1, 1) : null);

            Console.WriteLine($"Requesting completion at position {position} for {_textEditorViewModel.FilePath}");
            Console.WriteLine($"Line text: {lineText}");
            Console.WriteLine($"Trigger character: {triggerCharacter}");
            Console.WriteLine($"Word before cursor: '{wordBeforeCursor}'");

            var currentLine = _textEditorViewModel.TextBuffer.GetLineText(
                _textEditorViewModel.TextBuffer.GetLineIndexFromPosition(position));
            var precedingText = currentLine.Substring(0,
                (int)(position -
                      _textEditorViewModel.TextBuffer.GetLineStartPosition(
                          (int)_textEditorViewModel.TextBuffer.GetLineIndexFromPosition(position))));

            var context = new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = triggerCharacter
            };

            if (IsMethodInvocationContext(precedingText))
            {
                context.TriggerKind = CompletionTriggerKind.TriggerCharacter;
                context.TriggerCharacter = "(";
            }
            else if (IsMemberAccessContext(precedingText))
            {
                context.TriggerKind = CompletionTriggerKind.TriggerCharacter;
                context.TriggerCharacter = ".";
            }
            else if (IsImportContext(precedingText))
            {
                context.TriggerKind = CompletionTriggerKind.TriggerCharacter;
                context.TriggerCharacter = " import ";
            }

            var result =
                await _textEditorViewModel.LspClient.RequestCompletionAsync(_textEditorViewModel.FilePath, lspPosition,
                    context);

            if (result.Items == null || result.Items.Length == 0)
            {
                Console.WriteLine("No completion items received.");
                ClearCompletionItems();
                return;
            }

            Console.WriteLine($"Total completion items received: {result.Items.Length}");
            Console.WriteLine("First 5 unfiltered items:");
            foreach (var item in result.Items.Take(5)) Console.WriteLine($"  - {item.Label} (Kind: {item.Kind})");

            HandleCompletionResult(result, wordBeforeCursor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting completion: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            ClearCompletionItems();
        }
    }

    public async Task DebouncedRequestCompletionAsync(long position, char? lastTypedChar)
    {
        if (_completionCancellationTokenSource != null)
        {
            _completionCancellationTokenSource.Cancel();
            _completionCancellationTokenSource.Dispose();
        }

        _completionCancellationTokenSource = new CancellationTokenSource();
        var token = _completionCancellationTokenSource.Token;

        try
        {
            await Task.Delay(DefaultCompletionDebounceTime, token);
            if (!token.IsCancellationRequested) await RequestCompletionAsync(position, lastTypedChar);
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DebouncedRequestCompletionAsync: {ex.Message}");
        }
    }

    private void HandleCompletionResult(CompletionList result, string wordBeforeCursor)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (result.Items != null && result.Items.Any())
                {
                    var relevantItems = result.Items
                        .Select(item => new { Item = item, Score = CalculateRelevanceScore(item, wordBeforeCursor) })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .Take(20)
                        .Select(x => x.Item)
                        .ToArray();

                    Console.WriteLine($"Filtered items count: {relevantItems.Length}");
                    Console.WriteLine("First 5 filtered items:");
                    foreach (var item in relevantItems.Take(5))
                        Console.WriteLine(
                            $"  - {item.Label} (Kind: {item.Kind}, Score: {CalculateRelevanceScore(item, wordBeforeCursor)})");

                    if (relevantItems.Length > 0)
                    {
                        ShowCompletionSuggestions(relevantItems);
                    }
                    else
                    {
                        Console.WriteLine("No relevant completion items after filtering.");
                        ClearCompletionItems();
                    }
                }
                else
                {
                    Console.WriteLine("No items to show in CompletionPopupViewModel");
                    ClearCompletionItems();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling completion result: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void ClearCompletionItems()
    {
        _textEditorViewModel.CompletionPopupViewModel.UpdateCompletionItems(Array.Empty<CompletionItem>());
        HideCompletionSuggestions();
    }

    private bool IsCompletionTriggerCharacter(char? c)
    {
        if (c == null) return false;
        return char.IsLetterOrDigit(c.Value) || c == '.' || c == '_';
    }

    private bool IsMethodInvocationContext(string precedingText)
    {
        return precedingText.TrimEnd().EndsWith("(");
    }

    private bool IsMemberAccessContext(string precedingText)
    {
        return precedingText.TrimEnd().EndsWith(".");
    }

    private bool IsImportContext(string precedingText)
    {
        return precedingText.TrimStart().StartsWith("import ") || precedingText.TrimStart().StartsWith("from ");
    }

    private double CalculateRelevanceScore(CompletionItem item, string wordBeforeCursor)
    {
        double score = 0;

        if (string.IsNullOrEmpty(wordBeforeCursor))
            // If no word before cursor, give a base score to all items
            return 1;

        // Exact match bonus
        if (item.Label.Equals(wordBeforeCursor, StringComparison.OrdinalIgnoreCase)) score += 50;

        // Prefix match bonus
        if (item.Label.StartsWith(wordBeforeCursor, StringComparison.OrdinalIgnoreCase)) score += 30;

        // Substring match bonus
        if (item.Label.Contains(wordBeforeCursor, StringComparison.OrdinalIgnoreCase)) score += 20;

        // Fuzzy match score
        score += FuzzyMatch(item.Label, wordBeforeCursor);

        // Shorter suggestions bonus
        score += Math.Max(0, 10 - item.Label.Length);

        // Kind-based scoring
        score += GetKindScore(item.Kind);

        return score;
    }

    private int FuzzyMatch(string label, string word)
    {
        var score = 0;
        var labelIndex = 0;
        var consecutiveMatches = 0;

        for (var wordIndex = 0; wordIndex < word.Length; wordIndex++)
        {
            var found = false;
            for (var j = labelIndex; j < label.Length; j++)
                if (char.ToLower(label[j]) == char.ToLower(word[wordIndex]))
                {
                    found = true;
                    score += consecutiveMatches + 1;
                    consecutiveMatches++;
                    labelIndex = j + 1;
                    break;
                }

            if (!found) consecutiveMatches = 0;
        }

        // Bonus for start-of-word matches
        if (word.Length > 0 && label.Length > 0 && char.ToLower(word[0]) == char.ToLower(label[0])) score += 2;

        return score;
    }

    private int GetKindScore(CompletionItemKind? kind)
    {
        return kind switch
        {
            CompletionItemKind.Keyword => 5,
            CompletionItemKind.Method => 4,
            CompletionItemKind.Function => 4,
            CompletionItemKind.Property => 3,
            CompletionItemKind.Field => 3,
            CompletionItemKind.Variable => 2,
            _ => 1
        };
    }
}