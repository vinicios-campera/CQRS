using Domain.Models.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface IOrderWriteRepository
    {
        Task AddAsync(Order order);
    }
}