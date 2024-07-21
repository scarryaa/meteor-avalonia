using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Config;
using meteor.Core.Models.Rendering;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.UI.Interfaces;
using meteor.UI.Services;

namespace meteor.UI.Renderers;

public class EditorRenderer : IDisposable
{
    private readonly ThemeConfig _themeConfig;
    private bool _disposed;
    private const int BufferLines = 5;
    private readonly Action _invalidateView;
    private ITextMeasurer _textMeasurer;
    private DispatcherTimer _cursorBlinkTimer;
    private bool _showCursor = true;
    private ITabService _tabService;
    private int _totalLines;

    private IBrush _highlightBrush;
    private IBrush _commentBrush;
    private IPen _cursorPen;
    private double _fontSize;
    private IBrush _keywordBrush;
    private IBrush _plainTextBrush;
    private IBrush _selectionBrush;
    private IBrush _stringBrush;
    private Typeface _typeface;

    private readonly List<(int start, int length)> _lineInfo = new();


    public EditorRenderer(Action invalidateView, IThemeManager themeManager, ThemeConfig themeConfig)
    {
        _invalidateView = invalidateView;
        _themeConfig = themeConfig;
        UpdateThemeResources(themeManager);

        _cursorBlinkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _cursorBlinkTimer.Tick += (sender, args) =>
        {
            _showCursor = !_showCursor;
            _invalidateView();
        };
        _cursorBlinkTimer.Start();
    }

    private void UpdateThemeResources(IThemeManager themeManager)
    {
        var baseTheme = themeManager.GetBaseTheme();
        var fontFamilyUri = _themeConfig.FontFamilyUri;

        _typeface = new Typeface(new FontFamily(new Uri(fontFamilyUri), baseTheme["TextEditorFontFamily"].ToString()));
        _fontSize = Convert.ToDouble(baseTheme["TextEditorFontSize"]);
        _commentBrush = new SolidColorBrush(Color.Parse(baseTheme["CommentColor"].ToString()));
        _cursorPen = new Pen(new SolidColorBrush(Color.Parse(baseTheme["TextEditorCursor"].ToString())));
        _keywordBrush = new SolidColorBrush(Color.Parse(baseTheme["KeywordColor"].ToString()));
        _plainTextBrush = new SolidColorBrush(Color.Parse(baseTheme["Text"].ToString()));
        _selectionBrush = new SolidColorBrush(Color.Parse(baseTheme["TextEditorSelection"].ToString()));
        _stringBrush = new SolidColorBrush(Color.Parse(baseTheme["StringColor"].ToString()));
        _highlightBrush = new SolidColorBrush(Color.Parse(baseTheme["GutterHighlight"].ToString()));

        _textMeasurer = new AvaloniaTextMeasurer(_typeface, _fontSize);
    }

    public void UpdateTabService(ITabService tabService)
    {
        _tabService = tabService;
        UpdateLineInfo();
    }

    public void UpdateLineInfo()
    {
        var textBufferService = _tabService.GetActiveTextBufferService();

        _lineInfo.Clear();
        _totalLines = 0;
        var currentIndex = 0;

        while (currentIndex <= textBufferService.Length)
        {
            var lineEndIndex = textBufferService.IndexOf('\n', currentIndex);
            if (lineEndIndex == -1) lineEndIndex = textBufferService.Length;

            var lineLength = lineEndIndex - currentIndex;
            _lineInfo.Add((currentIndex, lineLength));
            _totalLines++;

            if (lineEndIndex == textBufferService.Length)
                break;

            currentIndex = lineEndIndex + 1;
        }
    }

    public void Render(DrawingContext context, Rect bounds,
        IEnumerable<SyntaxHighlightingResult> highlightingResults,
        (int start, int length) selection, int cursorPosition,
        double scrollOffset, double offsetX)
    {
        var textBufferService = _tabService.GetActiveTextBufferService();

        context.DrawRectangle(Brushes.White, null, bounds);

        var lineHeight = _textMeasurer.GetLineHeight();
        var firstVisibleLine = Math.Max(0, (int)(scrollOffset / lineHeight) - BufferLines);
        var lastVisibleLine =
            Math.Min(_totalLines - 1, (int)((scrollOffset + bounds.Height) / lineHeight) + BufferLines);

        for (var lineNumber = firstVisibleLine; lineNumber <= lastVisibleLine; lineNumber++)
        {
            if (lineNumber >= _lineInfo.Count) break;

            var (lineStart, lineLength) = _lineInfo[lineNumber];
            var lineY = lineNumber * lineHeight - scrollOffset;

            var renderContext = new RenderLineContext(lineStart, lineLength, lineY,
                highlightingResults, selection, cursorPosition, offsetX);

            RenderLine(context, textBufferService, renderContext, bounds);
        }
    }

    private void RenderLine(DrawingContext context, ITextBufferService textBufferService,
        RenderLineContext renderContext, Rect bounds)
    {
        DrawLineSelection(context, textBufferService, renderContext);

        var sb = new StringBuilder(renderContext.LineLength);
        textBufferService.GetTextSegment(renderContext.LineStart, renderContext.LineLength, sb);

        var formattedText = new FormattedText(
            sb.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _fontSize,
            _plainTextBrush
        );

        formattedText.MaxTextWidth = double.PositiveInfinity;

        ApplySyntaxHighlighting(formattedText, renderContext.LineStart, renderContext.LineLength,
            renderContext.HighlightingResults);

        var currentLineNumber = _tabService.GetActiveTab().EditorViewModel.CurrentLine - 1;

        var renderLineNumber = _lineInfo.FindIndex(info => info.start == renderContext.LineStart);

        if (currentLineNumber == renderLineNumber)
            context.FillRectangle(_highlightBrush,
                new Rect(0, renderContext.LineY, bounds.X + bounds.Width,
                    _textMeasurer.GetLineHeight()));

        context.DrawText(formattedText, new Point(-renderContext.OffsetX, renderContext.LineY));

        if (_showCursor && renderContext.CursorPosition >= renderContext.LineStart &&
            renderContext.CursorPosition <= renderContext.LineStart + renderContext.LineLength)
        {
            double cursorX;
            if (renderContext.LineLength == 0)
                cursorX = 0;
            else
                cursorX = _textMeasurer
                    .GetPositionAtIndex(sb.ToString(), renderContext.CursorPosition - renderContext.LineStart).X;

            context.DrawLine(_cursorPen,
                new Point(cursorX - renderContext.OffsetX + 1, renderContext.LineY),
                new Point(cursorX - renderContext.OffsetX + 1, renderContext.LineY + _textMeasurer.GetLineHeight()));
        }
    }

    private void DrawLineSelection(DrawingContext context, ITextBufferService textBufferService,
        RenderLineContext renderContext)
    {
        var lineEnd = renderContext.LineStart + renderContext.LineLength;

        if (renderContext.Selection.length != 0)
        {
            var selectionStart = renderContext.Selection.start;
            var selectionEnd = renderContext.Selection.start + renderContext.Selection.length;

            if (!(selectionEnd <= renderContext.LineStart || selectionStart >= lineEnd))
            {
                var startX = selectionStart > renderContext.LineStart
                    ? GetXPositionForIndex(textBufferService, renderContext.LineStart,
                        selectionStart - renderContext.LineStart)
                    : 0;
                var endX = selectionEnd < lineEnd
                    ? GetXPositionForIndex(textBufferService, renderContext.LineStart,
                        selectionEnd - renderContext.LineStart)
                    : GetXPositionForIndex(textBufferService, renderContext.LineStart, renderContext.LineLength);

                context.DrawRectangle(_selectionBrush, null,
                    new Rect(startX - renderContext.OffsetX, renderContext.LineY, endX - startX,
                        _textMeasurer.GetLineHeight()));
            }
        }
    }

    private double GetXPositionForIndex(ITextBufferService textBufferService, int start, int length)
    {
        var sb = new StringBuilder(length);
        textBufferService.GetTextSegment(start, length, sb);
        return _textMeasurer.GetStringWidth(sb.ToString());
    }

    private void ApplySyntaxHighlighting(FormattedText formattedText, int lineStart, int lineLength,
        IEnumerable<SyntaxHighlightingResult> highlightingResults)
    {
        foreach (var result in highlightingResults)
            if (result.StartIndex + result.Length > lineStart && result.StartIndex < lineStart + lineLength)
            {
                var highlightStart = Math.Max(0, result.StartIndex - lineStart);
                var highlightEnd = Math.Min(lineLength, result.StartIndex + result.Length - lineStart);
                var brush = GetBrushForHighlightingType(result.Type);
                formattedText.SetForegroundBrush(brush, highlightStart, highlightEnd - highlightStart);
            }
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _cursorBlinkTimer.Stop();
                _cursorBlinkTimer = null;
            }

            // Dispose unmanaged resources here, if any

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~EditorRenderer()
    {
        Dispose(false);
    }
}
