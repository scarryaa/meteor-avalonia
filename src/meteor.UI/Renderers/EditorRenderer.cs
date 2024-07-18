using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.UI.Renderers;

public class EditorRenderer
{
    private readonly IBrush _keywordBrush = Brushes.Blue;
    private readonly IBrush _commentBrush = Brushes.Green;
    private readonly IBrush _stringBrush = Brushes.Red;
    private readonly IBrush _plainTextBrush = Brushes.Black;

    public void Render(DrawingContext context, Rect bounds, string text,
        IEnumerable<SyntaxHighlightingResult> highlightingResults)
    {
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, bounds.Width, bounds.Height));

        var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), 13, Brushes.Black);

        foreach (var result in highlightingResults)
        {
            var brush = GetBrushForHighlightingType(result.Type);
            formattedText.SetForegroundBrush(brush, result.StartIndex, result.Length);
        }

        context.DrawText(formattedText, new Point(0, 0));
    }

    private IBrush GetBrushForHighlightingType(SyntaxHighlightingType type)
    {
        return type switch
        {
            SyntaxHighlightingType.Keyword => _keywordBrush,
            SyntaxHighlightingType.Comment => _commentBrush,
            SyntaxHighlightingType.String => _stringBrush,
            _ => _plainTextBrush
        };
    }
}