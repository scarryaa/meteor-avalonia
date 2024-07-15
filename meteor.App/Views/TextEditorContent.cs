using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.App.Rendering;
using meteor.Core.Interfaces.ViewModels;
using Microsoft.Extensions.Logging;

namespace meteor.App.Views;

public class TextEditorContent : Control
{
    private readonly TextEditor _parentEditor;
    private ITextEditorViewModel _viewModel;
    private double WidthPadding;
    private double HeightPadding;
    private readonly ILogger<TextEditorContent> _logger;

    public TextEditorContent(TextEditor parentEditor, ILogger<TextEditorContent> logger)
    {
        _logger = logger;
        _parentEditor = parentEditor;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel = _parentEditor.DataContext as ITextEditorViewModel;

        HeightPadding = _viewModel.LineHeight * 5;
        WidthPadding = _viewModel.FontSize * 10;

        UpdateSize();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_viewModel == null) return new Size(0, 0);

        var width = Math.Max(_viewModel.RequiredWidth, _parentEditor.ScrollViewer?.Viewport.Width ?? 0);
        var height = Math.Max(_viewModel.RequiredHeight, _parentEditor.ScrollViewer?.Viewport.Height ?? 0);

        _logger.LogDebug($"TextEditor Content Desired size: {width}, {height}");
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return new Size(
            Math.Max(finalSize.Width, _parentEditor.ScrollViewer?.Viewport.Width ?? 0),
            Math.Max(finalSize.Height, _parentEditor.ScrollViewer?.Viewport.Height ?? 0)
        );
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_parentEditor.RenderManager != null && _viewModel?.TextBuffer != null)
        {
            var drawingContext = new AvaloniaDrawingContext(context);
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var verticalOffset = _parentEditor.ScrollViewer?.Offset.Y ?? 0;
            var viewportHeight = _parentEditor.ScrollViewer?.Viewport.Height ?? Bounds.Height;

            _parentEditor.RenderManager.Render(drawingContext, verticalOffset, viewportHeight);
        }
    }

    public void UpdateSize()
    {
        if (_viewModel != null)
        {
            Width = Math.Max(_viewModel.RequiredWidth + WidthPadding, _parentEditor.ScrollViewer?.Viewport.Width ?? 0);
            Height = Math.Max(_viewModel.RequiredHeight + HeightPadding,
                _parentEditor.ScrollViewer?.Viewport.Height ?? 0);
            InvalidateMeasure();
        }
    }
}