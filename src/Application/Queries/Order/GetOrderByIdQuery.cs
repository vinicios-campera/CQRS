using Domain.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Queries.Order
{
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

    public class GetOrderByIdHandler(IOrderReadRepository repository) : IRequestHandler<GetOrderByIdQuery, OrderDto>
    {
        public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetByIdAsync(request.OrderId);
        }
    }
}