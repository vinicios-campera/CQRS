﻿using Confluent.Kafka;
using Domain.Events;
using Domain.Interfaces.Broker;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Commands.Order
{
    public record CreateOrderCommand(string CustomerName, decimal TotalAmount) : IRequest<Guid>;

    public class CreateOrderCommandHandler(IOrderWriteRepository repository,
        IProducer<string, string> kafkaProducer,
        IEventStore eventStore,
        IConfiguration configuration,
        ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Guid>
    {
        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = new Domain.Models.Entities.Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                TotalAmount = request.TotalAmount
            };

            await repository.AddAsync(order);

            var orderEvent = new OrderCreatedEvent
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            var eventMessage = JsonConvert.SerializeObject(orderEvent);

            await PublishMessageAsync(order.Id.ToString(), eventMessage);
            return order.Id;
        }

        private async Task PublishMessageAsync(string key, string eventMessage)
        {
            var syncType = configuration.GetValue<string>("SyncType");

            switch (syncType)
            {
                case "kafka":
                    await kafkaProducer.ProduceAsync("orders", new Message<string, string>
                    {
                        Key = key,
                        Value = eventMessage
                    });
                    break;

                case "eventstore":
                    await eventStore.AppendEventAsync($"orders", eventMessage);
                    break;

                default:
                    logger.LogInformation("Sincronizador não configurado");
                    break;
            }
        }
    }
}