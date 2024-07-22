namespace meteor.Core.Models;

public class DelayedAction
{
    private readonly Func<Task> _action;
    private readonly TimeSpan _delay;
    private Timer? _timer;

    public DelayedAction(Func<Task> action, TimeSpan delay)
    {
        _action = action;
        _delay = delay;
    }

    public void Trigger()
    {
        _timer?.Dispose();
        _timer = new Timer(async _ =>
        {
            await _action();
            _timer?.Dispose();
        }, null, _delay, Timeout.InfiniteTimeSpan);
    }
}