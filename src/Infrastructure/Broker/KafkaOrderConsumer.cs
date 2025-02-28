using Confluent.Kafka;
using Domain.DTOs;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Infrastructure.Broker
{
    public class KafkaOrderConsumer(IMongoClient mongoClient, IConsumer<string, string> kafkaConsumer) : BackgroundService
    {
        private readonly IMongoCollection<OrderDto> _orders
            = mongoClient.GetDatabase("OrdersDb").GetCollection<OrderDto>("Orders");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            kafkaConsumer.Subscribe("orders-topic");

            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = kafkaConsumer.Consume(stoppingToken);
                        var orderEvent = JsonConvert.DeserializeObject<OrderDto>(consumeResult.Value);
                        await _orders.InsertOneAsync(orderEvent, cancellationToken: stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Consumo cancelado.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro no consumidor: {ex.Message}");
                    }
                }
            }, stoppingToken);
        }

        public override void Dispose()
        {
            kafkaConsumer.Close();
            base.Dispose();
        }
    }
}