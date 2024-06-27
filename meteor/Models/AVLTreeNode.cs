namespace meteor.Models;

public class AVLTreeNode
{
    public string Value;
    public int Height;
    public AVLTreeNode Left;
    public AVLTreeNode Right;

    public AVLTreeNode(string value)
    {
        Value = value;
        Height = 1;
    }
}