using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Models;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Services;
using meteor.UI.ViewModels;

namespace meteor.UI.Models;

public class EditorInstance : IEditorInstance
{
    public IEditorViewModel EditorViewModel { get; }

    public EditorInstance(IEditorConfig config, ITextMeasurer textMeasurer, IClipboardManager clipboardManager,
        ITextAnalysisService textAnalysisService, IScrollManager scrollManager)
    {
        var textBufferService = new TextBufferService(textMeasurer, config);
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
            textMeasurer);
    }
}