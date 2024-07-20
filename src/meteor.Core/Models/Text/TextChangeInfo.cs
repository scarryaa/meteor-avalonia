namespace meteor.Core.Models.Text;

public class TextChangeInfo
{
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string NewText { get; set; }
}