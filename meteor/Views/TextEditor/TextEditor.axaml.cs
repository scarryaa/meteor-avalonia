using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.Services;
using meteor.ViewModels;
using meteor.Views.Contexts;
using meteor.Views.Services;
using meteor.Views.Utils;

namespace meteor.Views;

public partial class TextEditor : UserControl
{
    public const double SelectionEndPadding = 2;
    public const double LinePadding = 20;

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextEditor, FontFamily>(
            nameof(FontFamily),
            new FontFamily("avares://meteor/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(FontSize), 13);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TextEditor, double>(nameof(LineHeight), 20);

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(BackgroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> CursorBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(CursorBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(SelectionBrush),
            new SolidColorBrush(Color.FromArgb(100, 139, 205, 192)));

    public static readonly StyledProperty<IBrush> LineHighlightBrushProperty =
        AvaloniaProperty.Register<TextEditor, IBrush>(nameof(LineHighlightBrush),
            new SolidColorBrush(Color.Parse("#ededed")));

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush CursorBrush
    {
        get => GetValue(CursorBrushProperty);
        set => SetValue(CursorBrushProperty, value);
    }

    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public IBrush LineHighlightBrush
    {
        get => GetValue(LineHighlightBrushProperty);
        set => SetValue(LineHighlightBrushProperty, value);
    }

    private ScrollableTextEditorViewModel? _scrollableViewModel;
    private InputManager InputManager { get; set; }
    private ScrollManager ScrollManager { get; set; }
    private RenderManager RenderManager { get; set; }
    private TextEditorUtils TextEditorUtils { get; set; }
    private ClipboardManager ClipboardManager { get; set; }
    private CursorManager CursorManager { get; set; }
    private SelectionManager SelectionManager { get; set; }
    private LineManager LineManager { get; set; }
    private TextManipulator TextManipulator { get; set; }

    public TextEditor()
    {
        InitializeComponent();
        Initialize();
    }

    private void Initialize()
    {
        Cursor = new Cursor(StandardCursorType.Ibeam);
        DataContextChanged += OnDataContextChanged;
        Focusable = true;

        InitializeServices();
        SubscribeToProperties();
    }

    private void InitializeServices()
    {
        TextEditorUtils = new TextEditorUtils(null);
        InputManager = new InputManager();
        ScrollManager = new ScrollManager(null); // Initialize ScrollManager here
        ClipboardManager = new ClipboardManager(null, new ClipboardService(TopLevel.GetTopLevel(this)));
        CursorManager = new CursorManager(null);
        SelectionManager = new SelectionManager(null);
        LineManager = new LineManager();
        TextManipulator = new TextManipulator();
    }

    private void AssignServices(ScrollableTextEditorViewModel viewModel)
    {
        viewModel.TextEditorViewModel._scrollableViewModel = viewModel;

        TextEditorUtils = viewModel.TextEditorViewModel.TextEditorUtils;
        InputManager = viewModel.TextEditorViewModel.InputManager;
        ScrollManager = viewModel.TextEditorViewModel.ScrollManager;
        CursorManager = viewModel.TextEditorViewModel.CursorManager;
        SelectionManager = viewModel.TextEditorViewModel.SelectionManager;
        LineManager = viewModel.TextEditorViewModel.LineManager;
        TextManipulator = viewModel.TextEditorViewModel.TextManipulator;
        ClipboardManager = new ClipboardManager(null, new ClipboardService(TopLevel.GetTopLevel(this)));
    }

    private void RegisterEventHandlers()
    {
        Console.WriteLine("TextEditor - Registering event handlers");
        AddHandler(PointerWheelChangedEvent, InputManager.OnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, InputManager.OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, InputManager.OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, InputManager.OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, InputManager.OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, InputManager.OnTextInput, RoutingStrategies.Tunnel);
    }

    private void UnregisterEventHandlers()
    {
        Console.WriteLine("TextEditor - Unregistering event handlers");
        RemoveHandler(PointerWheelChangedEvent, InputManager.OnPointerWheelChanged);
        RemoveHandler(PointerPressedEvent, InputManager.OnPointerPressed);
        RemoveHandler(PointerMovedEvent, InputManager.OnPointerMoved);
        RemoveHandler(PointerReleasedEvent, InputManager.OnPointerReleased);
        RemoveHandler(KeyDownEvent, InputManager.OnKeyDown);
        RemoveHandler(TextInputEvent, InputManager.OnTextInput);
    }

    private void SubscribeToProperties()
    {
        this.GetObservable(FontFamilyProperty).Subscribe(OnFontFamilyChanged);
        this.GetObservable(FontSizeProperty).Subscribe(OnFontSizeChanged);
        this.GetObservable(LineHeightProperty).Subscribe(OnLineHeightChanged);
        this.GetObservable(BackgroundBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(CursorBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectionBrushProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(LineHighlightBrushProperty).Subscribe(_ => InvalidateVisual());
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_scrollableViewModel?.TextEditorViewModel != null)
        {
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _scrollableViewModel.TextEditorViewModel.SelectionChanged -= SelectionManager.OnSelectionChanged;
            UnregisterEventHandlers();
        }

        if (DataContext is ScrollableTextEditorViewModel scrollableViewModel)
        {
            _scrollableViewModel = scrollableViewModel;
            var viewModel = scrollableViewModel.TextEditorViewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.RequestFocus += OnRequestFocus;
            viewModel.SelectionChanged += SelectionManager.OnSelectionChanged;
            viewModel.InvalidateRequired += OnInvalidateRequired;

            var context = CreateContext();
            RenderManager = new RenderManager(context);
            RenderManager.AttachToViewModel(scrollableViewModel);

            scrollableViewModel.ScrollManager = ScrollManager;

            // Measure char width after view model is set
            TextEditorUtils.UpdateViewModel(viewModel);
            TextEditorUtils.MeasureCharWidth();

            LineManager.UpdateViewModel(viewModel);
            LineManager.UpdateLineCache(-1);

            viewModel.UpdateServices(viewModel);
            SelectionManager.UpdateViewModel(viewModel);
            AssignServices(scrollableViewModel);
            RegisterEventHandlers();
        }
    }

    private TextEditorContext CreateContext()
    {
        return new TextEditorContext(
            LineHeight,
            BackgroundBrush,
            LineHighlightBrush,
            SelectionBrush,
            CursorBrush,
            LinePadding,
            SelectionEndPadding,
            FontFamily,
            FontSize,
            Foreground,
            _scrollableViewModel);
    }

    private void OnInvalidateRequired(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TextEditorViewModel.TextBuffer):
                Dispatcher.UIThread.Post(InvalidateVisual);
                break;
            case nameof(TextEditorViewModel.CursorPosition):
                if (_scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor)
                    Dispatcher.UIThread.Post(ScrollManager.EnsureCursorVisible);
                break;
        }
    }

    public override void Render(DrawingContext context)
    {
        RenderManager?.Render(context);
    }

    private void OnRequestFocus(object? sender, EventArgs e)
    {
        Focus();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (_scrollableViewModel?.TextEditorViewModel != null)
            _scrollableViewModel.TextEditorViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void OnLineHeightChanged(double newLineHeight)
    {
        InvalidateVisual();
    }

    private void OnFontSizeChanged(double newFontSize)
    {
        if (_scrollableViewModel?.TextEditorViewModel != null) TextEditorUtils.MeasureCharWidth();
        InvalidateVisual();
    }

    private void OnFontFamilyChanged(FontFamily newFontFamily)
    {
        if (_scrollableViewModel?.TextEditorViewModel != null) TextEditorUtils.MeasureCharWidth();
        InvalidateVisual();
    }
}