namespace meteor.Core.Models;

public class TextChange
{
    public TextChange(int offset, int oldLength, int newLength, string newText, string oldText)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
        if (oldLength < 0)
            throw new ArgumentOutOfRangeException(nameof(oldLength), "Old length must be non-negative.");
        if (newLength < 0)
            throw new ArgumentOutOfRangeException(nameof(newLength), "New length must be non-negative.");
        if (newText == null)
            throw new ArgumentNullException(nameof(newText));
        if (oldText == null)
            throw new ArgumentNullException(nameof(oldText));
        if (newText.Length != newLength)
            throw new ArgumentException("New text length must match the specified new length.", nameof(newText));
        if (oldText.Length != oldLength)
            throw new ArgumentException("Old text length must match the specified old length.", nameof(oldText));

        Offset = offset;
        OldLength = oldLength;
        NewLength = newLength;
        NewText = newText;
        OldText = oldText;
    }

    /// <summary>
    ///     The position in the document where the change starts.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    ///     The length of the text that was replaced.
    /// </summary>
    public int OldLength { get; }

    /// <summary>
    ///     The length of the new text that was inserted.
    /// </summary>
    public int NewLength { get; }

    /// <summary>
    ///     The new text that was inserted.
    /// </summary>
    public string NewText { get; }

    /// <summary>
    ///     The old text that was replaced.
    /// </summary>
    public string OldText { get; }

    /// <summary>
    ///     Calculates the end position of the change in the document.
    /// </summary>
    public int EndOffset => Offset + NewLength;

    /// <summary>
    ///     Calculates the change in document length caused by this change.
    /// </summary>
    public int LengthDelta => NewLength - OldLength;

    /// <summary>
    ///     Determines whether this change represents an insertion.
    /// </summary>
    public bool IsInsertion => OldLength == 0 && NewLength > 0;

    /// <summary>
    ///     Determines whether this change represents a deletion.
    /// </summary>
    public bool IsDeletion => OldLength > 0 && NewLength == 0;

    /// <summary>
    ///     Determines whether this change represents a replacement.
    /// </summary>
    public bool IsReplacement => OldLength > 0 && NewLength > 0;

    public override string ToString()
    {
        return $"Change at {Offset}: {OldLength} chars replaced with {NewLength} chars";
    }
}