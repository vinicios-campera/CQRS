using Domain.Interfaces.Repositories;
using Domain.Models.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class OrderWriteRepository(OrderDbContext dbContext) : IOrderWriteRepository
    {
        private readonly OrderDbContext _dbContext = dbContext;

        public async Task AddAsync(Order order)
        {
            _dbContext.Set<Order>().Add(order);
            await _dbContext.SaveChangesAsync();
        }
    }
}