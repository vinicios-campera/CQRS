using Domain.DTOs;
using Domain.Events;
using EventStore.Client;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Text;

namespace EventStoreConsumer
{
    public class Worker(ILogger<Worker> logger, IMongoClient mongoClient, IConfiguration configuration) : BackgroundService
    {
        private readonly IMongoCollection<OrderDto> _orders
            = mongoClient.GetDatabase("OrdersDb").GetCollection<OrderDto>("Orders");

        private readonly EventStorePersistentSubscriptionsClient _clientPersistentSub
            = new(EventStoreClientSettings.Create(configuration.GetConnectionString("EventStore")!));

        private readonly EventStoreClient _client
            = new(EventStoreClientSettings.Create(configuration.GetConnectionString("EventStore")!));

        private readonly IEnumerable<string> _streams
            = configuration.GetSection("EventStore:Streams").Get<List<string>>()!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> subscriptionTasks = [];

            foreach (var stream in _streams)
                subscriptionTasks.Add(Task.Run(() => SubscribeToStream(stream, stoppingToken), stoppingToken));

            await Task.WhenAll(subscriptionTasks);
        }

        private async Task SubscribeToStream(string streamName, CancellationToken stoppingToken)
        {
            logger.LogInformation("Iniciando consumo para o stream: {StreamName}", streamName);

            var persistent = configuration.GetValue<bool>("EventStore:Persistent");

            if (persistent) //Só vai obter os eventos não reconhecidos
            {
                await _clientPersistentSub.SubscribeToStreamAsync(
                streamName,
                "orders-group",
                async (subscription, resolvedEvent, retryCount, cancellationToken) =>
                {
                    try
                    {
                        await HandleEvent(resolvedEvent, cancellationToken);
                        await subscription.Ack(resolvedEvent);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[Erro no consumo de {StreamName}]: {Ex}", streamName, ex);
                        await subscription.Nack(PersistentSubscriptionNakEventAction.Retry, $"Erro no processamento: {ex.Message}", resolvedEvent);
                    }
                },
                cancellationToken: stoppingToken);
            }
            else //Sem persistencia. Toda vez que reiniciar a aplicação, obtem todos os eventos
            {
                await _client.SubscribeToStreamAsync(
                streamName,
                FromStream.Start,
                async (subscription, resolvedEvent, cancellationToken) =>
                {
                    try
                    {
                        await HandleEvent(resolvedEvent, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[Erro no consumo de {StreamName}]: {Ex}", streamName, ex);
                    }
                },
                false,
                cancellationToken: stoppingToken);
            }
        }

        private async Task HandleEvent(ResolvedEvent resolvedEvent, CancellationToken ct)
        {
            if (resolvedEvent.Event.Data.Length == 0)
                return;

            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
            var eventType = resolvedEvent.Event.EventType;

            logger.LogInformation("Evento recebido: {EventType} - Dados: {EventData}", eventType, eventData);

            var jsonString = JsonConvert.DeserializeObject<string>(eventData)!;
            var orderEvent = JsonConvert.DeserializeObject<OrderCreatedEvent>(jsonString);

            var orderDto = new OrderDto
            {
                Id = orderEvent!.Id,
                CustomerName = orderEvent.CustomerName,
                TotalAmount = orderEvent.TotalAmount
            };

            await _orders.InsertOneAsync(orderDto, cancellationToken: ct);
        }
    }
}