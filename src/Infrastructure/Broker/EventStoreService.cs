using Domain.Interfaces.Events;
using EventStore.Client;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Broker
{
    public class EventStoreService(IConfiguration configuration) : IEventStore
    {
        private readonly EventStoreClient _client
            = new EventStoreClient(EventStoreClientSettings.Create(configuration.GetConnectionString("EventStore")));

        public async Task AppendEventAsync(string streamName, object @event)
        {
            var eventData = new EventData(
                EventStore.Client.Uuid.NewUuid(),
                @event.GetType().Name,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event))
            );

            await _client.AppendToStreamAsync(streamName, StreamState.Any, new[] { eventData });
        }

        public async Task<List<object>> ReadStreamAsync(string streamName)
        {
            var events = new List<object>();

            var result = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);
            await foreach (var resolvedEvent in result)
            {
                var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
                events.Add(JsonSerializer.Deserialize<object>(json));
            }

            return events;
        }
    }
}