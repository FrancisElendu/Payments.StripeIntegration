using Payments.StripeIntegration.Domain.Enums;
using Payments.StripeIntegration.Domain.Events;
using Payments.StripeIntegration.Shared;

namespace Payments.StripeIntegration.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid Id { get; private set; }

        public string StripePaymentIntentId { get; private set; }

        public decimal Amount { get; private set; }

        public string Currency { get; private set; }

        public PaymentStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public string? StripeEventId { get; set; }

        private Payment() { }

        public Payment(string stripePaymentIntentId, decimal amount, string currency)
        {
            Id = Guid.NewGuid();
            StripePaymentIntentId = stripePaymentIntentId;
            Amount = amount;
            Currency = currency;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            
        }

        public void MarkSucceeded()
        {
            Status = PaymentStatus.Succeeded;
            ProcessedAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentSucceededEvent(Id, StripeEventId));
        }
    }
}
