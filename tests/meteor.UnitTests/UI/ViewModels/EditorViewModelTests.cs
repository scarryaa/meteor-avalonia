using System.Text;
using meteor.Core.Enums;
using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.UI.ViewModels;
using Moq;

namespace meteor.UnitTests.UI.ViewModels;

public class EditorViewModelTests
{
    private readonly Mock<ITextBufferService> _textBufferServiceMock;
    private readonly Mock<ISyntaxHighlighter> _syntaxHighlighterMock;
    private readonly Mock<ISelectionService> _selectionServiceMock;
    private readonly Mock<ICursorService> _cursorServiceMock;
    private readonly Mock<IInputService> _inputServiceMock;
    private readonly Mock<IEditorSizeCalculator> _sizeCalculatorMock;
    private readonly EditorViewModel _viewModel;
    private readonly Mock<ITabService> _tabServiceMock;

    public EditorViewModelTests()
    {
        _tabServiceMock = new Mock<ITabService>();
        _textBufferServiceMock = new Mock<ITextBufferService>();
        _syntaxHighlighterMock = new Mock<ISyntaxHighlighter>();
        _selectionServiceMock = new Mock<ISelectionService>();
        _inputServiceMock = new Mock<IInputService>();
        _cursorServiceMock = new Mock<ICursorService>();
        _sizeCalculatorMock = new Mock<IEditorSizeCalculator>();
        _viewModel = new EditorViewModel(
            _textBufferServiceMock.Object,
            _tabServiceMock.Object,
            _syntaxHighlighterMock.Object,
            _selectionServiceMock.Object,
            _inputServiceMock.Object,
            _cursorServiceMock.Object,
            _sizeCalculatorMock.Object
        );

        _tabServiceMock.Setup(ts => ts.GetActiveTextBufferService()).Returns(_textBufferServiceMock.Object);
    }

    [Fact]
    public void Text_Get_ReturnsTextFromService()
    {
        var sb = new StringBuilder("hello");
        _textBufferServiceMock.Setup(t => t.AppendTo(It.IsAny<StringBuilder>()))
            .Callback<StringBuilder>(s => s.Append(sb));
        var text = _viewModel.Text;
        Assert.Equal("hello", text);
    }

    [Fact]
    public void Text_Set_CallsReplaceAllAndUpdatesHighlighting()
    {
        var oldText = "old text";
        var newText = "new text";
        var highlightingResults = new[] { new SyntaxHighlightingResult(0, 3, SyntaxHighlightingType.Keyword) };

        // Setup initial text
        _textBufferServiceMock.Setup(t => t.AppendTo(It.IsAny<StringBuilder>()))
            .Callback<StringBuilder>(s => s.Append(oldText));

        // Setup ReplaceAll to update the internal text
        _textBufferServiceMock.Setup(t => t.ReplaceAll(newText)).Callback(() =>
        {
            _textBufferServiceMock.Setup(t => t.AppendTo(It.IsAny<StringBuilder>()))
                .Callback<StringBuilder>(s =>
                {
                    s.Clear();
                    s.Append(newText);
                });
        });

        _syntaxHighlighterMock.Setup(s => s.Highlight(newText)).Returns(highlightingResults);

        _viewModel.Text = newText;

        _textBufferServiceMock.Verify(t => t.ReplaceAll(newText), Times.Once);
        _syntaxHighlighterMock.Verify(s => s.Highlight(newText), Times.Once);
        Assert.Equal(highlightingResults, _viewModel.HighlightingResults);
    }

    [Fact]
    public void InsertText_CallsInsertTextOnInputServiceAndUpdatesHighlighting()
    {
        var text = "world";
        _syntaxHighlighterMock.Setup(s => s.Highlight(It.IsAny<string>()))
            .Returns(new[] { new SyntaxHighlightingResult() });

        _viewModel.InsertText(0, text);

        _inputServiceMock.Verify(i => i.InsertText(text), Times.Once);
        _syntaxHighlighterMock.Verify(s => s.Highlight(It.IsAny<string>()), Times.Once);
        Assert.Single(_viewModel.HighlightingResults);
    }
    
    [Fact]
    public void DeleteText_CallsDeleteTextOnInputServiceAndUpdatesHighlighting()
    {
        var index = 0;
        var length = 1;
        _syntaxHighlighterMock.Setup(s => s.Highlight(It.IsAny<string>()))
            .Returns(new[] { new SyntaxHighlightingResult() });

        _viewModel.DeleteText(index, length);

        _inputServiceMock.Verify(i => i.DeleteText(index, length), Times.Once);
        _syntaxHighlighterMock.Verify(s => s.Highlight(It.IsAny<string>()), Times.Once);
        Assert.Single(_viewModel.HighlightingResults);
    }

    [Fact]
    public void OnPointerPressed_CallsHandlePointerPressedAndRaisesPropertyChanged()
    {
        var e = new PointerPressedEventArgs(5, 0, 0);
        var propertyChanged = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.Selection))
                propertyChanged = true;
        };

        _viewModel.OnPointerPressed(e);

        _inputServiceMock.Verify(i => i.HandlePointerPressed(e), Times.Once);
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnPointerMoved_CallsHandlePointerMovedAndRaisesPropertyChanged()
    {
        var e = new PointerEventArgs(10, 20, 5);
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.Selection))
                propertyChanged = true;
        };

        _viewModel.OnPointerMoved(e);

        _inputServiceMock.Verify(i => i.HandlePointerMoved(e), Times.Once);
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnPointerReleased_CallsHandlePointerReleasedAndRaisesPropertyChanged()
    {
        var e = new PointerReleasedEventArgs(5, 0, 0);
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.Selection))
                propertyChanged = true;
        };

        _viewModel.OnPointerReleased(e);

        _inputServiceMock.Verify(i => i.HandlePointerReleased(e), Times.Once);
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnTextInput_CallsHandleTextInputAndUpdatesHighlighting()
    {
        var e = new TextInputEventArgs("a");
        _syntaxHighlighterMock.Setup(s => s.Highlight(It.IsAny<string>()))
            .Returns(new[] { new SyntaxHighlightingResult() });

        _viewModel.OnTextInput(e);

        _inputServiceMock.Verify(i => i.HandleTextInput(e), Times.Once);
        _syntaxHighlighterMock.Verify(s => s.Highlight(It.IsAny<string>()), Times.Once);
        Assert.Single(_viewModel.HighlightingResults);
    }

    [Fact]
    public void OnKeyDown_CallsHandleKeyDownAndUpdatesHighlighting()
    {
        var e = new KeyEventArgs(Key.Enter);
        _syntaxHighlighterMock.Setup(s => s.Highlight(It.IsAny<string>()))
            .Returns(new[] { new SyntaxHighlightingResult() });

        _viewModel.OnKeyDown(e);

        _inputServiceMock.Verify(i => i.HandleKeyDown(e.Key, e.Modifiers), Times.Once);
        _syntaxHighlighterMock.Verify(s => s.Highlight(It.IsAny<string>()), Times.Once);
        Assert.Single(_viewModel.HighlightingResults);
    }
}