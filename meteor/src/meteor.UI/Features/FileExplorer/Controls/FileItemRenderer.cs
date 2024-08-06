using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

class FileItemRenderer
{
    private readonly double _indentWidth = 8;
    private readonly double _itemHeight = 24;
    private readonly double _leftPadding = 8;
    private readonly double _rightPadding = 8;
    private readonly Theme _currentTheme;
    private readonly IThemeManager _themeManager;
    private readonly IGitService _gitService;
    private readonly ScrollViewer _scrollViewer;
    private FileItem _selectedItem;
    private readonly Dictionary<string, Geometry> _iconCache = new Dictionary<string, Geometry>();
    private readonly Dictionary<string, FormattedText> _textCache = new Dictionary<string, FormattedText>();

    public FileItemRenderer(Theme currentTheme, IThemeManager themeManager, IGitService gitService, ScrollViewer scrollViewer, FileItem selectedItem)
    {
        _currentTheme = currentTheme;
        _themeManager = themeManager;
        _gitService = gitService;
        _scrollViewer = scrollViewer;
        _selectedItem = selectedItem;
    }

    public void UpdateSelectedItem(FileItem selectedItem)
    {
        _selectedItem = selectedItem;
    }

    public double RenderItems(DrawingContext context, Size bounds, IEnumerable<FileItem> items, int indentLevel, double y, Rect viewport)
    {
        foreach (var item in items)
        {
            if (y + _itemHeight > 0 && y < viewport.Height)
                RenderItem(context, bounds, item, indentLevel, y, viewport);

            y += _itemHeight;

            if (item.IsExpanded)
                y = RenderItems(context, bounds, item.Children, indentLevel + 1, y, viewport);

            if (y > viewport.Height)
                break;
        }

        return y;
    }

    private void RenderItem(DrawingContext context, Size bounds, FileItem item, int indentLevel, double y, Rect viewport)
    {
        RenderItemBackground(context, item, y, viewport);
        RenderItemChevron(context, item, indentLevel, y);
        RenderItemIcon(context, item, indentLevel, y);
        RenderItemText(context, bounds, item, indentLevel, y);
        RenderItemGitStatus(context, item, y, viewport);
    }

    private void RenderItemBackground(DrawingContext context, FileItem item, double y, Rect viewport)
    {
        if (item == _selectedItem)
        {
            var backgroundBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerSelectedItemBackgroundColor));
            context.FillRectangle(backgroundBrush, new Rect(0, y, viewport.Width, _itemHeight));

            var highlightBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerSelectedItemBackgroundColor));
            var borderThickness = 2;
            context.DrawRectangle(highlightBrush, null, new Rect(0, y, viewport.Width, _itemHeight), borderThickness);
        }
    }

    private void RenderItemIcon(DrawingContext context, FileItem item, int indentLevel, double y)
    {
        var iconSize = 16;
        var iconChar = item.IsDirectory ? "\uf07b" : "\uf15b"; // folder icon : file icon
        var iconBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerFileIconColor));

        if (!_iconCache.TryGetValue(iconChar, out var iconGeometry))
        {
            var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
            var typeface = new Typeface(fontAwesomeSolid);
            iconGeometry = CreateFormattedTextGeometry(iconChar, typeface, iconSize, iconBrush);
            _iconCache[iconChar] = iconGeometry;
        }

        var iconX = _leftPadding + indentLevel * _indentWidth + 20 - _scrollViewer.Offset.X;
        var iconY = y + (_itemHeight - iconSize) / 2;

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(iconX, iconY));
        context.DrawGeometry(iconBrush, null, iconGeometry);
    }

    private void RenderItemChevron(DrawingContext context, FileItem item, int indentLevel, double y)
    {
        if (!item.IsDirectory) return;

        var chevronBrush = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));
        var chevronSize = 10;
        var chevronChar = item.IsExpanded ? "\uf078" : "\uf054"; // chevron-down : chevron-right
        var fontAwesomeSolid = new FontFamily("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var typeface = new Typeface(fontAwesomeSolid);

        var chevronGeometry = CreateFormattedTextGeometry(chevronChar, typeface, chevronSize, chevronBrush);

        var chevronX = _leftPadding + indentLevel * _indentWidth - _scrollViewer.Offset.X;
        var chevronY = y + (_itemHeight - chevronSize) / 2;

        chevronGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(chevronX, chevronY));
        context.DrawGeometry(chevronBrush, null, chevronGeometry);
    }

    private void RenderItemText(DrawingContext context, Size bounds, FileItem item, int indentLevel, double y)
    {
        var textSize = 13;
        var typeface = new Typeface("San Francisco");

        var textBrush = GetTextBrushAccordingToGitStatus(item);

        if (!_textCache.TryGetValue(item.Name, out var formattedText))
        {
            formattedText = new FormattedText(
                item.Name,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                textSize,
                textBrush
            );
            _textCache[item.Name] = formattedText;
        }
        else
        {
            // Update the brush of the cached formatted text
            formattedText.SetForegroundBrush(textBrush);
        }

        var maxTextWidth = Math.Max(0, bounds.Width - _leftPadding - indentLevel * _indentWidth - 65 - _scrollViewer.Offset.X);
        formattedText.MaxTextWidth = maxTextWidth;
        formattedText.MaxLineCount = 1;
        formattedText.Trimming = TextTrimming.CharacterEllipsis;

        var textX = _leftPadding + indentLevel * _indentWidth + 40 - _scrollViewer.Offset.X;
        var textY = y + (_itemHeight - formattedText.Height) / 2 + 1;

        if (item.IsDirectory)
        {
            textX += 2;
        }

        context.DrawText(formattedText, new Point(textX, textY));
    }

    private SolidColorBrush GetTextBrushAccordingToGitStatus(FileItem item)
    {
        var gitStatus = item.GitStatus;
        return gitStatus switch
        {
            FileChangeType.Added => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GitAddedColor)),
            FileChangeType.Modified => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GitModifiedColor)),
            FileChangeType.Deleted => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GitDeletedColor)),
            FileChangeType.Renamed => new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.GitRenamedColor)),
            _ => new SolidColorBrush(Color.Parse(_currentTheme.TextColor))
        };
    }

    private void RenderItemGitStatus(DrawingContext context, FileItem item, double y, Rect viewport)
    {
        if (!_gitService.IsValidGitRepository(_gitService.GetRepositoryPath()))
            return;

        var gitStatus = item.GitStatus;
        if (gitStatus == null)
            return;

        var statusColor = GetStatusColor((FileChangeType)gitStatus);
        if (statusColor == Colors.Transparent)
            return;

        var statusBrush = new SolidColorBrush(statusColor);
        var circleSize = 8;
        var charSize = 12;
        var statusY = y + (_itemHeight - circleSize) / 2;
        var statusX = viewport.Width - _rightPadding - circleSize - _scrollViewer.Offset.X;

        if (item.IsDirectory)
        {
            var circleGeometry = new EllipseGeometry(new Rect(statusX, statusY, circleSize, circleSize));
            context.DrawGeometry(statusBrush, null, circleGeometry);
        }
        else
        {
            var statusChar = gitStatus switch
            {
                FileChangeType.Added => "A",
                FileChangeType.Modified => "M",
                FileChangeType.Deleted => "D",
                FileChangeType.Renamed => "R",
                _ => "U",
            };

            var typeface = new Typeface("Arial");

            var statusGeometry = CreateFormattedTextGeometry(statusChar, typeface, charSize, statusBrush);

            statusGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(statusX, statusY + (circleSize - charSize) / 2));
            context.DrawGeometry(statusBrush, null, statusGeometry);
        }
    }
    private Color GetStatusColor(FileChangeType status)
    {
        return status switch
        {
            FileChangeType.Added => Color.Parse(_themeManager.CurrentTheme.GitAddedColor),
            FileChangeType.Modified => Color.Parse(_themeManager.CurrentTheme.GitModifiedColor),
            FileChangeType.Deleted => Color.Parse(_themeManager.CurrentTheme.GitDeletedColor),
            FileChangeType.Renamed => Color.Parse(_themeManager.CurrentTheme.GitRenamedColor),
            _ => Colors.Transparent
        };
    }

    private Geometry CreateFormattedTextGeometry(string text, Typeface typeface, double size, IBrush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush
        ).BuildGeometry(new Point(0, 0));
    }
}