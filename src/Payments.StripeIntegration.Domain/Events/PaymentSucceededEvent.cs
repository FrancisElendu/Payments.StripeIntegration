using Payments.StripeIntegration.Shared;

namespace Payments.StripeIntegration.Domain.Events
{
    public class PaymentSucceededEvent : IDomainEvent
    {
        public Guid PaymentId { get; }
        public string StripeEventId { get; }

        public PaymentSucceededEvent(Guid paymentId, string stripeEventId)
        {
            PaymentId = paymentId;
            StripeEventId = stripeEventId;
        }
    }
}
