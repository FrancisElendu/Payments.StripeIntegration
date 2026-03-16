namespace Payments.StripeIntegration.Infrastructure.Persistence.Entities
{
    public class StripeEventLog
    {
        public string EventId { get; set; }

        public string EventType { get; set; }

        public string Payload { get; set; }

        public bool Processed { get; set; }

        public DateTime ReceivedAt { get; set; }
    }
}
