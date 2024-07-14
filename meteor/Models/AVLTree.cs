using System;

namespace meteor.Models;

public class AVLTree
{
    private AVLTreeNode _root;

    public int Length { get; private set; }

    public AVLTree(string initialText)
    {
        Insert(initialText);
    }

    public void Insert(string value)
    {
        _root = Insert(_root, value);
        Length += value.Length;
    }

    public void Delete(int startIndex, int length)
    {
        var text = GetText();
        text = text.Remove(startIndex, length);
        _root = null;
        Length = 0;
        Insert(text);
    }

    public string GetText()
    {
        return InOrderTraversal(_root);
    }

    private string InOrderTraversal(AVLTreeNode node)
    {
        if (node == null) return string.Empty;

        return InOrderTraversal(node.Left) + node.Value + InOrderTraversal(node.Right);
    }

    private AVLTreeNode Insert(AVLTreeNode node, string value)
    {
        if (node == null) return new AVLTreeNode(value);

        if (value.CompareTo(node.Value) < 0)
            node.Left = Insert(node.Left, value);
        else
            node.Right = Insert(node.Right, value);

        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));

        int balance = GetBalance(node);

        if (balance > 1 && value.CompareTo(node.Left.Value) < 0)
            return RightRotate(node);

        if (balance < -1 && value.CompareTo(node.Right.Value) > 0)
            return LeftRotate(node);

        if (balance > 1 && value.CompareTo(node.Left.Value) > 0)
        {
            node.Left = LeftRotate(node.Left);
            return RightRotate(node);
        }

        if (balance < -1 && value.CompareTo(node.Right.Value) < 0)
        {
            node.Right = RightRotate(node.Right);
            return LeftRotate(node);
        }

        return node;
    }

    private AVLTreeNode RightRotate(AVLTreeNode y)
    {
        var x = y.Left;
        var T2 = x.Right;

        x.Right = y;
        y.Left = T2;

        y.Height = Math.Max(Height(y.Left), Height(y.Right)) + 1;
        x.Height = Math.Max(Height(x.Left), Height(x.Right)) + 1;

        return x;
    }

    private AVLTreeNode LeftRotate(AVLTreeNode x)
    {
        var y = x.Right;
        var T2 = y.Left;

        y.Left = x;
        x.Right = T2;

        x.Height = Math.Max(Height(x.Left), Height(x.Right)) + 1;
        y.Height = Math.Max(Height(y.Left), Height(y.Right)) + 1;

        return y;
    }

    private int Height(AVLTreeNode node)
    {
        return node?.Height ?? 0;
    }

    private int GetBalance(AVLTreeNode node)
    {
        if (node == null) return 0;
        return Height(node.Left) - Height(node.Right);
    }
}