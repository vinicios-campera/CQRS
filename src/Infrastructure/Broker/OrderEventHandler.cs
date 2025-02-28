namespace Infrastructure.Broker
{
    using Domain.DTOs;
    using Domain.Events;
    using EventStore.Client;
    using Microsoft.Extensions.Configuration;
    using MongoDB.Driver;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class OrderEventHandler(IConfiguration configuration, IMongoClient mongoClient)
    {
        private readonly IMongoCollection<OrderDto> _orders
            = mongoClient.GetDatabase("OrdersDb").GetCollection<OrderDto>("Orders");

        private readonly EventStoreClient _client
            = new EventStoreClient(EventStoreClientSettings.Create(configuration.GetConnectionString("EventStore")));

        public async Task StartListening(CancellationToken cancellationToken)
        {
            var subscription = await _client.SubscribeToAllAsync(FromAll.Start, async (subscription, resolvedEvent, ct) =>
            {
                var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

                var orderDto = new OrderDto
                {
                    Id = orderEvent.Id,
                    CustomerName = orderEvent.CustomerName,
                    TotalAmount = orderEvent.TotalAmount
                };

                await _orders.InsertOneAsync(orderDto, cancellationToken: ct);
            }, cancellationToken: cancellationToken);
        }
    }
}