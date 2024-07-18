using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;
using meteor.UI.Renderers;

namespace meteor.UI.Views;

public partial class EditorView : UserControl
{
    private readonly EditorRenderer _editorRenderer;
    private IEditorViewModel? _viewModel;
    private DateTime _lastClickTime = DateTime.MinValue;
    private int _clickCount;

    public EditorView()
    {
        InitializeComponent();
        _editorRenderer = new EditorRenderer();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as IEditorViewModel;
        if (_viewModel != null)
            _viewModel.PropertyChanged += (_, _e) =>
            {
                if (_e.PropertyName == nameof(IEditorViewModel.Text) ||
                    _e.PropertyName == nameof(IEditorViewModel.HighlightingResults) ||
                    _e.PropertyName == nameof(IEditorViewModel.Selection))
                    InvalidateVisual();
            };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _viewModel?.OnPointerPressed(EventArgsAdapters.ToPointerPressedEventArgs(e, this));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _viewModel?.OnPointerMoved(EventArgsAdapters.ToPointerEventArgsModel(e, this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _viewModel?.OnPointerReleased(EventArgsAdapters.ToPointerReleasedEventArgs(e, this));
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _viewModel?.OnTextInput(EventArgsAdapters.ToTextInputEventArgsModel(e));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right ||
            e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Enter)
        {
            var meteorKey = KeyMapper.ToMeteorKey(e.Key);
            var meteorKeyEventArgs = new Core.Models.Events.KeyEventArgs(meteorKey);
            _viewModel?.OnKeyDown(meteorKeyEventArgs);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_viewModel != null)
            _editorRenderer.Render(context, Bounds, _viewModel.Text, _viewModel.HighlightingResults);
    }
}