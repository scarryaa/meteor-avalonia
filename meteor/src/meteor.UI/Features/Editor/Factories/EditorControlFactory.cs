using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Features.Editor.Controls;
using meteor.UI.Features.Editor.Interfaces;

namespace meteor.UI.Features.Editor.Factories;

public class EditorControlFactory : IEditorControlFactory
{
    private readonly IEditorConfig _config;
    private readonly IEditorInputHandler _inputHandler;
    private readonly IEditorLayoutManager _layoutManager;
    private readonly IPointerEventHandler _pointerEventHandler;
    private readonly IScrollManager _scrollManager;
    private readonly ITextMeasurer _textMeasurer;

    public EditorControlFactory(
        IScrollManager scrollManager,
        IEditorLayoutManager layoutManager,
        IEditorInputHandler inputHandler,
        IPointerEventHandler pointerEventHandler,
        ITextMeasurer textMeasurer,
        IEditorConfig config)
    {
        _scrollManager = scrollManager ?? throw new ArgumentNullException(nameof(scrollManager));
        _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
        _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        _pointerEventHandler = pointerEventHandler ?? throw new ArgumentNullException(nameof(pointerEventHandler));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public EditorControl CreateEditorControl(IEditorViewModel viewModel)
    {
        if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

        return new EditorControl(
            viewModel,
            _scrollManager,
            _layoutManager,
            _inputHandler,
            _pointerEventHandler,
            _textMeasurer,
            _config
        );
    }
}