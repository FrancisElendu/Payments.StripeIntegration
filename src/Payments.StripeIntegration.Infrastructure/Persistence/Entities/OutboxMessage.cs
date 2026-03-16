namespace Payments.StripeIntegration.Infrastructure.Persistence.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public DateTime OccurredOn { get; set; }

        public bool Processed { get; set; }
    }
}
