using System.Collections.Concurrent;
using Avalonia.Media;
using meteor.Enums;
using meteor.Interfaces;

namespace meteor.Views.Services;

public class MemoizedBrushGetter
{
    private readonly ConcurrentDictionary<SyntaxTokenType, IBrush> _brushCache = new();
    private readonly IThemeService _themeService;

    public MemoizedBrushGetter(IThemeService themeService)
    {
        _themeService = themeService;
    }

    public IBrush GetBrushForTokenType(SyntaxTokenType type)
    {
        return _brushCache.GetOrAdd(type, t =>
        {
            return t switch
            {
                SyntaxTokenType.Keyword => _themeService.GetResourceBrush("KeywordColor"),
                SyntaxTokenType.Comment => _themeService.GetResourceBrush("CommentColor"),
                SyntaxTokenType.String => _themeService.GetResourceBrush("StringColor"),
                SyntaxTokenType.Type => _themeService.GetResourceBrush("TypeColor"),
                SyntaxTokenType.Number => _themeService.GetResourceBrush("NumberColor"),
                _ => _themeService.GetResourceBrush("DefaultColor")
            };
        });
    }
}