using System;
using System.Collections.Generic;
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

    private void OnCursorPositionChanged(long position, List<long> lineStarts)
    {
        var index = lineStarts.BinarySearch(position);

        if (index < 0)
        {
            index = ~index - 1;
        }

        var row = Math.Max(1, index + 1);
        var column = (int)(position - lineStarts[index] + 1);

        CursorPosition = new Point(column, row);
    }
}