using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace meteor.Behaviors;

public class TextBoxKeyBehavior
{
    public static readonly AttachedProperty<bool> HandleTabProperty =
        AvaloniaProperty.RegisterAttached<TextBoxKeyBehavior, TextBox, bool>("HandleTab");

    public static void SetHandleTab(TextBox element, bool value)
    {
        element.SetValue(HandleTabProperty, value);
    }

    public static bool GetHandleTab(TextBox element)
    {
        return element.GetValue(HandleTabProperty);
    }

    static TextBoxKeyBehavior()
    {
        HandleTabProperty.Changed.AddClassHandler<TextBox>((x, e) => OnHandleTabChanged(x, e));
    }

    private static void OnHandleTabChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool newValue)
        {
            if (newValue)
                textBox.KeyDown += OnKeyDown;
            else
                textBox.KeyDown -= OnKeyDown;
        }
    }

    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab && sender is TextBox textBox)
        {
            e.Handled = true;

            if (textBox.Text == null)
            {
                textBox.Text = new string(' ', 4);
                textBox.CaretIndex = 4;
            }
            else
            {
                var caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, new string(' ', 4));
                textBox.CaretIndex = caretIndex + 4;
            }
        }
    }
}