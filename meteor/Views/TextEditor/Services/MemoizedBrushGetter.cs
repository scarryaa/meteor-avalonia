using System.Collections.Concurrent;
using Avalonia.Media;
using meteor.Enums;
using meteor.Interfaces;

namespace meteor.Views.Services;

public class MemoizedBrushGetter(IThemeService themeService)
{
    private readonly ConcurrentDictionary<SyntaxTokenType, IBrush> _brushCache = new();

    public IBrush GetBrushForTokenType(SyntaxTokenType type)
    {
        return _brushCache.GetOrAdd(type, t =>
        {
            return t switch
            {
                SyntaxTokenType.Keyword => themeService.GetResourceBrush("KeywordColor"),
                SyntaxTokenType.Comment => themeService.GetResourceBrush("CommentColor"),
                SyntaxTokenType.String => themeService.GetResourceBrush("StringColor"),
                SyntaxTokenType.Type => themeService.GetResourceBrush("TypeColor"),
                SyntaxTokenType.Number => themeService.GetResourceBrush("NumberColor"),
                _ => themeService.GetResourceBrush("DefaultColor")
            };
        });
    }
}