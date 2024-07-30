using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace meteor.UI.Behaviors;

public static class MiddleTabClickBehavior
{
    public static readonly AttachedProperty<ICommand> CloseCommandProperty =
        AvaloniaProperty.RegisterAttached<Interactive, ICommand>("CloseCommand", typeof(MiddleTabClickBehavior));

    public static readonly AttachedProperty<object> CloseCommandParameterProperty =
        AvaloniaProperty.RegisterAttached<Interactive, object>("CloseCommandParameter", typeof(MiddleTabClickBehavior));

    static MiddleTabClickBehavior()
    {
        CloseCommandProperty.Changed.Subscribe(
            new AnonymousObserver<AvaloniaPropertyChangedEventArgs<ICommand>>(OnCloseCommandChanged));
    }

    public static ICommand GetCloseCommand(Interactive element)
    {
        return element.GetValue(CloseCommandProperty);
    }

    public static void SetCloseCommand(Interactive element, ICommand value)
    {
        element.SetValue(CloseCommandProperty, value);
    }

    public static object GetCloseCommandParameter(Interactive element)
    {
        return element.GetValue(CloseCommandParameterProperty);
    }

    public static void SetCloseCommandParameter(Interactive element, object value)
    {
        element.SetValue(CloseCommandParameterProperty, value);
    }

    private static void OnCloseCommandChanged(AvaloniaPropertyChangedEventArgs<ICommand> e)
    {
        if (e.Sender is Control control)
        {
            control.PointerPressed -= Control_PointerPressed;
            if (e.NewValue != null) control.PointerPressed += Control_PointerPressed;
        }
    }

    private static void Control_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint((Control)sender).Properties.IsMiddleButtonPressed)
        {
            var command = GetCloseCommand((Interactive)sender);
            var commandParameter = GetCloseCommandParameter((Interactive)sender);
            if (command != null && command.CanExecute(commandParameter)) command.Execute(commandParameter);
        }
    }
}