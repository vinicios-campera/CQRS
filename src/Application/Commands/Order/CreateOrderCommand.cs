using Confluent.Kafka;
using Domain.Events;
using Domain.Interfaces.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Newtonsoft.Json;

namespace Application.Commands.Order
{
    public record CreateOrderCommand(string CustomerName, decimal TotalAmount) : IRequest<Guid>;

    public class CreateOrderCommandHandler(IOrderWriteRepository repository, 
        IProducer<string, string> kafkaProducer, 
        IEventStore eventStore) : IRequestHandler<CreateOrderCommand, Guid>
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

            //Eventual
            await kafkaProducer.ProduceAsync("orders-topic", new Message<string, string>
            {
                Key = order.Id.ToString(),
                Value = eventMessage
            });

            //Event Sourcing
            //await eventStore.AppendEventAsync($"order-{order.Id}", eventMessage);

            return order.Id;
        }
    }
}