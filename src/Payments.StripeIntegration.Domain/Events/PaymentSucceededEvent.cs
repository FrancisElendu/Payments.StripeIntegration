using Payments.StripeIntegration.Shared;

namespace Payments.StripeIntegration.Domain.Events
{
    public class PaymentSucceededEvent : IDomainEvent
    {
        public Guid PaymentId { get; }

        public PaymentSucceededEvent(Guid paymentId)
        {
            PaymentId = paymentId;
        }
    }
}
