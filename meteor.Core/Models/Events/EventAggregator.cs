using meteor.Core.Interfaces.Events;

namespace meteor.Core.Models.Events;

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<object>> _eventSubscribers = new();

    public void Publish<TEvent>(TEvent eventToPublish)
    {
        var eventType = typeof(TEvent);
        if (_eventSubscribers.ContainsKey(eventType))
            foreach (var subscriber in _eventSubscribers[eventType].OfType<Action<TEvent>>())
                subscriber(eventToPublish);
    }

    public void Subscribe<TEvent>(Action<TEvent> eventHandler)
    {
        var eventType = typeof(TEvent);
        if (!_eventSubscribers.ContainsKey(eventType)) _eventSubscribers[eventType] = new List<object>();
        _eventSubscribers[eventType].Add(eventHandler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> eventHandler)
    {
        var eventType = typeof(TEvent);
        if (_eventSubscribers.ContainsKey(eventType)) _eventSubscribers[eventType].Remove(eventHandler);
    }
}