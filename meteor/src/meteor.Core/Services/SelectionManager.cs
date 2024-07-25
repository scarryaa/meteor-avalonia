using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class SelectionManager : ISelectionManager
{
    private int _selectionAnchor;
    private const int ChunkSize = 4096;
    private readonly ITextBufferService _textBufferService;
    
    public Selection CurrentSelection { get; private set; }
    public bool HasSelection => CurrentSelection.Start != CurrentSelection.End;

    public event EventHandler SelectionChanged;

    public SelectionManager(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService ?? throw new ArgumentNullException(nameof(textBufferService));
        CurrentSelection = new Selection(0, 0);
    }

    public void StartSelection(int position)
    {
        _selectionAnchor = position;
        CurrentSelection = new Selection(position, position);
        OnSelectionChanged();
    }

    public void SetSelection(int start, int end)
    {
        var documentLength = _textBufferService.GetLength();

        start = Math.Max(0, Math.Min(start, documentLength));
        end = Math.Max(0, Math.Min(end, documentLength));

        CurrentSelection = new Selection(Math.Min(start, end), Math.Max(start, end));
        OnSelectionChanged();
    }

    public void ClearSelection()
    {
        CurrentSelection = new Selection(0, 0);
        OnSelectionChanged();
    }

    public string GetSelectedText(ITextBufferService textBufferService)
    {
        if (!HasSelection)
            return string.Empty;

        try
        {
            var startIndex = CurrentSelection.Start;
            var endIndex = CurrentSelection.End;
            var documentLength = textBufferService.GetLength();

            startIndex = Math.Max(0, Math.Min(startIndex, documentLength));
            endIndex = Math.Max(0, Math.Min(endIndex, documentLength));

            if (startIndex >= endIndex)
            {
                Console.WriteLine("GetSelectedText - Invalid selection range after bounds check");
                return string.Empty;
            }

            return textBufferService.GetContentSliceByIndex(startIndex, endIndex - startIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSelectedText: {ex.Message}");
            Console.WriteLine($"Selection - Start: {CurrentSelection.Start}, End: {CurrentSelection.End}");
            Console.WriteLine($"Document length: {textBufferService.GetLength()}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    public void ExtendSelection(int newPosition)
    {
        CurrentSelection = new Selection(
            Math.Min(_selectionAnchor, newPosition),
            Math.Max(_selectionAnchor, newPosition)
        );
        OnSelectionChanged();
    }

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
