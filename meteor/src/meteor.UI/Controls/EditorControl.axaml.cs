using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private ScrollViewer _scrollViewer;
    private EditorContentControl _contentControl;
    private readonly ITextBufferService _textBufferService;

    public EditorControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, ITextBufferService textBufferService)
    {
        Focusable = true;
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _textBufferService = textBufferService;

        SetupScrollViewer();
    }

    private void SetupScrollViewer()
    {
        _contentControl = new EditorContentControl(_viewModel, _textMeasurer);
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _contentControl
        };

        Content = _scrollViewer;

        _scrollViewer.ScrollChanged += (s, args) => { _contentControl.Offset = (s as ScrollViewer)!.Offset; };
        _scrollViewer.SizeChanged += (s, args) =>
            _contentControl.Viewport = (s as ScrollViewer)!.Viewport;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _viewModel.HandleKeyDown(KeyEventArgsAdapter.Convert(e));
        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _viewModel.HandleTextInput(TextInputEventArgsAdapter.Convert(e));
        _contentControl.InvalidateVisual();
        _contentControl.InvalidateMeasure();
    }
}
