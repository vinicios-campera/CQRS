using Domain.DTOs;
using Domain.Interfaces.Repositories;
using MongoDB.Driver;

namespace Infrastructure.Repositories
{
    public class OrderReadRepository(IMongoClient mongoClient) : IOrderReadRepository
    {
        private readonly IMongoCollection<OrderDto> _orders 
            = mongoClient.GetDatabase("OrdersDb").GetCollection<OrderDto>("Orders");

        public async Task<OrderDto> GetByIdAsync(Guid orderId)
        {
            return await _orders.Find(order => order.Id == orderId).FirstOrDefaultAsync();
        }
    }
}