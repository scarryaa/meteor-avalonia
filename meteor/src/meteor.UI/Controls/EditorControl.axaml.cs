using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Adapters;

namespace meteor.UI.Controls;

public partial class EditorControl : UserControl
{
    private readonly IEditorViewModel _viewModel;

    public EditorControl(IEditorViewModel viewModel)
    {
        Focusable = true;

        _viewModel = viewModel;

        InitializeComponent();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawRectangle(Brushes.White, null, Bounds);

        var content = _viewModel.Content;
        var formattedText = new FormattedText(content, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), 13, Brushes.Black);

        context.DrawText(formattedText, new Point(0, 0));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        _viewModel.HandleKeyDown(KeyEventArgsAdapter.Convert(e));
        InvalidateVisual();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        _viewModel.HandleTextInput(TextInputEventArgsAdapter.Convert(e));
        InvalidateVisual();
    }
}