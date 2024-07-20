using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using meteor.UI.Services;
using Moq;

namespace meteor.UnitTests.UI.Services;

public class ThemeManagerTests : IDisposable
{
    private readonly Avalonia.Application app;
    private bool disposed;

    public ThemeManagerTests()
    {
        app = new Avalonia.Application();
        app.RegisterServices();
        app.Initialize();
    }

    [AvaloniaFact]
    public void AddTheme_ValidTheme_StoresTheme()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var themeName = "DarkTheme";
        var theme = new ResourceDictionary();

        // Act
        themeManager.AddTheme(themeName, theme);

        // Assert
        var exception = Record.Exception(() => themeManager.SetTheme(themeName));
        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void SetTheme_ValidTheme_ChangesCurrentTheme()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var themeName = "LightTheme";
        var theme = new ResourceDictionary();
        themeManager.AddTheme(themeName, theme);

        // Act
        themeManager.SetTheme(themeName);

        // Assert
        Assert.Equal(themeName, themeManager.CurrentTheme);
    }

    [AvaloniaFact]
    public void SetTheme_InvalidTheme_ThrowsException()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var invalidThemeName = "NonExistentTheme";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => themeManager.SetTheme(invalidThemeName));
    }

    [AvaloniaFact]
    public void AddTheme_ExistingTheme_OverwritesTheme()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var themeName = "ThemeToOverwrite";
        var theme1 = new ResourceDictionary();
        var theme2 = new ResourceDictionary(); // New theme to overwrite

        themeManager.AddTheme(themeName, theme1);

        // Act
        themeManager.AddTheme(themeName, theme2);
        themeManager.SetTheme(themeName);

        // Assert
        Assert.Equal(themeName, themeManager.CurrentTheme);
    }

    [AvaloniaFact]
    public void AddTheme_EmptyThemeName_ThrowsException()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var emptyThemeName = string.Empty;
        var theme = new ResourceDictionary();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => themeManager.AddTheme(emptyThemeName, theme));
    }

    [AvaloniaFact]
    public void SetTheme_EmptyThemeName_ThrowsException()
    {
        // Arrange
        var appMock = new Mock<Avalonia.Application>();
        var themeManager = new ThemeManager(appMock.Object);
        var emptyThemeName = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => themeManager.SetTheme(emptyThemeName));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed) disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}