using Confluent.Kafka;
using Domain.DTOs;
using Domain.Events;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KafkaConsumer
{
    public class Worker(ILogger<Worker> logger, IMongoClient mongoClient, IConfiguration configuration) : BackgroundService
    {
        private readonly IMongoCollection<OrderDto> _orders
           = mongoClient.GetDatabase("OrdersDb").GetCollection<OrderDto>("Orders");

        private readonly IEnumerable<string> _topics
            = configuration.GetSection("Topics").Get<List<string>>()!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> consumerTasks = [];

            foreach (var topic in _topics)
                consumerTasks.Add(Task.Run(() => StartConsumer(topic, stoppingToken), stoppingToken));

            await Task.WhenAll(consumerTasks);
        }

        private void StartConsumer(string topic, CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = configuration.GetConnectionString("Kafka"),
                GroupId = $"group_{topic}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(topic);
            logger.LogInformation("Consumer iniciado para o tópico: {Topic}", topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        logger.LogInformation("[{Topic}] Mensagem recebida: {Message}", topic, consumeResult.Message.Value);
                        var orderEvent = JsonConvert.DeserializeObject<OrderCreatedEvent>(consumeResult.Message.Value);
                        var orderDto = new OrderDto
                        {
                            Id = orderEvent!.Id,
                            CustomerName = orderEvent.CustomerName,
                            TotalAmount = orderEvent.TotalAmount
                        };
                        _orders.InsertOne(orderDto, cancellationToken: stoppingToken);
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "[Erro no consumo de {Topic}]: {Ex}", topic, ex);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "Consumer {Topic} cancelado: {Ex}", topic, ex);
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}