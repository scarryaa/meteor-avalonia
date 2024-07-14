namespace tests.Mocks;

public class MockConsoleInput : IDisposable
{
    private readonly StringReader _stringReader;
    private readonly TextReader _originalInput;

    public MockConsoleInput(string input)
    {
        _stringReader = new StringReader(input);
        _originalInput = Console.In;
        Console.SetIn(_stringReader);
    }

    public void Dispose()
    {
        Console.SetIn(_originalInput);
        _stringReader.Dispose();
    }
}