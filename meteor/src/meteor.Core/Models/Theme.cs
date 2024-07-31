using System.Text.Json.Serialization;

namespace meteor.Core.Models;

public class Theme
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("isDark")]
    public bool IsDark { get; set; }

    // UI Colors
    [JsonPropertyName("borderBrush")]
    public string BorderBrush { get; set; } = "";
    [JsonPropertyName("background")]
    public string Background { get; set; } = "";
    [JsonPropertyName("foreground")]
    public string Foreground { get; set; } = "";
    [JsonPropertyName("dirtyIndicatorBrush")]
    public string DirtyIndicatorBrush { get; set; } = "";
    [JsonPropertyName("closeButtonForeground")]
    public string CloseButtonForeground { get; set; } = "";
    [JsonPropertyName("closeButtonBackground")]
    public string CloseButtonBackground { get; set; } = "";

    // Editor Colors
    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = "";
    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = "";
    [JsonPropertyName("currentLineHighlightColor")]
    public string CurrentLineHighlightColor { get; set; } = "";
    [JsonPropertyName("selectionColor")]
    public string SelectionColor { get; set; } = "";
    [JsonPropertyName("gutterBackgroundColor")]
    public string GutterBackgroundColor { get; set; } = "";
    [JsonPropertyName("gutterTextColor")]
    public string GutterTextColor { get; set; } = "";

    // Syntax Highlighting Colors
    [JsonPropertyName("keywordColor")]
    public string KeywordColor { get; set; } = "";
    [JsonPropertyName("stringColor")]
    public string StringColor { get; set; } = "";
    [JsonPropertyName("commentColor")]
    public string CommentColor { get; set; } = "";
    [JsonPropertyName("numberColor")]
    public string NumberColor { get; set; } = "";
    [JsonPropertyName("operatorColor")]
    public string OperatorColor { get; set; } = "";
    [JsonPropertyName("typeColor")]
    public string TypeColor { get; set; } = "";
    [JsonPropertyName("methodColor")]
    public string MethodColor { get; set; } = "";
    [JsonPropertyName("preprocessorColor")]
    public string PreprocessorColor { get; set; } = "";
    [JsonPropertyName("xmlDocColor")]
    public string XmlDocColor { get; set; } = "";
    [JsonPropertyName("attributeColor")]
    public string AttributeColor { get; set; } = "";
    [JsonPropertyName("namespaceColor")]
    public string NamespaceColor { get; set; } = "";
    [JsonPropertyName("linqColor")]
    public string LinqColor { get; set; } = "";
    [JsonPropertyName("lambdaColor")]
    public string LambdaColor { get; set; } = "";
    [JsonPropertyName("whitespaceColor")]
    public string WhitespaceColor { get; set; } = "";

    [JsonPropertyName("backgroundBrush")]
    public string BackgroundBrush { get; set; } = "";
    [JsonPropertyName("completionOverlayBackgroundBrush")]
    public string CompletionOverlayBackgroundBrush { get; set; } = "";
    [JsonPropertyName("completionOverlayBorderBrush")]
    public string CompletionOverlayBorderBrush { get; set; } = "";
    [JsonPropertyName("completionItemSelectedBrush")]
    public string CompletionItemSelectedBrush { get; set; } = "";
    [JsonPropertyName("textBrush")]
    public string TextBrush { get; set; } = "";
    [JsonPropertyName("completionItemHoverBrush")]
    public string CompletionItemHoverBrush { get; set; } = "";
    [JsonPropertyName("scrollBarBackgroundBrush")]
    public string ScrollBarBackgroundBrush { get; set; } = "";
    [JsonPropertyName("scrollBarThumbBrush")]
    public string ScrollBarThumbBrush { get; set; } = "";

    // Tab Colors
    [JsonPropertyName("tabBackgroundColor")]
    public string TabBackgroundColor { get; set; } = "";
    [JsonPropertyName("tabForegroundColor")]
    public string TabForegroundColor { get; set; } = "";
    [JsonPropertyName("activeTabBackgroundColor")]
    public string ActiveTabBackgroundColor { get; set; } = "";
    [JsonPropertyName("activeTabForegroundColor")]
    public string ActiveTabForegroundColor { get; set; } = "";

    // App Colors
    [JsonPropertyName("appBackgroundColor")]
    public string AppBackgroundColor { get; set; } = "";
    [JsonPropertyName("appForegroundColor")]
    public string AppForegroundColor { get; set; } = "";
}