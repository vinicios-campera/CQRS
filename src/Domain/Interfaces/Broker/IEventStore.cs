﻿namespace Domain.Interfaces.Broker
{
    public interface IEventStore
    {
        Task AppendEventAsync(string streamName, object @event);

        Task<List<object>> ReadStreamAsync(string streamName);
    }
}