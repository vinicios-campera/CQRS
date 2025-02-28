namespace Domain.Models.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}