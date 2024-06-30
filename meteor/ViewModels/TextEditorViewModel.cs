using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ICursorPositionService _cursorPositionService;
    private Rope _rope = new(string.Empty);
    private long _cursorPosition;
    private long _selectionStart = -1;
    private long _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _lineHeight;
    private double _windowWidth;
    private long[] _lineStarts = Array.Empty<long>();
    private readonly LineCountViewModel _lineCountViewModel;

    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public TextEditorViewModel(ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel)
    {
        _cursorPositionService = cursorPositionService;
        FontPropertiesViewModel = fontPropertiesViewModel;
        _lineCountViewModel = lineCountViewModel;

        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontFamily)
            .Subscribe(font => FontFamily = font);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontSize)
            .Subscribe(size => FontSize = size);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.LineHeight)
            .Subscribe(height => LineHeight = height);

        UpdateLineStarts();
    }

    public event EventHandler SelectionChanged;

    public FontFamily FontFamily
    {
        get => FontPropertiesViewModel.FontFamily;
        set => FontPropertiesViewModel.FontFamily = value;
    }

    public double FontSize
    {
        get => FontPropertiesViewModel.FontSize;
        set => FontPropertiesViewModel.FontSize = value;
    }

    public double LineHeight
    {
        get => FontPropertiesViewModel.LineHeight;
        set => this.RaiseAndSetIfChanged(ref _lineHeight, value);
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

    public long CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                _cursorPositionService.UpdateCursorPosition((long)_cursorPosition, _lineStarts);
                NotifySelectionChanged();
                this.RaisePropertyChanged();
            }
        }
    }

    public long SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionStart, value);
                NotifySelectionChanged();
            }
        }
    }

    public long SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionEnd, value);
                NotifySelectionChanged();
            }
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

    private void NotifySelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public long LineCount => _rope.LineCount;

    public Rope Rope
    {
        get => _rope;
        private set
        {
            this.RaiseAndSetIfChanged(ref _rope, value);
            this.RaisePropertyChanged(nameof(LineCount));
            _lineCountViewModel.LineCount = _rope.LineCount; // Update LineCountViewModel
        }
    }

    public void InsertText(long position, string text)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");
        if (string.IsNullOrEmpty(text) || position < 0 || position > _rope.Length) return;

        _rope.Insert((int)position, text);
        UpdateLineStarts();
        _lineCountViewModel.UpdateLineCount(LineCount); 
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(Rope));
        CursorPosition = position + text.Length;
        _lineCountViewModel.MaxLineNumber = LineCount;
    }

    public void DeleteText(long start, long length)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");

        if (length > 0)
        {
            _rope.Delete((int)start, (int)length);
            UpdateLineStarts();
            _lineCountViewModel.UpdateLineCount(LineCount);
            this.RaisePropertyChanged(nameof(LineCount));
            this.RaisePropertyChanged(nameof(Rope));
            _lineCountViewModel.MaxLineNumber = LineCount;
        }
    }

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    private void UpdateLineStarts()
    {
        var lineStarts = new List<long> { 0 };
        long lineStart = 0;

        while (lineStart < _rope.Length)
        {
            var nextNewline = _rope.IndexOf('\n', (int)lineStart);
            if (nextNewline == -1) break;

            lineStarts.Add(nextNewline + 1);
            lineStart = nextNewline + 1;
        }

        _lineStarts = lineStarts.ToArray();
    }
}
