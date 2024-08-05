using System.Text.Json.Serialization;

namespace meteor.Core.Models;

public class Theme
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("isDark")] public bool IsDark { get; set; }

    [JsonPropertyName("borderBrush")] public string BorderBrush { get; set; } = "";

    [JsonPropertyName("background")] public string Background { get; set; } = "";

    [JsonPropertyName("foreground")] public string Foreground { get; set; } = "";

    [JsonPropertyName("dirtyIndicatorBrush")]
    public string DirtyIndicatorBrush { get; set; } = "";

    [JsonPropertyName("closeButtonForeground")]
    public string CloseButtonForeground { get; set; } = "";

    [JsonPropertyName("closeButtonBackground")]
    public string CloseButtonBackground { get; set; } = "";

    [JsonPropertyName("textColor")] public string TextColor { get; set; } = "";

    [JsonPropertyName("backgroundColor")] public string BackgroundColor { get; set; } = "";

    [JsonPropertyName("currentLineHighlightColor")]
    public string CurrentLineHighlightColor { get; set; } = "";

    [JsonPropertyName("selectionColor")] public string SelectionColor { get; set; } = "";

    [JsonPropertyName("gutterBackgroundColor")]
    public string GutterBackgroundColor { get; set; } = "";

    [JsonPropertyName("gutterTextColor")] public string GutterTextColor { get; set; } = "";

    [JsonPropertyName("keywordColor")] public string KeywordColor { get; set; } = "";

    [JsonPropertyName("stringColor")] public string StringColor { get; set; } = "";

    [JsonPropertyName("commentColor")] public string CommentColor { get; set; } = "";

    [JsonPropertyName("numberColor")] public string NumberColor { get; set; } = "";

    [JsonPropertyName("operatorColor")] public string OperatorColor { get; set; } = "";

    [JsonPropertyName("typeColor")] public string TypeColor { get; set; } = "";

    [JsonPropertyName("methodColor")] public string MethodColor { get; set; } = "";

    [JsonPropertyName("preprocessorColor")]
    public string PreprocessorColor { get; set; } = "";

    [JsonPropertyName("xmlDocColor")] public string XmlDocColor { get; set; } = "";

    [JsonPropertyName("attributeColor")] public string AttributeColor { get; set; } = "";

    [JsonPropertyName("namespaceColor")] public string NamespaceColor { get; set; } = "";

    [JsonPropertyName("linqColor")] public string LinqColor { get; set; } = "";

    [JsonPropertyName("lambdaColor")] public string LambdaColor { get; set; } = "";

    [JsonPropertyName("whitespaceColor")] public string WhitespaceColor { get; set; } = "";

    [JsonPropertyName("tabBackgroundColor")]
    public string TabBackgroundColor { get; set; } = "";

    [JsonPropertyName("tabForegroundColor")]
    public string TabForegroundColor { get; set; } = "";

    [JsonPropertyName("tabBorderColor")] public string TabBorderColor { get; set; } = "";

    [JsonPropertyName("tabActiveBackgroundColor")]
    public string TabActiveBackgroundColor { get; set; } = "";

    [JsonPropertyName("tabActiveForegroundColor")]
    public string TabActiveForegroundColor { get; set; } = "";

    [JsonPropertyName("appBackgroundColor")]
    public string AppBackgroundColor { get; set; } = "";

    [JsonPropertyName("appForegroundColor")]
    public string AppForegroundColor { get; set; } = "";

    [JsonPropertyName("statusBarColor")] public string StatusBarColor { get; set; } = "";

    [JsonPropertyName("backgroundBrush")] public string BackgroundBrush { get; set; } = "";

    [JsonPropertyName("completionOverlayBackgroundBrush")]
    public string CompletionOverlayBackgroundBrush { get; set; } = "";

    [JsonPropertyName("completionOverlayBorderBrush")]
    public string CompletionOverlayBorderBrush { get; set; } = "";

    [JsonPropertyName("completionItemSelectedBrush")]
    public string CompletionItemSelectedBrush { get; set; } = "";

    [JsonPropertyName("textBrush")] public string TextBrush { get; set; } = "";

    [JsonPropertyName("completionItemHoverBrush")]
    public string CompletionItemHoverBrush { get; set; } = "";

    [JsonPropertyName("scrollBarBackgroundBrush")]
    public string ScrollBarBackgroundBrush { get; set; } = "";

    [JsonPropertyName("scrollBarThumbBrush")]
    public string ScrollBarThumbBrush { get; set; } = "";

    [JsonPropertyName("fileExplorerBackgroundColor")]
    public string FileExplorerBackgroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerForegroundColor")]
    public string FileExplorerForegroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerSelectedItemBackgroundColor")]
    public string FileExplorerSelectedItemBackgroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerSelectedItemForegroundColor")]
    public string FileExplorerSelectedItemForegroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerHoverItemBackgroundColor")]
    public string FileExplorerHoverItemBackgroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerHoverItemForegroundColor")]
    public string FileExplorerHoverItemForegroundColor { get; set; } = "";

    [JsonPropertyName("fileExplorerFolderIconColor")]
    public string FileExplorerFolderIconColor { get; set; } = "";

    [JsonPropertyName("fileExplorerFileIconColor")]
    public string FileExplorerFileIconColor { get; set; } = "";

    [JsonPropertyName("titleBarColor")] public string TitleBarColor { get; set; } = "";

    [JsonPropertyName("titleBarInactiveColor")]
    public string TitleBarInactiveColor { get; set; } = "";

    [JsonPropertyName("buttonHoverColor")]
    public string ButtonHoverColor { get; set; } = "";

    [JsonPropertyName("buttonPressedColor")]
    public string ButtonPressedColor { get; set; } = "";

    [JsonPropertyName("buttonActiveColor")]
    public string ButtonActiveColor { get; set; } = "";

    [JsonPropertyName("buttonBorderColor")]
    public string ButtonBorderColor { get; set; } = "";

    [JsonPropertyName("searchViewBackgroundColor")]
    public string SearchViewBackgroundColor { get; set; } = "";

    [JsonPropertyName("searchViewBorderColor")]
    public string SearchViewBorderColor { get; set; } = "";

    [JsonPropertyName("buttonColor")]
    public string ButtonColor { get; set; } = "";

    [JsonPropertyName("highlightBrush")]
    public string HighlightBrush { get; set; } = "";

    [JsonPropertyName("accentBrush")]
    public string AccentBrush { get; set; } = "";

    // Command Palette
    [JsonPropertyName("commandPaletteBackgroundColor")]
    public string CommandPaletteBackgroundColor { get; set; } = "";

    [JsonPropertyName("commandPaletteForegroundColor")]
    public string CommandPaletteForegroundColor { get; set; } = "";

    // Git
    [JsonPropertyName("gitAddedColor")] public string GitAddedColor { get; set; } = "";

    [JsonPropertyName("gitAddedColorChar")] public string GitAddedColorChar { get; set; } = "";

    [JsonPropertyName("gitModifiedColor")] public string GitModifiedColor { get; set; } = "";
    [JsonPropertyName("gitModifiedColorChar")] public string GitModifiedColorChar { get; set; } = "";

    [JsonPropertyName("gitDeletedColor")] public string GitDeletedColor { get; set; } = "";

    [JsonPropertyName("gitDeletedColorChar")] public string GitDeletedColorChar { get; set; } = "";

    [JsonPropertyName("gitRenamedColor")] public string GitRenamedColor { get; set; } = "";

    [JsonPropertyName("gitRenamedColorChar")] public string GitRenamedColorChar { get; set; } = "";
}