using System;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private Rope _rope;
    private int _cursorPosition;
    private int _selectionStart;
    private int _selectionEnd;
    private bool _isSelecting;
    private double _lineHeight = 20;
    private double _windowHeight;
    private double _windowWidth;

    public TextEditorViewModel()
    {
        _rope = new Rope(string.Empty);
        _cursorPosition = 0;
        _selectionStart = -1;
        _selectionEnd = -1;
        _isSelecting = false;
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
            if (_cursorPosition != value) this.RaiseAndSetIfChanged(ref _cursorPosition, value);
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

        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(Rope));
    }
    
    public void DeleteText(int start, int length)
    {
        if (_rope == null) throw new InvalidOperationException("Rope is not initialized.");

        if (length > 0)
        {
            _rope.Delete(start, length);
            this.RaisePropertyChanged(nameof(Rope));
        }
    }
    
    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }
}