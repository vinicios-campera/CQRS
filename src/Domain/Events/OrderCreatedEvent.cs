namespace Domain.Events
{
    public class OrderCreatedEvent
    {
        public OrderCreatedEvent()
        { }

        public OrderCreatedEvent(Guid orderId, string? customerName, decimal totalAmount)
        {
            Id = orderId;
            CustomerName = customerName;
            TotalAmount = totalAmount;
        }

        public OrderCreatedEvent(Guid orderId, string? customerName, decimal totalAmount, DateTime createdAt) : this(orderId, customerName, totalAmount)
        {
            CreatedAt = createdAt;
        }

        public Guid Id { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}