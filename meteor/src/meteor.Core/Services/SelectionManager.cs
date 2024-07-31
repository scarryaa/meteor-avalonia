using System.Diagnostics;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class SelectionManager : ISelectionManager
{
    private const int ChunkSize = 4096;
    private readonly ITextBufferService _textBufferService;
    private bool _isSelectionInProgress;
    private int _selectionAnchor;

    public SelectionManager(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        CurrentSelection = new Selection(0, 0);
    }

    public Selection CurrentSelection { get; private set; }
    public bool HasSelection => CurrentSelection.Start != CurrentSelection.End;

    public event EventHandler SelectionChanged;

    public void StartSelection(int position)
    {
        position = ClampPosition(position);
        _selectionAnchor = position;
        CurrentSelection = new Selection(position, position);
        _isSelectionInProgress = true;
        OnSelectionChanged();
    }

    public void SetSelection(int start, int end)
    {
        start = ClampPosition(start);
        end = ClampPosition(end);

        CurrentSelection = new Selection(Math.Min(start, end), Math.Max(start, end));
        OnSelectionChanged();
    }

    public void ClearSelection()
    {
        CurrentSelection = new Selection(0, 0);
        _isSelectionInProgress = false;
        OnSelectionChanged();
    }

    public string GetSelectedText(ITextBufferService textBufferService)
    {
        if (textBufferService == null)
            throw new ArgumentNullException(nameof(textBufferService));

        if (!HasSelection)
            return string.Empty;

        try
        {
            var startIndex = ClampPosition(CurrentSelection.Start);
            var endIndex = ClampPosition(CurrentSelection.End);

            if (startIndex >= endIndex)
            {
                Debug.WriteLine("GetSelectedText - Invalid selection range after bounds check");
                return string.Empty;
            }

            return textBufferService.GetContentSliceByIndex(startIndex, endIndex - startIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetSelectedText: {ex.Message}");
            Debug.WriteLine($"Selection - Start: {CurrentSelection.Start}, End: {CurrentSelection.End}");
            Debug.WriteLine($"Document length: {textBufferService.GetLength()}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    public void ExtendSelection(int newPosition)
    {
        newPosition = ClampPosition(newPosition);
        CurrentSelection = new Selection(
            Math.Min(_selectionAnchor, newPosition),
            Math.Max(_selectionAnchor, newPosition)
        );
        OnSelectionChanged();
    }

    public void UpdateSelection(int position, bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (!_isSelectionInProgress)
                StartSelection(CurrentSelection.Start != CurrentSelection.End ? CurrentSelection.Start : position);
            ExtendSelection(position);
        }
        else
        {
            if (_isSelectionInProgress) ClearSelection();
            _selectionAnchor = position;
        }
    }

    public void HandleMouseSelection(int position, bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (!_isSelectionInProgress) StartSelection(_selectionAnchor);
            ExtendSelection(position);
        }
        else
        {
            StartSelection(position);
        }
    }

    public void HandleKeyboardSelection(int position, bool isShiftPressed)
    {
        UpdateSelection(position, isShiftPressed);
    }

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private int ClampPosition(int position)
    {
        return Math.Max(0, Math.Min(position, _textBufferService.GetLength()));
    }
}