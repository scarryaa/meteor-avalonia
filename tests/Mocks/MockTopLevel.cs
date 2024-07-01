using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace tests.Mocks;

public class MockTopLevel : Window
{
    public IClipboard MockClipboard { get; set; }

    public IClipboard Clipboard => MockClipboard;
}