namespace Domain.DTOs
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}