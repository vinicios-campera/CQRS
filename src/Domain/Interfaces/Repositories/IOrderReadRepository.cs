using Domain.DTOs;

namespace Domain.Interfaces.Repositories
{
    public interface IOrderReadRepository
    {
        Task<OrderDto> GetByIdAsync(Guid orderId);
    }
}