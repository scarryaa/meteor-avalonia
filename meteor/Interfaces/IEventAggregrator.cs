using System;

namespace meteor.Interfaces;

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventToPublish);
    void Subscribe<TEvent>(Action<TEvent> eventHandler);
}