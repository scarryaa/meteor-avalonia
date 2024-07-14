using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}