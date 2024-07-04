using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using meteor.ViewModels;

namespace meteor.Views;

public partial class CommandPalette : UserControl
{
    public static readonly AttachedProperty<BoxShadows> DynamicBoxShadowProperty =
        AvaloniaProperty.RegisterAttached<CommandPalette, Control, BoxShadows>("DynamicBoxShadow");

    public static void SetDynamicBoxShadow(Control control, BoxShadows value)
    {
        control.SetValue(DynamicBoxShadowProperty, value);
    }

    private void OnFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => CommandTextBox.Focus(), DispatcherPriority.Background);
    }

    public CommandPalette()
    {
        InitializeComponent();
        UpdateBoxShadow();

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CommandPaletteViewModel viewModel)
        {
            viewModel.FocusRequested += OnFocusRequested;
            viewModel.ThemeChanged += OnThemeChanged;
            UpdateBoxShadow();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateBoxShadow();
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateBoxShadow();
    }

    private void UpdateBoxShadow()
    {
        if (Application.Current?.ActualThemeVariant != null &&
            TryFindResource("CommandPaletteBoxShadowColor", out var resource))
        {
            var color = (Color)resource;
            var boxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 12,
                Spread = 0,
                Color = color
            });

            SetDynamicBoxShadow(this, boxShadow);
        }
    }

    private bool TryFindResource(object key, out object? resource)
    {
        resource = null;
        var theme = Application.Current?.ActualThemeVariant;
        return Application.Current?.TryGetResource(key, theme, out resource) == true;
    }
}