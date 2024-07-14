using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.avalonia.ViewModels;

namespace meteor.avalonia.Views;

public partial class CodeEditorView : UserControl
{
    private CodeEditorViewModel ViewModel => DataContext as CodeEditorViewModel;
    private readonly DispatcherTimer _invalidationTimer;
    private Vector _scrollOffset;

    public CodeEditorView()
    {
        InitializeComponent();
        Focusable = true;

        _invalidationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _invalidationTimer.Tick += (sender, args) =>
        {
            Console.WriteLine("InvalidateVisual called by DispatcherTimer");
            _invalidationTimer.Stop();
            InvalidateVisual();
        };

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        this.GetObservable(BoundsProperty).Subscribe(bounds =>
        {
            if (ViewModel != null)
            {
                ViewModel.CanvasWidth = bounds.Width;
                ViewModel.CanvasHeight = bounds.Height;
                ViewModel.RenderManager.UpdateViewport(bounds.Size);
                Console.WriteLine($"Bounds updated: Width = {bounds.Width}, Height = {bounds.Height}");
                InvalidateVisual();
            }
        });

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (sender is CodeEditorView editorView)
        {
            if (editorView.DataContext is CodeEditorViewModel oldViewModel)
                oldViewModel.InvalidateVisualRequested -= OnInvalidateVisualRequested;

            if (editorView.DataContext is CodeEditorViewModel newViewModel)
                newViewModel.InvalidateVisualRequested += OnInvalidateVisualRequested;
        }
    }

    private void OnInvalidateVisualRequested(object sender, EventArgs e)
    {
        Console.WriteLine("InvalidateVisual requested by ViewModel");
        RequestInvalidateVisual();
    }

    private void RequestInvalidateVisual()
    {
        if (!_invalidationTimer.IsEnabled) _invalidationTimer.Start();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (ViewModel != null)
        {
            Console.WriteLine($"Rendering view: Size = {Bounds.Size}, Scroll Offset = {_scrollOffset}");
            using (context.PushPreTransform(Matrix.CreateTranslation(-_scrollOffset.X, -_scrollOffset.Y)))
            {
                ViewModel.Render(context, Bounds.Size);
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var delta = e.Delta.Y;
        _scrollOffset =
            new Vector(_scrollOffset.X, Math.Max(0, _scrollOffset.Y - delta * 40));
        ViewModel?.RenderManager.SetScrollPosition(_scrollOffset.Y);
        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var position = e.GetPosition(this) + _scrollOffset;
        ViewModel?.HandlePointerPressed(position);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this) + _scrollOffset;
        var isLeftButtonPressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        ViewModel?.HandlePointerMoved(position, isLeftButtonPressed);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        ViewModel?.HandleKeyPress(e);
        e.Handled = true;
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        Console.WriteLine(e);
        ViewModel?.HandleTextInput(e.Text);
        e.Handled = true;
    }
}