using System.Numerics;
using Avalonia;
using meteor.Interfaces;
using ReactiveUI;

namespace meteor.ViewModels;

public class StatusPaneViewModel : ViewModelBase
{
    private Point _cursorPosition = new(1, 1);
    private readonly ICursorPositionService _cursorPositionService;

    public StatusPaneViewModel(ICursorPositionService cursorPositionService)
    {
        _cursorPositionService = cursorPositionService;
        _cursorPositionService.CursorPositionChanged += OnCursorPositionChanged;
    }

    public Point CursorPosition
    {
        get => _cursorPosition;
        set => this.RaiseAndSetIfChanged(ref _cursorPosition, value);
    }

    private void OnCursorPositionChanged(long position, long[] lineStarts)
    {
        int row = 1, column = 1;
        for (var i = 0; i < lineStarts.Length; i++)
        {
            if (position < lineStarts[i]) break;
            row = i + 1;
            column = (int)(position - lineStarts[i] + 1);
        }

        CursorPosition = new Point(column, row);
    }
}