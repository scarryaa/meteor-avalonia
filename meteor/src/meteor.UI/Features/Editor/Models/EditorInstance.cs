using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Models;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Services;
using meteor.UI.Features.Editor.ViewModels;

namespace meteor.UI.Features.Editor.Models;

public class EditorInstance : IEditorInstance
{
    public EditorInstance(IEditorConfig config, ITextMeasurer textMeasurer, IClipboardManager clipboardManager,
        ITextAnalysisService textAnalysisService, IScrollManager scrollManager, IStatusBarService statusBarService)
    {
        var textBufferService = new TextBufferService(new TextBuffer(), textMeasurer, config);
        var cursorManager = new CursorManager(textBufferService, config);
        var selectionManager = new SelectionManager(textBufferService);
        var inputManager = new InputManager(
            textBufferService,
            cursorManager,
            clipboardManager,
            selectionManager,
            textAnalysisService,
            scrollManager);

        EditorViewModel = new EditorViewModel(
            textBufferService,
            cursorManager,
            inputManager,
            selectionManager,
            config,
            textMeasurer,
            new CompletionProvider(textBufferService),
            statusBarService);

        inputManager.SetViewModel(EditorViewModel);
    }

    public IEditorViewModel EditorViewModel { get; }
}