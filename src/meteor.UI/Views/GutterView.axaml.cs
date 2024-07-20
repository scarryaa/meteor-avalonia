using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Config;
using meteor.UI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI.Views;

public partial class GutterView : UserControl
{
    public static readonly StyledProperty<IGutterViewModel> ViewModelProperty =
        AvaloniaProperty.Register<GutterView, IGutterViewModel>(nameof(ViewModel));

    public IGutterViewModel ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private FormattedText? _cachedFormattedText;
    private int _cachedLineNumber = -1;
    private readonly IThemeManager _themeManager;
    private readonly ThemeConfig _themeConfig;

    private const double EndPadding = 10;

    private Typeface _typeface;
    private double _fontSize;
    private IBrush _backgroundBrush;
    private IBrush _highlightBrush;
    private IBrush _defaultBrush;
    private IBrush _selectedBrush;

    static GutterView()
    {
        AffectsRender<GutterView>(ViewModelProperty);
    }

    public GutterView()
    {
        if (Application.Current is App app)
        {
            var serviceProvider = app.ServiceProvider;
            _themeManager = serviceProvider.GetRequiredService<IThemeManager>();
            _themeConfig = serviceProvider.GetRequiredService<ThemeConfig>();
        }
        else
        {
            throw new InvalidOperationException("Unable to access the application's service provider.");
        }

        this.GetObservable(ViewModelProperty)
            .Subscribe(new AnonymousObserver<IGutterViewModel>(vm =>
            {
                if (vm != null) vm.PropertyChanged += ViewModel_PropertyChanged;
            }));

        _themeManager.ThemeChanged += (_, __) =>
        {
            UpdateThemeResources();
            InvalidateVisual();
        };

        UpdateThemeResources();

        PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel != null)
        {
            var delta = e.Delta.Y;
            var newOffset = ViewModel.ScrollOffset - delta * ViewModel.LineHeight * 4;

            var maxScrollOffset = Math.Max(0, ViewModel.TotalHeight - ViewModel.ViewportHeight);
            newOffset = Math.Clamp(newOffset, 0, maxScrollOffset);

            ViewModel.UpdateScrollOffset(newOffset);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
    }

    private void UpdateThemeResources()
    {
        var baseTheme = _themeManager.GetBaseTheme();
        var fontFamilyUri = new Uri(_themeConfig.GutterFontFamilyUri);

        _typeface = new Typeface(new FontFamily(fontFamilyUri, baseTheme["GutterFontFamily"].ToString()));
        _fontSize = Convert.ToDouble(baseTheme["GutterFontSize"]);
        _backgroundBrush = new SolidColorBrush(Color.Parse(baseTheme["GutterBackground"].ToString()));
        _highlightBrush = new SolidColorBrush(Color.Parse(baseTheme["GutterHighlight"].ToString()));
        _defaultBrush = new SolidColorBrush(Color.Parse(baseTheme["GutterDefault"].ToString()));
        _selectedBrush = new SolidColorBrush(Color.Parse(baseTheme["GutterSelected"].ToString()));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = new Size(ViewModel?.GutterWidth ?? 0, availableSize.Height);
        if (ViewModel != null) ViewModel.ViewportHeight = availableSize.Height;
        return result;
    }

    public override void Render(DrawingContext context)
    {
        if (ViewModel == null) return;

        var size = Bounds.Size;

        context.FillRectangle(_backgroundBrush, new Rect(size));

        var startLine = Math.Max(1, (int)(ViewModel.ScrollOffset / ViewModel.LineHeight) + 1);
        var endLine = Math.Min(ViewModel.LineCount, startLine + (int)(size.Height / ViewModel.LineHeight) + 1);

        for (var lineNumber = startLine; lineNumber <= endLine; lineNumber++)
        {
            var text = GetFormattedText(lineNumber);
            var y = (lineNumber - 1) * ViewModel.LineHeight - ViewModel.ScrollOffset;

            if (lineNumber == ViewModel.CurrentLine)
                context.FillRectangle(_highlightBrush, new Rect(0, y, size.Width, ViewModel.LineHeight));

            var textX = size.Width - text.Width - EndPadding * 2.5;

            context.DrawText(text, new Point(textX, y + (ViewModel.LineHeight - text.Height) / 2));

            DrawCollapseExpandIcon(context, lineNumber, y);
            DrawBreakpoint(context, lineNumber, y);
        }

        ViewModel.VisibleLineCount = endLine - startLine + 1;
    }

    private void DrawCollapseExpandIcon(DrawingContext context, int lineNumber, double y)
    {
        // TODO: Implement
    }

    private void DrawBreakpoint(DrawingContext context, int lineNumber, double y)
    {
        // TODO: Implement
    }

    private FormattedText GetFormattedText(int lineNumber)
    {
        if (_cachedLineNumber != lineNumber)
        {
            _cachedFormattedText = new FormattedText(
                lineNumber.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                _fontSize,
                lineNumber == ViewModel.CurrentLine ? _selectedBrush : _defaultBrush);
            _cachedLineNumber = lineNumber;
        }

        return _cachedFormattedText!;
    }
}