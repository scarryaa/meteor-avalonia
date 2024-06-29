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
    private BigInteger _cursorPosition;
    private BigInteger _selectionStart = -1;
    private BigInteger _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _lineHeight = 20;
    private double _windowWidth;
    private BigInteger[] _lineStarts = Array.Empty<BigInteger>();
    private FontFamily _fontFamily;
    private double _fontSize;

    public TextEditorViewModel(ICursorPositionService cursorPositionService)
    {
        _cursorPositionService = cursorPositionService;
        _fontFamily = new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");
        UpdateLineStarts();
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => this.RaiseAndSetIfChanged(ref _fontFamily, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => this.RaiseAndSetIfChanged(ref _fontSize, value);
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

    public BigInteger CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                this.RaisePropertyChanged();
                _cursorPositionService.UpdateCursorPosition((long)_cursorPosition, _lineStarts);
            }
        }
    }

    public BigInteger SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value) this.RaiseAndSetIfChanged(ref _selectionStart, value);
        }
    }

    public BigInteger SelectionEnd
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

    public BigInteger LineCount => _rope.LineCount;

    public Rope Rope
    {
        get => _rope;
        private set
        {
            this.RaiseAndSetIfChanged(ref _rope, value);
            this.RaisePropertyChanged(nameof(LineCount));
        }
    }

    public void InsertText(BigInteger position, string text)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");
        if (string.IsNullOrEmpty(text) || position < 0 || position > _rope.Length) return;

        _rope.Insert((int)position, text);
        UpdateLineStarts();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(Rope));
        CursorPosition = position + text.Length;
    }

    public void DeleteText(BigInteger start, BigInteger length)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");

        if (length > 0)
        {
            _rope.Delete((int)start, (int)length);
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
        var lineStarts = new List<BigInteger> { 0 };
        BigInteger lineStart = 0;

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