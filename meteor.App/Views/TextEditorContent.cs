using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.App.Rendering;
using meteor.Core.Interfaces.ViewModels;
using Microsoft.Extensions.Logging;

namespace meteor.App.Views;

public class TextEditorContent(TextEditor parentEditor, ILogger<TextEditorContent> logger) : Control
{
    private ITextEditorViewModel _viewModel;
    private double WidthPadding;
    private double HeightPadding;

    public void ForceInvalidate()
    {
        InvalidateVisual();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel = parentEditor.DataContext as ITextEditorViewModel;

        HeightPadding = _viewModel.LineHeight * 5;
        WidthPadding = _viewModel.FontSize * 10;

        UpdateSize();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_viewModel == null) return new Size(0, 0);

        var width = Math.Max(_viewModel.RequiredWidth, parentEditor.ScrollViewer?.Viewport.Width ?? 0);
        var height = Math.Max(_viewModel.RequiredHeight, parentEditor.ScrollViewer?.Viewport.Height ?? 0);

        logger.LogDebug($"TextEditor Content Desired size: {width}, {height}");
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return new Size(
            Math.Max(finalSize.Width, parentEditor.ScrollViewer?.Viewport.Width ?? 0),
            Math.Max(finalSize.Height, parentEditor.ScrollViewer?.Viewport.Height ?? 0)
        );
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (parentEditor.RenderManager != null && _viewModel?.TextBuffer != null)
        {
            var drawingContext = new AvaloniaDrawingContext(context);
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var verticalOffset = parentEditor.ScrollViewer?.Offset.Y ?? 0;
            var viewportHeight = parentEditor.ScrollViewer?.Viewport.Height ?? Bounds.Height;

            parentEditor.RenderManager.Render(drawingContext, verticalOffset, viewportHeight);
        }
    }

    public void UpdateSize()
    {
        if (_viewModel != null)
        {
            Width = Math.Max(_viewModel.RequiredWidth + WidthPadding, parentEditor.ScrollViewer?.Viewport.Width ?? 0);
            Height = Math.Max(_viewModel.RequiredHeight + HeightPadding,
                parentEditor.ScrollViewer?.Viewport.Height ?? 0);
            InvalidateMeasure();
        }
    }
}