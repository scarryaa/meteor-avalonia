namespace meteor.Models;

public class AVLTreeNode(string value)
{
    public string Value = value;
    public int Height = 1;
    public AVLTreeNode Left;
    public AVLTreeNode Right;
}