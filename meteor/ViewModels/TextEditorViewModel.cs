using System;
using System.Collections.Generic;
using meteor.Interfaces;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ICursorPositionService _cursorPositionService;
    private Rope _rope = new(string.Empty);
    private int _cursorPosition;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    private bool _isSelecting;
    private double _lineHeight = 20;
    private double _windowHeight;
    private double _windowWidth;
    private int[] _lineStarts = Array.Empty<int>();

    public TextEditorViewModel(ICursorPositionService cursorPositionService)
    {
        _cursorPositionService = cursorPositionService;
        UpdateLineStarts();
    }
    

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            if (_lineHeight != value) this.RaiseAndSetIfChanged(ref _lineHeight, value);
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (_windowHeight != value) this.RaiseAndSetIfChanged(ref _windowHeight, value);
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (_windowWidth != value) this.RaiseAndSetIfChanged(ref _windowWidth, value);
        }
    }

    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                this.RaisePropertyChanged();
                _cursorPositionService.UpdateCursorPosition(_cursorPosition, _lineStarts);
            }
        }
    }

    public int SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value) this.RaiseAndSetIfChanged(ref _selectionStart, value);
        }
    }

    public int SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd != value) this.RaiseAndSetIfChanged(ref _selectionEnd, value);
        }
    }

    public bool IsSelecting
    {
        get => _isSelecting;
        set
        {
            if (_isSelecting != value) this.RaiseAndSetIfChanged(ref _isSelecting, value);
        }
    }

    public int LineCount => _rope.LineCount;

    public Rope Rope
    {
        get => _rope;
        private set
        {
            this.RaiseAndSetIfChanged(ref _rope, value);
            this.RaisePropertyChanged(nameof(LineCount));
        }
    }

    public void InsertText(int position, string text)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");
        if (string.IsNullOrEmpty(text) || position < 0 || position > _rope.Length) return;

        _rope.Insert(position, text);
        UpdateLineStarts();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(Rope));
        CursorPosition = position + text.Length;
    }

    public void DeleteText(int start, int length)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");

        if (length > 0)
        {
            _rope.Delete(start, length);
            UpdateLineStarts();
            this.RaisePropertyChanged(nameof(Rope));
        }
    }

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    private void UpdateLineStarts()
    {
        var lineStarts = new List<int> { 0 };
        var lineStart = 0;

        while (lineStart < _rope.Length)
        {
            var nextNewline = _rope.IndexOf('\n', lineStart);
            if (nextNewline == -1) break;

            lineStarts.Add(nextNewline + 1);
            lineStart = nextNewline + 1;
        }

        _lineStarts = lineStarts.ToArray();
    }
}
